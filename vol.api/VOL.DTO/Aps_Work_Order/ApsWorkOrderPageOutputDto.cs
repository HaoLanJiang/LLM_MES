using System;

namespace VOL.DTO.Aps_Work_Order
{
    /// <summary>
    /// 排产工单分页查询返回 DTO
    /// </summary>
    public class ApsWorkOrderPageOutputDto
    {
        /// <summary>
        /// 工单主键
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 工单号
        /// </summary>
        public string? WorkOrderNo { get; set; }

        /// <summary>
        /// 客户名称
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// 客户优先级
        /// </summary>
        public int? CustomerPriority { get; set; }

        /// <summary>
        /// 产品编码
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// 产品名称
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// 订单数量
        /// </summary>
        public decimal OrderQty { get; set; }

        /// <summary>
        /// 最早开始时间
        /// </summary>
        public DateTime EarliestStartTime { get; set; }

        /// <summary>
        /// 最晚交付日期
        /// </summary>
        public DateTime LatestDeliveryDate { get; set; }

        /// <summary>
        /// 预计加工总分钟数
        /// </summary>
        public int ProcessMinutes { get; set; }

        /// <summary>
        /// 指定或适用设备
        /// </summary>
        public string? RequiredMachine { get; set; }

        /// <summary>
        /// 指定设备ID，多个使用逗号分隔
        /// </summary>
        public string? RequiredMachineId { get; set; }

        /// <summary>
        /// 换型分组
        /// </summary>
        public string? ChangeoverGroup { get; set; }

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
