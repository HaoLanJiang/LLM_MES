using VOL.DTO.BaseDTO;

namespace VOL.DTO.Aps_Schedule_Result
{
    /// <summary>
    /// 排产结果分页查询入参
    /// </summary>
    public class ApsScheduleResultPageQueryInputDto : PageQueryBaseDto
    {
        /// <summary>
        /// 用户ID（排产结果创建人ID）
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// 工单号
        /// </summary>
        public string? WorkOrderNo { get; set; }

        /// <summary>
        /// 设备编码
        /// </summary>
        public string? MachineCode { get; set; }

        /// <summary>
        /// 排产状态
        /// </summary>
        public string? ScheduleStatus { get; set; }
    }
}
