/*
*所有关于Aps_Shift类的业务代码接口应在此处编写
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using VOL.DTO.Aps_Shift;
namespace VOL.APS.IServices
{
    public partial interface IAps_ShiftService
    {
        PageGridData<ApsShiftPageOutputDto> GetShiftPageList(ApsShiftPageQueryInputDto input);
    }
 }
