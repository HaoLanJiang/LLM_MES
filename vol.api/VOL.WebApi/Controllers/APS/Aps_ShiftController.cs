/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹Aps_ShiftController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.APS.IServices;
namespace VOL.APS.Controllers
{
    [Route("api/Aps_Shift")]
    [PermissionTable(Name = "Aps_Shift")]
    public partial class Aps_ShiftController : ApiBaseController<IAps_ShiftService>
    {
        public Aps_ShiftController(IAps_ShiftService service)
        : base(service)
        {
        }
    }
}

