using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VOL.Core.Extensions.AutofacManager;
using VOL.LLM.IRepositories;

namespace VOL.LLM.Services
{
    public class LLM_Service : IDependency
    {
        //依赖注入Illm_conversatioRepository
        Illm_conversatioRepository _llm_conversatioRepository = AutofacContainerModule.GetService<Illm_conversatioRepository>();


        public LLM_Service() { }

    }
}
