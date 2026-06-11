using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using VOL.APS.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.Utilities;
using VOL.DTO.Aps_Work_Order;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class Aps_Work_OrderService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAps_Work_OrderRepository _repository;

        [ActivatorUtilitiesConstructor]
        public Aps_Work_OrderService(
            IAps_Work_OrderRepository dbRepository,
            IHttpContextAccessor httpContextAccessor)
            : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// 分页获取工单列表。
        /// </summary>
        /// <param name="input">分页查询条件，包含工单号、客户名称、排产状态、页码、每页条数及排序信息。</param>
        /// <returns>返回工单分页数据集合。</returns>
        public PageGridData<ApsWorkOrderPageOutputDto> GetWorkOrderPageList(ApsWorkOrderPageQueryInputDto input)
        {
            input ??= new ApsWorkOrderPageQueryInputDto();
            input.Page = input.Page <= 0 ? 1 : input.Page;
            input.Rows = input.Rows <= 0 ? 30 : input.Rows;

            IQueryable<Aps_Work_Order> query = _repository.DbContext
                .Set<Aps_Work_Order>()
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(input.WorkOrderNo))
            {
                string workOrderNo = input.WorkOrderNo.Trim();
                query = query.Where(x => x.WorkOrderNo.Contains(workOrderNo));
            }

            if (!string.IsNullOrWhiteSpace(input.CustomerName))
            {
                string customerName = input.CustomerName.Trim();
                query = query.Where(x => x.CustomerName.Contains(customerName));
            }


            if (!string.IsNullOrWhiteSpace(input.ScheduleStatus))
            {
                string scheduleStatus = input.ScheduleStatus.Trim();
                query = query.Where(x => x.ScheduleStatus == scheduleStatus);
            }

            int total = query.Count();
            bool isDesc = (input.Order ?? "asc").ToLower() == "desc";

            query = (input.Sort ?? string.Empty).ToLower() switch
            {
                "customername" => isDesc ? query.OrderByDescending(x => x.CustomerName) : query.OrderBy(x => x.CustomerName),
                "latestdeliverydate" => isDesc ? query.OrderByDescending(x => x.LatestDeliveryDate) : query.OrderBy(x => x.LatestDeliveryDate),
                "earlieststarttime" => isDesc ? query.OrderByDescending(x => x.EarliestStartTime) : query.OrderBy(x => x.EarliestStartTime),
                "orderqty" => isDesc ? query.OrderByDescending(x => x.OrderQty) : query.OrderBy(x => x.OrderQty),
                "processminutes" => isDesc ? query.OrderByDescending(x => x.ProcessMinutes) : query.OrderBy(x => x.ProcessMinutes),
                "schedulestatus" => isDesc ? query.OrderByDescending(x => x.ScheduleStatus) : query.OrderBy(x => x.ScheduleStatus),
                _ => isDesc ? query.OrderByDescending(x => x.WorkOrderNo) : query.OrderBy(x => x.WorkOrderNo)
            };

            var rows = query
                .Skip((input.Page - 1) * input.Rows)
                .Take(input.Rows)
                .Select(x => new ApsWorkOrderPageOutputDto
                {
                    Id = x.Id,
                    WorkOrderNo = x.WorkOrderNo,
                    CustomerName = x.CustomerName,
                    CustomerPriority = x.CustomerPriority,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,
                    OrderQty = x.OrderQty,
                    EarliestStartTime = x.EarliestStartTime,
                    LatestDeliveryDate = x.LatestDeliveryDate,
                    ProcessMinutes = x.ProcessMinutes,
                    RequiredMachine = x.RequiredMachine,
                    RequiredMachineId = x.RequiredMachineId,
                    ChangeoverGroup = x.ChangeoverGroup,
                    ScheduleStatus = x.ScheduleStatus,
                    Remark = x.Remark
                })
                .ToList();

            return new PageGridData<ApsWorkOrderPageOutputDto>
            {
                total = total,
                rows = rows,
                status = 0,
                msg = string.Empty
            };
        }

        /// <summary>
        /// 新增工单数据。
        /// </summary>
        /// <param name="saveModel">保存模型，主表数据中应包含工单信息。</param>
        /// <returns>返回新增操作结果。</returns>
        public WebResponseContent CreateTestWorkOrders(CreateApsWorkOrderTestDataInputDto input)
        {
            input ??= new CreateApsWorkOrderTestDataInputDto();
            int count = input.Count <= 0 ? 10 : input.Count;
            if (count > 200)
            {
                return new WebResponseContent().Error("测试数据一次最多只能新增200条");
            }

            string[] customerNames =
            {
                "苏州精密制造有限公司",
                "南京新能源科技有限公司",
                "常州机电设备有限公司",
                "上海工业材料有限公司",
                "宁波电子科技有限公司",
                "杭州智能制造有限公司",
                "广州机械制造有限公司",
                "天津重工集团",
                "深圳新能源有限公司",
                "华东汽车零部件有限公司"
            };
            string[] productCodes = { "P-1001", "P-1002", "P-1003", "P-1004", "P-1005" };
            string[] productNames = { "电机壳体", "控制面板", "传动轴", "连接支架", "精密齿轮" };
            string[] changeoverGroups = { "A组", "B组", "C组", "D组", "E组" };
            string[] scheduleStatuses =
                {
                "待排产",
                //"排产中", "已排产","已完成"
            };
            string[] customerLevel = { "一级客户", "二级客户", "三级客户", "四级客户", "五级客户" };

            DateTime now = DateTime.Now;
            Random random = new Random();
            List<Aps_Machine> machineList = _repository.DbContext
                .Set<Aps_Machine>()
                .AsNoTracking()
                .OrderBy(x => x.MachineCode)
                .ToList();
            List<Aps_Work_Order> addList = new List<Aps_Work_Order>();

            for (int i = 0; i < count; i++)
            {
                int customerIndex = i % customerNames.Length;
                int productIndex = i % productCodes.Length;
                int changeoverIndex = i % changeoverGroups.Length;
                int scheduleIndex = i % scheduleStatuses.Length;
                DateTime startTime = now.Date.AddDays(i % 15).AddHours(8 + (i % 3) * 2);
                int processMinutes = 60 + i * 15;
                (string requiredMachine, string requiredMachineId) = BuildTestRequiredMachine(machineList, i);
                Aps_Work_Order entity = new Aps_Work_Order
                {
                    Id = Guid.NewGuid(),
                    WorkOrderNo = $"TEST{now:yyyyMMddHHmmssfff}{i + 1:000}",
                    CustomerName = customerNames[customerIndex],
                    CustomerPriority = customerIndex + 1,
                    ProductCode = productCodes[productIndex],
                    ProductName = productNames[productIndex],
                    OrderQty = (100 + i) * random.Next(10, 16),
                    EarliestStartTime = startTime,
                    LatestDeliveryDate = startTime.AddDays(3 + (i % 5)),
                    ProcessMinutes = processMinutes,
                    RequiredMachine = requiredMachine,
                    RequiredMachineId = requiredMachineId,
                    ChangeoverGroup = changeoverGroups[changeoverIndex],
                    ScheduleStatus = scheduleStatuses[scheduleIndex],
                    Remark = $"测试工单-{now:yyyy-MM-dd HH:mm:ss}"
                };
                entity.SetCreateDefaultVal();
                addList.Add(entity);
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                _repository.AddRange(addList);
                _repository.SaveChanges();
                return new WebResponseContent().OK($"成功新增{addList.Count}条测试工单", new
                {
                    count = addList.Count,
                    workOrderNos = addList.Select(x => x.WorkOrderNo).ToList()
                });
            });
        }

        public override WebResponseContent Add(SaveModel saveModel)
        {
            if (saveModel?.MainData == null || saveModel.MainData.Count == 0)
            {
                return new WebResponseContent().Error("新增数据不能为空");
            }

            Aps_Work_Order entity = saveModel.MainData.MapToEntity(new Aps_Work_Order());
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;

            WebResponseContent machineResult = ResolveRequiredMachines(entity);
            if (!machineResult.Status)
            {
                return machineResult;
            }

            WebResponseContent validateResult = ValidateWorkOrder(entity, false);
            if (!validateResult.Status)
            {
                return validateResult;
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                entity.SetCreateDefaultVal();
                _repository.Add(entity, true);
                return new WebResponseContent().OK(ResponseType.SaveSuccess);
            });
        }

        /// <summary>
        /// 更新工单数据。
        /// </summary>
        /// <param name="saveModel">保存模型，主表数据中应包含要修改的工单主键及工单信息。</param>
        /// <returns>返回更新操作结果。</returns>
        public override WebResponseContent Update(SaveModel saveModel)
        {
            if (saveModel?.MainData == null || saveModel.MainData.Count == 0)
            {
                return new WebResponseContent().Error("修改数据不能为空");
            }

            Guid? id = saveModel.MainData.TryGetValue("Id", out object? keyValue) ? keyValue.GetGuid() : null;
            if (id == null || id == Guid.Empty)
            {
                return new WebResponseContent().Error("主键不能为空");
            }

            Aps_Work_Order dbEntity = _repository.FindFirst(x => x.Id == id.Value);
            if (dbEntity == null)
            {
                return new WebResponseContent().Error("要修改的数据不存在");
            }

            dbEntity = saveModel.MainData.MapToEntity(dbEntity);

            WebResponseContent machineResult = ResolveRequiredMachines(dbEntity);
            if (!machineResult.Status)
            {
                return machineResult;
            }

            WebResponseContent validateResult = ValidateWorkOrder(dbEntity, true);
            if (!validateResult.Status)
            {
                return validateResult;
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                dbEntity.SetModifyDefaultVal();
                _repository.Update(dbEntity, x => new
                {
                    x.WorkOrderNo,
                    x.CustomerName,
                    x.CustomerPriority,
                    x.ProductCode,
                    x.ProductName,
                    x.OrderQty,
                    x.EarliestStartTime,
                    x.LatestDeliveryDate,
                    x.ProcessMinutes,
                    x.RequiredMachine,
                    x.RequiredMachineId,
                    x.ChangeoverGroup,
                    x.ScheduleStatus,
                    x.Remark,
                    x.ModifyID,
                    x.Modifier,
                    x.ModifyDate
                }, true);
                return new WebResponseContent().OK(ResponseType.SaveSuccess);
            });
        }

        /// <summary>
        /// 删除工单数据。
        /// </summary>
        /// <param name="keys">要删除的数据主键集合。</param>
        /// <param name="delList">是否按列表删除模式执行删除。</param>
        /// <returns>返回删除操作结果。</returns>
        public override WebResponseContent Del(object[] keys, bool delList = false)
        {
            if (keys == null || keys.Length == 0)
            {
                return new WebResponseContent().Error("请选择要删除的数据");
            }

            Guid[] ids = keys
                .Select(x => x.GetGuid())
                .Where(x => x.HasValue && x.Value != Guid.Empty)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                return new WebResponseContent().Error("未获取到有效主键");
            }

            int existsCount = _repository.FindAsIQueryable(x => ids.Contains(x.Id)).Count();
            if (existsCount != ids.Length)
            {
                return new WebResponseContent().Error("部分要删除的数据不存在，请刷新后重试");
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                _repository.DeleteWithKeys(ids.Cast<object>().ToArray(), delList);
                return new WebResponseContent().OK(ResponseType.DelSuccess);
            });
        }

        /// <summary>
        /// 校验工单数据的合法性。
        /// </summary>
        /// <param name="entity">待校验的工单实体。</param>
        /// <param name="isUpdate">是否为更新操作，<c>true</c> 表示更新，<c>false</c> 表示新增。</param>
        /// <returns>返回校验结果，校验失败时包含对应错误信息。</returns>
        private WebResponseContent ResolveRequiredMachines(Aps_Work_Order entity)
        {
            string requiredMachineText = (entity.RequiredMachine ?? string.Empty).Replace('，', ',').Trim();
            entity.RequiredMachineId = string.Empty;

            if (string.IsNullOrWhiteSpace(requiredMachineText))
            {
                entity.RequiredMachine = string.Empty;
                entity.RequiredMachineId = string.Empty;
                return new WebResponseContent(true);
            }

            List<string> tokens = requiredMachineText
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (tokens.Count == 0)
            {
                entity.RequiredMachine = string.Empty;
                entity.RequiredMachineId = string.Empty;
                return new WebResponseContent(true);
            }

            List<Guid> machineIds = new List<Guid>();
            foreach (string token in tokens)
            {
                if (!Guid.TryParse(token, out Guid machineId) || machineId == Guid.Empty)
                {
                    return new WebResponseContent().Error($"指定的设备ID无效: {token}");
                }

                machineIds.Add(machineId);
            }

            List<Aps_Machine> machineList = _repository.DbContext
                .Set<Aps_Machine>()
                .AsNoTracking()
                .Where(x => machineIds.Contains(x.Id))
                .ToList();

            Dictionary<Guid, Aps_Machine> idMap = machineList
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.First());

            List<Aps_Machine> selectedMachines = new List<Aps_Machine>();
            foreach (string token in tokens)
            {
                if (Guid.TryParse(token, out Guid machineId)
                    && idMap.TryGetValue(machineId, out Aps_Machine? machine)
                    && machine != null)
                {
                    selectedMachines.Add(machine);
                    continue;
                }

                return new WebResponseContent().Error($"指定的机器不存在: {token}");
            }

            List<Aps_Machine> distinctMachines = selectedMachines
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();

            entity.RequiredMachine = string.Join(",", distinctMachines.Select(x => x.MachineCode));
            entity.RequiredMachineId = string.Join(",", distinctMachines.Select(x => x.Id.ToString()));
            return new WebResponseContent(true);
        }

        private static (string requiredMachine, string requiredMachineId) BuildTestRequiredMachine(List<Aps_Machine> machineList, int index)
        {
            if (machineList == null || machineList.Count == 0 || index % 3 == 0)
            {
                return (string.Empty, string.Empty);
            }

            if (machineList.Count == 1 || index % 3 == 1)
            {
                Aps_Machine machine = machineList[index % machineList.Count];
                return (machine.MachineCode ?? string.Empty, machine.Id.ToString());
            }

            Aps_Machine firstMachine = machineList[index % machineList.Count];
            Aps_Machine secondMachine = machineList[(index + 1) % machineList.Count];
            if (firstMachine.Id == secondMachine.Id)
            {
                return (firstMachine.MachineCode ?? string.Empty, firstMachine.Id.ToString());
            }

            return ($"{firstMachine.MachineCode},{secondMachine.MachineCode}", $"{firstMachine.Id},{secondMachine.Id}");
        }

        private WebResponseContent ValidateWorkOrder(Aps_Work_Order entity, bool isUpdate)
        {
            entity.WorkOrderNo = entity.WorkOrderNo?.Trim();
            entity.CustomerName = entity.CustomerName?.Trim();
            entity.ProductCode = entity.ProductCode?.Trim();
            entity.ProductName = entity.ProductName?.Trim();
            entity.RequiredMachine = entity.RequiredMachine?.Trim();
            entity.RequiredMachineId = entity.RequiredMachineId?.Trim();
            entity.ChangeoverGroup = entity.ChangeoverGroup?.Trim();
            entity.ScheduleStatus = entity.ScheduleStatus?.Trim();
            entity.Remark = entity.Remark?.Trim();

            if (string.IsNullOrWhiteSpace(entity.WorkOrderNo))
            {
                return new WebResponseContent().Error("工单号不能为空");
            }

            if (string.IsNullOrWhiteSpace(entity.CustomerName))
            {
                return new WebResponseContent().Error("客户名称不能为空");
            }

            if (entity.OrderQty <= 0)
            {
                return new WebResponseContent().Error("订单数量必须大于0");
            }

            if (entity.ProcessMinutes <= 0)
            {
                return new WebResponseContent().Error("预计加工总分钟数必须大于0");
            }

            if (entity.EarliestStartTime == default)
            {
                return new WebResponseContent().Error("最早开始时间不能为空");
            }

            if (entity.LatestDeliveryDate == default)
            {
                return new WebResponseContent().Error("最晚交付日期不能为空");
            }

            if (entity.EarliestStartTime > entity.LatestDeliveryDate)
            {
                return new WebResponseContent().Error("最早开始时间不能晚于最晚交付日期");
            }

            if (entity.CustomerPriority != null && (entity.CustomerPriority < 1 || entity.CustomerPriority > 5))
            {
                return new WebResponseContent().Error("客户优先级必须在1到5之间");
            }

            if (entity.WorkOrderNo.Length > 50)
            {
                return new WebResponseContent().Error("工单号长度不能超过50");
            }

            if (entity.CustomerName.Length > 100)
            {
                return new WebResponseContent().Error("客户名称长度不能超过100");
            }

            if (!string.IsNullOrEmpty(entity.ProductCode) && entity.ProductCode.Length > 50)
            {
                return new WebResponseContent().Error("产品编码长度不能超过50");
            }

            if (!string.IsNullOrEmpty(entity.ProductName) && entity.ProductName.Length > 100)
            {
                return new WebResponseContent().Error("产品名称长度不能超过100");
            }

            if (!string.IsNullOrEmpty(entity.RequiredMachine) && entity.RequiredMachine.Length > 50)
            {
                return new WebResponseContent().Error("指定设备长度不能超过50");
            }

            if (!string.IsNullOrEmpty(entity.RequiredMachineId) && entity.RequiredMachineId.Length > 255)
            {
                return new WebResponseContent().Error("RequiredMachineId length cannot exceed 255");
            }

            if (!string.IsNullOrEmpty(entity.ChangeoverGroup) && entity.ChangeoverGroup.Length > 50)
            {
                return new WebResponseContent().Error("换型分组长度不能超过50");
            }

            if (!string.IsNullOrEmpty(entity.ScheduleStatus) && entity.ScheduleStatus.Length > 20)
            {
                return new WebResponseContent().Error("排产状态长度不能超过20");
            }

            if (!string.IsNullOrEmpty(entity.Remark) && entity.Remark.Length > 500)
            {
                return new WebResponseContent().Error("备注长度不能超过500");
            }

            bool exists = isUpdate
                ? _repository.Exists(x => x.WorkOrderNo == entity.WorkOrderNo && x.Id != entity.Id)
                : _repository.Exists(x => x.WorkOrderNo == entity.WorkOrderNo);

            if (exists)
            {
                return new WebResponseContent().Error("工单号已存在");
            }

            return new WebResponseContent(true);
        }
    }
}
