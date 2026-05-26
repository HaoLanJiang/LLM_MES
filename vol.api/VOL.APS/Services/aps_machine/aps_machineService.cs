/*
 *Author：jxx
 *Contact：283591387@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下Aps_MachineService与IAps_MachineService中编写
 */
using VOL.APS.IRepositories;
using VOL.APS.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class Aps_MachineService : ServiceBase<Aps_Machine, IAps_MachineRepository>
    , IAps_MachineService, IDependency
    {
    public static IAps_MachineService Instance
    {
      get { return AutofacContainerModule.GetService<IAps_MachineService>(); } }
    }
 }
