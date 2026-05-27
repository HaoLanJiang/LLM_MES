using VOL.DTO.BaseDTO;

namespace VOL.DTO.Aps_Machine
{
    /// <summary>
    /// 设备分页查询入参
    /// </summary>
    public class ApsMachinePageQueryInputDto : PageQueryBaseDto
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        public string? MachineCode { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string? MachineName { get; set; }
    }
}
