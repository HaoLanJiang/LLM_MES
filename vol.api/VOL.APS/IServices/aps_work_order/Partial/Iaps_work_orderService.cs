/*
*所有关于Aps_Work_Order类的业务代码接口应在此处编写
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using VOL.DTO.Aps_Work_Order;
namespace VOL.APS.IServices
{
    public partial interface IAps_Work_OrderService
    {
        /// <summary>
        /// 分页获取工单列表。
        /// </summary>
        /// <param name="input">分页查询条件，包含工单号、客户名称、排产状态、页码、每页条数及排序信息。</param>
        /// <returns>返回工单分页数据集合。</returns>
        PageGridData<ApsWorkOrderPageOutputDto> GetWorkOrderPageList(ApsWorkOrderPageQueryInputDto input);
    }
 }
