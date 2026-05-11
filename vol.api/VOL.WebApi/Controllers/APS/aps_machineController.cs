/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹aps_machineController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.APS.IServices;
namespace VOL.APS.Controllers
{
    [Route("api/aps_machine")]
    [PermissionTable(Name = "aps_machine")]
    public partial class aps_machineController : ApiBaseController<Iaps_machineService>
    {
        public aps_machineController(Iaps_machineService service)
        : base(service)
        {
        }
    }
}

