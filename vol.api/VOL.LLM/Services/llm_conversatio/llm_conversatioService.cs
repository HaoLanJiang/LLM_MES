/*
 *Author：jxx
 *Contact：283591387@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下llm_conversatioService与Illm_conversatioService中编写
 */
using VOL.LLM.IRepositories;
using VOL.LLM.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.LLM.Services
{
    public partial class llm_conversatioService : ServiceBase<llm_conversatio, Illm_conversatioRepository>
    , Illm_conversatioService, IDependency
    {
    public static Illm_conversatioService Instance
    {
      get { return AutofacContainerModule.GetService<Illm_conversatioService>(); } }
    }
 }
