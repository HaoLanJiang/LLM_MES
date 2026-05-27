/*
 *所有关于Aps_Machine类的业务代码应在此处编写
 */
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
using VOL.DTO.Aps_Machine;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class Aps_MachineService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAps_MachineRepository _repository;

        [ActivatorUtilitiesConstructor]
        public Aps_MachineService(
            IAps_MachineRepository dbRepository,
            IHttpContextAccessor httpContextAccessor)
            : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// 分页获取设备列表。
        /// </summary>
        /// <param name="input">分页查询条件，包含设备编码、设备名称、页码、每页条数及排序信息。</param>
        /// <returns>返回设备分页数据集合。</returns>
        public PageGridData<ApsMachinePageOutputDto> GetMachinePageList(ApsMachinePageQueryInputDto input)
        {
            input ??= new ApsMachinePageQueryInputDto();
            input.Page = input.Page <= 0 ? 1 : input.Page;
            input.Rows = input.Rows <= 0 ? 30 : input.Rows;

            IQueryable<Aps_Machine> query = _repository.DbContext
                .Set<Aps_Machine>()
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(input.MachineCode))
            {
                string machineCode = input.MachineCode.Trim();
                query = query.Where(x => x.MachineCode.Contains(machineCode));
            }

            if (!string.IsNullOrWhiteSpace(input.MachineName))
            {
                string machineName = input.MachineName.Trim();
                query = query.Where(x => x.MachineName.Contains(machineName));
            }

            int total = query.Count();
            bool isDesc = (input.Order ?? "asc").ToLower() == "desc";

            query = (input.Sort ?? string.Empty).ToLower() switch
            {
                "machinename" => isDesc
                    ? query.OrderByDescending(x => x.MachineName)
                    : query.OrderBy(x => x.MachineName),
                "capacityminutesperday" => isDesc
                    ? query.OrderByDescending(x => x.CapacityMinutesPerDay)
                    : query.OrderBy(x => x.CapacityMinutesPerDay),
                "remark" => isDesc
                    ? query.OrderByDescending(x => x.Remark)
                    : query.OrderBy(x => x.Remark),
                _ => isDesc
                    ? query.OrderByDescending(x => x.MachineCode)
                    : query.OrderBy(x => x.MachineCode)
            };

            var rows = query
                .Skip((input.Page - 1) * input.Rows)
                .Take(input.Rows)
                .Select(x => new ApsMachinePageOutputDto
                {
                    Id = x.Id,
                    MachineCode = x.MachineCode,
                    MachineName = x.MachineName,
                    CapacityMinutesPerDay = x.CapacityMinutesPerDay,
                    Remark = x.Remark
                })
                .ToList();

            return new PageGridData<ApsMachinePageOutputDto>
            {
                total = total,
                rows = rows,
                status = 0,
                msg = string.Empty
            };
        }

        /// <summary>
        /// 新增设备数据。
        /// </summary>
        /// <param name="saveModel">保存模型，主表数据中应包含设备信息。</param>
        /// <returns>返回新增操作结果。</returns>
        public override WebResponseContent Add(SaveModel saveModel)
        {
            if (saveModel?.MainData == null || saveModel.MainData.Count == 0)
            {
                return new WebResponseContent().Error("新增数据不能为空");
            }

            Aps_Machine entity = saveModel.MainData.MapToEntity(new Aps_Machine());
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;

            WebResponseContent validateResult = ValidateMachine(entity, false);
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
        /// 更新设备数据。
        /// </summary>
        /// <param name="saveModel">保存模型，主表数据中应包含要修改的设备主键及设备信息。</param>
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

            Aps_Machine dbEntity = _repository.FindFirst(x => x.Id == id.Value);
            if (dbEntity == null)
            {
                return new WebResponseContent().Error("要修改的数据不存在");
            }

            dbEntity = saveModel.MainData.MapToEntity(dbEntity);

            WebResponseContent validateResult = ValidateMachine(dbEntity, true);
            if (!validateResult.Status)
            {
                return validateResult;
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                dbEntity.SetModifyDefaultVal();
                _repository.Update(dbEntity, x => new
                {
                    x.MachineCode,
                    x.MachineName,
                    x.CapacityMinutesPerDay,
                    x.Remark,
                    x.ModifyID,
                    x.Modifier,
                    x.ModifyDate
                }, true);
                return new WebResponseContent().OK(ResponseType.SaveSuccess);
            });
        }

        /// <summary>
        /// 删除设备数据。
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
        /// 校验设备数据的合法性。
        /// </summary>
        /// <param name="entity">待校验的设备实体。</param>
        /// <param name="isUpdate">是否为更新操作，<c>true</c> 表示更新，<c>false</c> 表示新增。</param>
        /// <returns>返回校验结果，校验失败时包含对应错误信息。</returns>
        private WebResponseContent ValidateMachine(Aps_Machine entity, bool isUpdate)
        {
            entity.MachineCode = entity.MachineCode?.Trim();
            entity.MachineName = entity.MachineName?.Trim();
            entity.Remark = entity.Remark?.Trim();

            if (string.IsNullOrWhiteSpace(entity.MachineCode))
            {
                return new WebResponseContent().Error("设备编码不能为空");
            }

            if (string.IsNullOrWhiteSpace(entity.MachineName))
            {
                return new WebResponseContent().Error("设备名称不能为空");
            }

            if (entity.MachineCode.Length > 50)
            {
                return new WebResponseContent().Error("设备编码长度不能超过50");
            }

            if (entity.MachineName.Length > 100)
            {
                return new WebResponseContent().Error("设备名称长度不能超过100");
            }

            if (!string.IsNullOrEmpty(entity.Remark) && entity.Remark.Length > 500)
            {
                return new WebResponseContent().Error("备注长度不能超过500");
            }

            if (entity.CapacityMinutesPerDay < 0)
            {
                return new WebResponseContent().Error("每日产能分钟不能小于0");
            }

            bool exists = isUpdate
                ? _repository.Exists(x => x.MachineCode == entity.MachineCode && x.Id != entity.Id)
                : _repository.Exists(x => x.MachineCode == entity.MachineCode);

            if (exists)
            {
                return new WebResponseContent().Error("设备编码已存在");
            }

            return new WebResponseContent(true);
        }
    }
}
