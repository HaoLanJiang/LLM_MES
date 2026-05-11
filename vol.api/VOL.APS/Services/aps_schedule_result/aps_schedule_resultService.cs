/*
 *Author：jxx
 *Contact：283591387@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下aps_schedule_resultService与Iaps_schedule_resultService中编写
 */
using VOL.APS.IRepositories;
using VOL.APS.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class aps_schedule_resultService : ServiceBase<aps_schedule_result, Iaps_schedule_resultRepository>
    , Iaps_schedule_resultService, IDependency
    {
    public static Iaps_schedule_resultService Instance
    {
      get { return AutofacContainerModule.GetService<Iaps_schedule_resultService>(); } }
    }
 }
