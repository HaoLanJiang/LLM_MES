/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹Aps_Work_OrderController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.APS.IServices;
namespace VOL.APS.Controllers
{
    [Route("api/Aps_Work_Order")]
    [PermissionTable(Name = "Aps_Work_Order")]
    public partial class Aps_Work_OrderController : ApiBaseController<IAps_Work_OrderService>
    {
        public Aps_Work_OrderController(IAps_Work_OrderService service)
        : base(service)
        {
        }
    }
}

