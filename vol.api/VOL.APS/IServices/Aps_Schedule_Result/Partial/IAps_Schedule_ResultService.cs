/*
*所有关于Aps_Schedule_Result类的业务代码接口应在此处编写
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using VOL.DTO.Aps_Schedule_Result;
namespace VOL.APS.IServices
{
    public partial interface IAps_Schedule_ResultService
    {
        /// <summary>
        /// 分页获取排产结果列表。
        /// </summary>
        /// <param name="input">分页查询条件，包含工单号、设备编码、排产状态、页码、每页条数及排序信息。</param>
        /// <returns>返回排产结果分页数据集合。</returns>
        PageGridData<ApsScheduleResultPageOutputDto> GetScheduleResultPageList(ApsScheduleResultPageQueryInputDto input);
    }
 }
