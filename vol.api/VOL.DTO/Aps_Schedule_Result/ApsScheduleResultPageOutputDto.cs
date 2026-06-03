using System;

namespace VOL.DTO.Aps_Schedule_Result
{
    /// <summary>
    /// 排产结果分页查询返回 DTO
    /// </summary>
    public class ApsScheduleResultPageOutputDto
    {
        /// <summary>
        /// 主键
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 工单ID
        /// </summary>
        public Guid WorkOrderId { get; set; }

        /// <summary>
        /// 工单号
        /// </summary>
        public string? WorkOrderNo { get; set; }

        /// <summary>
        /// 设备编码
        /// </summary>
        public string? MachineCode { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string? MachineName { get; set; }

        /// <summary>
        /// 计划开始时间
        /// </summary>
        public DateTime PlanStartTime { get; set; }

        /// <summary>
        /// 计划结束时间
        /// </summary>
        public DateTime PlanEndTime { get; set; }

        /// <summary>
        /// 计划生产分钟数
        /// </summary>
        public int PlanMinutes { get; set; }

        /// <summary>
        /// 订单数量
        /// </summary>
        public decimal OrderQty { get; set; }

        /// <summary>
        /// 客户名称
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// 客户优先级
        /// </summary>
        public int? CustomerPriority { get; set; }

        /// <summary>
        /// 最早开始时间
        /// </summary>
        public DateTime? EarliestStartTime { get; set; }

        /// <summary>
        /// 最晚交付日期
        /// </summary>
        public DateTime? LatestDeliveryDate { get; set; }

        /// <summary>
        /// 是否延期
        /// </summary>
        public sbyte? IsDelay { get; set; }

        /// <summary>
        /// 延期分钟数
        /// </summary>
        public int? DelayMinutes { get; set; }

        /// <summary>
        /// 排产状态
        /// </summary>
        public string? ScheduleStatus { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}
