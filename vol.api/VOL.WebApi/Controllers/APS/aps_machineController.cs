/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹Aps_MachineController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.APS.IServices;
namespace VOL.APS.Controllers
{
    [Route("api/Aps_Machine")]
    [PermissionTable(Name = "Aps_Machine")]
    public partial class Aps_MachineController : ApiBaseController<IAps_MachineService>
    {
        public Aps_MachineController(IAps_MachineService service)
        : base(service)
        {
        }
    }
}

