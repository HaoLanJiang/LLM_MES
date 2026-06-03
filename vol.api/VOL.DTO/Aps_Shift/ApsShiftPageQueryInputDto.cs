using VOL.DTO.BaseDTO;

namespace VOL.DTO.Aps_Shift
{
    /// <summary>
    /// 排产班次分页查询入参
    /// </summary>
    public class ApsShiftPageQueryInputDto : PageQueryBaseDto
    {
        /// <summary>
        /// 班次编码
        /// </summary>
        public string? shift_code { get; set; }

        /// <summary>
        /// 班次名称
        /// </summary>
        public string? shift_name { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public sbyte? enable_flag { get; set; }
    }
}
