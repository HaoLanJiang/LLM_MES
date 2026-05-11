/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹aps_work_orderController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.APS.IServices;
using VOL.Core.Services;

namespace VOL.APS.Controllers
{
    [Route("api/aps_work_order")]
    [PermissionTable(Name = "aps_work_order")]
    public partial class aps_work_orderController : ApiBaseController<Iaps_work_orderService>
    {
        public aps_work_orderController(Iaps_work_orderService service)
        : base(service)
        {
        }
    }
}

