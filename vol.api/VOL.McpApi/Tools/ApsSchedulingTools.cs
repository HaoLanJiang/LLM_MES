using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using VOL.APS.IServices;
using VOL.DTO.Aps_Schedule_Result;

namespace VOL.McpApi.Tools;

/// <summary>
/// MCP adapters for the existing APS application services. These methods contain
/// no scheduling rules; all domain behavior remains in VOL.APS services.
/// </summary>
[McpServerToolType]
public sealed class ApsSchedulingTools
{
    [McpServerTool(
        Name = "aps_get_schedule_results",
        Title = "查询排产结果",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false)]
    [Description("分页查询当前排产结果，可按工单号、设备编码和排产状态筛选。")]
    public static string GetScheduleResults(
        IAps_Schedule_ResultService scheduleResultService,
        [Description("工单号；不传则不过滤")] string? workOrderNo = null,
        [Description("设备编码；不传则不过滤")] string? machineCode = null,
        [Description("排产状态；不传则不过滤")] string? scheduleStatus = null,
        [Description("页码，从 1 开始")] int page = 1,
        [Description("每页条数，最大 100")] int rows = 30)
    {
        var input = new ApsScheduleResultPageQueryInputDto
        {
            WorkOrderNo = workOrderNo,
            MachineCode = machineCode,
            ScheduleStatus = scheduleStatus,
            Page = Math.Max(page, 1),
            Rows = Math.Clamp(rows, 1, 100)
        };

        return JsonSerializer.Serialize(scheduleResultService.GetScheduleResultPageList(input));
    }

}
