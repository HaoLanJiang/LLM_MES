using System;
using System.Collections.Generic;

namespace VOL.DTO.Aps_ProductionScheduling
{
    public class RunProductionSchedulingInputDto
    {
        public string SortMode { get; set; } = "规则优先模式";

        public List<string> RuleSequence { get; set; } = new();

        public DateTime? StartDate { get; set; }

        /// <summary>排程窗口结束日期（含当天）；预览默认只看 14 天。</summary>
        public DateTime? EndDate { get; set; }

        public List<Guid> WorkOrderIds { get; set; } = new();
    }
}
