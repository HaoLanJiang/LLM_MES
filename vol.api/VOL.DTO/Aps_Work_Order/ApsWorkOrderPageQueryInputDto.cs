using VOL.DTO.BaseDTO;

namespace VOL.DTO.Aps_Work_Order
{
    /// <summary>
    /// 鎺掍骇宸ュ崟鍒嗛〉鏌ヨ鍏ュ弬
    /// </summary>
    public class ApsWorkOrderPageQueryInputDto : PageQueryBaseDto
    {
        /// <summary>
        /// 宸ュ崟鍙?
        /// </summary>
        public string? WorkOrderNo { get; set; }

        /// <summary>
        /// 瀹㈡埛鍚嶇О
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// 瀹㈡埛绛夌骇
        /// </summary>
        public string? CustomerLevel { get; set; }

        /// <summary>
        /// 鎺掍骇鐘舵€?
        /// </summary>
        public string? ScheduleStatus { get; set; }
    }
}
