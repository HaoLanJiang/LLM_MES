using VOL.DTO.BaseDTO;

namespace VOL.DTO.Aps_Work_Order
{
    /// <summary>
    /// 排产工单分页查询入参
    /// </summary>
    public class ApsWorkOrderPageQueryInputDto : PageQueryBaseDto
    {
        /// <summary>
        /// 工单号
        /// </summary>
        public string? WorkOrderNo { get; set; }

        /// <summary>
        /// 客户名称
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// 排产状态
        /// </summary>
        public string? ScheduleStatus { get; set; }
    }
}
