/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *Repository提供数据库操作，如果要增加数据库操作请在当前目录下Partial文件夹Aps_Schedule_TimeRepository编写代码
 */
using VOL.APS.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.EFDbContext;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.APS.Repositories
{
    public partial class Aps_Schedule_TimeRepository : RepositoryBase<Aps_Schedule_Time> , IAps_Schedule_TimeRepository
    {
    public Aps_Schedule_TimeRepository(VOLContext dbContext)
    : base(dbContext)
    {

    }
    public static IAps_Schedule_TimeRepository Instance
    {
      get {  return AutofacContainerModule.GetService<IAps_Schedule_TimeRepository>(); } }
    }
}
