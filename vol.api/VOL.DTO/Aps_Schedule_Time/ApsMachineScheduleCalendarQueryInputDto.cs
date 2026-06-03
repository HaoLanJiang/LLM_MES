using System;

namespace VOL.DTO.Aps_Schedule_Time
{
    public class ApsMachineScheduleCalendarQueryInputDto
    {
        public string? MachineCode { get; set; }

        public string? MachineName { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }
    }
}
