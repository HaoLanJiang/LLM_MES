# VOL.McpApi

独立的 MCP HTTP 宿主。它直接调用 `VOL.APS`、`VOL.MES`、`VOL.Sys` 中已注册的服务，不通过 `VOL.WebApi` 的 REST Controller 转发。

## 本地运行

开发环境会读取相邻 `VOL.WebApi/appsettings.json`，以复用现有数据库和 JWT 配置：

```powershell
dotnet run --project VOL.McpApi
```

MCP Streamable HTTP 端点为 `http://localhost:9992/mcp`。当前开发配置不执行 JWT 验证；请仅在可信内网使用，或通过反向代理/IP 白名单限制访问来源。

## 传输兼容性

- 新版 MCP 客户端使用 Streamable HTTP：`/mcp`。
- 旧版 SSE MCP 客户端使用：`/mcp/sse`（服务端会在 SSE 事件中返回对应的 `/mcp/message?...` 请求地址）。Dify 的 `MCP FunctionCalling` 插件应配置此地址。

## 部署配置

发布时不会读取相邻的 `VOL.WebApi` 配置文件。以 `appsettings.Production.template.json` 为模板创建受部署系统保护的 `appsettings.Production.json`，或通过环境变量配置当前项目所需的 `Connection`、`Secret`、`CreateMember`、`ModifyMember` 和 `SnowFlakeOptions` 节点；这些配置与现有 WebApi 保持一致即可。不要把真实密钥写入模板或提交到 Git。

## 初始工具

- `aps_get_schedule_results`：直接调用现有 `IAps_Schedule_ResultService`，只读查询排产结果。
- `aps_list_machines`：直接调用现有 `IAps_MachineService`，只读查询设备编码、名称、每日产能和设备清单。

当前不会暴露 `IProductionSchedulingService.RunProductionScheduling`。该服务会重建排产结果；在业务层具备“生成草案 → 用户确认 → 提交草案”和并发控制前，不应将其作为 MCP 写入工具公开。
