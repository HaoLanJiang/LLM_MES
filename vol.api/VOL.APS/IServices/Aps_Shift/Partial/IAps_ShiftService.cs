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
        /// <summary>
        /// 分页获取班次列表。
        /// </summary>
        /// <param name="input">分页查询条件，包含班次编码、名称、启用状态、页码、每页条数及排序信息。</param>
        /// <returns>返回班次分页数据集合。</returns>
        PageGridData<ApsShiftPageOutputDto> GetShiftPageList(ApsShiftPageQueryInputDto input);
    }
 }
