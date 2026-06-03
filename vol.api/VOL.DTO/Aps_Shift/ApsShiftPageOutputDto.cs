using System;

namespace VOL.DTO.Aps_Shift
{
    /// <summary>
    /// 排产班次分页查询返回 DTO
    /// </summary>
    public class ApsShiftPageOutputDto
    {
        /// <summary>
        /// 主键
        /// </summary>
        public long id { get; set; }

        /// <summary>
        /// 班次编码
        /// </summary>
        public string? shift_code { get; set; }

        /// <summary>
        /// 班次名称
        /// </summary>
        public string? shift_name { get; set; }

        /// <summary>
        /// 班次类型
        /// </summary>
        public sbyte shift_type { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public string? start_time { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public string? end_time { get; set; }

        /// <summary>
        /// 是否跨天
        /// </summary>
        public sbyte cross_day_flag { get; set; }

        /// <summary>
        /// 工作分钟数
        /// </summary>
        public int work_minutes { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public sbyte enable_flag { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? remark { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime create_time { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime update_time { get; set; }
    }
}
