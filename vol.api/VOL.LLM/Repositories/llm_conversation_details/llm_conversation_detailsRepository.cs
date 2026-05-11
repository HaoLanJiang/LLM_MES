/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *Repository提供数据库操作，如果要增加数据库操作请在当前目录下Partial文件夹llm_conversation_detailsRepository编写代码
 */
using VOL.LLM.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.EFDbContext;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.LLM.Repositories
{
    public partial class llm_conversation_detailsRepository : RepositoryBase<llm_conversation_details> , Illm_conversation_detailsRepository
    {
    public llm_conversation_detailsRepository(VOLContext dbContext)
    : base(dbContext)
    {

    }
    public static Illm_conversation_detailsRepository Instance
    {
      get {  return AutofacContainerModule.GetService<Illm_conversation_detailsRepository>(); } }
    }
}
