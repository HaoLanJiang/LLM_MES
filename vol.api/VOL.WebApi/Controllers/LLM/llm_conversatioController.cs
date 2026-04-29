/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹llm_conversatioController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.LLM.IServices;
namespace VOL.LLM.Controllers
{
    [Route("api/llm_conversatio")]
    [PermissionTable(Name = "llm_conversatio")]
    public partial class llm_conversatioController : ApiBaseController<Illm_conversatioService>
    {
        public llm_conversatioController(Illm_conversatioService service)
        : base(service)
        {
        }
    }
}

