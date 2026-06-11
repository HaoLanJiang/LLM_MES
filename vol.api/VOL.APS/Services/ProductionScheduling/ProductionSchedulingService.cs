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
        private const int DefaultChangeoverMinutes = 30;
        private const int TailGapToleranceMinutes = 5;
        private const decimal QuantityTolerance = 0.0001m;

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
            // 只要前端明确选择了排产日期，就按该日期 00:00:00 起算；
            // 未传日期时，才从当前时间开始排，避免把“当天”误截成点击时刻。
            DateTime startBoundary = input.StartDate?.Date ?? now;

            // 读取未来可用的排产时间片，后续会基于保留的排产结果重算占用分钟。
            List<ScheduleTimeSlotState> slotStates = _scheduleTimeRepository
                .FindAsIQueryable(x => x.enable_flag == 1
                    && x.status == 1
                    && x.available_minutes > 0
                    && x.end_datetime > startBoundary)
                .OrderBy(x => x.schedule_date)
                .ThenBy(x => x.line_code)
                .ThenBy(x => x.start_datetime)
                .ToList()
                .Select(x => new ScheduleTimeSlotState
                {
                    Entity = x,
                    OriginalUsedMinutes = x.used_minutes,
                    OriginalRemainMinutes = x.remain_minutes
                })
                .ToList();

            if (slotStates.Count == 0)
            {
                return new WebResponseContent().Error("未找到可用的排产时间");
            }

            // 查询待排产工单，排产前会先清空本次范围内的历史结果，因此这里只排除已完成工单。
            IQueryable<Aps_Work_Order> workOrderQuery = _workOrderRepository
                .FindAsIQueryable(x => x.ScheduleStatus != "已完成");

            if (input.WorkOrderIds?.Count > 0)
            {
                // 如果前端指定了工单范围，则仅对这些工单执行排产。
                HashSet<Guid> targetIds = input.WorkOrderIds
                    .Where(x => x != Guid.Empty)
                    .ToHashSet();
                workOrderQuery = workOrderQuery.Where(x => targetIds.Contains(x.Id));
            }

            List<Aps_Work_Order> candidateWorkOrders = workOrderQuery.ToList();
            if (candidateWorkOrders.Count == 0)
            {
                return new WebResponseContent().Error("未找到可排产的工单");
            }

            HashSet<Guid> protectedWorkOrderIds = candidateWorkOrders
                .Where(x => IsInProductionStatus(x.ScheduleStatus))
                .Select(x => x.Id)
                .ToHashSet();

            List<Aps_Work_Order> workOrders = candidateWorkOrders
                .Where(x => !protectedWorkOrderIds.Contains(x.Id))
                .ToList();
            if (workOrders.Count == 0)
            {
                return new WebResponseContent().OK("当前选择的工单均处于生产中，已保留现场排产结果，未执行重排");
            }

            HashSet<Guid> workOrderIds = workOrders.Select(x => x.Id).ToHashSet();
            bool clearAllExistingResults = input.WorkOrderIds == null || input.WorkOrderIds.Count == 0;
            List<Aps_Schedule_Result> scheduleResultsToClear = (clearAllExistingResults
                    ? _scheduleResultRepository.FindAsIQueryable(x => true)
                    : _scheduleResultRepository.FindAsIQueryable(x => workOrderIds.Contains(x.WorkOrderId)))
                .Where(x => !protectedWorkOrderIds.Contains(x.WorkOrderId))
                .ToList();
            HashSet<Guid> scheduleResultIdsToClear = scheduleResultsToClear
                .Select(x => x.Id)
                .ToHashSet();

            HashSet<string> machineCodes = slotStates
                .Select(x => (x.Entity.line_code ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (Aps_Schedule_Result existingSchedule in scheduleResultsToClear)
            {
                string machineCode = (existingSchedule.MachineCode ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(machineCode))
                {
                    machineCodes.Add(machineCode);
                }
            }

            List<MachineScheduleSnapshot> retainedMachineSchedules = _scheduleResultRepository
                .FindAsIQueryable(x => x.MachineCode != null
                    && x.MachineCode != string.Empty
                    && !scheduleResultIdsToClear.Contains(x.Id)
                    && x.PlanEndTime > startBoundary
                    && machineCodes.Contains(x.MachineCode))
                .Select(x => new MachineScheduleSnapshot
                {
                    MachineCode = x.MachineCode,
                    PlanStartTime = x.PlanStartTime,
                    PlanEndTime = x.PlanEndTime
                })
                .ToList();

            Dictionary<string, List<TimeRange>> occupiedTimeRangesByMachine = BuildOccupiedTimeRanges(retainedMachineSchedules);
            ApplySlotUsageBaseline(slotStates, occupiedTimeRangesByMachine);

            Dictionary<string, MachineCapacitySnapshot> machineCapacities = _scheduleResultRepository.DbContext
                .Set<Aps_Machine>()
                .Where(x => machineCodes.Contains(x.MachineCode))
                .ToList()
                .Select(x => new MachineCapacitySnapshot
                {
                    MachineCode = (x.MachineCode ?? string.Empty).Trim(),
                    MachineName = (x.MachineName ?? string.Empty).Trim(),
                    // 当前库里没有独立“每小时产能”字段，这里先按每日产能折算小时产能。
                    HourlyCapacity = x.CapacityMinutesPerDay > 0
                        ? decimal.Round(x.CapacityMinutesPerDay.Value / 24m, 4, MidpointRounding.AwayFromZero)
                        : 0m
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.MachineCode))
                .GroupBy(x => x.MachineCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            // 构造工单运行态。由于本次会先清掉非生产中工单的旧排产结果，所以这些工单都从完整订单数量重新排。
            List<WorkOrderScheduleState> orderStates = workOrders
                .Select(x =>
                {
                    decimal existingScheduledQty = 0m;
                    decimal remainingQty = ClampQuantity(NormalizeDiscreteQuantity(x.OrderQty));

                    return new WorkOrderScheduleState
                    {
                        WorkOrder = x,
                        ExistingScheduledQty = existingScheduledQty,
                        RemainingQty = remainingQty,
                        AllowedMachineCodes = ParseMachineCodes(x.RequiredMachine)
                    };
                })
                .Where(x => x.RemainingQty > QuantityTolerance)
                .ToList();

            if (orderStates.Count == 0)
            {
                return new WebResponseContent().OK("工单已全部排产，无需重复生成", BuildSummary(sortMode, rules, new List<Aps_Schedule_Result>(), workOrders.Count, orderStates));
            }

            // 统一预计算综合评分，综合评分模式会直接使用该分值排序。
            ApplyComprehensiveScores(orderStates);

            // 记录每台设备最近一次生产的换型组，供“同组连续生产优先”规则使用。
            Dictionary<string, string> lastGroupByMachine = LoadLastGroupByMachine(scheduleResultIdsToClear);
            List<Aps_Schedule_Result> addResults = new List<Aps_Schedule_Result>();

            // 逐个时间片为设备分配工单。
            foreach (ScheduleTimeSlotState slotState in slotStates)
            {
                string machineCode = (slotState.Entity.line_code ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(machineCode))
                {
                    continue;
                }

                while (slotState.RemainingMinutes > 0)
                {
                    List<TimeRange> occupiedRanges = GetOccupiedTimeRanges(occupiedTimeRangesByMachine, machineCode);
                    DateTime cursor = GetNextAvailableCursor(slotState, startBoundary, occupiedRanges);
                    if (cursor >= slotState.Entity.end_datetime)
                    {
                        break;
                    }

                    DateTime freeWindowEnd = GetFreeWindowEnd(slotState.Entity.end_datetime, cursor, occupiedRanges);
                    int currentWindowMinutes = Math.Max(0, (int)Math.Floor((freeWindowEnd - cursor).TotalMinutes));
                    int availableWindowMinutes = Math.Min(slotState.RemainingMinutes, currentWindowMinutes);
                    if (availableWindowMinutes <= 0)
                    {
                        break;
                    }

                    // 先筛出可在当前设备生产、且仍有剩余工时的候选工单。
                    List<WorkOrderScheduleState> machineCandidates = orderStates
                        .Where(x => x.RemainingQty > QuantityTolerance
                            && IsMachineMatched(x, machineCode)
                            && GetProductionQtyPerMinute(x, GetMachineCapacity(machineCapacities, machineCode)) > 0
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

                        string futureLastGroup = lastGroupByMachine.TryGetValue(machineCode, out string? futureGroupValue)
                            ? futureGroupValue
                            : string.Empty;
                        List<WorkOrderScheduleState> nextStartCandidates = machineCandidates
                            .Where(x => x.WorkOrder.EarliestStartTime == nextStartTime)
                            .ToList();

                        if (nextStartCandidates.Count > 0)
                        {
                            WorkOrderScheduleState futureSelectedOrder = SelectNextOrder(nextStartCandidates, sortMode, rules, futureLastGroup);
                            int preloadChangeoverMinutes = GetRequiredChangeoverMinutes(futureLastGroup, futureSelectedOrder.WorkOrder.ChangeoverGroup);
                            int gapMinutes = Math.Max(0, (int)Math.Floor((nextStartTime - cursor).TotalMinutes));
                            if (preloadChangeoverMinutes > 0
                                && gapMinutes >= preloadChangeoverMinutes
                                && availableWindowMinutes >= preloadChangeoverMinutes)
                            {
                                DateTime changeoverStartTime = cursor;
                                DateTime changeoverEndTime = cursor.AddMinutes(preloadChangeoverMinutes);
                                addResults.Add(BuildChangeoverResult(
                                    machineCode,
                                    slotState.Entity.line_name,
                                    changeoverStartTime,
                                    changeoverEndTime,
                                    futureLastGroup,
                                    futureSelectedOrder.WorkOrder.ChangeoverGroup,
                                    sortMode,
                                    rules));

                                slotState.Apply(preloadChangeoverMinutes);
                                AddOccupiedTimeRange(occupiedRanges, changeoverStartTime, changeoverEndTime);

                                string preparedGroup = (futureSelectedOrder.WorkOrder.ChangeoverGroup ?? string.Empty).Trim();
                                if (!string.IsNullOrWhiteSpace(preparedGroup))
                                {
                                    lastGroupByMachine[machineCode] = preparedGroup;
                                }

                                slotState.SetCursorFloor(changeoverEndTime);
                                continue;
                            }
                        }

                        slotState.SetCursorFloor(nextStartTime);
                        continue;
                    }

                    string lastGroup = lastGroupByMachine.TryGetValue(machineCode, out string? groupValue)
                        ? groupValue
                        : string.Empty;

                    MachineCapacitySnapshot? machineCapacity = GetMachineCapacity(machineCapacities, machineCode);

                    // 根据排序模式与规则，选择当前窗口里真正能排入的一张工单，尽量避免机器空档。
                    WorkOrderScheduleState? selectedOrder = SelectFeasibleNextOrder(
                        availableNow,
                        sortMode,
                        rules,
                        lastGroup,
                        machineCapacity,
                        availableWindowMinutes);
                    if (selectedOrder == null)
                    {
                        break;
                    }

                    decimal qtyPerMinute = GetProductionQtyPerMinute(selectedOrder, machineCapacity);
                    if (qtyPerMinute <= 0)
                    {
                        break;
                    }

                    int changeoverMinutes = GetRequiredChangeoverMinutes(lastGroup, selectedOrder.WorkOrder.ChangeoverGroup);
                    if (changeoverMinutes > 0)
                    {
                        if (availableWindowMinutes <= changeoverMinutes)
                        {
                            break;
                        }

                        DateTime changeoverStartTime = cursor;
                        DateTime changeoverEndTime = cursor.AddMinutes(changeoverMinutes);
                        addResults.Add(BuildChangeoverResult(
                            machineCode,
                            slotState.Entity.line_name,
                            changeoverStartTime,
                            changeoverEndTime,
                            lastGroup,
                            selectedOrder.WorkOrder.ChangeoverGroup,
                            sortMode,
                            rules));

                        slotState.Apply(changeoverMinutes);
                        AddOccupiedTimeRange(occupiedRanges, changeoverStartTime, changeoverEndTime);
                        cursor = changeoverEndTime;
                        freeWindowEnd = GetFreeWindowEnd(slotState.Entity.end_datetime, cursor, occupiedRanges);
                        currentWindowMinutes = Math.Max(0, (int)Math.Floor((freeWindowEnd - cursor).TotalMinutes));
                        availableWindowMinutes = Math.Min(slotState.RemainingMinutes, currentWindowMinutes);
                        if (availableWindowMinutes <= 0)
                        {
                            break;
                        }
                    }

                    // 先确定优先工单，再按设备产能换算本次可生产数量与所需分钟数。
                    decimal plannedQty = CalculatePlannedQuantity(selectedOrder.RemainingQty, qtyPerMinute, availableWindowMinutes);
                    if (plannedQty <= QuantityTolerance)
                    {
                        break;
                    }

                    int plannedMinutes = CalculateRequiredMinutes(plannedQty, qtyPerMinute);
                    if (plannedMinutes <= 0 || plannedMinutes > availableWindowMinutes)
                    {
                        break;
                    }

                    int tailGapMinutes = availableWindowMinutes - plannedMinutes;
                    if (tailGapMinutes > 0 && tailGapMinutes <= TailGapToleranceMinutes)
                    {
                        plannedMinutes = availableWindowMinutes;
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
                        OrderQty = plannedQty,
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
                    selectedOrder.RemainingQty = ClampQuantity(selectedOrder.RemainingQty - plannedQty);
                    selectedOrder.ScheduledQtyThisRun += plannedQty;
                    slotState.Apply(plannedMinutes);
                    AddOccupiedTimeRange(occupiedRanges, planStartTime, planEndTime);

                    string changeoverGroup = (selectedOrder.WorkOrder.ChangeoverGroup ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(changeoverGroup))
                    {
                        lastGroupByMachine[machineCode] = changeoverGroup;
                    }
                }
            }

            // 将基线回滚和本次新增排产统一回写到排产时间表。
            DateTime updateTime = DateTime.Now;
            List<Aps_Schedule_Time> touchedSlots = slotStates
                .Where(x => x.PlannedMinutes > 0
                    || x.Entity.used_minutes != x.OriginalUsedMinutes
                    || x.Entity.remain_minutes != x.OriginalRemainMinutes)
                .Select(x =>
                {
                    x.Entity.used_minutes += x.PlannedMinutes;
                    x.Entity.remain_minutes = Math.Max(x.Entity.remain_minutes - x.PlannedMinutes, 0);
                    x.Entity.update_time = updateTime;
                    return x.Entity;
                })
                .ToList();

            Dictionary<Guid, Aps_Work_Order> trackedOrders = _workOrderRepository
                .FindAsIQueryable(x => workOrderIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x);

            foreach (WorkOrderScheduleState orderState in orderStates)
            {
                if (!trackedOrders.TryGetValue(orderState.WorkOrder.Id, out Aps_Work_Order? orderEntity) || orderEntity == null)
                {
                    continue;
                }

                // 按累计已排数量判断工单状态：全部排完为已排产，部分排完为排产中。
                decimal totalScheduledQty = orderState.ExistingScheduledQty + orderState.ScheduledQtyThisRun;
                if (totalScheduledQty >= orderEntity.OrderQty - QuantityTolerance)
                {
                    orderEntity.ScheduleStatus = "已排产";
                }
                else if (totalScheduledQty > QuantityTolerance)
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
                if (scheduleResultsToClear.Count > 0)
                {
                    _scheduleResultRepository.DbContext.Set<Aps_Schedule_Result>().RemoveRange(scheduleResultsToClear);
                }

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
                string message = addResults.Count > 0
                    ? "排产完成"
                    : "已清空可重排的旧排产结果，但本次未找到满足条件的可排产产能";
                return new WebResponseContent().OK(message, BuildSummary(sortMode, rules, addResults, workOrders.Count, orderStates));
            });
        }

        /// <summary>
        /// 读取每台设备最近一次排产对应的换型组。
        /// </summary>
        /// <returns>设备编码与最近一次换型组的映射。</returns>
        private Dictionary<string, string> LoadLastGroupByMachine(HashSet<Guid> excludedScheduleResultIds)
        {
            // 先按排产结束时间倒序查询，再为每台设备保留最近一条记录。
            List<MachineGroupSnapshot> snapshots = (from result in _scheduleResultRepository.FindAsIQueryable(x => !string.IsNullOrWhiteSpace(x.MachineCode))
                                                    join workOrder in _workOrderRepository.FindAsIQueryable(x => true)
                                                        on result.WorkOrderId equals workOrder.Id
                                                    where !excludedScheduleResultIds.Contains(result.Id)
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
        /// 从候选工单中选出当前窗口里可以实际排入的最优先工单。
        /// </summary>
        private static WorkOrderScheduleState? SelectFeasibleNextOrder(
            List<WorkOrderScheduleState> candidates,
            ProductionSchedulingSortMode sortMode,
            List<ProductionSchedulingRule> rules,
            string lastGroup,
            MachineCapacitySnapshot? machineCapacity,
            int availableWindowMinutes)
        {
            if (candidates.Count == 0 || availableWindowMinutes <= 0)
            {
                return null;
            }

            candidates.Sort((left, right) => CompareOrder(left, right, sortMode, rules, lastGroup));
            foreach (WorkOrderScheduleState candidate in candidates)
            {
                decimal qtyPerMinute = GetProductionQtyPerMinute(candidate, machineCapacity);
                if (qtyPerMinute <= 0)
                {
                    continue;
                }

                int changeoverMinutes = GetRequiredChangeoverMinutes(lastGroup, candidate.WorkOrder.ChangeoverGroup);
                if (availableWindowMinutes <= changeoverMinutes)
                {
                    continue;
                }

                int maxProductionMinutes = availableWindowMinutes - changeoverMinutes;
                if (maxProductionMinutes <= 0)
                {
                    continue;
                }

                decimal maxSchedulableQty = CalculatePlannedQuantity(candidate.RemainingQty, qtyPerMinute, maxProductionMinutes);
                if (maxSchedulableQty > QuantityTolerance)
                {
                    return candidate;
                }
            }

            return null;
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
        /// 根据当前表结构折算设备产能下的每分钟产量。
        /// </summary>
        private static decimal GetProductionQtyPerMinute(WorkOrderScheduleState orderState, MachineCapacitySnapshot? machineCapacity)
        {
            return GetProductionQtyPerMinute(orderState.WorkOrder, machineCapacity);
        }

        /// <summary>
        /// 根据当前表结构折算设备产能下的每分钟产量。
        /// </summary>
        private static decimal GetProductionQtyPerMinute(Aps_Work_Order workOrder, MachineCapacitySnapshot? machineCapacity)
        {
            if (machineCapacity != null && machineCapacity.HourlyCapacity > 0)
            {
                return machineCapacity.HourlyCapacity / 60m;
            }

            if (workOrder.ProcessMinutes > 0 && workOrder.OrderQty > 0)
            {
                return workOrder.OrderQty / workOrder.ProcessMinutes;
            }

            return 0m;
        }

        /// <summary>
        /// 读取设备产能快照。
        /// </summary>
        private static MachineCapacitySnapshot? GetMachineCapacity(
            Dictionary<string, MachineCapacitySnapshot> machineCapacities,
            string machineCode)
        {
            string normalizedMachineCode = (machineCode ?? string.Empty).Trim();
            return machineCapacities.TryGetValue(normalizedMachineCode, out MachineCapacitySnapshot? machineCapacity)
                ? machineCapacity
                : null;
        }

        /// <summary>
        /// 计算排完剩余数量至少需要多少分钟。
        /// </summary>
        private static int CalculateRequiredMinutes(decimal remainingQty, decimal qtyPerMinute)
        {
            if (remainingQty <= QuantityTolerance || qtyPerMinute <= 0)
            {
                return 0;
            }

            return Math.Max(1, (int)decimal.Ceiling(remainingQty / qtyPerMinute));
        }

        /// <summary>
        /// 计算本次排产分钟数下可完成的数量。
        /// </summary>
        private static decimal CalculatePlannedQuantity(decimal remainingQty, decimal qtyPerMinute, int plannedMinutes)
        {
            if (remainingQty <= QuantityTolerance || qtyPerMinute <= 0 || plannedMinutes <= 0)
            {
                return 0m;
            }

            decimal wholeRemainingQty = NormalizeDiscreteQuantity(remainingQty);
            if (wholeRemainingQty <= 0)
            {
                return 0m;
            }

            decimal producedQty = decimal.Floor(qtyPerMinute * plannedMinutes);
            if (producedQty <= 0)
            {
                return 0m;
            }

            return producedQty >= wholeRemainingQty
                ? wholeRemainingQty
                : producedQty;
        }

        /// <summary>
        /// 计算是否需要插入清厂时间。
        /// </summary>
        private static int GetRequiredChangeoverMinutes(string currentGroup, string nextGroup)
        {
            string normalizedCurrentGroup = (currentGroup ?? string.Empty).Trim();
            string normalizedNextGroup = (nextGroup ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedCurrentGroup)
                || string.IsNullOrWhiteSpace(normalizedNextGroup)
                || string.Equals(normalizedCurrentGroup, normalizedNextGroup, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return DefaultChangeoverMinutes;
        }

        /// <summary>
        /// 构造清厂排产结果。
        /// </summary>
        private static Aps_Schedule_Result BuildChangeoverResult(
            string machineCode,
            string machineName,
            DateTime planStartTime,
            DateTime planEndTime,
            string currentGroup,
            string nextGroup,
            ProductionSchedulingSortMode sortMode,
            List<ProductionSchedulingRule> rules)
        {
            Aps_Schedule_Result changeoverResult = new Aps_Schedule_Result
            {
                Id = Guid.NewGuid(),
                WorkOrderId = Guid.Empty,
                WorkOrderNo = "清厂",
                MachineCode = machineCode,
                MachineName = machineName,
                PlanStartTime = planStartTime,
                PlanEndTime = planEndTime,
                PlanMinutes = Math.Max(1, (int)Math.Ceiling((planEndTime - planStartTime).TotalMinutes)),
                OrderQty = 0m,
                IsDelay = 0,
                DelayMinutes = 0,
                ScheduleStatus = "清厂",
                Remark = $"{BuildResultRemark(sortMode, rules)}; 清厂: {(currentGroup ?? string.Empty).Trim()} -> {(nextGroup ?? string.Empty).Trim()}"
            };
            changeoverResult.SetCreateDefaultVal();
            return changeoverResult;
        }

        /// <summary>
        /// 对数量做统一截断，避免小数精度导致出现负数。
        /// </summary>
        private static decimal ClampQuantity(decimal quantity)
        {
            return quantity <= QuantityTolerance ? 0m : decimal.Round(quantity, 4, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 将排产数量统一归整为整件，避免结果出现小数拆分。
        /// </summary>
        private static decimal NormalizeDiscreteQuantity(decimal quantity)
        {
            return quantity <= QuantityTolerance
                ? 0m
                : decimal.Round(quantity, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 判断工单是否处于现场正在生产的状态。
        /// 这类工单不参与自动清理，避免影响现场执行。
        /// </summary>
        private static bool IsInProductionStatus(string scheduleStatus)
        {
            string normalizedStatus = NormalizeKey(scheduleStatus);
            return normalizedStatus is "生产中" or "开工中" or "已开工" or "加工中" or "执行中" or "在制";
        }

        /// <summary>
        /// 根据保留的排产结果，重算未来时间片的已使用和剩余分钟数。
        /// </summary>
        private static void ApplySlotUsageBaseline(
            List<ScheduleTimeSlotState> slotStates,
            Dictionary<string, List<TimeRange>> occupiedTimeRangesByMachine)
        {
            foreach (ScheduleTimeSlotState slotState in slotStates)
            {
                string machineCode = (slotState.Entity.line_code ?? string.Empty).Trim();
                List<TimeRange> occupiedRanges = GetOccupiedTimeRanges(occupiedTimeRangesByMachine, machineCode);
                int usedMinutes = CalculateOccupiedMinutes(
                    slotState.Entity.start_datetime,
                    slotState.Entity.end_datetime,
                    occupiedRanges);

                slotState.Entity.used_minutes = usedMinutes;
                slotState.Entity.remain_minutes = Math.Max(slotState.Entity.available_minutes - usedMinutes, 0);
                slotState.PlannedMinutes = 0;
                slotState.CursorFloor = null;
            }
        }

        /// <summary>
        /// 构造每台设备的已占用时间轴。
        /// </summary>
        private static Dictionary<string, List<TimeRange>> BuildOccupiedTimeRanges(List<MachineScheduleSnapshot> schedules)
        {
            Dictionary<string, List<TimeRange>> occupiedTimeRanges = new Dictionary<string, List<TimeRange>>(StringComparer.OrdinalIgnoreCase);
            foreach (MachineScheduleSnapshot schedule in schedules)
            {
                string machineCode = (schedule.MachineCode ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(machineCode) || schedule.PlanEndTime <= schedule.PlanStartTime)
                {
                    continue;
                }

                List<TimeRange> machineRanges = GetOccupiedTimeRanges(occupiedTimeRanges, machineCode);
                AddOccupiedTimeRange(machineRanges, schedule.PlanStartTime, schedule.PlanEndTime);
            }

            return occupiedTimeRanges;
        }

        /// <summary>
        /// 获取设备已占用时间集合。
        /// </summary>
        private static List<TimeRange> GetOccupiedTimeRanges(
            Dictionary<string, List<TimeRange>> occupiedTimeRangesByMachine,
            string machineCode)
        {
            string normalizedMachineCode = (machineCode ?? string.Empty).Trim();
            if (!occupiedTimeRangesByMachine.TryGetValue(normalizedMachineCode, out List<TimeRange>? occupiedRanges))
            {
                occupiedRanges = new List<TimeRange>();
                occupiedTimeRangesByMachine[normalizedMachineCode] = occupiedRanges;
            }

            return occupiedRanges;
        }

        /// <summary>
        /// 将新的占用区间合并进设备时间轴。
        /// </summary>
        private static void AddOccupiedTimeRange(List<TimeRange> occupiedRanges, DateTime startTime, DateTime endTime)
        {
            if (endTime <= startTime)
            {
                return;
            }

            occupiedRanges.Add(new TimeRange
            {
                StartTime = startTime,
                EndTime = endTime
            });

            occupiedRanges.Sort((left, right) => DateTime.Compare(left.StartTime, right.StartTime));
            for (int index = 0; index < occupiedRanges.Count - 1;)
            {
                TimeRange currentRange = occupiedRanges[index];
                TimeRange nextRange = occupiedRanges[index + 1];
                if (currentRange.EndTime < nextRange.StartTime)
                {
                    index++;
                    continue;
                }

                currentRange.EndTime = currentRange.EndTime >= nextRange.EndTime
                    ? currentRange.EndTime
                    : nextRange.EndTime;
                occupiedRanges.RemoveAt(index + 1);
            }
        }

        /// <summary>
        /// 找到当前时间片内、且不与已有结果重叠的下一个可排时间。
        /// </summary>
        private static DateTime GetNextAvailableCursor(
            ScheduleTimeSlotState slotState,
            DateTime startBoundary,
            List<TimeRange> occupiedRanges)
        {
            DateTime cursor = slotState.GetCursor(startBoundary);
            while (cursor < slotState.Entity.end_datetime)
            {
                TimeRange? overlapRange = occupiedRanges.FirstOrDefault(x => x.StartTime < cursor.AddTicks(1) && x.EndTime > cursor);
                if (overlapRange == null)
                {
                    return cursor;
                }

                cursor = overlapRange.EndTime;
            }

            return slotState.Entity.end_datetime;
        }

        /// <summary>
        /// 获取当前位置到下一个占用区间前的连续空档结束时间。
        /// </summary>
        private static DateTime GetFreeWindowEnd(DateTime slotEndTime, DateTime cursor, List<TimeRange> occupiedRanges)
        {
            TimeRange? nextRange = occupiedRanges
                .Where(x => x.StartTime > cursor)
                .OrderBy(x => x.StartTime)
                .FirstOrDefault();
            if (nextRange == null || nextRange.StartTime >= slotEndTime)
            {
                return slotEndTime;
            }

            return nextRange.StartTime;
        }

        /// <summary>
        /// 计算时间片内已被历史结果占用的分钟数。
        /// </summary>
        private static int CalculateOccupiedMinutes(DateTime slotStartTime, DateTime slotEndTime, List<TimeRange> occupiedRanges)
        {
            double occupiedMinutes = occupiedRanges
                .Where(x => x.EndTime > slotStartTime && x.StartTime < slotEndTime)
                .Sum(x =>
                {
                    DateTime overlapStart = x.StartTime > slotStartTime ? x.StartTime : slotStartTime;
                    DateTime overlapEnd = x.EndTime < slotEndTime ? x.EndTime : slotEndTime;
                    return overlapEnd > overlapStart
                        ? (overlapEnd - overlapStart).TotalMinutes
                        : 0d;
                });

            return Math.Max(0, (int)Math.Round(occupiedMinutes, MidpointRounding.AwayFromZero));
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
            int fullyScheduledOrderCount = orderStates.Count(x => x.RemainingQty <= QuantityTolerance);
            int partialScheduledOrderCount = orderStates.Count(x => x.RemainingQty > QuantityTolerance && x.ExistingScheduledQty + x.ScheduledQtyThisRun > QuantityTolerance);
            int unscheduledOrderCount = orderStates.Count(x => x.ExistingScheduledQty + x.ScheduledQtyThisRun <= QuantityTolerance);

            // 汇总工单排产完成度、排产片段数量、受影响设备和日期等信息，供前端展示。
            return new
            {
                sortMode = sortMode == ProductionSchedulingSortMode.ComprehensiveScore ? "综合评分模式" : "规则优先模式",
                ruleSequence = rules.Select(GetRuleDisplayName).ToList(),
                totalWorkOrderCount,
                candidateWorkOrderCount = orderStates.Count,
                scheduledWorkOrderCount = orderStates.Count(x => x.ScheduledQtyThisRun > QuantityTolerance),
                fullyScheduledOrderCount,
                partialScheduledOrderCount,
                unscheduledOrderCount,
                scheduledSegmentCount = results.Count,
                totalPlannedMinutes = results.Sum(x => x.PlanMinutes),
                totalPlannedQty = results.Where(x => x.WorkOrderId != Guid.Empty).Sum(x => x.OrderQty),
                changeoverSegmentCount = results.Count(x => x.ScheduleStatus == "清厂"),
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

            public decimal ExistingScheduledQty { get; set; }

            public decimal RemainingQty { get; set; }

            public decimal ScheduledQtyThisRun { get; set; }

            public double ComprehensiveScore { get; set; }

            public HashSet<string> AllowedMachineCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class ScheduleTimeSlotState
        {
            public Aps_Schedule_Time Entity { get; set; } = null!;

            public int OriginalUsedMinutes { get; set; }

            public int OriginalRemainMinutes { get; set; }

            public int PlannedMinutes { get; set; }

            public DateTime? CursorFloor { get; set; }

            // 动态剩余分钟数 = 原始剩余分钟数 - 本次排产过程中已经分配出去的分钟数。
            public int RemainingMinutes => Math.Max(Entity.remain_minutes - PlannedMinutes, 0);

            /// <summary>
            /// 获取当前时间片真正还能继续排产的起始时间。
            /// </summary>
            public DateTime GetCursor(DateTime startBoundary)
            {
                DateTime cursor = Entity.start_datetime.AddMinutes(Entity.used_minutes + PlannedMinutes);
                if (CursorFloor.HasValue && CursorFloor.Value > cursor)
                {
                    cursor = CursorFloor.Value;
                }

                if (cursor < startBoundary)
                {
                    cursor = startBoundary;
                }

                return cursor > Entity.end_datetime ? Entity.end_datetime : cursor;
            }

            /// <summary>
            /// 将当前时间片的排产游标推进到指定时间。
            /// </summary>
            public void SetCursorFloor(DateTime cursor)
            {
                DateTime normalizedCursor = cursor < Entity.start_datetime
                    ? Entity.start_datetime
                    : cursor;
                CursorFloor = normalizedCursor > Entity.end_datetime
                    ? Entity.end_datetime
                    : normalizedCursor;
            }

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

        private sealed class MachineCapacitySnapshot
        {
            public string MachineCode { get; set; } = string.Empty;

            public string MachineName { get; set; } = string.Empty;

            public decimal HourlyCapacity { get; set; }
        }

        private sealed class MachineScheduleSnapshot
        {
            public string MachineCode { get; set; } = string.Empty;

            public DateTime PlanStartTime { get; set; }

            public DateTime PlanEndTime { get; set; }
        }

        private sealed class TimeRange
        {
            public DateTime StartTime { get; set; }

            public DateTime EndTime { get; set; }
        }
    }
}
