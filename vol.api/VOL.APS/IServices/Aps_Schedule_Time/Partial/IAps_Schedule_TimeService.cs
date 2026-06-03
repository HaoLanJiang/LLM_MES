/*
*所有关于Aps_Schedule_Time类的业务代码接口应在此处编写
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using System.Collections.Generic;
using VOL.DTO.Aps_Schedule_Time;
namespace VOL.APS.IServices
{
    public partial interface IAps_Schedule_TimeService
    {
        List<ApsMachineScheduleCalendarOutputDto> GetMachineScheduleCalendar(ApsMachineScheduleCalendarQueryInputDto input);
        WebResponseContent GenerateFutureScheduleTime(GenerateApsScheduleTimeInputDto input);
    }
 }
