<template>
    <div class="production-schedule-calendar">
        <el-card shadow="never" class="query-card">
            <template #header>
                <div class="card-title">查询条件</div>
            </template>

            <el-form :model="queryForm" inline label-width="100px">
                <el-form-item label="设备编号">
                    <el-input
                        v-model="queryForm.machineCode"
                        clearable
                        placeholder="请输入设备编号"
                        @keyup.enter="handleSearch"
                    />
                </el-form-item>

                <el-form-item label="设备名称">
                    <el-input
                        v-model="queryForm.machineName"
                        clearable
                        placeholder="请输入设备名称"
                        @keyup.enter="handleSearch"
                    />
                </el-form-item>

                <el-form-item>
                    <el-button type="primary" @click="handleSearch">查询</el-button>
                    <el-button @click="handleReset">重置</el-button>
                </el-form-item>
            </el-form>
        </el-card>

        <el-card shadow="never" class="content-card">
            <template #header>
                <div class="content-header">
                    <div class="card-title">
                        {{ isDetailMode ? '设备生产日历' : '设备列表' }}
                    </div>

                    <div v-if="isDetailMode" class="detail-toolbar">
                        <div class="toolbar-device">
                            <span class="device-name">{{ currentMachine.MachineName || currentMachine.MachineCode }}</span>
                            <span class="device-code">{{ currentMachine.MachineCode }}</span>
                        </div>

                        <div class="toolbar-actions">
                            <el-date-picker
                                v-model="monthValue"
                                type="month"
                                value-format="YYYY-MM"
                                placeholder="请选择月份"
                                @change="handleMonthChange"
                            />
                            <el-button @click="goToPrevMonth">上个月</el-button>
                            <el-button @click="goToCurrentMonth">本月</el-button>
                            <el-button @click="goToNextMonth">下个月</el-button>
                            <el-button @click="backToList">返回列表</el-button>
                        </div>
                    </div>
                </div>
            </template>

            <div v-if="!isDetailMode" v-loading="listLoading" class="machine-list-wrap">
                <div v-if="machineList.length" class="machine-grid">
                    <div
                        v-for="item in machineList"
                        :key="item.Id || item.MachineCode"
                        class="machine-card"
                        @click="openMachineCalendar(item)"
                    >
                        <div class="machine-card-top">
                            <div class="machine-title">
                                <div class="machine-name">{{ item.MachineName || '-' }}</div>
                                <div class="machine-code">{{ item.MachineCode || '-' }}</div>
                            </div>
                            <el-tag type="primary">查看日历</el-tag>
                        </div>

                        <div class="machine-card-body">
                            <div class="machine-info-item">
                                <span class="label">日产能</span>
                                <span class="value">{{ formatMinutes(item.CapacityMinutesPerDay) }}</span>
                            </div>
                            <div class="machine-info-item">
                                <span class="label">备注</span>
                                <span class="value remark">{{ item.Remark || '暂无' }}</span>
                            </div>
                        </div>
                    </div>
                </div>

                <el-empty v-else description="暂无设备数据" />
            </div>

            <div v-else v-loading="calendarLoading" class="calendar-wrap">
                <el-calendar v-model="calendarValue">
                    <template #header="{ date }">
                        <div class="calendar-header">
                            <div class="calendar-title">
                                <span>{{ date }}</span>
                                <span class="calendar-subtitle">每天显示该设备的生产时间，无数据则显示“休”</span>
                                <span class="calendar-subtitle">双击可编辑内容</span>
                            </div>
                        </div>
                    </template>

                    <template #date-cell="{ data }">
                        <div
                            class="calendar-cell"
                            :class="{
                                'is-rest': isRestDay(data.day),
                                'has-part-rest': hasPartRestDay(data.day)
                            }"
                            @dblclick="openDayEditDialog(data.day)"
                        >
                            <div class="cell-top">
                                <div class="cell-day">{{ getDayNumber(data.day) }}</div>
                                <div v-if="getRestBadgeText(data.day)" class="cell-badge">
                                    {{ getRestBadgeText(data.day) }}
                                </div>
                            </div>
                            <div class="cell-main">
                                {{ getDisplayText(data.day) }}
                            </div>
                            <div class="cell-extra" :title="getCellTooltip(data.day)">
                                {{ getCellExtra(data.day) }}
                            </div>
                        </div>
                    </template>
                </el-calendar>
            </div>
        </el-card>

        <el-dialog v-model="dayDialogVisible" title="班次时间调整" width="920px" destroy-on-close class="schedule-edit-dialog">
            <div v-loading="dayDialogLoading" class="day-dialog">
                <div class="day-dialog-header">
                    <div class="day-dialog-device">
                        <div class="day-dialog-eyebrow">设备排产调整</div>
                        <div class="day-dialog-title">{{ currentMachine.MachineName || currentMachine.MachineCode }}</div>
                        <div class="day-dialog-code">{{ currentMachine.MachineCode }}</div>
                    </div>

                    <div class="day-dialog-date-wrap">
                        <div class="day-dialog-date-label">当前日期</div>
                        <el-tag type="success" size="large" effect="dark">{{ selectedScheduleDate }}</el-tag>
                    </div>
                </div>

                <el-empty v-if="!dayShiftList.length && !dayDialogLoading" description="当天没有可编辑的班次数据" />

                <div v-else class="shift-edit-list">
                    <div v-for="item in dayShiftList" :key="item.Id" class="shift-edit-card" :class="{ 'is-rest-card': item.IsRest }">
                        <div class="shift-edit-top">
                            <div class="shift-edit-name-wrap">
                                <div class="shift-edit-icon">{{ (item.ShiftName || item.ShiftCode || '班').slice(0, 1) }}</div>
                                <div class="shift-edit-name">
                                    <span>{{ item.ShiftName || item.ShiftCode }}</span>
                                    <span class="shift-edit-code">{{ item.ShiftCode }}</span>
                                </div>
                            </div>

                            <div class="shift-edit-status">
                                <el-tag :type="item.IsRest ? 'info' : 'primary'" effect="plain">
                                    {{ item.IsRest ? '休息中' : '生产中' }}
                                </el-tag>
                                <el-switch v-model="item.IsRest" active-text="休息" inactive-text="开工" />
                            </div>
                        </div>

                        <div class="shift-edit-body" :class="{ 'is-disabled-body': item.IsRest }">
                            <el-row :gutter="16">
                                <el-col :span="12">
                                    <div class="edit-label">开始时间</div>
                                    <el-date-picker
                                        v-model="item.StartDateTime"
                                        type="datetime"
                                        value-format="YYYY-MM-DD HH:mm:ss"
                                        format="YYYY-MM-DD HH:mm:ss"
                                        placeholder="请选择开始时间"
                                        style="width: 100%"
                                    />
                                </el-col>
                                <el-col :span="12">
                                    <div class="edit-label">结束时间</div>
                                    <el-date-picker
                                        v-model="item.EndDateTime"
                                        type="datetime"
                                        value-format="YYYY-MM-DD HH:mm:ss"
                                        format="YYYY-MM-DD HH:mm:ss"
                                        placeholder="请选择结束时间"
                                        style="width: 100%"
                                    />
                                </el-col>
                            </el-row>

                            <div class="shift-edit-time-preview">
                                <div class="time-preview-label">时间预览</div>
                                <div class="time-preview-value">
                                    {{ item.StartDateTime || '--' }} 至 {{ item.EndDateTime || '--' }}
                                </div>
                            </div>
                        </div>

                        <el-row :gutter="16" class="shift-edit-metrics">
                            <el-col :span="12">
                                <div class="metric-box">
                                    <div class="metric-label">已用分钟</div>
                                    <div class="metric-value">{{ item.UsedMinutes || 0 }}</div>
                                </div>
                            </el-col>
                            <el-col :span="12">
                                <div class="metric-box">
                                    <div class="metric-label">可用分钟</div>
                                    <div class="metric-value">{{ item.AvailableMinutes || 0 }}</div>
                                </div>
                            </el-col>
                        </el-row>

                        <div class="shift-edit-tip">
                            跨天班次请直接选择真实日期时间，例如 2026-06-03 20:00:00 到 2026-06-04 08:00:00。
                        </div>
                    </div>
                </div>
            </div>

            <template #footer>
                <el-button @click="dayDialogVisible = false">取消</el-button>
                <el-button type="primary" :loading="dayDialogSubmitLoading" @click="saveDayScheduleTime">保存</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
import { computed, getCurrentInstance, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const { proxy } = getCurrentInstance()
const route = useRoute()
const router = useRouter()

const listLoading = ref(false)
const calendarLoading = ref(false)
const machineList = ref([])
const calendarRows = ref([])
const calendarMap = ref({})
const calendarValue = ref(new Date())
const monthValue = ref('')
const dayDialogVisible = ref(false)
const dayDialogLoading = ref(false)
const dayDialogSubmitLoading = ref(false)
const selectedScheduleDate = ref('')
const dayShiftList = ref([])

const queryForm = reactive({
    machineCode: '',
    machineName: ''
})

const currentMachine = reactive({
    MachineCode: '',
    MachineName: ''
})

const isDetailMode = computed(() => !!currentMachine.MachineCode)

const getMonthKey = (date) => {
    if (!(date instanceof Date) || Number.isNaN(date.getTime())) {
        return ''
    }

    return `${date.getFullYear()}-${date.getMonth() + 1}`
}

const formatDate = (date) => {
    const year = date.getFullYear()
    const month = `${date.getMonth() + 1}`.padStart(2, '0')
    const day = `${date.getDate()}`.padStart(2, '0')
    return `${year}-${month}-${day}`
}

const syncMonthValue = () => {
    monthValue.value = `${calendarValue.value.getFullYear()}-${`${calendarValue.value.getMonth() + 1}`.padStart(2, '0')}`
}

const buildCalendarMap = (rows) => {
    const map = {}
    ;(rows || []).forEach((item) => {
        if (item?.Date) {
            map[item.Date] = item
        }
    })
    calendarMap.value = map
}

const loadMachineList = async () => {
    listLoading.value = true
    try {
        const result = await proxy.http.post('api/Aps_Machine/GetMachinePageList', {
            Page: 1,
            Rows: 999,
            Sort: 'MachineCode',
            Order: 'asc',
            MachineCode: queryForm.machineCode,
            MachineName: queryForm.machineName
        })

        machineList.value = result?.rows || []
    } finally {
        listLoading.value = false
    }
}

const loadCalendarData = async () => {
    if (!currentMachine.MachineCode) {
        return
    }

    calendarLoading.value = true
    try {
        const result = await proxy.http.post('api/Aps_Schedule_Time/GetMachineScheduleCalendar', {
            MachineCode: currentMachine.MachineCode,
            MachineName: currentMachine.MachineName,
            Year: calendarValue.value.getFullYear(),
            Month: calendarValue.value.getMonth() + 1
        })

        calendarRows.value = result?.rows || []
        buildCalendarMap(calendarRows.value)
    } finally {
        calendarLoading.value = false
    }
}

const handleSearch = () => {
    backToList(false)
    loadMachineList()
}

const handleReset = () => {
    queryForm.machineCode = ''
    queryForm.machineName = ''
    backToList(false)
    loadMachineList()
}

const openMachineCalendar = async (item) => {
    currentMachine.MachineCode = item.MachineCode || ''
    currentMachine.MachineName = item.MachineName || ''
    calendarValue.value = new Date()
    syncMonthValue()

    await router.push({
        path: route.path,
        query: {
            machineCode: currentMachine.MachineCode,
            machineName: currentMachine.MachineName
        }
    })

    loadCalendarData()
}

const openDayEditDialog = async (day) => {
    if (!isDetailMode.value || !day) {
        return
    }

    selectedScheduleDate.value = day
    dayDialogVisible.value = true
    dayDialogLoading.value = true
    try {
        const result = await proxy.http.post('api/Aps_Schedule_Time/GetMachineScheduleTimeDetail', {
            MachineCode: currentMachine.MachineCode,
            ScheduleDate: day
        })

        dayShiftList.value = result?.rows || []
    } finally {
        dayDialogLoading.value = false
    }
}

const saveDayScheduleTime = async () => {
    if (!selectedScheduleDate.value) {
        return
    }

    if (!dayShiftList.value.length) {
        return proxy.$message.error('当天没有可保存的班次数据')
    }

    const invalidItem = dayShiftList.value.find((item) => !item.StartDateTime || !item.EndDateTime)
    if (invalidItem) {
        return proxy.$message.error(`请完整填写班次【${invalidItem.ShiftName || invalidItem.ShiftCode}】的开始和结束时间`)
    }

    dayDialogSubmitLoading.value = true
    try {
        const result = await proxy.http.post('api/Aps_Schedule_Time/SaveMachineScheduleTimeDetail', {
            MachineCode: currentMachine.MachineCode,
            ScheduleDate: selectedScheduleDate.value,
            Items: dayShiftList.value.map((item) => ({
                Id: item.Id,
                ShiftId: item.ShiftId,
                ShiftCode: item.ShiftCode,
                StartDateTime: item.StartDateTime,
                EndDateTime: item.EndDateTime,
                IsRest: !!item.IsRest
            }))
        })

        if (!result?.status) {
            return proxy.$message.error(result?.message || '保存失败')
        }

        proxy.$message.success(result.message || '保存成功')
        dayDialogVisible.value = false
        await loadCalendarData()
    } finally {
        dayDialogSubmitLoading.value = false
    }
}

const backToList = async (clearRoute = true) => {
    currentMachine.MachineCode = ''
    currentMachine.MachineName = ''
    calendarRows.value = []
    calendarMap.value = {}

    if (clearRoute) {
        await router.push({
            path: route.path,
            query: {}
        })
    }
}

const handleMonthChange = () => {
    if (!monthValue.value) {
        syncMonthValue()
        return
    }

    calendarValue.value = new Date(`${monthValue.value}-01`)
}

const goToPrevMonth = () => {
    calendarValue.value = new Date(calendarValue.value.getFullYear(), calendarValue.value.getMonth() - 1, 1)
}

const goToCurrentMonth = () => {
    calendarValue.value = new Date()
}

const goToNextMonth = () => {
    calendarValue.value = new Date(calendarValue.value.getFullYear(), calendarValue.value.getMonth() + 1, 1)
}

const getCalendarItem = (day) => {
    return calendarMap.value[day]
}

const getDisplayText = (day) => {
    const item = getCalendarItem(day)
    if (!item || item.DisplayText === 'REST') {
        return '休'
    }

    return `${item.AvailableMinutes || 0} 分钟`
}

const getCellExtra = (day) => {
    const item = getCalendarItem(day)
    if (!item) {
        return '无排产数据'
    }

    if (item.RestShiftNames && item.TimeRangeText) {
        return `${item.TimeRangeText} / ${item.RestShiftNames}休息`
    }

    if (item.RestShiftNames) {
        return `${item.RestShiftNames}休息`
    }

    return item.TimeRangeText || item.ShiftNames || '已排产'
}

const getCellTooltip = (day) => {
    const item = getCalendarItem(day)
    if (!item) {
        return '休'
    }

    const texts = [
        `生产时间：${item.DisplayText === 'REST' ? '休' : `${item.AvailableMinutes || 0} 分钟`}`,
        item.TimeRangeText ? `时间段：${item.TimeRangeText}` : '',
        item.ShiftNames ? `班次：${item.ShiftNames}` : '',
        item.RestShiftNames ? `休息班次：${item.RestShiftNames}` : '',
        `已用/剩余：${item.UsedMinutes || 0}/${item.RemainMinutes || 0} 分钟`
    ].filter(Boolean)

    return texts.join('\n')
}

const isRestDay = (day) => {
    return !getCalendarItem(day) || getCalendarItem(day)?.DisplayText === 'REST'
}

const hasPartRestDay = (day) => {
    const item = getCalendarItem(day)
    return !!(item && item.DisplayText !== 'REST' && item.RestShiftNames)
}

const getRestBadgeText = (day) => {
    const item = getCalendarItem(day)
    if (!item) {
        return ''
    }

    if (item.DisplayText === 'REST') {
        return '全天休息'
    }

    if (item.RestShiftNames) {
        return '部分休息'
    }

    return ''
}

const getDayNumber = (day) => {
    return Number((day || '').split('-').pop())
}

const formatMinutes = (minutes) => {
    if (minutes === null || minutes === undefined) {
        return '-'
    }

    return `${minutes} 分钟`
}

const restoreDetailFromRoute = async () => {
    const machineCode = route.query.machineCode ? String(route.query.machineCode) : ''
    const machineName = route.query.machineName ? String(route.query.machineName) : ''

    if (!machineCode) {
        return
    }

    currentMachine.MachineCode = machineCode
    currentMachine.MachineName = machineName
    syncMonthValue()
    await loadCalendarData()
}

onMounted(async () => {
    syncMonthValue()
    await loadMachineList()
    await restoreDetailFromRoute()
})

watch(
    calendarValue,
    async (newValue, oldValue) => {
        syncMonthValue()

        if (!isDetailMode.value) {
            return
        }

        if (getMonthKey(newValue) === getMonthKey(oldValue)) {
            return
        }

        await loadCalendarData()
    },
    { flush: 'post' }
)
</script>

<style scoped lang="less">
.production-schedule-calendar {
    padding: 8px;
    background: #f5f7fa;
    min-height: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    box-sizing: border-box;
}

.query-card {
    margin-bottom: 8px;
    flex-shrink: 0;
}

.content-card {
    flex: 1;
    min-height: 0;

    :deep(.el-card__body) {
        height: 100%;
        min-height: 0;
        display: flex;
        flex-direction: column;
    }
}

.card-title {
    font-size: 16px;
    font-weight: 600;
    color: #303133;
}

.content-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    flex-wrap: wrap;
}

.detail-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    flex: 1;
    flex-wrap: wrap;
}

.toolbar-device {
    display: flex;
    align-items: baseline;
    gap: 10px;
    color: #606266;
}

.device-name {
    font-size: 16px;
    font-weight: 600;
    color: #303133;
}

.device-code {
    font-size: 13px;
    color: #909399;
}

.toolbar-actions {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}

.machine-list-wrap,
.calendar-wrap {
    flex: 1;
    min-height: 0;
}

.machine-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    gap: 16px;
}

.machine-card {
    border: 1px solid #e4e7ed;
    border-radius: 12px;
    padding: 18px;
    background: linear-gradient(180deg, #ffffff 0%, #f8fbff 100%);
    cursor: pointer;
    transition: all 0.2s ease;
}

.machine-card:hover {
    border-color: #409eff;
    box-shadow: 0 8px 24px rgba(64, 158, 255, 0.12);
    transform: translateY(-2px);
}

.machine-card-top {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
    margin-bottom: 16px;
}

.machine-title {
    min-width: 0;
}

.machine-name {
    font-size: 18px;
    font-weight: 600;
    color: #303133;
    line-height: 1.4;
    word-break: break-all;
}

.machine-code {
    margin-top: 6px;
    font-size: 13px;
    color: #909399;
    word-break: break-all;
}

.machine-card-body {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.machine-info-item {
    display: flex;
    align-items: flex-start;
    gap: 12px;
    font-size: 14px;
}

.machine-info-item .label {
    width: 56px;
    flex-shrink: 0;
    color: #909399;
}

.machine-info-item .value {
    color: #303133;
    word-break: break-all;
}

.machine-info-item .remark {
    color: #606266;
}

.calendar-header {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.calendar-title {
    display: flex;
    flex-direction: column;
    gap: 4px;
    color: #303133;
    font-weight: 600;
}

.calendar-subtitle {
    font-size: 12px;
    color: #909399;
    font-weight: 400;
}

.calendar-cell {
    height: 100%;
    min-height: 92px;
    padding: 8px 6px;
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
    gap: 8px;
    background: #f8fbff;
    border-radius: 8px;
    border: 1px solid transparent;
}

.calendar-cell.is-rest {
    background: linear-gradient(180deg, #fff7f7 0%, #fff1f1 100%);
    border-color: #f3c7c7;
}

.calendar-cell.has-part-rest {
    background: linear-gradient(180deg, #fffdfa 0%, #fff6eb 100%);
    border-color: #f3ddae;
}

.cell-top {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 8px;
}

.cell-day {
    font-size: 16px;
    font-weight: 600;
    color: #303133;
}

.cell-badge {
    flex-shrink: 0;
    padding: 2px 8px;
    border-radius: 999px;
    font-size: 11px;
    line-height: 18px;
    color: #fff;
    background: #e67e22;
}

.calendar-cell.is-rest .cell-badge {
    background: #d9534f;
}

.cell-main {
    font-size: 15px;
    font-weight: 600;
    color: #409eff;
    line-height: 1.4;
}

.calendar-cell.is-rest .cell-main {
    color: #d9534f;
}

.calendar-cell.has-part-rest .cell-main {
    color: #c27b17;
}

.cell-extra {
    font-size: 12px;
    line-height: 1.5;
    color: #606266;
    word-break: break-all;
}

.day-dialog {
    min-height: 120px;
}

.day-dialog-header {
    display: flex;
    align-items: stretch;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 20px;
}

.day-dialog-device,
.day-dialog-date-wrap {
    border-radius: 16px;
    padding: 16px 18px;
    background: linear-gradient(135deg, #f8fbff 0%, #eef5ff 100%);
    border: 1px solid #e3edf9;
}

.day-dialog-device {
    flex: 1;
    min-width: 0;
}

.day-dialog-date-wrap {
    min-width: 160px;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: flex-end;
    gap: 8px;
}

.day-dialog-eyebrow,
.day-dialog-date-label {
    font-size: 12px;
    color: #7a8ca5;
    letter-spacing: 0.5px;
}

.day-dialog-title {
    margin-top: 4px;
    font-size: 20px;
    font-weight: 700;
    color: #303133;
}

.day-dialog-code {
    margin-top: 6px;
    font-size: 13px;
    color: #6f7d90;
}

.shift-edit-list {
    display: flex;
    flex-direction: column;
    gap: 18px;
}

.shift-edit-card {
    border: 1px solid #dde7f2;
    border-radius: 18px;
    padding: 18px;
    background: linear-gradient(180deg, #ffffff 0%, #f8fbff 100%);
    box-shadow: 0 10px 30px rgba(31, 95, 163, 0.08);
}

.shift-edit-card.is-rest-card {
    background: linear-gradient(180deg, #ffffff 0%, #f7f7f7 100%);
    border-color: #e5e7eb;
    box-shadow: none;
}

.shift-edit-top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    margin-bottom: 18px;
}

.shift-edit-name-wrap {
    display: flex;
    align-items: center;
    gap: 14px;
}

.shift-edit-icon {
    width: 42px;
    height: 42px;
    border-radius: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, #409eff 0%, #6bb5ff 100%);
    color: #fff;
    font-size: 18px;
    font-weight: 700;
}

.shift-edit-name {
    display: flex;
    flex-direction: column;
    gap: 4px;
    color: #303133;
}

.shift-edit-name > span:first-child {
    font-size: 17px;
    font-weight: 700;
}

.shift-edit-code {
    font-size: 13px;
    color: #909399;
    font-weight: 400;
}

.shift-edit-status {
    display: flex;
    align-items: center;
    gap: 12px;
}

.shift-edit-body {
    border-radius: 14px;
    padding: 16px;
    background: rgba(255, 255, 255, 0.82);
    border: 1px solid #edf3fa;
}

.shift-edit-body.is-disabled-body {
    background: #f5f5f5;
    border-color: #ebedf0;
}

.edit-label {
    margin-bottom: 8px;
    font-size: 13px;
    color: #606266;
    font-weight: 600;
}

.shift-edit-time-preview {
    margin-top: 14px;
    padding: 12px 14px;
    border-radius: 12px;
    background: #f4f8fc;
}

.time-preview-label {
    font-size: 12px;
    color: #7b8794;
    margin-bottom: 6px;
}

.time-preview-value {
    font-size: 14px;
    color: #334155;
    font-weight: 600;
    word-break: break-all;
}

.shift-edit-metrics {
    margin-top: 14px;
}

.metric-box {
    border-radius: 12px;
    padding: 12px 14px;
    background: #f8fafc;
    border: 1px solid #e8eef5;
}

.metric-label {
    font-size: 12px;
    color: #7b8794;
}

.metric-value {
    margin-top: 6px;
    font-size: 20px;
    font-weight: 700;
    color: #1f2937;
}

.shift-edit-tip {
    margin-top: 12px;
    font-size: 12px;
    color: #6b7280;
    line-height: 1.6;
    padding-left: 2px;
}

.query-card :deep(.el-card__body) {
    padding-bottom: 4px;
}

.query-card :deep(.el-form) {
    margin-bottom: -18px;
}

.calendar-wrap :deep(.el-calendar) {
    height: 100%;
}

.calendar-wrap :deep(.el-calendar-table .el-calendar-day) {
    height: 120px;
    padding: 6px;
}

@media (max-width: 768px) {
    .machine-grid {
        grid-template-columns: 1fr;
    }

    .detail-toolbar,
    .toolbar-actions {
        width: 100%;
    }

    .toolbar-actions :deep(.el-date-editor.el-input) {
        width: 100%;
    }

    .day-dialog-header,
    .shift-edit-top,
    .shift-edit-status {
        flex-direction: column;
        align-items: stretch;
    }

    .day-dialog-date-wrap {
        min-width: 0;
        align-items: flex-start;
    }
}
</style>
