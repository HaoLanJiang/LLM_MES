/*
 *接口编写处...
*如果接口需要做Action的权限验证，请在Action上使用属性
*如: [ApiActionPermission("Aps_Schedule_Time",Enums.ActionPermissionOptions.Search)]
 */
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using VOL.Entity.DomainModels;
using VOL.APS.IServices;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.DTO.Aps_Schedule_Time;

namespace VOL.APS.Controllers
{
    public partial class Aps_Schedule_TimeController
    {
        private readonly IAps_Schedule_TimeService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Aps_Schedule_TimeController(
            IAps_Schedule_TimeService service,
            IHttpContextAccessor httpContextAccessor
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取设备月度排产日历数据。
        /// </summary>
        /// <param name="input">设备排产日历查询条件。</param>
        /// <returns>返回设备排产日历列表。</returns>
        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetMachineScheduleCalendar")]
        public ActionResult GetMachineScheduleCalendar([FromBody] ApsMachineScheduleCalendarQueryInputDto input)
        {
            return JsonNormal(new { rows = _service.GetMachineScheduleCalendar(input) });
        }

        /// <summary>
        /// 获取设备指定日期的班次排产明细。
        /// </summary>
        /// <param name="input">班次排产明细查询条件。</param>
        /// <returns>返回班次排产明细列表。</returns>
        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetMachineScheduleTimeDetail")]
        public ActionResult GetMachineScheduleTimeDetail([FromBody] ApsMachineScheduleTimeDetailQueryInputDto input)
        {
            return JsonNormal(new { rows = _service.GetMachineScheduleTimeDetail(input) });
        }

        /// <summary>
        /// 保存设备指定日期的班次排产明细。
        /// </summary>
        /// <param name="input">班次排产明细保存参数。</param>
        /// <returns>返回保存结果。</returns>
        [ApiActionPermission(ActionPermissionOptions.Update)]
        [HttpPost, Route("SaveMachineScheduleTimeDetail")]
        public ActionResult SaveMachineScheduleTimeDetail([FromBody] SaveApsMachineScheduleTimeDetailInputDto input)
        {
            return Json(_service.SaveMachineScheduleTimeDetail(input));
        }

        /// <summary>
        /// 批量生成未来排产时间。
        /// </summary>
        /// <param name="input">排产时间生成参数。</param>
        /// <returns>返回生成结果。</returns>
        [AllowAnonymous]
        [HttpPost, Route("GenerateFutureScheduleTime")]
        public ActionResult GenerateFutureScheduleTime([FromBody] GenerateApsScheduleTimeInputDto input)
        {
            return Json(_service.GenerateFutureScheduleTime(input));
        }
    }
}
