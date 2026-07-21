using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VOL.Core.Configuration;
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
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = AppSetting.Secret.Audience,
            ValidIssuer = AppSetting.Secret.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSetting.Secret.JWT))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .AddAuthorizationFilters()
    .WithTools<ApsSchedulingTools>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticHttpContext();

app.MapMcp("/mcp").RequireAuthorization();
app.Run();
