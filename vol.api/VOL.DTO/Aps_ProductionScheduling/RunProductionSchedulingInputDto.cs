using System;
using System.Collections.Generic;

namespace VOL.DTO.Aps_ProductionScheduling
{
    public class RunProductionSchedulingInputDto
    {
        public string SortMode { get; set; } = "规则优先模式";

        public List<string> RuleSequence { get; set; } = new();

        public DateTime? StartDate { get; set; }

        public List<Guid> WorkOrderIds { get; set; } = new();
    }
}
