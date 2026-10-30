# AI 排程助手

前端页面已迁移到 Vue 项目，源码位于 src/views/aps/AiProductionScheduling.vue。启动 vol.web 和 VOL.WebApi 后，从排产结果页点击“AI 排程助手”进入；也可以访问 /#/AiProductionScheduling。

前端通过系统已有的 HTTP 封装自动携带登录令牌，不需要手动粘贴 JWT。

## 配置自然语言解析

在 WebApi 进程环境中设置：

- APS_SCHEDULING_AI_API_KEY：OpenAI API key
- APS_SCHEDULING_AI_MODEL：可选，默认 gpt-4o-mini
- APS_SCHEDULING_AI_ENDPOINT：可选，默认 https://api.openai.com/v1/responses

自然语言接口只解析排程范围和规则，再由服务端查询真实工单。AI 不会生成数据库 ID、SQL 或排程时间。没有配置 AI API key 时，页面仍可手动填写排程范围并调用只读预览接口。

## 接口

POST /api/Aps_Schedule_Result/InterpretProductionScheduling 将自然语言转换为可检查的结构化条件，并返回真实匹配工单。

POST /api/Aps_Schedule_Result/PreviewProductionScheduling 根据用户确认后的排程条件计算草案。此接口不保存排程结果。

示例请求：

{
  "sortMode": "规则优先模式",
  "ruleSequence": ["交期优先", "同组连续生产优先"],
  "startDate": "2026-10-29",
  "endDate": "2026-11-11",
  "workOrderIds": []
}

workOrderIds 为空时，预览所有符合条件的工单。预览日期范围最多为 90 天。
