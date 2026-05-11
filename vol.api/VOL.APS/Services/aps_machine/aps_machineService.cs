/*
 *Author：jxx
 *Contact：283591387@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下aps_machineService与Iaps_machineService中编写
 */
using VOL.APS.IRepositories;
using VOL.APS.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class aps_machineService : ServiceBase<aps_machine, Iaps_machineRepository>
    , Iaps_machineService, IDependency
    {
    public static Iaps_machineService Instance
    {
      get { return AutofacContainerModule.GetService<Iaps_machineService>(); } }
    }
 }
