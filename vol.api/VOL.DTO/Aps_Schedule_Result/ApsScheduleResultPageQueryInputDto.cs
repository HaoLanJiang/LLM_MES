using VOL.DTO.BaseDTO;

namespace VOL.DTO.Aps_Schedule_Result
{
    /// <summary>
    /// 排产结果分页查询入参
    /// </summary>
    public class ApsScheduleResultPageQueryInputDto : PageQueryBaseDto
    {
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
