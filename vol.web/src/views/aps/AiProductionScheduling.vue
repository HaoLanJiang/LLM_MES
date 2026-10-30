<template>
    <div class="ai-scheduling-page">
        <section class="hero">
            <div>
                <div class="eyebrow"><el-icon><MagicStick /></el-icon> AI + APS</div>
                <h1>AI 排程助手</h1>
                <p>用自然语言描述目标，确认工单和优先规则，再生成可检查的排程草案。</p>
            </div>
            <el-tag class="readonly-tag" effect="dark" round>只读预览 · 不写入排程结果</el-tag>
        </section>

        <el-row :gutter="16">
            <el-col :xs="24" :lg="9">
                <el-card shadow="never" class="input-card">
                    <template #header>
                        <div class="section-heading">
                            <span class="step-number">1</span>
                            <div><strong>描述排程要求</strong><small>AI 提取范围和规则，工单由系统实际查询</small></div>
                        </div>
                    </template>

                    <el-input
                        v-model="requestText"
                        type="textarea"
                        :rows="4"
                        maxlength="2000"
                        show-word-limit
                        resize="vertical"
                        placeholder="例如：把工单 WO-20261029-001 从明天开始排，优先交期，尽量减少清厂"
                        @keyup.ctrl.enter="interpretRequest" />
                    <div class="example-row">
                        <span>试试：</span>
                        <el-button link type="primary" @click="applyExample">指定工单并优先交期</el-button>
                    </div>
                    <el-button type="primary" :loading="interpretLoading" :disabled="!requestText.trim()" class="full-button" @click="interpretRequest">
                        <el-icon><MagicStick /></el-icon>解析要求
                    </el-button>
                    <el-alert v-if="interpretMessage" :title="interpretMessage" :type="interpretStatus" :closable="false" show-icon class="status-alert" />

                    <el-divider />

                    <div class="section-heading subsection-heading">
                        <span class="step-number muted">2</span>
                        <div><strong>确认排程范围</strong><small>可修改识别结果；工单留空表示按日期预览所有符合条件的工单</small></div>
                    </div>

                    <el-form :model="form" label-position="top" class="schedule-form">
                        <el-form-item label="工单范围">
                            <el-select v-model="form.workOrderIds" multiple filterable clearable collapse-tags collapse-tags-tooltip placeholder="解析后选择工单；留空表示全部符合条件的工单" style="width: 100%">
                                <el-option v-for="order in resolvedOrders" :key="getField(order, 'id', 'Id')" :label="getField(order, 'workOrderNo', 'WorkOrderNo')" :value="getField(order, 'id', 'Id')">
                                    <div class="order-option">
                                        <span>{{ getField(order, 'workOrderNo', 'WorkOrderNo') }}</span>
                                        <small>{{ getField(order, 'customerName', 'CustomerName') || '未填写客户' }}</small>
                                    </div>
                                </el-option>
                            </el-select>
                        </el-form-item>
                        <el-row :gutter="12">
                            <el-col :span="12">
                                <el-form-item label="开始日期">
                                    <el-date-picker v-model="form.startDate" type="date" value-format="YYYY-MM-DD" placeholder="开始日期" style="width: 100%" />
                                </el-form-item>
                            </el-col>
                            <el-col :span="12">
                                <el-form-item label="结束日期">
                                    <el-date-picker v-model="form.endDate" type="date" value-format="YYYY-MM-DD" placeholder="结束日期" style="width: 100%" />
                                </el-form-item>
                            </el-col>
                        </el-row>
                        <el-form-item label="排序模式">
                            <el-radio-group v-model="form.sortMode">
                                <el-radio-button label="规则优先模式" />
                                <el-radio-button label="综合评分模式" />
                            </el-radio-group>
                        </el-form-item>
                        <el-form-item v-if="form.sortMode === '规则优先模式'" label="优先规则（按选择顺序）">
                            <el-select v-model="form.ruleSequence" multiple clearable :multiple-limit="3" placeholder="选择一个或多个规则" style="width: 100%">
                                <el-option v-for="rule in ruleOptions" :key="rule" :label="rule" :value="rule" />
                            </el-select>
                        </el-form-item>
                        <el-alert v-else title="综合评分模式沿用当前排程引擎的评分规则。" type="info" :closable="false" show-icon />
                    </el-form>

                    <el-button type="success" :loading="previewLoading" class="full-button preview-button" @click="previewSchedule">
                        <el-icon><DataAnalysis /></el-icon>生成排程预览
                    </el-button>
                    <p class="privacy-note">预览只在服务端临时计算，不会保存或覆盖现有排程。</p>
                </el-card>
            </el-col>

            <el-col :xs="24" :lg="15">
                <el-card shadow="never" class="preview-card">
                    <template #header>
                        <div class="section-heading">
                            <span class="step-number">3</span>
                            <div><strong>检查排程草案</strong><small>对比影响指标并查看设备上的排程片段</small></div>
                        </div>
                    </template>

                    <div v-if="!previewData && !previewMessage" class="empty-state">
                        <div class="empty-icon"><el-icon><DataAnalysis /></el-icon></div>
                        <strong>排程草案会显示在这里</strong>
                        <p>解析要求并确认范围后，点击“生成排程预览”。</p>
                    </div>
                    <el-alert v-if="previewMessage" :title="previewMessage" :type="previewStatus" :closable="false" show-icon class="preview-alert" />

                    <template v-if="previewData">
                        <div class="metric-grid">
                            <div v-for="metric in metrics" :key="metric.label" class="metric-card">
                                <span>{{ metric.label }}</span><strong>{{ formatNumber(metric.value) }}</strong>
                            </div>
                        </div>
                        <div class="impact-panel">
                            <div class="impact-title"><el-icon><TrendCharts /></el-icon>本次预览影响</div>
                            <div class="impact-grid">
                                <div>
                                    <span>延期工单</span>
                                    <strong>{{ fieldOrZero(impact, 'beforeDelayedOrderCount', 'BeforeDelayedOrderCount') }} <el-icon><Right /></el-icon> {{ fieldOrZero(impact, 'afterDelayedOrderCount', 'AfterDelayedOrderCount') }}</strong>
                                </div>
                                <div>
                                    <span>清厂片段</span>
                                    <strong>{{ fieldOrZero(impact, 'beforeChangeoverCount', 'BeforeChangeoverCount') }} <el-icon><Right /></el-icon> {{ fieldOrZero(impact, 'afterChangeoverCount', 'AfterChangeoverCount') }}
                                        <el-tag size="small" :type="changeoverDelta > 0 ? 'danger' : 'success'">{{ changeoverDelta > 0 ? '+' : '' }}{{ changeoverDelta }}</el-tag>
                                    </strong>
                                </div>
                            </div>
                        </div>
                        <div class="segment-heading"><strong>排程片段</strong><el-tag type="info" effect="plain">{{ segments.length }} 条</el-tag></div>
                        <el-table :data="segments" border stripe height="390" empty-text="当前范围没有可生成的排程片段" class="segments-table">
                            <el-table-column label="工单号" min-width="150" show-overflow-tooltip>
                                <template #default="{ row }">{{ getField(row, 'workOrderNo', 'WorkOrderNo') || '—' }}</template>
                            </el-table-column>
                            <el-table-column label="设备" min-width="150" show-overflow-tooltip>
                                <template #default="{ row }">{{ getField(row, 'machineCode', 'MachineCode') }} · {{ getField(row, 'machineName', 'MachineName') }}</template>
                            </el-table-column>
                            <el-table-column label="开始时间" min-width="165">
                                <template #default="{ row }">{{ getField(row, 'planStartTime', 'PlanStartTime') }}</template>
                            </el-table-column>
                            <el-table-column label="结束时间" min-width="165">
                                <template #default="{ row }">{{ getField(row, 'planEndTime', 'PlanEndTime') }}</template>
                            </el-table-column>
                            <el-table-column label="数量" width="95" align="right">
                                <template #default="{ row }">{{ formatNumber(getField(row, 'orderQty', 'OrderQty')) }}</template>
                            </el-table-column>
                            <el-table-column label="状态" width="100" align="center">
                                <template #default="{ row }">
                                    <el-tag v-if="getField(row, 'scheduleStatus', 'ScheduleStatus') === '清厂'" type="warning" size="small">清厂</el-tag>
                                    <el-tag v-else-if="Number(getField(row, 'isDelay', 'IsDelay') || 0) > 0" type="danger" size="small">延期</el-tag>
                                    <span v-else>{{ getField(row, 'scheduleStatus', 'ScheduleStatus') || '排产' }}</span>
                                </template>
                            </el-table-column>
                        </el-table>
                        <div class="result-footnote">已排 {{ formatNumber(getField(summary, 'scheduledWorkOrderCount', 'ScheduledWorkOrderCount')) }} 张工单；预览结果不会写入数据库。</div>
                    </template>
                </el-card>
            </el-col>
        </el-row>
    </div>
</template>

<script setup>
import { computed, getCurrentInstance, reactive, ref } from 'vue'
import { DataAnalysis, MagicStick, Right, TrendCharts } from '@element-plus/icons-vue'

const { proxy } = getCurrentInstance()
const ruleOptions = ['客户优先级优先', '交期优先', '同组连续生产优先']
const dateText = (date) => {
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    return year + '-' + month + '-' + day
}
const today = new Date()
const endDate = new Date(today)
endDate.setDate(endDate.getDate() + 13)

const requestText = ref('')
const interpretLoading = ref(false)
const previewLoading = ref(false)
const interpretMessage = ref('')
const interpretStatus = ref('info')
const previewMessage = ref('')
const previewStatus = ref('info')
const resolvedOrders = ref([])
const previewData = ref(null)
const form = reactive({
    sortMode: '规则优先模式',
    ruleSequence: ['交期优先', '同组连续生产优先'],
    startDate: dateText(today),
    endDate: dateText(endDate),
    workOrderIds: []
})

const summary = computed(() => previewData.value?.summary || {})
const impact = computed(() => previewData.value?.impact || {})
const segments = computed(() => previewData.value?.segments || [])
const changeoverDelta = computed(() => Number(getField(impact.value, 'changeoverDelta', 'ChangeoverDelta') || 0))
const metrics = computed(() => [
    { label: '候选工单', value: getField(summary.value, 'candidateWorkOrderCount', 'CandidateWorkOrderCount') },
    { label: '排程片段', value: getField(summary.value, 'scheduledSegmentCount', 'ScheduledSegmentCount') },
    { label: '计划分钟', value: getField(summary.value, 'totalPlannedMinutes', 'TotalPlannedMinutes') },
    { label: '计划数量', value: getField(summary.value, 'totalPlannedQty', 'TotalPlannedQty') },
    { label: '完整排程', value: getField(summary.value, 'fullyScheduledOrderCount', 'FullyScheduledOrderCount') },
    { label: '未排工单', value: getField(summary.value, 'unscheduledOrderCount', 'UnscheduledOrderCount') }
])

function getField(object, camelName, pascalName) {
    return object?.[camelName] ?? object?.[pascalName]
}

function fieldOrZero(object, camelName, pascalName) {
    return getField(object, camelName, pascalName) ?? 0
}

function formatNumber(value) {
    const number = Number(value || 0)
    return Number.isFinite(number) ? number.toLocaleString('zh-CN', { maximumFractionDigits: 2 }) : '0'
}

function applyExample() {
    requestText.value = '将工单 WO-20261029-001 从明天开始排，优先交期并尽量减少清厂'
}

async function interpretRequest() {
    if (!requestText.value.trim()) return
    interpretLoading.value = true
    interpretMessage.value = ''
    try {
        const result = await proxy.http.post('api/Aps_Schedule_Result/InterpretProductionScheduling', { request: requestText.value.trim() })
        if (!result?.status) throw new Error(result?.message || '解析失败')

        const input = result.schedulingInput || result.SchedulingInput || {}
        const orders = result.resolvedOrders || result.ResolvedOrders || []
        resolvedOrders.value = orders
        form.sortMode = getField(input, 'sortMode', 'SortMode') || '规则优先模式'
        form.startDate = String(getField(input, 'startDate', 'StartDate') || form.startDate).slice(0, 10)
        form.endDate = String(getField(input, 'endDate', 'EndDate') || form.endDate).slice(0, 10)
        form.ruleSequence = form.sortMode === '规则优先模式' ? (getField(input, 'ruleSequence', 'RuleSequence') || []) : []
        form.workOrderIds = getField(input, 'workOrderIds', 'WorkOrderIds') || []
        previewData.value = null
        interpretStatus.value = 'success'
        interpretMessage.value = (result.message || '解析完成') + '：匹配到 ' + orders.length + ' 张工单，请确认后再生成预览。'
    } catch (error) {
        interpretStatus.value = 'error'
        interpretMessage.value = error?.message || error?.data?.message || '解析失败，请检查 AI 服务配置和网络连接。'
    } finally {
        interpretLoading.value = false
    }
}

async function previewSchedule() {
    if (!form.startDate || !form.endDate) {
        previewStatus.value = 'error'
        previewMessage.value = '请选择排程开始日期和结束日期。'
        return
    }
    if (form.startDate > form.endDate) {
        previewStatus.value = 'error'
        previewMessage.value = '结束日期不能早于开始日期。'
        return
    }

    previewLoading.value = true
    previewMessage.value = ''
    previewData.value = null
    try {
        const result = await proxy.http.post('api/Aps_Schedule_Result/PreviewProductionScheduling', {
            sortMode: form.sortMode,
            ruleSequence: form.sortMode === '规则优先模式' ? form.ruleSequence : [],
            startDate: form.startDate,
            endDate: form.endDate,
            workOrderIds: form.workOrderIds
        })
        if (!result?.status) throw new Error(result?.message || '预览失败')
        previewData.value = result.data || result.Data || {}
        previewStatus.value = 'success'
        previewMessage.value = (result.message || '预览草案已生成') + '。没有数据被修改。'
    } catch (error) {
        previewStatus.value = 'error'
        previewMessage.value = error?.message || error?.data?.message || '预览失败，请检查排程参数后重试。'
    } finally {
        previewLoading.value = false
    }
}
</script>

<style scoped lang="less">
.ai-scheduling-page {
    min-height: 100%;
    padding: 12px;
    background: #f3f6fb;
    box-sizing: border-box;
    color: #263445;
}
.hero {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 20px;
    margin-bottom: 16px;
    padding: 22px 26px;
    border: 1px solid #dce7f5;
    border-radius: 12px;
    background: linear-gradient(115deg, #f7faff 0%, #edf4ff 62%, #f7f4ff 100%);
}
.eyebrow {
    display: flex;
    align-items: center;
    gap: 6px;
    margin-bottom: 4px;
    color: #4569a8;
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.08em;
}
.hero h1 { margin: 0; color: #1e2e48; font-size: 24px; line-height: 1.3; }
.hero p { margin: 7px 0 0; color: #66758b; font-size: 13px; }
.readonly-tag { flex-shrink: 0; background: #3868bb; border-color: #3868bb; }
.input-card, .preview-card { height: 100%; border-color: #e2e8f0; border-radius: 10px; }
.section-heading { display: flex; align-items: center; gap: 10px; }
.section-heading strong { display: block; color: #2b3a4f; font-size: 15px; }
.section-heading small { display: block; margin-top: 3px; color: #8a96a8; font-size: 12px; font-weight: 400; }
.step-number {
    display: inline-flex;
    width: 27px;
    height: 27px;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    border-radius: 50%;
    background: #eaf1ff;
    color: #3768bb;
    font-size: 13px;
    font-weight: 700;
}
.step-number.muted { background: #eff2f6; color: #7e8998; }
.example-row { display: flex; align-items: center; gap: 4px; margin: 5px 0 14px; color: #9aa5b4; font-size: 12px; }
.full-button { width: 100%; height: 40px; gap: 6px; }
.preview-button { margin-top: 4px; }
.status-alert, .preview-alert { margin-top: 14px; }
.subsection-heading { margin-bottom: 14px; }
.schedule-form { margin-top: 4px; }
.schedule-form :deep(.el-form-item) { margin-bottom: 14px; }
.schedule-form :deep(.el-form-item__label) { padding-bottom: 5px; color: #5e6b7c; font-size: 12px; }
.order-option { display: flex; justify-content: space-between; gap: 12px; }
.order-option small { color: #9aa5b4; }
.privacy-note { margin: 10px 0 0; color: #8a96a8; font-size: 12px; text-align: center; }
.empty-state { display: flex; min-height: 380px; flex-direction: column; align-items: center; justify-content: center; color: #7e8998; text-align: center; }
.empty-icon { display: grid; width: 58px; height: 58px; margin-bottom: 14px; place-items: center; border-radius: 18px; background: #edf3ff; color: #4d75bc; font-size: 26px; }
.empty-state strong { color: #506075; font-size: 15px; }
.empty-state p { margin: 7px 0 0; font-size: 12px; }
.metric-grid { display: grid; grid-template-columns: repeat(3, minmax(100px, 1fr)); gap: 10px; }
.metric-card { min-height: 76px; padding: 12px 14px; border: 1px solid #e6ebf2; border-radius: 9px; background: #fbfcfe; }
.metric-card span, .impact-grid span { display: block; color: #8a96a8; font-size: 12px; }
.metric-card strong { display: block; margin-top: 5px; color: #2d466b; font-size: 21px; line-height: 1.2; }
.impact-panel { margin: 14px 0 18px; padding: 13px 15px; border: 1px solid #dce9fb; border-radius: 9px; background: #f5f9ff; }
.impact-title { display: flex; align-items: center; gap: 6px; margin-bottom: 11px; color: #4569a8; font-size: 13px; font-weight: 700; }
.impact-grid { display: grid; grid-template-columns: repeat(2, minmax(150px, 1fr)); gap: 12px; }
.impact-grid strong { display: flex; align-items: center; gap: 7px; margin-top: 5px; color: #364b69; font-size: 15px; }
.impact-grid strong .el-icon { color: #96a3b5; font-size: 12px; }
.segment-heading { display: flex; align-items: center; justify-content: space-between; margin-bottom: 10px; color: #35455d; font-size: 14px; }
.segments-table { width: 100%; }
.result-footnote { margin-top: 10px; color: #8995a5; font-size: 12px; text-align: right; }
@media (max-width: 991px) {
    .input-card { margin-bottom: 16px; }
}
@media (max-width: 640px) {
    .hero { align-items: flex-start; flex-direction: column; padding: 18px; }
    .metric-grid { grid-template-columns: repeat(2, minmax(100px, 1fr)); }
    .impact-grid { grid-template-columns: 1fr; }
    .hero h1 { font-size: 21px; }
}
</style>
