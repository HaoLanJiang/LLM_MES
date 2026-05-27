/*
 *Author：jxx
 *Contact：283591387@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下Aps_Work_OrderService与IAps_Work_OrderService中编写
 */
using VOL.APS.IRepositories;
using VOL.APS.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class Aps_Work_OrderService : ServiceBase<Aps_Work_Order, IAps_Work_OrderRepository>
    , IAps_Work_OrderService, IDependency
    {
    public static IAps_Work_OrderService Instance
    {
      get { return AutofacContainerModule.GetService<IAps_Work_OrderService>(); } }
    }
 }
