using Microsoft.AspNetCore.Authorization;
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

        /// <summary>
        /// 分页查询工单列表。
        /// </summary>
        /// <param name="input">工单分页查询条件。</param>
        /// <returns>返回工单分页数据。</returns>
        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetWorkOrderPageList")]
        public ActionResult GetWorkOrderPageList([FromBody] ApsWorkOrderPageQueryInputDto input)
        {
            return JsonNormal(_service.GetWorkOrderPageList(input));
        }

        /// <summary>
        /// 批量新增工单测试数据
        /// </summary>
        /// <param name="input">测试数据生成参数</param>
        /// <returns>新增结果</returns>
        //[ApiActionPermission(ActionPermissionOptions.Add)]
        [AllowAnonymous]
        [HttpPost, Route("CreateTestWorkOrders")]
        public ActionResult CreateTestWorkOrders([FromBody] CreateApsWorkOrderTestDataInputDto input)
        {
            return Json(_service.CreateTestWorkOrders(input));
        }
    }
}
