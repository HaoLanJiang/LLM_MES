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
        /// <summary>
        /// 分页获取设备列表。
        /// </summary>
        /// <param name="input">分页查询条件，包含设备编码、设备名称、页码、每页条数及排序信息。</param>
        /// <returns>返回设备分页数据集合。</returns>
        PageGridData<ApsMachinePageOutputDto> GetMachinePageList(ApsMachinePageQueryInputDto input);
    }
 }
