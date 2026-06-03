/*
 *Author：jxx
 *Contact：283591387@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下Aps_Schedule_ResultService与IAps_Schedule_ResultService中编写
 */
using VOL.APS.IRepositories;
using VOL.APS.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class Aps_Schedule_ResultService : ServiceBase<Aps_Schedule_Result, IAps_Schedule_ResultRepository>
    , IAps_Schedule_ResultService, IDependency
    {
    public static IAps_Schedule_ResultService Instance
    {
      get { return AutofacContainerModule.GetService<IAps_Schedule_ResultService>(); } }
    }
 }
