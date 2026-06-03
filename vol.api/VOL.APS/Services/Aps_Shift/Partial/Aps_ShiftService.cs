using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using VOL.APS.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.Configuration;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.Utilities;
using VOL.DTO.Aps_Shift;
using VOL.Entity.DomainModels;
using Yitter.IdGenerator;

namespace VOL.APS.Services
{
    public partial class Aps_ShiftService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAps_ShiftRepository _repository;

        [ActivatorUtilitiesConstructor]
        public Aps_ShiftService(
            IAps_ShiftRepository dbRepository,
            IHttpContextAccessor httpContextAccessor)
            : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// 分页获取班次列表。
        /// </summary>
        /// <param name="input">分页查询条件，包含班次编码、名称、启用状态、页码、每页条数及排序信息。</param>
        /// <returns>返回班次分页数据集合。</returns>
        public PageGridData<ApsShiftPageOutputDto> GetShiftPageList(ApsShiftPageQueryInputDto input)
        {
            input ??= new ApsShiftPageQueryInputDto();
            input.Page = input.Page <= 0 ? 1 : input.Page;
            input.Rows = input.Rows <= 0 ? 30 : input.Rows;

            IQueryable<Aps_Shift> query = _repository.DbContext
                .Set<Aps_Shift>()
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(input.shift_code))
            {
                string shiftCode = input.shift_code.Trim();
                query = query.Where(x => x.shift_code.Contains(shiftCode));
            }

            if (!string.IsNullOrWhiteSpace(input.shift_name))
            {
                string shiftName = input.shift_name.Trim();
                query = query.Where(x => x.shift_name.Contains(shiftName));
            }

            if (input.enable_flag != null)
            {
                query = query.Where(x => x.enable_flag == input.enable_flag);
            }

            int total = query.Count();
            bool isDesc = (input.Order ?? "asc").ToLower() == "desc";

            query = (input.Sort ?? string.Empty).ToLower() switch
            {
                "shift_name" => isDesc ? query.OrderByDescending(x => x.shift_name) : query.OrderBy(x => x.shift_name),
                "work_minutes" => isDesc ? query.OrderByDescending(x => x.work_minutes) : query.OrderBy(x => x.work_minutes),
                "enable_flag" => isDesc ? query.OrderByDescending(x => x.enable_flag) : query.OrderBy(x => x.enable_flag),
                "create_time" => isDesc ? query.OrderByDescending(x => x.create_time) : query.OrderBy(x => x.create_time),
                _ => isDesc ? query.OrderByDescending(x => x.shift_code) : query.OrderBy(x => x.shift_code)
            };

            var rows = query
                .Skip((input.Page - 1) * input.Rows)
                .Take(input.Rows)
                .AsEnumerable()
                .Select(x => new ApsShiftPageOutputDto
                {
                    id = x.id,
                    shift_code = x.shift_code,
                    shift_name = x.shift_name,
                    shift_type = x.shift_type,
                    start_time = x.start_time.ToString(@"hh\:mm\:ss"),
                    end_time = x.end_time.ToString(@"hh\:mm\:ss"),
                    cross_day_flag = x.cross_day_flag,
                    work_minutes = x.work_minutes,
                    enable_flag = x.enable_flag,
                    remark = x.remark,
                    create_time = x.create_time,
                    update_time = x.update_time
                })
                .ToList();

            return new PageGridData<ApsShiftPageOutputDto>
            {
                total = total,
                rows = rows,
                status = 0,
                msg = string.Empty
            };
        }

        /// <summary>
        /// 新增班次数据。
        /// </summary>
        /// <param name="saveModel">保存模型，主表数据中应包含班次信息。</param>
        /// <returns>返回新增操作结果。</returns>
        public override WebResponseContent Add(SaveModel saveModel)
        {
            if (saveModel?.MainData == null || saveModel.MainData.Count == 0)
            {
                return new WebResponseContent().Error("新增数据不能为空");
            }

            Aps_Shift entity = saveModel.MainData.MapToEntity(new Aps_Shift());
            if (entity.id <= 0)
            {
                entity.id = AppSetting.EnableSnowFlakeID ? YitIdHelper.NextId() : DateTime.Now.Ticks;
            }

            WebResponseContent validateResult = ValidateShift(entity, false);
            if (!validateResult.Status)
            {
                return validateResult;
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                DateTime now = DateTime.Now;
                entity.create_time = now;
                entity.update_time = now;
                _repository.Add(entity, true);
                return new WebResponseContent().OK(ResponseType.SaveSuccess);
            });
        }

        /// <summary>
        /// 更新班次数据。
        /// </summary>
        /// <param name="saveModel">保存模型，主表数据中应包含要修改的班次主键及班次信息。</param>
        /// <returns>返回更新操作结果。</returns>
        public override WebResponseContent Update(SaveModel saveModel)
        {
            if (saveModel?.MainData == null || saveModel.MainData.Count == 0)
            {
                return new WebResponseContent().Error("修改数据不能为空");
            }

            long id = saveModel.MainData.TryGetValue("id", out object? keyValue) ? keyValue.GetLong() : 0;
            if (id <= 0)
            {
                return new WebResponseContent().Error("主键不能为空");
            }

            Aps_Shift dbEntity = _repository.FindFirst(x => x.id == id);
            if (dbEntity == null)
            {
                return new WebResponseContent().Error("要修改的数据不存在");
            }

            dbEntity = saveModel.MainData.MapToEntity(dbEntity);

            WebResponseContent validateResult = ValidateShift(dbEntity, true);
            if (!validateResult.Status)
            {
                return validateResult;
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                dbEntity.update_time = DateTime.Now;
                _repository.Update(dbEntity, x => new
                {
                    x.shift_code,
                    x.shift_name,
                    x.shift_type,
                    x.start_time,
                    x.end_time,
                    x.cross_day_flag,
                    x.work_minutes,
                    x.enable_flag,
                    x.remark,
                    x.update_time
                }, true);
                return new WebResponseContent().OK(ResponseType.SaveSuccess);
            });
        }

        /// <summary>
        /// 删除班次数据。
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

            long[] ids = keys
                .Select(x => x.GetLong())
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                return new WebResponseContent().Error("未获取到有效主键");
            }

            int existsCount = _repository.FindAsIQueryable(x => ids.Contains(x.id)).Count();
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
        /// 校验班次数据的合法性。
        /// </summary>
        /// <param name="entity">待校验的班次实体。</param>
        /// <param name="isUpdate">是否为更新操作，<c>true</c> 表示更新，<c>false</c> 表示新增。</param>
        /// <returns>返回校验结果，校验失败时包含对应错误信息。</returns>
        private WebResponseContent ValidateShift(Aps_Shift entity, bool isUpdate)
        {
            entity.shift_code = entity.shift_code?.Trim();
            entity.shift_name = entity.shift_name?.Trim();
            entity.remark = entity.remark?.Trim();

            if (string.IsNullOrWhiteSpace(entity.shift_code))
            {
                return new WebResponseContent().Error("班次编码不能为空");
            }

            if (string.IsNullOrWhiteSpace(entity.shift_name))
            {
                return new WebResponseContent().Error("班次名称不能为空");
            }

            if (entity.shift_type != 0 && entity.shift_type != 1)
            {
                return new WebResponseContent().Error("班次类型只能为0或1");
            }

            if (entity.start_time < TimeSpan.Zero || entity.start_time >= TimeSpan.FromDays(1))
            {
                return new WebResponseContent().Error("开始时间不能为空");
            }

            if (entity.end_time < TimeSpan.Zero || entity.end_time >= TimeSpan.FromDays(1))
            {
                return new WebResponseContent().Error("结束时间不能为空");
            }

            if (entity.cross_day_flag != 0 && entity.cross_day_flag != 1)
            {
                return new WebResponseContent().Error("是否跨天只能为0或1");
            }

            if (entity.work_minutes <= 0)
            {
                return new WebResponseContent().Error("工作分钟数必须大于0");
            }

            if (entity.enable_flag != 0 && entity.enable_flag != 1)
            {
                return new WebResponseContent().Error("是否启用只能为0或1");
            }

            if (entity.shift_code.Length > 50)
            {
                return new WebResponseContent().Error("班次编码长度不能超过50");
            }

            if (entity.shift_name.Length > 100)
            {
                return new WebResponseContent().Error("班次名称长度不能超过100");
            }

            if (!string.IsNullOrEmpty(entity.remark) && entity.remark.Length > 500)
            {
                return new WebResponseContent().Error("备注长度不能超过500");
            }

            bool exists = isUpdate
                ? _repository.Exists(x => x.shift_code == entity.shift_code && x.id != entity.id)
                : _repository.Exists(x => x.shift_code == entity.shift_code);

            if (exists)
            {
                return new WebResponseContent().Error("班次编码已存在");
            }

            return new WebResponseContent(true);
        }
    }
}
