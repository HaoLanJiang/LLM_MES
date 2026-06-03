namespace VOL.DTO.Aps_Schedule_Time
{
    public class ApsMachineScheduleCalendarOutputDto
    {
        public string? Date { get; set; }

        public int AvailableMinutes { get; set; }

        public int UsedMinutes { get; set; }

        public int RemainMinutes { get; set; }

        public int Status { get; set; }

        public string? ShiftNames { get; set; }

        public string? TimeRangeText { get; set; }

        public string? DisplayText { get; set; }
    }
}
