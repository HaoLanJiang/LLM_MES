using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using VOL.APS.IServices;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.DTO.Aps_Shift;

namespace VOL.APS.Controllers
{
    public partial class Aps_ShiftController
    {
        private readonly IAps_ShiftService _service;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Aps_ShiftController(
            IAps_ShiftService service,
            IHttpContextAccessor httpContextAccessor)
            : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetShiftPageList")]
        public ActionResult GetShiftPageList([FromBody] ApsShiftPageQueryInputDto input)
        {
            return JsonNormal(_service.GetShiftPageList(input));
        }
    }
}
