/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *Repository提供数据库操作，如果要增加数据库操作请在当前目录下Partial文件夹aps_work_orderRepository编写代码
 */
using VOL.APS.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.EFDbContext;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Repositories
{
    public partial class aps_work_orderRepository : RepositoryBase<aps_work_order> , Iaps_work_orderRepository
    {
    public aps_work_orderRepository(VOLContext dbContext)
    : base(dbContext)
    {

    }
    public static Iaps_work_orderRepository Instance
    {
      get {  return AutofacContainerModule.GetService<Iaps_work_orderRepository>(); } }
    }
}
