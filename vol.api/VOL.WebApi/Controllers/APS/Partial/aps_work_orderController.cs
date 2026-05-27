using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using VOL.APS.IServices;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.DTO.Aps_Work_Order;

namespace VOL.APS.Controllers
{
    public partial class Aps_Work_OrderController
    {
        private readonly IAps_Work_OrderService _service;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Aps_Work_OrderController(
            IAps_Work_OrderService service,
            IHttpContextAccessor httpContextAccessor)
            : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetWorkOrderPageList")]
        public ActionResult GetWorkOrderPageList([FromBody] ApsWorkOrderPageQueryInputDto input)
        {
            return JsonNormal(_service.GetWorkOrderPageList(input));
        }
    }
}
