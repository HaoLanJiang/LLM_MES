using Microsoft.AspNetCore.Mvc;
using VOL.APS.IServices;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.DTO.Aps_ProductionScheduling;

namespace VOL.APS.Controllers
{
    public partial class Aps_Schedule_ResultController
    {
        /// <summary>
        /// 执行生产排产并写入排产结果。
        /// </summary>
        /// <param name="input">排产参数。</param>
        /// <param name="productionSchedulingService">排产服务。</param>
        /// <returns>返回排产执行结果。</returns>
        [ApiActionPermission(ActionPermissionOptions.Update)]
        [HttpPost, Route("RunProductionScheduling")]
        public ActionResult RunProductionScheduling(
            [FromBody] RunProductionSchedulingInputDto input,
            [FromServices] IProductionSchedulingService productionSchedulingService)
        {
            return Json(productionSchedulingService.RunProductionScheduling(input));
        }
    }
}
