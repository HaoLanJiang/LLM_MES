using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using VOL.APS.IServices;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.DTO.Aps_Schedule_Result;

namespace VOL.APS.Controllers
{
    public partial class Aps_Schedule_ResultController
    {
        private readonly IAps_Schedule_ResultService _service;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Aps_Schedule_ResultController(
            IAps_Schedule_ResultService service,
            IHttpContextAccessor httpContextAccessor)
            : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetScheduleResultPageList")]
        public ActionResult GetScheduleResultPageList([FromBody] ApsScheduleResultPageQueryInputDto input)
        {
            return JsonNormal(_service.GetScheduleResultPageList(input));
        }
    }
}
