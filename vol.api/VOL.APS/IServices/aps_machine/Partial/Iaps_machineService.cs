/*
*所有关于Aps_Machine类的业务代码接口应在此处编写
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using VOL.DTO.Aps_Machine;
namespace VOL.APS.IServices
{
    public partial interface IAps_MachineService
    {
        PageGridData<ApsMachinePageOutputDto> GetMachinePageList(ApsMachinePageQueryInputDto input);
    }
 }
