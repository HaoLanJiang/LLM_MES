/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹llm_conversation_detailsController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.LLM.IServices;
namespace VOL.LLM.Controllers
{
    [Route("api/llm_conversation_details")]
    [PermissionTable(Name = "llm_conversation_details")]
    public partial class llm_conversation_detailsController : ApiBaseController<Illm_conversation_detailsService>
    {
        public llm_conversation_detailsController(Illm_conversation_detailsService service)
        : base(service)
        {
        }
    }
}

