using VOL.Core.Extensions;
using VOL.McpApi.Tools;

var builder = WebApplication.CreateBuilder(args);

// Development runs share the current WebApi configuration so the same database
// and JWT settings are used. Production configuration belongs to this host and
// is supplied through its appsettings files or environment variables.
if (builder.Environment.IsDevelopment())
{
    var webApiSettingsPath = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, "..", "VOL.WebApi", "appsettings.json"));
    if (File.Exists(webApiSettingsPath))
    {
        builder.Configuration.AddJsonFile(webApiSettingsPath, optional: false, reloadOnChange: false);
    }

    var webApiEnvironmentSettingsPath = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, "..", "VOL.WebApi", $"appsettings.{builder.Environment.EnvironmentName}.json"));
    if (File.Exists(webApiEnvironmentSettingsPath))
    {
        builder.Configuration.AddJsonFile(webApiEnvironmentSettingsPath, optional: false, reloadOnChange: false);
    }
}

// Load McpApi-specific settings after the shared settings so its host settings
// can be changed without affecting the existing REST API.
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddModule(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        // Keep Streamable HTTP at /mcp for current clients, and expose
        // /mcp/sse + /mcp/message for older MCP clients such as the
        // Dify MCP FunctionCalling plugin.
        options.Stateless = false;
        options.EnableLegacySse = true;
    })
    .WithTools<ApsSchedulingTools>()
    .WithTools<ApsMachineTools>();

var app = builder.Build();

app.UseStaticHttpContext();

app.MapMcp("/mcp");
app.Run();
