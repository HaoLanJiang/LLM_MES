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
                            </div>
                        </div>
                    </template>

                    <template #date-cell="{ data }">
                        <div class="calendar-cell" :class="{ 'is-rest': isRestDay(data.day) }">
                            <div class="cell-day">{{ getDayNumber(data.day) }}</div>
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
    </div>
</template>

<script setup>
import { computed, getCurrentInstance, onMounted, reactive, ref } from 'vue'
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

const queryForm = reactive({
    machineCode: '',
    machineName: ''
})

const currentMachine = reactive({
    MachineCode: '',
    MachineName: ''
})

const isDetailMode = computed(() => !!currentMachine.MachineCode)

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
    loadCalendarData()
}

const goToPrevMonth = () => {
    calendarValue.value = new Date(calendarValue.value.getFullYear(), calendarValue.value.getMonth() - 1, 1)
    syncMonthValue()
    loadCalendarData()
}

const goToCurrentMonth = () => {
    calendarValue.value = new Date()
    syncMonthValue()
    loadCalendarData()
}

const goToNextMonth = () => {
    calendarValue.value = new Date(calendarValue.value.getFullYear(), calendarValue.value.getMonth() + 1, 1)
    syncMonthValue()
    loadCalendarData()
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
        `已用/剩余：${item.UsedMinutes || 0}/${item.RemainMinutes || 0} 分钟`
    ].filter(Boolean)

    return texts.join('\n')
}

const isRestDay = (day) => {
    return !getCalendarItem(day) || getCalendarItem(day)?.DisplayText === 'REST'
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
}

.calendar-cell.is-rest {
    background: #fafafa;
}

.cell-day {
    font-size: 16px;
    font-weight: 600;
    color: #303133;
}

.cell-main {
    font-size: 15px;
    font-weight: 600;
    color: #409eff;
    line-height: 1.4;
}

.calendar-cell.is-rest .cell-main {
    color: #909399;
}

.cell-extra {
    font-size: 12px;
    line-height: 1.5;
    color: #606266;
    word-break: break-all;
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
}
</style>
