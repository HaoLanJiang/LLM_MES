using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using VOL.APS.IServices;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.DTO.Aps_Machine;

namespace VOL.APS.Controllers
{
    public partial class Aps_MachineController
    {
        private readonly IAps_MachineService _service;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [ActivatorUtilitiesConstructor]
        public Aps_MachineController(
            IAps_MachineService service,
            IHttpContextAccessor httpContextAccessor)
            : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 分页查询设备列表。
        /// </summary>
        /// <param name="input">设备分页查询条件。</param>
        /// <returns>返回设备分页结果。</returns>
        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetMachinePageList")]
        public ActionResult GetMachinePageList([FromBody] ApsMachinePageQueryInputDto input)
        {
            return JsonNormal(_service.GetMachinePageList(input));
        }
    }
}
