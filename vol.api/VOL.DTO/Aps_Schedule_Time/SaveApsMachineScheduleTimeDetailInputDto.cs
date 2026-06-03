using System.Collections.Generic;

namespace VOL.DTO.Aps_Schedule_Time
{
    public class SaveApsMachineScheduleTimeDetailInputDto
    {
        public string? MachineCode { get; set; }

        public string? ScheduleDate { get; set; }

        public List<SaveApsMachineScheduleTimeDetailItemDto> Items { get; set; } = new();
    }

    public class SaveApsMachineScheduleTimeDetailItemDto
    {
        public long Id { get; set; }

        public long ShiftId { get; set; }

        public string? ShiftCode { get; set; }

        public string? StartDateTime { get; set; }

        public string? EndDateTime { get; set; }

        public bool IsRest { get; set; }
    }
}
