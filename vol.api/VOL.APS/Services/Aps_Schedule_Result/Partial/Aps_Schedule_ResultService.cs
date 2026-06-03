using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using VOL.APS.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.Utilities;
using VOL.DTO.Aps_Schedule_Result;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class Aps_Schedule_ResultService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAps_Schedule_ResultRepository _repository;

        [ActivatorUtilitiesConstructor]
        public Aps_Schedule_ResultService(
            IAps_Schedule_ResultRepository dbRepository,
            IHttpContextAccessor httpContextAccessor)
            : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// 分页获取排产结果列表。
        /// </summary>
        /// <param name="input">分页查询条件，包含工单号、设备编码、排产状态、页码、每页条数及排序信息。</param>
        /// <returns>返回排产结果分页数据集合。</returns>
        public PageGridData<ApsScheduleResultPageOutputDto> GetScheduleResultPageList(ApsScheduleResultPageQueryInputDto input)
        {
            input ??= new ApsScheduleResultPageQueryInputDto();
            input.Page = input.Page <= 0 ? 1 : input.Page;
            input.Rows = input.Rows <= 0 ? 30 : input.Rows;

            IQueryable<Aps_Schedule_Result> query = _repository.DbContext
                .Set<Aps_Schedule_Result>()
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(input.WorkOrderNo))
            {
                string workOrderNo = input.WorkOrderNo.Trim();
                query = query.Where(x => x.WorkOrderNo.Contains(workOrderNo));
            }

            if (!string.IsNullOrWhiteSpace(input.MachineCode))
            {
                string machineCode = input.MachineCode.Trim();
                query = query.Where(x => x.MachineCode.Contains(machineCode));
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
                "machinecode" => isDesc ? query.OrderByDescending(x => x.MachineCode) : query.OrderBy(x => x.MachineCode),
                "machinename" => isDesc ? query.OrderByDescending(x => x.MachineName) : query.OrderBy(x => x.MachineName),
                "planstarttime" => isDesc ? query.OrderByDescending(x => x.PlanStartTime) : query.OrderBy(x => x.PlanStartTime),
                "planendtime" => isDesc ? query.OrderByDescending(x => x.PlanEndTime) : query.OrderBy(x => x.PlanEndTime),
                "schedulestatus" => isDesc ? query.OrderByDescending(x => x.ScheduleStatus) : query.OrderBy(x => x.ScheduleStatus),
                _ => isDesc ? query.OrderByDescending(x => x.WorkOrderNo) : query.OrderBy(x => x.WorkOrderNo)
            };

            var rows = query
                .Skip((input.Page - 1) * input.Rows)
                .Take(input.Rows)
                .Select(x => new ApsScheduleResultPageOutputDto
                {
                    Id = x.Id,
                    WorkOrderId = x.WorkOrderId,
                    WorkOrderNo = x.WorkOrderNo,
                    MachineCode = x.MachineCode,
                    MachineName = x.MachineName,
                    PlanStartTime = x.PlanStartTime,
                    PlanEndTime = x.PlanEndTime,
                    PlanMinutes = x.PlanMinutes,
                    OrderQty = x.OrderQty,
                    CustomerName = x.CustomerName,
                    CustomerPriority = x.CustomerPriority,
                    EarliestStartTime = x.EarliestStartTime,
                    LatestDeliveryDate = x.LatestDeliveryDate,
                    IsDelay = x.IsDelay,
                    DelayMinutes = x.DelayMinutes,
                    ScheduleStatus = x.ScheduleStatus,
                    Remark = x.Remark
                })
                .ToList();

            return new PageGridData<ApsScheduleResultPageOutputDto>
            {
                total = total,
                rows = rows,
                status = 0,
                msg = string.Empty
            };
        }

        /// <summary>
        /// 新增排产结果数据。
        /// </summary>
        /// <param name="saveModel">保存模型，主表数据中应包含排产结果信息。</param>
        /// <returns>返回新增操作结果。</returns>
        public override WebResponseContent Add(SaveModel saveModel)
        {
            if (saveModel?.MainData == null || saveModel.MainData.Count == 0)
            {
                return new WebResponseContent().Error("新增数据不能为空");
            }

            Aps_Schedule_Result entity = saveModel.MainData.MapToEntity(new Aps_Schedule_Result());
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;

            WebResponseContent validateResult = ValidateScheduleResult(entity, false);
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
        /// 更新排产结果数据。
        /// </summary>
        /// <param name="saveModel">保存模型，主表数据中应包含要修改的排产结果主键及业务信息。</param>
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

            Aps_Schedule_Result dbEntity = _repository.FindFirst(x => x.Id == id.Value);
            if (dbEntity == null)
            {
                return new WebResponseContent().Error("要修改的数据不存在");
            }

            dbEntity = saveModel.MainData.MapToEntity(dbEntity);

            WebResponseContent validateResult = ValidateScheduleResult(dbEntity, true);
            if (!validateResult.Status)
            {
                return validateResult;
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                dbEntity.SetModifyDefaultVal();
                _repository.Update(dbEntity, x => new
                {
                    x.WorkOrderId,
                    x.WorkOrderNo,
                    x.MachineCode,
                    x.MachineName,
                    x.PlanStartTime,
                    x.PlanEndTime,
                    x.PlanMinutes,
                    x.OrderQty,
                    x.CustomerName,
                    x.CustomerPriority,
                    x.EarliestStartTime,
                    x.LatestDeliveryDate,
                    x.IsDelay,
                    x.DelayMinutes,
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
        /// 删除排产结果数据。
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
        /// 校验排产结果数据的合法性。
        /// </summary>
        /// <param name="entity">待校验的排产结果实体。</param>
        /// <param name="isUpdate">是否为更新操作，<c>true</c> 表示更新，<c>false</c> 表示新增。</param>
        /// <returns>返回校验结果，校验失败时包含对应错误信息。</returns>
        private WebResponseContent ValidateScheduleResult(Aps_Schedule_Result entity, bool isUpdate)
        {
            entity.WorkOrderNo = entity.WorkOrderNo?.Trim();
            entity.MachineCode = entity.MachineCode?.Trim();
            entity.MachineName = entity.MachineName?.Trim();
            entity.CustomerName = entity.CustomerName?.Trim();
            entity.ScheduleStatus = entity.ScheduleStatus?.Trim();
            entity.Remark = entity.Remark?.Trim();

            if (entity.WorkOrderId == Guid.Empty)
            {
                return new WebResponseContent().Error("工单ID不能为空");
            }

            if (string.IsNullOrWhiteSpace(entity.WorkOrderNo))
            {
                return new WebResponseContent().Error("工单号不能为空");
            }

            if (string.IsNullOrWhiteSpace(entity.MachineCode))
            {
                return new WebResponseContent().Error("设备编码不能为空");
            }

            if (entity.PlanStartTime == default)
            {
                return new WebResponseContent().Error("计划开始时间不能为空");
            }

            if (entity.PlanEndTime == default)
            {
                return new WebResponseContent().Error("计划结束时间不能为空");
            }

            if (entity.PlanStartTime >= entity.PlanEndTime)
            {
                return new WebResponseContent().Error("计划开始时间必须早于计划结束时间");
            }

            if (entity.PlanMinutes <= 0)
            {
                return new WebResponseContent().Error("计划生产分钟数必须大于0");
            }

            if (entity.OrderQty <= 0)
            {
                return new WebResponseContent().Error("订单数量必须大于0");
            }

            if (!string.IsNullOrEmpty(entity.WorkOrderNo) && entity.WorkOrderNo.Length > 50)
            {
                return new WebResponseContent().Error("工单号长度不能超过50");
            }

            if (!string.IsNullOrEmpty(entity.MachineCode) && entity.MachineCode.Length > 50)
            {
                return new WebResponseContent().Error("设备编码长度不能超过50");
            }

            if (!string.IsNullOrEmpty(entity.MachineName) && entity.MachineName.Length > 100)
            {
                return new WebResponseContent().Error("设备名称长度不能超过100");
            }

            if (!string.IsNullOrEmpty(entity.CustomerName) && entity.CustomerName.Length > 100)
            {
                return new WebResponseContent().Error("客户名称长度不能超过100");
            }

            if (entity.CustomerPriority != null && (entity.CustomerPriority < 1 || entity.CustomerPriority > 5))
            {
                return new WebResponseContent().Error("客户优先级必须在1到5之间");
            }

            if (entity.EarliestStartTime != null && entity.LatestDeliveryDate != null
                && entity.EarliestStartTime > entity.LatestDeliveryDate)
            {
                return new WebResponseContent().Error("最早开始时间不能晚于最晚交付日期");
            }

            if (entity.IsDelay != null && entity.IsDelay != 0 && entity.IsDelay != 1)
            {
                return new WebResponseContent().Error("是否延期只能为0或1");
            }

            if (entity.DelayMinutes < 0)
            {
                return new WebResponseContent().Error("延期分钟数不能小于0");
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
                ? _repository.Exists(x => x.WorkOrderId == entity.WorkOrderId
                    && x.MachineCode == entity.MachineCode
                    && x.PlanStartTime == entity.PlanStartTime
                    && x.Id != entity.Id)
                : _repository.Exists(x => x.WorkOrderId == entity.WorkOrderId
                    && x.MachineCode == entity.MachineCode
                    && x.PlanStartTime == entity.PlanStartTime);

            if (exists)
            {
                return new WebResponseContent().Error("相同工单、设备和开始时间的排产结果已存在");
            }

            return new WebResponseContent(true);
        }
    }
}
