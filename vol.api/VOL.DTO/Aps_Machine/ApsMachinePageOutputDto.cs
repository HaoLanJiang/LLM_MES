namespace VOL.DTO.Aps_Machine
{
    /// <summary>
    /// 设备分页查询返回 DTO
    /// </summary>
    public class ApsMachinePageOutputDto
    {
        /// <summary>
        /// 设备主键
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 设备编码
        /// </summary>
        public string? MachineCode { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string? MachineName { get; set; }

        /// <summary>
        /// 每日产能分钟
        /// </summary>
        public int? CapacityMinutesPerDay { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}
