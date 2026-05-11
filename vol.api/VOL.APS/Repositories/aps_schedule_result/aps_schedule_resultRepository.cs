/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *Repository提供数据库操作，如果要增加数据库操作请在当前目录下Partial文件夹aps_schedule_resultRepository编写代码
 */
using VOL.APS.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.EFDbContext;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Repositories
{
    public partial class aps_schedule_resultRepository : RepositoryBase<aps_schedule_result> , Iaps_schedule_resultRepository
    {
    public aps_schedule_resultRepository(VOLContext dbContext)
    : base(dbContext)
    {

    }
    public static Iaps_schedule_resultRepository Instance
    {
      get {  return AutofacContainerModule.GetService<Iaps_schedule_resultRepository>(); } }
    }
}
