using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using VOL.APS.IServices;
using VOL.DTO.Aps_Machine;

namespace VOL.McpApi.Tools;

/// <summary>
/// Read-only MCP tools for APS equipment information.
/// </summary>
[McpServerToolType]
public sealed class ApsMachineTools
{
    [McpServerTool(
        Name = "aps_list_machines",
        Title = "查询 APS 设备",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false)]
    [Description("查询 APS 设备主数据。适用于询问设备是否存在、设备名称、每日产能或设备清单；不要用于查询具体工单的排产结果。")]
    public static string ListMachines(
        IAps_MachineService machineService,
        [Description("设备编码，可使用完整或部分编码；不传则查询全部设备。")] string? machineCode = null,
        [Description("设备名称，可使用完整或部分名称；不传则不按名称筛选。")] string? machineName = null,
        [Description("页码，从 1 开始。")] int page = 1,
        [Description("每页条数，最大 100。")] int rows = 30)
    {
        var input = new ApsMachinePageQueryInputDto
        {
            MachineCode = machineCode,
            MachineName = machineName,
            Page = Math.Max(page, 1),
            Rows = Math.Clamp(rows, 1, 100)
        };

        return JsonSerializer.Serialize(machineService.GetMachinePageList(input));
    }
}
