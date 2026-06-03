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
        /// <summary>
        /// 获取设备月度排产日历数据。
        /// </summary>
        /// <param name="input">查询条件，包含设备编码、设备名称以及目标年月。</param>
        /// <returns>返回按日期汇总的设备排产日历数据集合。</returns>
        List<ApsMachineScheduleCalendarOutputDto> GetMachineScheduleCalendar(ApsMachineScheduleCalendarQueryInputDto input);

        /// <summary>
        /// 获取指定设备在指定日期的班次排产明细。
        /// </summary>
        /// <param name="input">查询条件，包含设备编码和排产日期。</param>
        /// <returns>返回当日班次排产明细集合。</returns>
        List<ApsMachineScheduleTimeDetailOutputDto> GetMachineScheduleTimeDetail(ApsMachineScheduleTimeDetailQueryInputDto input);

        /// <summary>
        /// 保存设备指定日期的班次排产明细。
        /// </summary>
        /// <param name="input">保存参数，包含设备编码、排产日期及需要更新的班次明细。</param>
        /// <returns>返回保存操作结果。</returns>
        WebResponseContent SaveMachineScheduleTimeDetail(SaveApsMachineScheduleTimeDetailInputDto input);

        /// <summary>
        /// 按设备和启用班次批量生成未来排产时间。
        /// </summary>
        /// <param name="input">生成参数，包含开始日期和生成天数。</param>
        /// <returns>返回生成操作结果以及生成统计信息。</returns>
        WebResponseContent GenerateFutureScheduleTime(GenerateApsScheduleTimeInputDto input);
    }
 }
