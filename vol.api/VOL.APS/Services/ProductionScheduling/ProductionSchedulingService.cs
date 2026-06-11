using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using VOL.APS.IRepositories;
using VOL.APS.IServices;
using VOL.Core.Extensions;
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.Utilities;
using VOL.DTO.Aps_ProductionScheduling;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    /// <summary>
    /// 生产排产服务。
    /// 负责读取可用排产时间、待排产工单，并生成排产结果后回写相关状态。
    /// </summary>
    public class ProductionSchedulingService : IProductionSchedulingService, IDependency
    {
        private readonly IAps_Schedule_ResultRepository _scheduleResultRepository;
        private readonly IAps_Schedule_TimeRepository _scheduleTimeRepository;
        private readonly IAps_Work_OrderRepository _workOrderRepository;

        [ActivatorUtilitiesConstructor]
        /// <summary>
        /// 初始化生产排产服务。
        /// </summary>
        /// <param name="scheduleResultRepository">排产结果仓储，同时复用其 DbContext 操作 APS 相关表。</param>
        /// <param name="scheduleTimeRepository">排产时间仓储。</param>
        /// <param name="workOrderRepository">工单仓储。</param>
        public ProductionSchedulingService(
            IAps_Schedule_ResultRepository scheduleResultRepository,
            IAps_Schedule_TimeRepository scheduleTimeRepository,
            IAps_Work_OrderRepository workOrderRepository)
        {
            _scheduleResultRepository = scheduleResultRepository;
            _scheduleTimeRepository = scheduleTimeRepository;
            _workOrderRepository = workOrderRepository;
        }

        /// <summary>
        /// 执行生产排产。
        /// 该方法会按指定排序模式为工单分配设备和时间，并写入排产结果表。
        /// </summary>
        /// <param name="input">排产入参。</param>
        /// <returns>返回排产执行结果及汇总数据。</returns>
        public WebResponseContent RunProductionScheduling(RunProductionSchedulingInputDto input)
        {
            input ??= new RunProductionSchedulingInputDto();

            // 解析排序模式与规则顺序，并确定本次排产的起始时间边界。
            ProductionSchedulingSortMode sortMode = ParseSortMode(input.SortMode);
            List<ProductionSchedulingRule> rules = BuildRuleSequence(input.RuleSequence);
            DateTime now = DateTime.Now;
            DateTime startBoundary = ((input.StartDate ?? now.Date).Date > now)
                ? (input.StartDate ?? now.Date).Date
                : now;

            // 读取未来仍有剩余产能的可用排产时间片。
            List<ScheduleTimeSlotState> slotStates = _scheduleTimeRepository
                .FindAsIQueryable(x => x.enable_flag == 1
                    && x.status == 1
                    && x.remain_minutes > 0
                    && x.start_datetime >= startBoundary)
                .OrderBy(x => x.schedule_date)
                .ThenBy(x => x.line_code)
                .ThenBy(x => x.start_datetime)
                .ToList()
                .Select(x => new ScheduleTimeSlotState { Entity = x })
                .ToList();

            if (slotStates.Count == 0)
            {
                return new WebResponseContent().Error("未找到可用的排产时间");
            }

            // 查询待排产工单，默认排除已完成和已排产工单。
            IQueryable<Aps_Work_Order> workOrderQuery = _workOrderRepository
                .FindAsIQueryable(x => x.ScheduleStatus != "已完成" && x.ScheduleStatus != "已排产");

            if (input.WorkOrderIds?.Count > 0)
            {
                // 如果前端指定了工单范围，则仅对这些工单执行排产。
                HashSet<Guid> targetIds = input.WorkOrderIds
                    .Where(x => x != Guid.Empty)
                    .ToHashSet();
                workOrderQuery = workOrderQuery.Where(x => targetIds.Contains(x.Id));
            }

            List<Aps_Work_Order> workOrders = workOrderQuery.ToList();
            if (workOrders.Count == 0)
            {
                return new WebResponseContent().Error("未找到可排产的工单");
            }

            // 汇总历史已排产分钟数，避免对同一工单重复计算全部工时。
            HashSet<Guid> workOrderIds = workOrders.Select(x => x.Id).ToHashSet();
            Dictionary<Guid, int> existingScheduledMinutesMap = _scheduleResultRepository
                .FindAsIQueryable(x => workOrderIds.Contains(x.WorkOrderId))
                .GroupBy(x => x.WorkOrderId)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.PlanMinutes));

            // 构造工单运行态，计算剩余待排分钟数并解析允许生产的设备集合。
            List<WorkOrderScheduleState> orderStates = workOrders
                .Select(x =>
                {
                    int existingMinutes = existingScheduledMinutesMap.TryGetValue(x.Id, out int scheduledMinutes)
                        ? scheduledMinutes
                        : 0;
                    int remainingMinutes = Math.Max(x.ProcessMinutes - existingMinutes, 0);

                    return new WorkOrderScheduleState
                    {
                        WorkOrder = x,
                        ExistingScheduledMinutes = existingMinutes,
                        RemainingMinutes = remainingMinutes,
                        AllowedMachineCodes = ParseMachineCodes(x.RequiredMachine)
                    };
                })
                .Where(x => x.RemainingMinutes > 0)
                .ToList();

            if (orderStates.Count == 0)
            {
                return new WebResponseContent().OK("工单已全部排产，无需重复生成", BuildSummary(sortMode, rules, new List<Aps_Schedule_Result>(), workOrders.Count, orderStates));
            }

            // 统一预计算综合评分，综合评分模式会直接使用该分值排序。
            ApplyComprehensiveScores(orderStates);

            // 记录每台设备最近一次生产的换型组，供“同组连续生产优先”规则使用。
            Dictionary<string, string> lastGroupByMachine = LoadLastGroupByMachine();
            List<Aps_Schedule_Result> addResults = new List<Aps_Schedule_Result>();

            // 逐个时间片为设备分配工单。
            foreach (ScheduleTimeSlotState slotState in slotStates)
            {
                string machineCode = (slotState.Entity.line_code ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(machineCode))
                {
                    continue;
                }

                // 当前游标表示该时间片已经排到的时间位置。
                DateTime cursor = slotState.Entity.start_datetime;
                while (slotState.RemainingMinutes > 0)
                {
                    // 先筛出可在当前设备生产、且仍有剩余工时的候选工单。
                    List<WorkOrderScheduleState> machineCandidates = orderStates
                        .Where(x => x.RemainingMinutes > 0
                            && IsMachineMatched(x, machineCode)
                            && x.WorkOrder.EarliestStartTime < slotState.Entity.end_datetime)
                        .ToList();

                    if (machineCandidates.Count == 0)
                    {
                        break;
                    }

                    List<WorkOrderScheduleState> availableNow = machineCandidates
                        .Where(x => x.WorkOrder.EarliestStartTime <= cursor)
                        .ToList();

                    if (availableNow.Count == 0)
                    {
                        // 如果此刻没有可立即开工的工单，则将游标推进到最近的最早开工时间。
                        DateTime nextStartTime = machineCandidates.Min(x => x.WorkOrder.EarliestStartTime);
                        if (nextStartTime >= slotState.Entity.end_datetime)
                        {
                            break;
                        }

                        cursor = nextStartTime > cursor ? nextStartTime : cursor;
                        continue;
                    }

                    string lastGroup = lastGroupByMachine.TryGetValue(machineCode, out string? groupValue)
                        ? groupValue
                        : string.Empty;

                    // 根据排序模式与规则，从当前候选工单中选出最优先的一张工单。
                    WorkOrderScheduleState selectedOrder = SelectNextOrder(availableNow, sortMode, rules, lastGroup);

                    // 实际可排分钟数同时受工单剩余工时、时间片剩余分钟和当前时间窗口三重限制。
                    int windowMinutes = Math.Max(0, (int)Math.Floor((slotState.Entity.end_datetime - cursor).TotalMinutes));
                    int plannedMinutes = Math.Min(selectedOrder.RemainingMinutes, Math.Min(slotState.RemainingMinutes, windowMinutes));
                    if (plannedMinutes <= 0)
                    {
                        break;
                    }

                    // 生成本次排产结果片段，并计算是否延期及延期分钟数。
                    DateTime planStartTime = cursor;
                    DateTime planEndTime = cursor.AddMinutes(plannedMinutes);
                    bool isDelay = planEndTime > selectedOrder.WorkOrder.LatestDeliveryDate;
                    int delayMinutes = isDelay
                        ? (int)Math.Ceiling((planEndTime - selectedOrder.WorkOrder.LatestDeliveryDate).TotalMinutes)
                        : 0;

                    Aps_Schedule_Result scheduleResult = new Aps_Schedule_Result
                    {
                        Id = Guid.NewGuid(),
                        WorkOrderId = selectedOrder.WorkOrder.Id,
                        WorkOrderNo = selectedOrder.WorkOrder.WorkOrderNo,
                        MachineCode = machineCode,
                        MachineName = slotState.Entity.line_name,
                        PlanStartTime = planStartTime,
                        PlanEndTime = planEndTime,
                        PlanMinutes = plannedMinutes,
                        OrderQty = selectedOrder.WorkOrder.OrderQty,
                        CustomerName = selectedOrder.WorkOrder.CustomerName,
                        CustomerPriority = selectedOrder.WorkOrder.CustomerPriority,
                        EarliestStartTime = selectedOrder.WorkOrder.EarliestStartTime,
                        LatestDeliveryDate = selectedOrder.WorkOrder.LatestDeliveryDate,
                        IsDelay = (sbyte)(isDelay ? 1 : 0),
                        DelayMinutes = delayMinutes,
                        ScheduleStatus = isDelay ? "已延期" : "已排产",
                        Remark = BuildResultRemark(sortMode, rules)
                    };
                    scheduleResult.SetCreateDefaultVal();

                    // 回写运行态，方便继续在后续时间片中接着排剩余工时。
                    addResults.Add(scheduleResult);
                    selectedOrder.RemainingMinutes -= plannedMinutes;
                    selectedOrder.ScheduledMinutesThisRun += plannedMinutes;
                    slotState.Apply(plannedMinutes);
                    cursor = planEndTime;

                    string changeoverGroup = (selectedOrder.WorkOrder.ChangeoverGroup ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(changeoverGroup))
                    {
                        lastGroupByMachine[machineCode] = changeoverGroup;
                    }
                }
            }

            // 汇总本次真正受到影响的工单，用于后续更新工单状态。
            List<WorkOrderScheduleState> touchedOrderStates = orderStates
                .Where(x => x.ScheduledMinutesThisRun > 0
                    || (x.ExistingScheduledMinutes > 0 && x.RemainingMinutes > 0))
                .ToList();

            if (addResults.Count == 0)
            {
                return new WebResponseContent().OK("未找到满足条件的可排产产能，未生成排产结果", BuildSummary(sortMode, rules, addResults, workOrders.Count, orderStates));
            }

            // 将本次在内存中占用的分钟数回写到排产时间表。
            DateTime updateTime = DateTime.Now;
            List<Aps_Schedule_Time> touchedSlots = slotStates
                .Where(x => x.PlannedMinutes > 0)
                .Select(x =>
                {
                    x.Entity.used_minutes += x.PlannedMinutes;
                    x.Entity.remain_minutes = Math.Max(x.Entity.remain_minutes - x.PlannedMinutes, 0);
                    x.Entity.update_time = updateTime;
                    return x.Entity;
                })
                .ToList();

            // 查询并更新本次受影响工单的排产状态。
            HashSet<Guid> touchedOrderIds = touchedOrderStates
                .Select(x => x.WorkOrder.Id)
                .ToHashSet();

            Dictionary<Guid, Aps_Work_Order> trackedOrders = _workOrderRepository
                .FindAsIQueryable(x => touchedOrderIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x);

            foreach (WorkOrderScheduleState orderState in touchedOrderStates)
            {
                if (!trackedOrders.TryGetValue(orderState.WorkOrder.Id, out Aps_Work_Order? orderEntity) || orderEntity == null)
                {
                    continue;
                }

                // 按累计已排工时判断工单状态：全部排完为已排产，部分排完为排产中。
                int totalScheduledMinutes = orderState.ExistingScheduledMinutes + orderState.ScheduledMinutesThisRun;
                if (totalScheduledMinutes >= orderEntity.ProcessMinutes)
                {
                    orderEntity.ScheduleStatus = "已排产";
                }
                else if (totalScheduledMinutes > 0)
                {
                    orderEntity.ScheduleStatus = "排产中";
                }
                else
                {
                    orderEntity.ScheduleStatus = "待排产";
                }

                orderEntity.SetModifyDefaultVal();
            }

            // 在同一事务中统一提交排产结果、时间片占用和工单状态更新，避免数据不一致。
            return _scheduleResultRepository.DbContextBeginTransaction(() =>
            {
                _scheduleResultRepository.AddRange(addResults);
                if (touchedSlots.Count > 0)
                {
                    _scheduleTimeRepository.UpdateRange(touchedSlots);
                }

                if (trackedOrders.Count > 0)
                {
                    _workOrderRepository.UpdateRange(trackedOrders.Values.ToList());
                }

                _scheduleResultRepository.SaveChanges();
                return new WebResponseContent().OK("排产完成", BuildSummary(sortMode, rules, addResults, workOrders.Count, orderStates));
            });
        }

        /// <summary>
        /// 读取每台设备最近一次排产对应的换型组。
        /// </summary>
        /// <returns>设备编码与最近一次换型组的映射。</returns>
        private Dictionary<string, string> LoadLastGroupByMachine()
        {
            // 先按排产结束时间倒序查询，再为每台设备保留最近一条记录。
            List<MachineGroupSnapshot> snapshots = (from result in _scheduleResultRepository.FindAsIQueryable(x => !string.IsNullOrWhiteSpace(x.MachineCode))
                                                    join workOrder in _workOrderRepository.FindAsIQueryable(x => true)
                                                        on result.WorkOrderId equals workOrder.Id
                                                    orderby result.PlanEndTime descending
                                                    select new MachineGroupSnapshot
                                                    {
                                                        MachineCode = result.MachineCode,
                                                        ChangeoverGroup = workOrder.ChangeoverGroup
                                                    })
                .ToList();

            Dictionary<string, string> lastGroupByMachine = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (MachineGroupSnapshot snapshot in snapshots)
            {
                // 同一设备只保留第一条，也就是最近一次生产的换型组。
                string machineCode = (snapshot.MachineCode ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(machineCode) || lastGroupByMachine.ContainsKey(machineCode))
                {
                    continue;
                }

                lastGroupByMachine[machineCode] = (snapshot.ChangeoverGroup ?? string.Empty).Trim();
            }

            return lastGroupByMachine;
        }

        /// <summary>
        /// 从候选工单中选出当前最优先的工单。
        /// </summary>
        /// <param name="candidates">候选工单集合。</param>
        /// <param name="sortMode">排序模式。</param>
        /// <param name="rules">规则优先模式下的规则顺序。</param>
        /// <param name="lastGroup">当前设备最近一次生产的换型组。</param>
        /// <returns>排序后的首个工单。</returns>
        private static WorkOrderScheduleState SelectNextOrder(
            List<WorkOrderScheduleState> candidates,
            ProductionSchedulingSortMode sortMode,
            List<ProductionSchedulingRule> rules,
            string lastGroup)
        {
            // 统一使用比较器排序，排序后的第一个元素即当前应优先排产的工单。
            candidates.Sort((left, right) => CompareOrder(left, right, sortMode, rules, lastGroup));
            return candidates[0];
        }

        /// <summary>
        /// 比较两个工单的优先顺序。
        /// </summary>
        /// <param name="left">左侧工单。</param>
        /// <param name="right">右侧工单。</param>
        /// <param name="sortMode">排序模式。</param>
        /// <param name="rules">规则顺序。</param>
        /// <param name="lastGroup">当前设备最近一次生产的换型组。</param>
        /// <returns>小于 0 表示 left 优先，大于 0 表示 right 优先。</returns>
        private static int CompareOrder(
            WorkOrderScheduleState left,
            WorkOrderScheduleState right,
            ProductionSchedulingSortMode sortMode,
            List<ProductionSchedulingRule> rules,
            string lastGroup)
        {
            if (sortMode == ProductionSchedulingSortMode.ComprehensiveScore)
            {
                // 综合评分模式下，直接按综合分值倒序排序。
                int scoreCompare = right.ComprehensiveScore.CompareTo(left.ComprehensiveScore);
                if (scoreCompare != 0)
                {
                    return scoreCompare;
                }
            }
            else
            {
                // 规则优先模式按前端传入的规则顺序逐条比较，能分出先后就立即返回。
                foreach (ProductionSchedulingRule rule in rules)
                {
                    int compare = rule switch
                    {
                        ProductionSchedulingRule.CustomerPriority => CompareCustomerPriority(left, right),
                        ProductionSchedulingRule.DeliveryDate => DateTime.Compare(left.WorkOrder.LatestDeliveryDate, right.WorkOrder.LatestDeliveryDate),
                        ProductionSchedulingRule.SameGroup => CompareSameGroup(left, right, lastGroup),
                        _ => 0
                    };

                    if (compare != 0)
                    {
                        return compare;
                    }
                }
            }

            // 主排序规则无法区分时，继续用最早开工时间、订单数量和工单号做兜底比较。
            int earliestStartCompare = DateTime.Compare(left.WorkOrder.EarliestStartTime, right.WorkOrder.EarliestStartTime);
            if (earliestStartCompare != 0)
            {
                return earliestStartCompare;
            }

            int qtyCompare = decimal.Compare(right.WorkOrder.OrderQty, left.WorkOrder.OrderQty);
            if (qtyCompare != 0)
            {
                return qtyCompare;
            }

            return string.CompareOrdinal(left.WorkOrder.WorkOrderNo ?? string.Empty, right.WorkOrder.WorkOrderNo ?? string.Empty);
        }

        /// <summary>
        /// 比较客户优先级。
        /// 数值越小优先级越高。
        /// </summary>
        /// <param name="left">左侧工单。</param>
        /// <param name="right">右侧工单。</param>
        /// <returns>比较结果。</returns>
        private static int CompareCustomerPriority(WorkOrderScheduleState left, WorkOrderScheduleState right)
        {
            int leftPriority = left.WorkOrder.CustomerPriority ?? int.MaxValue;
            int rightPriority = right.WorkOrder.CustomerPriority ?? int.MaxValue;
            return leftPriority.CompareTo(rightPriority);
        }

        /// <summary>
        /// 比较工单是否与当前设备最近一次生产的换型组一致。
        /// 命中同组的工单优先。
        /// </summary>
        /// <param name="left">左侧工单。</param>
        /// <param name="right">右侧工单。</param>
        /// <param name="lastGroup">设备最近一次换型组。</param>
        /// <returns>比较结果。</returns>
        private static int CompareSameGroup(WorkOrderScheduleState left, WorkOrderScheduleState right, string lastGroup)
        {
            // 如果设备当前没有历史换型组，则该规则不生效。
            if (string.IsNullOrWhiteSpace(lastGroup))
            {
                return 0;
            }

            // 同组优先，不同组则交由其他规则继续比较。
            bool leftMatch = string.Equals((left.WorkOrder.ChangeoverGroup ?? string.Empty).Trim(), lastGroup, StringComparison.OrdinalIgnoreCase);
            bool rightMatch = string.Equals((right.WorkOrder.ChangeoverGroup ?? string.Empty).Trim(), lastGroup, StringComparison.OrdinalIgnoreCase);

            if (leftMatch == rightMatch)
            {
                return 0;
            }

            return leftMatch ? -1 : 1;
        }

        /// <summary>
        /// 判断工单是否允许在指定设备上生产。
        /// </summary>
        /// <param name="orderState">工单运行态。</param>
        /// <param name="machineCode">设备编码。</param>
        /// <returns>允许则返回 true。</returns>
        private static bool IsMachineMatched(WorkOrderScheduleState orderState, string machineCode)
        {
            // 未指定设备时视为任意设备可生产；否则必须命中指定设备集合。
            return orderState.AllowedMachineCodes.Count == 0 || orderState.AllowedMachineCodes.Contains(machineCode);
        }

        /// <summary>
        /// 解析工单指定设备文本，转换为设备编码集合。
        /// </summary>
        /// <param name="requiredMachine">逗号分隔的设备编码文本。</param>
        /// <returns>设备编码集合。</returns>
        private static HashSet<string> ParseMachineCodes(string requiredMachine)
        {
            if (string.IsNullOrWhiteSpace(requiredMachine))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            // 去掉空项和多余空白，方便后续直接做设备匹配。
            return requiredMachine
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 计算工单的综合评分。
        /// </summary>
        /// <param name="orderStates">待计算的工单运行态集合。</param>
        private static void ApplyComprehensiveScores(List<WorkOrderScheduleState> orderStates)
        {
            if (orderStates.Count == 0)
            {
                return;
            }

            // 获取归一化范围，避免不同维度的数值直接相加失真。
            decimal minQty = orderStates.Min(x => x.WorkOrder.OrderQty);
            decimal maxQty = orderStates.Max(x => x.WorkOrder.OrderQty);
            long minDeliveryTicks = orderStates.Min(x => x.WorkOrder.LatestDeliveryDate.Ticks);
            long maxDeliveryTicks = orderStates.Max(x => x.WorkOrder.LatestDeliveryDate.Ticks);

            foreach (WorkOrderScheduleState orderState in orderStates)
            {
                // 客户优先级越高分越高，订单数量越大分越高，交期越早越紧急分越高。
                double customerPriorityScore = NormalizeCustomerPriority(orderState.WorkOrder.CustomerPriority);
                double orderQtyScore = NormalizeDecimal(orderState.WorkOrder.OrderQty, minQty, maxQty);
                double urgencyScore = NormalizeUrgency(orderState.WorkOrder.LatestDeliveryDate.Ticks, minDeliveryTicks, maxDeliveryTicks);

                // 综合评分权重：客户优先级 50%，订单数量 20%，交期紧急度 30%。
                orderState.ComprehensiveScore = customerPriorityScore * 0.5
                    + orderQtyScore * 0.2
                    + urgencyScore * 0.3;
            }
        }

        /// <summary>
        /// 将客户优先级归一化为 0 到 1 之间的分值。
        /// 1 级客户分值最高，5 级客户分值最低。
        /// </summary>
        /// <param name="customerPriority">客户优先级。</param>
        /// <returns>归一化后的优先级分值。</returns>
        private static double NormalizeCustomerPriority(int? customerPriority)
        {
            if (customerPriority == null || customerPriority < 1 || customerPriority > 5)
            {
                return 0;
            }

            return 1d - ((customerPriority.Value - 1d) / 4d);
        }

        /// <summary>
        /// 将数值归一化为 0 到 1 之间。
        /// </summary>
        /// <param name="value">当前值。</param>
        /// <param name="minValue">最小值。</param>
        /// <param name="maxValue">最大值。</param>
        /// <returns>归一化后的分值。</returns>
        private static double NormalizeDecimal(decimal value, decimal minValue, decimal maxValue)
        {
            // 当所有值都相同，直接返回 1，避免分母为 0。
            if (maxValue <= minValue)
            {
                return 1;
            }

            return (double)((value - minValue) / (maxValue - minValue));
        }

        /// <summary>
        /// 将交期紧急度归一化为 0 到 1 之间。
        /// 交期越早，得分越高。
        /// </summary>
        /// <param name="currentTicks">当前工单交期 ticks。</param>
        /// <param name="minTicks">最早交期 ticks。</param>
        /// <param name="maxTicks">最晚交期 ticks。</param>
        /// <returns>归一化后的紧急度分值。</returns>
        private static double NormalizeUrgency(long currentTicks, long minTicks, long maxTicks)
        {
            // 当所有工单交期相同，统一返回 1，避免分母为 0。
            if (maxTicks <= minTicks)
            {
                return 1;
            }

            return (double)(maxTicks - currentTicks) / (maxTicks - minTicks);
        }

        /// <summary>
        /// 解析前端传入的排序模式文本。
        /// </summary>
        /// <param name="sortMode">排序模式文本。</param>
        /// <returns>内部排序模式枚举。</returns>
        private static ProductionSchedulingSortMode ParseSortMode(string sortMode)
        {
            string normalized = NormalizeKey(sortMode);
            return normalized switch
            {
                "综合评分模式" or "综合评分" or "comprehensivescore" or "score" => ProductionSchedulingSortMode.ComprehensiveScore,
                _ => ProductionSchedulingSortMode.RulePriority
            };
        }

        /// <summary>
        /// 构造规则优先模式下的规则顺序。
        /// 如果前端未传有效规则，则回退到默认规则。
        /// </summary>
        /// <param name="ruleSequence">前端传入的规则文本集合。</param>
        /// <returns>内部规则顺序集合。</returns>
        private static List<ProductionSchedulingRule> BuildRuleSequence(List<string> ruleSequence)
        {
            List<ProductionSchedulingRule> rules = new List<ProductionSchedulingRule>();
            foreach (string rule in ruleSequence ?? new List<string>())
            {
                // 将前端规则文本映射为内部规则枚举，并自动去重。
                string normalized = NormalizeKey(rule);
                ProductionSchedulingRule? currentRule = normalized switch
                {
                    "客户优先级优先" or "客户优先级" or "customerpriority" => ProductionSchedulingRule.CustomerPriority,
                    "交期优先" or "交期" or "deliverydate" or "latestdeliverydate" => ProductionSchedulingRule.DeliveryDate,
                    "同组连续生产优先" or "同组连续生产" or "同组" or "changeovergroup" or "samegroup" => ProductionSchedulingRule.SameGroup,
                    _ => null
                };

                if (currentRule != null && !rules.Contains(currentRule.Value))
                {
                    rules.Add(currentRule.Value);
                }
            }

            // 默认规则顺序：客户优先级优先、交期优先、同组连续生产优先。
            if (rules.Count == 0)
            {
                rules.Add(ProductionSchedulingRule.CustomerPriority);
                rules.Add(ProductionSchedulingRule.DeliveryDate);
                rules.Add(ProductionSchedulingRule.SameGroup);
            }

            return rules;
        }

        /// <summary>
        /// 构造排产结果备注。
        /// </summary>
        /// <param name="sortMode">排序模式。</param>
        /// <param name="rules">规则顺序。</param>
        /// <returns>备注文本。</returns>
        private static string BuildResultRemark(ProductionSchedulingSortMode sortMode, List<ProductionSchedulingRule> rules)
        {
            string modeText = sortMode == ProductionSchedulingSortMode.ComprehensiveScore ? "综合评分模式" : "规则优先模式";
            string ruleText = string.Join(" > ", rules.Select(GetRuleDisplayName));
            return $"{modeText}自动排产: {ruleText}";
        }

        /// <summary>
        /// 构造本次排产汇总信息。
        /// </summary>
        /// <param name="sortMode">排序模式。</param>
        /// <param name="rules">规则顺序。</param>
        /// <param name="results">本次新增的排产结果。</param>
        /// <param name="totalWorkOrderCount">工单总数。</param>
        /// <param name="orderStates">工单运行态集合。</param>
        /// <returns>用于接口返回的汇总对象。</returns>
        private static object BuildSummary(
            ProductionSchedulingSortMode sortMode,
            List<ProductionSchedulingRule> rules,
            List<Aps_Schedule_Result> results,
            int totalWorkOrderCount,
            List<WorkOrderScheduleState> orderStates)
        {
            int fullyScheduledOrderCount = orderStates.Count(x => x.RemainingMinutes <= 0);
            int partialScheduledOrderCount = orderStates.Count(x => x.RemainingMinutes > 0 && x.ExistingScheduledMinutes + x.ScheduledMinutesThisRun > 0);
            int unscheduledOrderCount = orderStates.Count(x => x.ExistingScheduledMinutes + x.ScheduledMinutesThisRun == 0);

            // 汇总工单排产完成度、排产片段数量、受影响设备和日期等信息，供前端展示。
            return new
            {
                sortMode = sortMode == ProductionSchedulingSortMode.ComprehensiveScore ? "综合评分模式" : "规则优先模式",
                ruleSequence = rules.Select(GetRuleDisplayName).ToList(),
                totalWorkOrderCount,
                candidateWorkOrderCount = orderStates.Count,
                scheduledWorkOrderCount = orderStates.Count(x => x.ScheduledMinutesThisRun > 0),
                fullyScheduledOrderCount,
                partialScheduledOrderCount,
                unscheduledOrderCount,
                scheduledSegmentCount = results.Count,
                totalPlannedMinutes = results.Sum(x => x.PlanMinutes),
                affectedMachineCount = results.Select(x => x.MachineCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Count(),
                affectedScheduleDayCount = results.Select(x => x.PlanStartTime.Date).Distinct().Count()
            };
        }

        /// <summary>
        /// 获取规则的中文展示名称。
        /// </summary>
        /// <param name="rule">规则枚举。</param>
        /// <returns>规则展示文本。</returns>
        private static string GetRuleDisplayName(ProductionSchedulingRule rule)
        {
            return rule switch
            {
                ProductionSchedulingRule.CustomerPriority => "客户优先级优先",
                ProductionSchedulingRule.DeliveryDate => "交期优先",
                ProductionSchedulingRule.SameGroup => "同组连续生产优先",
                _ => rule.ToString()
            };
        }

        /// <summary>
        /// 对字符串做统一格式化，便于做模式和规则匹配。
        /// </summary>
        /// <param name="value">原始文本。</param>
        /// <returns>标准化后的键值。</returns>
        private static string NormalizeKey(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", string.Empty).ToLowerInvariant();
        }

        private enum ProductionSchedulingSortMode
        {
            RulePriority = 1,
            ComprehensiveScore = 2
        }

        private enum ProductionSchedulingRule
        {
            CustomerPriority = 1,
            DeliveryDate = 2,
            SameGroup = 3
        }

        private sealed class WorkOrderScheduleState
        {
            public Aps_Work_Order WorkOrder { get; set; } = null!;

            public int ExistingScheduledMinutes { get; set; }

            public int RemainingMinutes { get; set; }

            public int ScheduledMinutesThisRun { get; set; }

            public double ComprehensiveScore { get; set; }

            public HashSet<string> AllowedMachineCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class ScheduleTimeSlotState
        {
            public Aps_Schedule_Time Entity { get; set; } = null!;

            public int PlannedMinutes { get; set; }

            // 动态剩余分钟数 = 原始剩余分钟数 - 本次排产过程中已经分配出去的分钟数。
            public int RemainingMinutes => Math.Max(Entity.remain_minutes - PlannedMinutes, 0);

            /// <summary>
            /// 为当前时间片累加已分配的分钟数。
            /// </summary>
            /// <param name="plannedMinutes">本次新增分配分钟数。</param>
            public void Apply(int plannedMinutes)
            {
                PlannedMinutes += plannedMinutes;
            }
        }

        private sealed class MachineGroupSnapshot
        {
            public string MachineCode { get; set; } = string.Empty;

            public string ChangeoverGroup { get; set; } = string.Empty;
        }
    }
}
