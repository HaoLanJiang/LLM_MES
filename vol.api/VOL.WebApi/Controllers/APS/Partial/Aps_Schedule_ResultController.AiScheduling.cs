using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VOL.APS.IRepositories;
using VOL.APS.IServices;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.DTO.Aps_ProductionScheduling;
using VOL.Entity.DomainModels;

namespace VOL.APS.Controllers
{
    public partial class Aps_Schedule_ResultController
    {
        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("PreviewProductionScheduling")]
        public ActionResult PreviewProductionScheduling(
            [FromBody] RunProductionSchedulingInputDto input,
            [FromServices] IProductionSchedulingService productionSchedulingService)
        {
            return Json(productionSchedulingService.PreviewProductionScheduling(input));
        }

        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("InterpretProductionScheduling")]
        public async Task<ActionResult> InterpretProductionScheduling(
            [FromBody] InterpretProductionSchedulingInputDto input,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IConfiguration configuration,
            [FromServices] IAps_Work_OrderRepository workOrderRepository,
            CancellationToken cancellationToken)
        {
            string requestText = (input?.Request ?? string.Empty).Trim();
            if (requestText.Length == 0 || requestText.Length > 2000)
            {
                return Json(new { status = false, message = "请填写排程要求（最多 2000 字）" });
            }

            string apiKey = configuration["APS_SCHEDULING_AI_API_KEY"]
                ?? configuration["OpenAI:ApiKey"]
                ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Json(new { status = false, message = "未配置 APS_SCHEDULING_AI_API_KEY，当前无法解析自然语言；可直接调用预览接口提交结构化参数" });
            }

            string model = configuration["APS_SCHEDULING_AI_MODEL"] ?? "gpt-4o-mini";
            object requestBody = new
            {
                model,
                input = new object[]
                {
                    new
                    {
                        role = "system",
                        content = $"将排程要求提取为 JSON。今天是 {DateTime.Today:yyyy-MM-dd}。只提取工单号或客户名、开始日期、排程天数、排序模式和规则顺序；不猜测工单号，不发明工厂约束。没有提到的字段用 null 或空数组。可用规则：客户优先级优先、交期优先、同组连续生产优先。"
                    },
                    new { role = "user", content = requestText }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "aps_scheduling_intent",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                customerName = new { type = new[] { "string", "null" } },
                                workOrderNumbers = new { type = "array", items = new { type = "string" } },
                                startDate = new { type = new[] { "string", "null" } },
                                horizonDays = new { type = new[] { "integer", "null" } },
                                sortMode = new { type = "string", @enum = new[] { "规则优先模式", "综合评分模式" } },
                                ruleSequence = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "string",
                                        @enum = new[] { "客户优先级优先", "交期优先", "同组连续生产优先" }
                                    }
                                }
                            },
                            required = new[] { "customerName", "workOrderNumbers", "startDate", "horizonDays", "sortMode", "ruleSequence" },
                            additionalProperties = false
                        }
                    }
                }
            };

            string outputText;
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                using HttpResponseMessage response = await client.PostAsync(
                    configuration["APS_SCHEDULING_AI_ENDPOINT"] ?? "https://api.openai.com/v1/responses",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
                    cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { status = false, message = $"AI 服务请求失败（HTTP {(int)response.StatusCode}），请检查模型配置" });
                }

                using JsonDocument responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                outputText = ExtractResponseText(responseJson.RootElement);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Json(new { status = false, message = "AI 解析超时，请稍后重试" });
            }
            catch (Exception)
            {
                return Json(new { status = false, message = "AI 解析失败，请检查服务地址和网络" });
            }

            SchedulingIntent? intent;
            try
            {
                intent = JsonSerializer.Deserialize<SchedulingIntent>(outputText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return Json(new { status = false, message = "AI 返回内容无法解析，请重新描述工单范围" });
            }

            if (intent == null)
            {
                return Json(new { status = false, message = "AI 没有识别出排程要求" });
            }

            List<string> workOrderNumbers = (intent.WorkOrderNumbers ?? new List<string>())
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(101)
                .ToList();
            string customerName = (intent.CustomerName ?? string.Empty).Trim();
            if (workOrderNumbers.Count == 0 && customerName.Length == 0)
            {
                return Json(new { status = false, message = "请明确客户名称或工单号，我才能确定本次排程范围" });
            }
            if (workOrderNumbers.Count > 100)
            {
                return Json(new { status = false, message = "单次最多解析 100 个工单，请缩小范围" });
            }

            string completedStatus = ApsScheduleStatus.Completed.ToStorageValue();
            IQueryable<Aps_Work_Order> query = workOrderRepository.FindAsIQueryable(x => x.ScheduleStatus != completedStatus);
            if (workOrderNumbers.Count > 0)
            {
                query = query.Where(x => workOrderNumbers.Contains(x.WorkOrderNo));
            }
            else
            {
                query = query.Where(x => x.CustomerName == customerName);
            }

            List<Aps_Work_Order> resolvedOrders = query.ToList();
            if (customerName.Length > 0)
            {
                resolvedOrders = resolvedOrders
                    .Where(x => string.Equals((x.CustomerName ?? string.Empty).Trim(), customerName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            if (resolvedOrders.Count == 0)
            {
                return Json(new { status = false, message = "没有找到匹配的未完成工单，请检查客户名称或工单号" });
            }
            if (resolvedOrders.Count > 100)
            {
                return Json(new { status = false, message = $"匹配到 {resolvedOrders.Count} 张工单，请补充工单号或缩小客户范围" });
            }

            DateTime? startDate = null;
            if (!string.IsNullOrWhiteSpace(intent.StartDate))
            {
                if (!DateTime.TryParseExact(intent.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    return Json(new { status = false, message = "无法确认开始日期，请明确写出日期" });
                }
                startDate = parsedDate.Date;
            }

            int horizonDays = intent.HorizonDays ?? 14;
            if (horizonDays < 1 || horizonDays > 90)
            {
                return Json(new { status = false, message = "排程天数需在 1 到 90 天之间" });
            }

            List<string> allowedRules = new() { "客户优先级优先", "交期优先", "同组连续生产优先" };
            List<string> rules = (intent.RuleSequence ?? new List<string>())
                .Where(allowedRules.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            RunProductionSchedulingInputDto schedulingInput = new()
            {
                SortMode = intent.SortMode == "综合评分模式" ? "综合评分模式" : "规则优先模式",
                RuleSequence = rules,
                StartDate = startDate,
                EndDate = (startDate ?? DateTime.Today).AddDays(horizonDays - 1),
                WorkOrderIds = resolvedOrders.Select(x => x.Id).ToList()
            };

            return Json(new
            {
                status = true,
                message = "已将要求转换为排程条件，请检查识别的范围后生成预览",
                schedulingInput,
                resolvedOrders = resolvedOrders.Select(x => new
                {
                    x.Id,
                    x.WorkOrderNo,
                    x.CustomerName,
                    x.ProductName,
                    x.OrderQty,
                    x.LatestDeliveryDate,
                    x.ScheduleStatus
                }).ToList()
            });
        }

        private static string ExtractResponseText(JsonElement response)
        {
            if (response.TryGetProperty("output", out JsonElement output) && output.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in output.EnumerateArray())
                {
                    if (!item.TryGetProperty("content", out JsonElement content) || content.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }
                    foreach (JsonElement part in content.EnumerateArray())
                    {
                        if (part.TryGetProperty("type", out JsonElement type)
                            && type.GetString() == "output_text"
                            && part.TryGetProperty("text", out JsonElement text))
                        {
                            return text.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            throw new JsonException("Response text not found");
        }

        private sealed class SchedulingIntent
        {
            public string? CustomerName { get; set; }
            public List<string> WorkOrderNumbers { get; set; } = new();
            public string? StartDate { get; set; }
            public int? HorizonDays { get; set; }
            public string? SortMode { get; set; }
            public List<string> RuleSequence { get; set; } = new();
        }
    }
}
