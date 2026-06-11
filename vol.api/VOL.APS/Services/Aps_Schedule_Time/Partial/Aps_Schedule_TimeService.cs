/*
 *所有关于Aps_Schedule_Time类的业务代码应在此处编写
 *可使用repository调用常用方法，获取EF/Dapper等信息
 *如需事务请使用repository.DbContextBeginTransaction
 *用户信息、权限、角色等可使用UserContext.Current操作
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VOL.APS.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.Configuration;
using VOL.Core.Enums;
using VOL.Core.Utilities;
using VOL.DTO.Aps_Schedule_Time;
using VOL.Entity.DomainModels;
using Yitter.IdGenerator;

namespace VOL.APS.Services
{
    public partial class Aps_Schedule_TimeService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAps_Schedule_TimeRepository _repository;

        [ActivatorUtilitiesConstructor]
        public Aps_Schedule_TimeService(
            IAps_Schedule_TimeRepository dbRepository,
            IHttpContextAccessor httpContextAccessor)
            : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
        }

        /// <summary>
        /// 获取设备月度排产日历数据。
        /// </summary>
        /// <param name="input">查询条件，包含设备编码、设备名称以及目标年月。</param>
        /// <returns>返回按日期汇总的设备排产日历数据集合。</returns>
        public List<ApsMachineScheduleCalendarOutputDto> GetMachineScheduleCalendar(ApsMachineScheduleCalendarQueryInputDto input)
        {
            input ??= new ApsMachineScheduleCalendarQueryInputDto();

            string? machineCode = input.MachineCode?.Trim();
            string? machineName = input.MachineName?.Trim();
            int year = input.Year > 0 ? input.Year : DateTime.Now.Year;
            int month = input.Month is >= 1 and <= 12 ? input.Month : DateTime.Now.Month;

            DateTime currentMonthStart = new DateTime(year, month, 1);
            DateTime startDate = currentMonthStart.AddMonths(-1);
            DateTime nextMonthLastDate = currentMonthStart.AddMonths(2).AddDays(-1);
            DateTime endDate = nextMonthLastDate.AddDays(1);

            var query = _repository.DbContext
                .Set<Aps_Schedule_Time>()
                .AsNoTracking()
                .Where(x => x.schedule_date >= startDate && x.schedule_date < endDate);

            if (!string.IsNullOrWhiteSpace(machineCode))
            {
                query = query.Where(x => x.line_code == machineCode);
            }
            else if (!string.IsNullOrWhiteSpace(machineName))
            {
                query = query.Where(x => x.line_name == machineName);
            }

            return query
                .ToList()
                .GroupBy(x => x.schedule_date.Date)
                .OrderBy(x => x.Key)
                .Select(group =>
                {
                    var activeGroup = group.Where(x => x.status == 1 && x.available_minutes > 0).ToList();
                    var restGroup = group.Where(x => x.status != 1 || x.available_minutes <= 0).ToList();
                    int availableMinutes = group.Sum(x => x.available_minutes);
                    int usedMinutes = group.Sum(x => x.used_minutes);
                    int remainMinutes = group.Sum(x => x.remain_minutes);
                    List<string> shiftNames = activeGroup
                        .Select(x => x.shift_name)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();
                    List<string> restShiftNames = restGroup
                        .Select(x => x.shift_name)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();
                    List<string> timeRanges = activeGroup
                        .OrderBy(x => x.start_datetime)
                        .Select(x => $"{x.start_datetime:HH:mm}-{x.end_datetime:HH:mm}")
                        .Distinct()
                        .ToList();
                    int status = group.Any(x => x.status != 1) ? group.Max(x => (int)x.status) : 1;

                    return new ApsMachineScheduleCalendarOutputDto
                    {
                        Date = group.Key.ToString("yyyy-MM-dd"),
                        AvailableMinutes = availableMinutes,
                        UsedMinutes = usedMinutes,
                        RemainMinutes = remainMinutes,
                        Status = status,
                        ShiftNames = string.Join(" / ", shiftNames),
                        RestShiftNames = string.Join(" / ", restShiftNames),
                        TimeRangeText = string.Join(", ", timeRanges),
                        DisplayText = availableMinutes > 0 ? availableMinutes.ToString() : "REST"
                    };
                })
                .ToList();
        }

        /// <summary>
        /// 获取指定设备在指定日期的班次排产明细。
        /// </summary>
        /// <param name="input">查询条件，包含设备编码和排产日期。</param>
        /// <returns>返回当日班次排产明细集合。</returns>
        public List<ApsMachineScheduleTimeDetailOutputDto> GetMachineScheduleTimeDetail(ApsMachineScheduleTimeDetailQueryInputDto input)
        {
            input ??= new ApsMachineScheduleTimeDetailQueryInputDto();

            string machineCode = input.MachineCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(machineCode))
            {
                return new List<ApsMachineScheduleTimeDetailOutputDto>();
            }

            if (!DateTime.TryParse(input.ScheduleDate, out DateTime scheduleDate))
            {
                return new List<ApsMachineScheduleTimeDetailOutputDto>();
            }

            DateTime targetDate = scheduleDate.Date;
            DateTime nextDate = targetDate.AddDays(1);

            List<ApsMachineScheduleTimeDetailOutputDto> shiftList = _repository.DbContext
                .Set<Aps_Schedule_Time>()
                .AsNoTracking()
                .Where(x => x.line_code == machineCode
                    && x.schedule_date >= targetDate
                    && x.schedule_date < nextDate)
                .OrderBy(x => x.start_datetime)
                .Select(x => new ApsMachineScheduleTimeDetailOutputDto
                {
                    Id = x.id,
                    ScheduleDate = x.schedule_date.ToString("yyyy-MM-dd"),
                    MachineCode = x.line_code,
                    MachineName = x.line_name,
                    ShiftId = x.shift_id,
                    ShiftCode = x.shift_code,
                    ShiftName = x.shift_name,
                    StartDateTime = x.start_datetime.ToString("yyyy-MM-dd HH:mm:ss"),
                    EndDateTime = x.end_datetime.ToString("yyyy-MM-dd HH:mm:ss"),
                    AvailableMinutes = x.available_minutes,
                    UsedMinutes = x.used_minutes,
                    RemainMinutes = x.remain_minutes,
                    Status = x.status,
                    IsRest = x.status != 1 || x.available_minutes <= 0
                })
                .ToList();

            if (shiftList.Count == 0)
            {
                return shiftList;
            }

            DateTime minShiftStart = shiftList
                .Select(x => DateTime.TryParse(x.StartDateTime, out DateTime value) ? value : (DateTime?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty(targetDate)
                .Min();

            DateTime maxShiftEnd = shiftList
                .Select(x => DateTime.TryParse(x.EndDateTime, out DateTime value) ? value : (DateTime?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty(nextDate)
                .Max();

            List<Aps_Schedule_Result> orderList = _repository.DbContext
                .Set<Aps_Schedule_Result>()
                .AsNoTracking()
                .Where(x => x.MachineCode == machineCode
                    && x.PlanStartTime < maxShiftEnd
                    && x.PlanEndTime > minShiftStart)
                .OrderBy(x => x.PlanStartTime)
                .ToList();

            foreach (ApsMachineScheduleTimeDetailOutputDto shift in shiftList)
            {
                if (!DateTime.TryParse(shift.StartDateTime, out DateTime shiftStart)
                    || !DateTime.TryParse(shift.EndDateTime, out DateTime shiftEnd))
                {
                    continue;
                }

                shift.OrderList = orderList
                    .Where(x => x.PlanStartTime < shiftEnd && x.PlanEndTime > shiftStart)
                    .OrderBy(x => x.PlanStartTime)
                    .Select(x => new ApsMachineScheduleOrderInfoDto
                    {
                        Id = x.Id,
                        WorkOrderId = x.WorkOrderId,
                        WorkOrderNo = x.WorkOrderNo,
                        MachineCode = x.MachineCode,
                        MachineName = x.MachineName,
                        PlanStartTime = x.PlanStartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        PlanEndTime = x.PlanEndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        PlanMinutes = x.PlanMinutes,
                        OrderQty = x.OrderQty,
                        CustomerName = x.CustomerName,
                        CustomerPriority = x.CustomerPriority,
                        ScheduleStatus = x.ScheduleStatus,
                        Remark = x.Remark
                    })
                    .ToList();
            }

            return shiftList;
        }

        /// <summary>
        /// 保存设备指定日期的班次排产明细。
        /// </summary>
        /// <param name="input">保存参数，包含设备编码、排产日期及需要更新的班次明细。</param>
        /// <returns>返回保存操作结果。</returns>
        public WebResponseContent SaveMachineScheduleTimeDetail(SaveApsMachineScheduleTimeDetailInputDto input)
        {
            input ??= new SaveApsMachineScheduleTimeDetailInputDto();
            string machineCode = input.MachineCode?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(machineCode))
            {
                return new WebResponseContent().Error("设备编号不能为空");
            }

            if (!DateTime.TryParse(input.ScheduleDate, out DateTime scheduleDate))
            {
                return new WebResponseContent().Error("排产日期格式不正确");
            }

            if (input.Items == null || input.Items.Count == 0)
            {
                return new WebResponseContent().Error("未获取到可保存的班次数据");
            }

            DateTime targetDate = scheduleDate.Date;
            DateTime nextDate = targetDate.AddDays(1);
            List<long> ids = input.Items.Where(x => x.Id > 0).Select(x => x.Id).Distinct().ToList();

            List<Aps_Schedule_Time> dbList = _repository.DbContext
                .Set<Aps_Schedule_Time>()
                .Where(x => x.line_code == machineCode
                    && x.schedule_date >= targetDate
                    && x.schedule_date < nextDate
                    && ids.Contains(x.id))
                .ToList();

            if (dbList.Count != ids.Count)
            {
                return new WebResponseContent().Error("部分班次数据不存在，请刷新后重试");
            }

            Dictionary<long, Aps_Schedule_Time> dbMap = dbList.ToDictionary(x => x.id, x => x);
            DateTime now = DateTime.Now;

            foreach (SaveApsMachineScheduleTimeDetailItemDto item in input.Items)
            {
                if (!dbMap.TryGetValue(item.Id, out Aps_Schedule_Time? entity) || entity == null)
                {
                    return new WebResponseContent().Error("班次数据不存在，请刷新后重试");
                }

                if (!DateTime.TryParse(item.StartDateTime, out DateTime startDateTime))
                {
                    return new WebResponseContent().Error($"班次[{entity.shift_name}]开始时间格式不正确");
                }

                if (!DateTime.TryParse(item.EndDateTime, out DateTime endDateTime))
                {
                    return new WebResponseContent().Error($"班次[{entity.shift_name}]结束时间格式不正确");
                }

                if (endDateTime <= startDateTime)
                {
                    return new WebResponseContent().Error($"班次[{entity.shift_name}]结束时间必须大于开始时间");
                }

                int availableMinutes = item.IsRest ? 0 : (int)Math.Round((endDateTime - startDateTime).TotalMinutes, MidpointRounding.AwayFromZero);
                if (!item.IsRest && availableMinutes <= 0)
                {
                    return new WebResponseContent().Error($"班次[{entity.shift_name}]工作分钟数必须大于0");
                }

                entity.start_datetime = startDateTime;
                entity.end_datetime = endDateTime;
                entity.available_minutes = availableMinutes;
                entity.used_minutes = item.IsRest ? 0 : entity.used_minutes;
                entity.remain_minutes = item.IsRest ? 0 : Math.Max(availableMinutes - entity.used_minutes, 0);
                entity.status = (sbyte)(item.IsRest ? 4 : 1);
                entity.update_time = now;
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                _repository.UpdateRange(dbList);
                _repository.SaveChanges();
                return new WebResponseContent().OK("保存成功");
            });
        }

        /// <summary>
        /// 按设备和启用班次批量生成未来排产时间。
        /// </summary>
        /// <param name="input">生成参数，包含开始日期和生成天数。</param>
        /// <returns>返回生成操作结果以及生成统计信息。</returns>
        public WebResponseContent GenerateFutureScheduleTime(GenerateApsScheduleTimeInputDto input)
        {
            input ??= new GenerateApsScheduleTimeInputDto();

            DateTime startDate = (input.StartDate ?? DateTime.Today).Date;
            int days = input.Days <= 0 ? 90 : input.Days;
            if (days > 365)
            {
                return new WebResponseContent().Error("生成天数不能超过365天");
            }

            DateTime endDateExclusive = startDate.AddDays(days);

            List<Aps_Machine> machineList = _repository.DbContext
                .Set<Aps_Machine>()
                .AsNoTracking()
                .OrderBy(x => x.MachineCode)
                .ToList();

            if (machineList.Count == 0)
            {
                return new WebResponseContent().Error("未找到设备数据，无法生成排产时间");
            }

            List<Aps_Shift> shiftList = _repository.DbContext
                .Set<Aps_Shift>()
                .AsNoTracking()
                .Where(x => x.enable_flag == 1)
                .OrderBy(x => x.shift_code)
                .ToList();

            if (shiftList.Count == 0)
            {
                return new WebResponseContent().Error("未找到启用的班次数据，无法生成排产时间");
            }

            List<Aps_Schedule_Time> existsList = _repository.DbContext
                .Set<Aps_Schedule_Time>()
                .AsNoTracking()
                .Where(x => x.schedule_date >= startDate && x.schedule_date < endDateExclusive)
                .Select(x => new Aps_Schedule_Time
                {
                    schedule_date = x.schedule_date,
                    line_code = x.line_code,
                    shift_code = x.shift_code
                })
                .ToList();

            HashSet<string> existsKeys = existsList
                .Select(x => BuildScheduleTimeKey(x.schedule_date.Date, x.line_code, x.shift_code))
                .ToHashSet();

            List<Aps_Schedule_Time> addList = new List<Aps_Schedule_Time>();
            DateTime now = DateTime.Now;
            int skipCount = 0;

            for (int i = 0; i < days; i++)
            {
                DateTime currentDate = startDate.AddDays(i);

                foreach (Aps_Machine machine in machineList)
                {
                    foreach (Aps_Shift shift in shiftList)
                    {
                        string key = BuildScheduleTimeKey(currentDate, machine.MachineCode, shift.shift_code);
                        if (existsKeys.Contains(key))
                        {
                            skipCount++;
                            continue;
                        }

                        DateTime startDateTime = currentDate.Date.Add(shift.start_time);
                        DateTime endDateTime = currentDate.Date.Add(shift.end_time);
                        if (shift.cross_day_flag == 1 || endDateTime <= startDateTime)
                        {
                            endDateTime = endDateTime.AddDays(1);
                        }

                        addList.Add(new Aps_Schedule_Time
                        {
                            id = AppSetting.EnableSnowFlakeID ? YitIdHelper.NextId() : DateTime.Now.Ticks + addList.Count,
                            schedule_date = currentDate.Date,
                            line_code = machine.MachineCode,
                            line_name = machine.MachineName,
                            shift_id = shift.id,
                            shift_code = shift.shift_code,
                            shift_name = shift.shift_name,
                            start_datetime = startDateTime,
                            end_datetime = endDateTime,
                            available_minutes = shift.work_minutes,
                            used_minutes = 0,
                            remain_minutes = shift.work_minutes,
                            status = 1,
                            enable_flag = 1,
                            remark = $"system generated on {now:yyyy-MM-dd HH:mm:ss}",
                            create_time = now,
                            update_time = now
                        });

                        existsKeys.Add(key);
                    }
                }
            }

            if (addList.Count == 0)
            {
                return new WebResponseContent().OK("未来排产时间已存在，无需重复生成", new
                {
                    startDate = startDate.ToString("yyyy-MM-dd"),
                    days,
                    machineCount = machineList.Count,
                    shiftCount = shiftList.Count,
                    addCount = 0,
                    skipCount
                });
            }

            return _repository.DbContextBeginTransaction(() =>
            {
                _repository.AddRange(addList);
                _repository.SaveChanges();

                return new WebResponseContent().OK("未来排产时间生成成功", new
                {
                    startDate = startDate.ToString("yyyy-MM-dd"),
                    days,
                    machineCount = machineList.Count,
                    shiftCount = shiftList.Count,
                    addCount = addList.Count,
                    skipCount
                });
            });
        }

        /// <summary>
        /// 构造排产时间唯一键。
        /// </summary>
        /// <param name="scheduleDate">排产日期。</param>
        /// <param name="lineCode">设备编码。</param>
        /// <param name="shiftCode">班次编码。</param>
        /// <returns>返回由日期、设备和班次组成的唯一键。</returns>
        private static string BuildScheduleTimeKey(DateTime scheduleDate, string lineCode, string shiftCode)
        {
            return $"{scheduleDate:yyyy-MM-dd}|{lineCode}|{shiftCode}";
        }
    }
}
