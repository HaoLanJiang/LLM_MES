/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹Aps_Schedule_TimeController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.APS.IServices;
namespace VOL.APS.Controllers
{
    [Route("api/Aps_Schedule_Time")]
    [PermissionTable(Name = "Aps_Schedule_Time")]
    public partial class Aps_Schedule_TimeController : ApiBaseController<IAps_Schedule_TimeService>
    {
        public Aps_Schedule_TimeController(IAps_Schedule_TimeService service)
        : base(service)
        {
        }
    }
}

