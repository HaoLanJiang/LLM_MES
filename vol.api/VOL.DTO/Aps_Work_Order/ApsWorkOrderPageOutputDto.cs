using System;

namespace VOL.DTO.Aps_Work_Order
{
    /// <summary>
    /// 鎺掍骇宸ュ崟鍒嗛〉鏌ヨ杩斿洖 DTO
    /// </summary>
    public class ApsWorkOrderPageOutputDto
    {
        /// <summary>
        /// 宸ュ崟涓婚敭
        /// </summary>
        public Guid Id { get; set; }

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
        /// 瀹㈡埛浼樺厛绾?
        /// </summary>
        public int? CustomerPriority { get; set; }

        /// <summary>
        /// 浜у搧缂栫爜
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// 浜у搧鍚嶇О
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// 璁㈠崟鏁伴噺
        /// </summary>
        public decimal OrderQty { get; set; }

        /// <summary>
        /// 鏈€鏃╁紑濮嬫椂闂?
        /// </summary>
        public DateTime EarliestStartTime { get; set; }

        /// <summary>
        /// 鏈€鏅氫氦浠樻棩鏈?
        /// </summary>
        public DateTime LatestDeliveryDate { get; set; }

        /// <summary>
        /// 棰勮鍔犲伐鎬诲垎閽熸暟
        /// </summary>
        public int ProcessMinutes { get; set; }

        /// <summary>
        /// 鎸囧畾鎴栭€傜敤璁惧
        /// </summary>
        public string? RequiredMachine { get; set; }

        /// <summary>
        /// 鎸囧畾璁惧ID锛屽涓娇鐢ㄩ€楀彿鍒嗛殧
        /// </summary>
        public string? RequiredMachineId { get; set; }

        /// <summary>
        /// 鎹㈠瀷鍒嗙粍
        /// </summary>
        public string? ChangeoverGroup { get; set; }

        /// <summary>
        /// 鎺掍骇鐘舵€?
        /// </summary>
        public string? ScheduleStatus { get; set; }

        /// <summary>
        /// 澶囨敞
        /// </summary>
        public string? Remark { get; set; }
    }
}
