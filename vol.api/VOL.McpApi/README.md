# VOL.McpApi

独立的 MCP HTTP 宿主。它直接调用 `VOL.APS`、`VOL.MES`、`VOL.Sys` 中已注册的服务，不通过 `VOL.WebApi` 的 REST Controller 转发。

## 本地运行

开发环境会读取相邻 `VOL.WebApi/appsettings.json`，以复用现有数据库和 JWT 配置：

```powershell
dotnet run --project VOL.McpApi
```

MCP Streamable HTTP 端点为 `http://localhost:9992/mcp`，所有请求都需要当前系统签发的 Bearer JWT。

## 部署配置

发布时不会读取相邻的 `VOL.WebApi` 配置文件。以 `appsettings.Production.template.json` 为模板创建受部署系统保护的 `appsettings.Production.json`，或通过环境变量配置当前项目所需的 `Connection`、`Secret`、`CreateMember`、`ModifyMember` 和 `SnowFlakeOptions` 节点；这些配置与现有 WebApi 保持一致即可。不要把真实密钥写入模板或提交到 Git。

## 初始工具

- `aps_get_schedule_results`：直接调用现有 `IAps_Schedule_ResultService`，只读查询排产结果。

当前不会暴露 `IProductionSchedulingService.RunProductionScheduling`。该服务会重建排产结果；在业务层具备“生成草案 → 用户确认 → 提交草案”和并发控制前，不应将其作为 MCP 写入工具公开。
