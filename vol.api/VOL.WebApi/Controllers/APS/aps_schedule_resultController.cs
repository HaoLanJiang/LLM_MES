/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹aps_schedule_resultController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.APS.IServices;
namespace VOL.APS.Controllers
{
    [Route("api/aps_schedule_result")]
    [PermissionTable(Name = "aps_schedule_result")]
    public partial class aps_schedule_resultController : ApiBaseController<Iaps_schedule_resultService>
    {
        public aps_schedule_resultController(Iaps_schedule_resultService service)
        : base(service)
        {
        }
    }
}

