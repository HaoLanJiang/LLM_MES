/*
 *Author：jxx
 *Contact：283591387@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下aps_work_orderService与Iaps_work_orderService中编写
 */
using VOL.APS.IRepositories;
using VOL.APS.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Services
{
    public partial class aps_work_orderService : ServiceBase<aps_work_order, Iaps_work_orderRepository>
    , Iaps_work_orderService, IDependency
    {
    public static Iaps_work_orderService Instance
    {
      get { return AutofacContainerModule.GetService<Iaps_work_orderService>(); } }
    }
 }
