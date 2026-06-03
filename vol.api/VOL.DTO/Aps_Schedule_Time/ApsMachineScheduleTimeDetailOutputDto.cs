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
    }
}
