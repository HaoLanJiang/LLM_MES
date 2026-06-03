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

        public List<ApsMachineScheduleCalendarOutputDto> GetMachineScheduleCalendar(ApsMachineScheduleCalendarQueryInputDto input)
        {
            input ??= new ApsMachineScheduleCalendarQueryInputDto();

            string? machineCode = input.MachineCode?.Trim();
            string? machineName = input.MachineName?.Trim();
            int year = input.Year > 0 ? input.Year : DateTime.Now.Year;
            int month = input.Month is >= 1 and <= 12 ? input.Month : DateTime.Now.Month;

            DateTime startDate = new DateTime(year, month, 1);
            DateTime endDate = startDate.AddMonths(1);

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
                    int availableMinutes = group.Sum(x => x.available_minutes);
                    int usedMinutes = group.Sum(x => x.used_minutes);
                    int remainMinutes = group.Sum(x => x.remain_minutes);
                    List<string> shiftNames = group
                        .Select(x => x.shift_name)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();
                    List<string> timeRanges = group
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
                        TimeRangeText = string.Join(", ", timeRanges),
                        DisplayText = availableMinutes > 0 ? availableMinutes.ToString() : "REST"
                    };
                })
                .ToList();
        }

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
                _repository.DbContext.Set<Aps_Schedule_Time>().AddRange(addList);
                _repository.DbContext.SaveChanges();

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

        private static string BuildScheduleTimeKey(DateTime scheduleDate, string lineCode, string shiftCode)
        {
            return $"{scheduleDate:yyyy-MM-dd}|{lineCode}|{shiftCode}";
        }
    }
}
