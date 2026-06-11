namespace VOL.DTO.Aps_Schedule_Time
{
    public class ApsMachineScheduleTimeDetailOutputDto
    {
        public long Id { get; set; }

        public string? ScheduleDate { get; set; }

        public string? MachineCode { get; set; }

        public string? MachineName { get; set; }

        public long ShiftId { get; set; }

        public string? ShiftCode { get; set; }

        public string? ShiftName { get; set; }

        public string? StartDateTime { get; set; }

        public string? EndDateTime { get; set; }

        public int AvailableMinutes { get; set; }

        public int UsedMinutes { get; set; }

        public int RemainMinutes { get; set; }

        public int Status { get; set; }

        public bool IsRest { get; set; }

        public List<ApsMachineScheduleOrderInfoDto> OrderList { get; set; } = new();
    }

    public class ApsMachineScheduleOrderInfoDto
    {
        public Guid Id { get; set; }

        public Guid WorkOrderId { get; set; }

        public string? WorkOrderNo { get; set; }

        public string? MachineCode { get; set; }

        public string? MachineName { get; set; }

        public string? PlanStartTime { get; set; }

        public string? PlanEndTime { get; set; }

        public int PlanMinutes { get; set; }

        public decimal OrderQty { get; set; }

        public string? CustomerName { get; set; }

        public int? CustomerPriority { get; set; }

        public string? ScheduleStatus { get; set; }

        public string? Remark { get; set; }
    }
}
