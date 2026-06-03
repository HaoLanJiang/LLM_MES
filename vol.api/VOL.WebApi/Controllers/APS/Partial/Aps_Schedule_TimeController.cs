/*
 *接口编写处...
*如果接口需要做Action的权限验证，请在Action上使用属性
*如: [ApiActionPermission("Aps_Schedule_Time",Enums.ActionPermissionOptions.Search)]
 */
using Microsoft.AspNetCore.Mvc;
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

        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetMachineScheduleCalendar")]
        public ActionResult GetMachineScheduleCalendar([FromBody] ApsMachineScheduleCalendarQueryInputDto input)
        {
            return JsonNormal(new { rows = _service.GetMachineScheduleCalendar(input) });
        }

        [ApiActionPermission(ActionPermissionOptions.Add)]
        [HttpPost, Route("GenerateFutureScheduleTime")]
        public ActionResult GenerateFutureScheduleTime([FromBody] GenerateApsScheduleTimeInputDto input)
        {
            return Json(_service.GenerateFutureScheduleTime(input));
        }
    }
}
