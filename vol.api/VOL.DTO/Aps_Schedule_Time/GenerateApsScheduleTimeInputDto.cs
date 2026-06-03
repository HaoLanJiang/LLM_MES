using System;

namespace VOL.DTO.Aps_Schedule_Time
{
    public class GenerateApsScheduleTimeInputDto
    {
        public DateTime? StartDate { get; set; }

        public int Days { get; set; } = 90;
    }
}
