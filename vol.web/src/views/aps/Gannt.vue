<template>
    <div class="aps-gannt-page">
        <el-card shadow="never" class="query-card">
            <template #header>
                <div class="card-title">设备甘特排程</div>
            </template>

            <el-form :model="queryForm" inline label-width="100px">
                <el-form-item label="开始日期">
                    <el-date-picker
                        v-model="queryForm.startDate"
                        type="date"
                        value-format="YYYY-MM-DD"
                        placeholder="请选择开始日期"
                    />
                </el-form-item>

                <el-form-item label="工单号">
                    <el-input
                        v-model="queryForm.workOrderNo"
                        clearable
                        placeholder="请输入工单号"
                        @keyup.enter="handleSearch"
                    />
                </el-form-item>

                <el-form-item label="设备编码">
                    <el-input
                        v-model="queryForm.machineCode"
                        clearable
                        placeholder="请输入设备编码"
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

                <el-form-item label="排产状态">
                    <el-select
                        v-model="queryForm.scheduleStatus"
                        clearable
                        placeholder="请选择排产状态"
                        style="width: 180px"
                    >
                        <el-option
                            v-for="item in scheduleStatusOptions"
                            :key="item.value"
                            :label="item.label"
                            :value="item.value"
                        />
                    </el-select>
                </el-form-item>

                <el-form-item>
                    <el-button type="primary" :loading="loading" @click="handleSearch">查询</el-button>
                    <el-button @click="handleReset">重置</el-button>
                    <el-button type="success" :loading="saveLoading" @click="handleSaveChanges">
                        保存调整({{ dirtyCount }})
                    </el-button>
                    <el-button @click="handleUndo">撤回</el-button>
                    <el-button @click="handleRefresh">刷新</el-button>
                </el-form-item>
            </el-form>
        </el-card>

        <el-card shadow="never" class="board-card">
            <template #header>
                <div class="board-header">
                    <div class="board-title-wrap">
                        <div class="card-title">设备排程时间图</div>
                        <div class="board-subtitle">
                            左侧固定设备，右侧按时间展示工单块，支持拖拽调整、右键编辑与删除。
                        </div>
                    </div>

                    <div class="board-summary">
                        <div class="summary-pill">设备 {{ lines.length }}</div>
                        <div class="summary-pill">工单 {{ tasks.length }}</div>
                        <div class="summary-pill is-dirty">待保存 {{ dirtyCount }}</div>
                    </div>
                </div>

                <div class="legend-row">
                    <div class="legend-item">
                        <span class="legend-dot status-scheduled" />
                        <span>已排产</span>
                    </div>
                    <div class="legend-item">
                        <span class="legend-dot status-running" />
                        <span>生产中</span>
                    </div>
                    <div class="legend-item">
                        <span class="legend-dot status-pending" />
                        <span>待排产</span>
                    </div>
                    <div class="legend-item">
                        <span class="legend-dot status-completed" />
                        <span>已完成</span>
                    </div>
                    <div class="legend-item">
                        <span class="legend-dot status-delayed" />
                        <span>已延期</span>
                    </div>
                </div>
            </template>

            <div
                v-loading="pageLoading"
                class="gantt-panel"
            >
                <div ref="scrollWrapper" class="gantt-scroll-wrapper" @click="closeAllOverlays">
                    <div class="gantt-container" :style="{ width: containerWidth }">
                        <div class="gantt-header">
                            <div class="fixed-cols-header">
                                <div class="col-item w-140">设备编码</div>
                                <div class="col-item w-160">设备名称</div>
                            </div>

                            <div class="time-axis">
                                <div
                                    v-for="date in dates"
                                    :key="date"
                                    class="date-group"
                                    :style="{ width: DAY_PX + 'px' }"
                                >
                                    <div class="date-label">{{ date }}</div>
                                    <div class="hour-labels">
                                        <div class="hour-item">00:00 - 12:00</div>
                                        <div class="hour-item">12:00 - 24:00</div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div v-if="lines.length" ref="ganttBody" class="gantt-body">
                            <div v-for="line in lines" :key="line.id" class="gantt-row">
                                <div class="fixed-cols">
                                    <div class="col-item w-140 code-cell">{{ line.code || '-' }}</div>
                                    <div class="col-item w-160 name-cell">{{ line.name || '-' }}</div>
                                </div>

                                <div class="drop-zone" :style="{ width: axisWidth + 'px' }">
                                    <div class="grid-line-layer" :style="gridBackgroundStyle" />

                                    <el-tooltip
                                        v-for="task in lineTasksMap[line.id] || []"
                                        :key="task.id"
                                        placement="top"
                                        effect="dark"
                                    >
                                        <template #content>
                                            <div class="tooltip-title">{{ task.label }}</div>
                                            <div>设备：{{ task.machineName || task.machineCode || '-' }}</div>
                                            <div>开始：{{ formatDateTimeByHours(task.start) }}</div>
                                            <div>结束：{{ formatDateTimeByHours(task.start + task.duration) }}</div>
                                            <div>状态：{{ task.status || '-' }}</div>
                                        </template>

                                        <div
                                            class="task-block"
                                            :class="[
                                                taskClassName(task),
                                                {
                                                    'is-drag-source': isDraggingTask(task.id),
                                                    'is-dirty-task': isDirtyTask(task.id)
                                                }
                                            ]"
                                            :style="computeTaskStyle(task)"
                                            @mousedown.left.stop.prevent="onTaskMouseDown($event, task)"
                                            @contextmenu.prevent.stop="openContextMenu($event, task)"
                                        >
                                            <div class="task-content">
                                                <div class="task-id">{{ task.label }}</div>
                                                <div v-if="task.duration >= 2" class="task-time">
                                                    {{ formatTimeRange(task) }}
                                                </div>
                                            </div>
                                        </div>
                                    </el-tooltip>
                                </div>
                            </div>
                        </div>

                        <div v-else class="empty-state">
                            <el-empty description="暂无可展示的排程数据" />
                        </div>
                    </div>
                </div>
            </div>
        </el-card>

        <div
            v-if="contextMenu.visible"
            class="context-menu"
            :style="{ top: `${contextMenu.y}px`, left: `${contextMenu.x}px` }"
        >
            <div class="menu-item" @click="handleEditClick">修改工单</div>
            <div class="menu-item text-red" @click="handleDelete">删除工单</div>
        </div>

        <div
            v-if="dragState.active"
            class="task-block drag-preview"
            :class="dragState.previewClassName"
            :style="dragPreviewStyle"
        >
            <div class="task-content">
                <div class="task-id">{{ dragState.label }}</div>
                <div class="task-time">{{ dragPreviewTimeRange }}</div>
            </div>
        </div>

        <el-dialog
            v-model="editModal.visible"
            title="编辑排程详情"
            width="520px"
            destroy-on-close
            @close="resetEditModal"
        >
            <el-form label-width="100px" size="default">
                <el-form-item label="工单号">
                    <el-input v-model="editModal.form.label" />
                </el-form-item>

                <el-form-item label="设备">
                    <el-select
                        v-model="editModal.form.lineId"
                        placeholder="请选择设备"
                        style="width: 100%"
                        @change="handleEditLineChange"
                    >
                        <el-option
                            v-for="item in lineOptions"
                            :key="item.id"
                            :label="`${item.code || '-'} / ${item.name || '-'}`"
                            :value="item.id"
                        />
                    </el-select>
                </el-form-item>

                <el-form-item label="开始时间">
                    <el-date-picker
                        v-model="editModal.startTimeValue"
                        type="datetime"
                        format="YYYY-MM-DD HH:mm"
                        placeholder="请选择开始时间"
                        style="width: 100%"
                        @change="handleStartTimeChange"
                    />
                </el-form-item>

                <el-form-item label="持续时长">
                    <el-input-number
                        v-model="editModal.form.duration"
                        :min="0.5"
                        :step="0.5"
                        controls-position="right"
                        style="width: 220px"
                        @change="handleDurationChange"
                    />
                    <span class="time-unit">小时</span>
                </el-form-item>

                <el-form-item label="结束时间">
                    <el-date-picker
                        v-model="editModal.endTimeValue"
                        type="datetime"
                        format="YYYY-MM-DD HH:mm"
                        placeholder="请选择结束时间"
                        style="width: 100%"
                        @change="handleEndTimeChange"
                    />
                </el-form-item>

                <el-form-item label="排产状态">
                    <el-select v-model="editModal.form.status" style="width: 100%">
                        <el-option
                            v-for="item in scheduleStatusOptions"
                            :key="item.value"
                            :label="item.label"
                            :value="item.value"
                        />
                    </el-select>
                </el-form-item>

                <el-form-item label="备注">
                    <el-input
                        v-model="editModal.form.remark"
                        type="textarea"
                        :rows="3"
                        maxlength="200"
                        show-word-limit
                    />
                </el-form-item>
            </el-form>

            <template #footer>
                <el-button @click="editModal.visible = false">取消</el-button>
                <el-button type="primary" @click="saveEdit">确定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
import { computed, getCurrentInstance, onBeforeUnmount, onMounted, reactive, ref } from 'vue'

const { proxy } = getCurrentInstance()

const FIXED_COLS_WIDTH = 300
const HOUR_PX = 28
const DAY_PX = HOUR_PX * 24
const ROW_HEIGHT = 76
const TASK_TOP = 12
const MIN_DURATION = 0.5

const scheduleStatusOptions = [
    { label: '待排产', value: '待排产' },
    { label: '排产中', value: '排产中' },
    { label: '已排产', value: '已排产' },
    { label: '已完成', value: '已完成' },
    { label: '已延期', value: '已延期' }
]

const scrollWrapper = ref()
const ganttBody = ref()
const loading = ref(false)
const saveLoading = ref(false)
const lines = ref([])
const tasks = ref([])
const dates = ref([])
const baseDate = ref(null)
const snapshot = ref(null)
const dirtyTaskIds = ref([])
const sourceRowMap = ref({})

const queryForm = reactive({
    startDate: formatDateOnly(new Date()),
    workOrderNo: '',
    machineCode: '',
    machineName: '',
    scheduleStatus: ''
})

const contextMenu = reactive(createEmptyContextMenu())
const editModal = reactive(createEmptyEditModal())
const dragState = reactive(createEmptyDragState())

const pageLoading = computed(() => loading.value || saveLoading.value)
const axisWidth = computed(() => dates.value.length * DAY_PX)
const containerWidth = computed(() => `${axisWidth.value + FIXED_COLS_WIDTH}px`)
const dirtyCount = computed(() => dirtyTaskIds.value.length)
const lineOptions = computed(() => lines.value.map((item) => ({ ...item })))
const dragPreviewStyle = computed(() => ({
    left: `${dragState.left}px`,
    top: `${dragState.top}px`,
    width: `${dragState.width}px`
}))
const dragPreviewTimeRange = computed(() => {
    if (!dragState.active) {
        return ''
    }

    return `${toSimpleTime(dragState.previewStart)}-${toSimpleTime(dragState.previewStart + dragState.duration)}`
})
const gridBackgroundStyle = computed(() => {
    const step = HOUR_PX / 2
    return {
        backgroundSize: `${step}px 100%`,
        backgroundImage: 'linear-gradient(90deg, #eef2f7 1px, transparent 1px)'
    }
})
const lineTasksMap = computed(() => {
    const map = {}
    ;[...tasks.value]
        .sort((prev, next) => prev.start - next.start)
        .forEach((task) => {
            if (!map[task.lineId]) {
                map[task.lineId] = []
            }
            map[task.lineId].push(task)
        })

    return map
})

function createEmptyContextMenu() {
    return {
        visible: false,
        x: 0,
        y: 0,
        task: null
    }
}

function createEmptyEditModal() {
    return {
        visible: false,
        form: {
            id: '',
            lineId: '',
            label: '',
            machineCode: '',
            machineName: '',
            start: 0,
            duration: 0.5,
            status: '',
            remark: ''
        },
        startTimeValue: null,
        endTimeValue: null
    }
}

function createEmptyDragState() {
    return {
        active: false,
        taskId: '',
        previewLineId: '',
        previewStart: 0,
        duration: 0,
        width: 0,
        left: 0,
        top: 0,
        offsetX: 0,
        moved: false,
        label: '',
        previewClassName: 'status-scheduled',
        startClientX: 0,
        startClientY: 0,
        lastClientX: 0,
        lastClientY: 0,
        frameId: null
    }
}

function resetContextMenu() {
    Object.assign(contextMenu, createEmptyContextMenu())
}

function resetEditModal() {
    Object.assign(editModal, createEmptyEditModal())
}

function resetDragState() {
    cancelDragFrame()
    Object.assign(dragState, createEmptyDragState())
}

function cloneData(data) {
    return JSON.parse(JSON.stringify(data))
}

function takeSnapshot() {
    snapshot.value = {
        tasks: cloneData(tasks.value),
        dirtyTaskIds: [...dirtyTaskIds.value]
    }
}

function formatDateOnly(date) {
    const year = date.getFullYear()
    const month = `${date.getMonth() + 1}`.padStart(2, '0')
    const day = `${date.getDate()}`.padStart(2, '0')
    return `${year}-${month}-${day}`
}

function formatDateTime(date) {
    if (!(date instanceof Date) || Number.isNaN(date.getTime())) {
        return ''
    }

    const year = date.getFullYear()
    const month = `${date.getMonth() + 1}`.padStart(2, '0')
    const day = `${date.getDate()}`.padStart(2, '0')
    const hour = `${date.getHours()}`.padStart(2, '0')
    const minute = `${date.getMinutes()}`.padStart(2, '0')
    const second = `${date.getSeconds()}`.padStart(2, '0')
    return `${year}-${month}-${day} ${hour}:${minute}:${second}`
}

function parseDate(value) {
    if (!value) {
        return null
    }

    if (value instanceof Date) {
        return Number.isNaN(value.getTime()) ? null : value
    }

    const normalized = typeof value === 'string' ? value.replace('T', ' ') : value
    const date = new Date(normalized)
    return Number.isNaN(date.getTime()) ? null : date
}

function parseDateOnly(value) {
    if (!value) {
        return null
    }

    const date = new Date(`${value} 00:00:00`)
    return Number.isNaN(date.getTime()) ? null : date
}

function addHours(date, hours) {
    return new Date(date.getTime() + hours * 3600000)
}

function buildDateAxis(startDate, maxTime) {
    const axis = []
    const cursor = new Date(startDate)

    while (cursor <= maxTime || !axis.length) {
        axis.push(formatDateOnly(cursor))
        cursor.setDate(cursor.getDate() + 1)
    }

    return axis
}

function buildLineId(row, index) {
    const code = String(row.MachineCode || row.machineCode || '').trim()
    const name = String(row.MachineName || row.machineName || '').trim()
    return `machine-${code || name || index}`
}

function buildGanttData(rows) {
    const normalizedLines = []
    const lineMap = {}
    const normalizedTasks = []
    const nextSourceRowMap = {}
    let minTime = null
    let maxTime = null

    const boundaryDate = parseDateOnly(queryForm.startDate)

    ;(rows || []).forEach((row, index) => {
        const startTime = parseDate(row.PlanStartTime)
        const endTime = parseDate(row.PlanEndTime)

        if (!startTime || !endTime || endTime <= startTime) {
            return
        }

        if (boundaryDate && endTime <= boundaryDate) {
            return
        }

        const lineId = buildLineId(row, index)
        if (!lineMap[lineId]) {
            lineMap[lineId] = true
            normalizedLines.push({
                id: lineId,
                code: row.MachineCode || '',
                name: row.MachineName || '',
                raw: row
            })
        }

        if (!minTime || startTime < minTime) {
            minTime = new Date(startTime)
        }
        if (!maxTime || endTime > maxTime) {
            maxTime = new Date(endTime)
        }

        const visibleStart = boundaryDate && startTime < boundaryDate ? new Date(boundaryDate) : startTime
        nextSourceRowMap[row.Id] = cloneData(row)

        normalizedTasks.push({
            id: row.Id,
            lineId,
            machineCode: row.MachineCode || '',
            machineName: row.MachineName || '',
            workOrderNo: row.WorkOrderNo || '',
            label: row.WorkOrderNo || '',
            status: row.ScheduleStatus || '已排产',
            remark: row.Remark || '',
            startTimeText: row.PlanStartTime || '',
            endTimeText: row.PlanEndTime || '',
            startDateTime: startTime,
            endDateTime: endTime,
            visibleStartDateTime: visibleStart
        })
    })

    normalizedLines.sort((prev, next) => {
        const codeCompare = String(prev.code || '').localeCompare(String(next.code || ''))
        if (codeCompare !== 0) {
            return codeCompare
        }
        return String(prev.name || '').localeCompare(String(next.name || ''))
    })

    if (!normalizedTasks.length || !maxTime) {
        lines.value = normalizedLines
        tasks.value = []
        dates.value = []
        baseDate.value = null
        sourceRowMap.value = nextSourceRowMap
        snapshot.value = null
        dirtyTaskIds.value = []
        return
    }

    const axisBaseDate = boundaryDate || new Date(minTime)
    axisBaseDate.setHours(0, 0, 0, 0)
    baseDate.value = axisBaseDate
    dates.value = buildDateAxis(axisBaseDate, maxTime)
    lines.value = normalizedLines
    sourceRowMap.value = nextSourceRowMap

    const baseMs = axisBaseDate.getTime()
    tasks.value = normalizedTasks.map((task) => ({
        id: task.id,
        lineId: task.lineId,
        machineCode: task.machineCode,
        machineName: task.machineName,
        workOrderNo: task.workOrderNo,
        label: task.label,
        status: task.status,
        remark: task.remark,
        start: Math.max(0, (task.visibleStartDateTime.getTime() - baseMs) / 3600000),
        duration: Math.max(MIN_DURATION, (task.endDateTime.getTime() - task.visibleStartDateTime.getTime()) / 3600000)
    }))

    snapshot.value = null
    dirtyTaskIds.value = []
}

async function loadData(showSuccessMessage = false) {
    loading.value = true
    try {
        const result = await proxy.http.post('api/Aps_Schedule_Result/GetScheduleResultPageList', {
            Page: 1,
            Rows: 9999,
            Sort: 'PlanStartTime',
            Order: 'asc',
            WorkOrderNo: queryForm.workOrderNo,
            MachineCode: queryForm.machineCode,
            ScheduleStatus: queryForm.scheduleStatus
        })

        let rows = result?.rows || []

        if (queryForm.machineName) {
            const keyword = String(queryForm.machineName).trim().toLowerCase()
            rows = rows.filter((item) => String(item.MachineName || '').toLowerCase().includes(keyword))
        }

        buildGanttData(rows)

        if (showSuccessMessage) {
            proxy.$message.success('查询成功')
        }
    } finally {
        loading.value = false
    }
}

async function confirmDiscardUnsavedChanges() {
    if (!dirtyTaskIds.value.length) {
        return true
    }

    return proxy
        .$confirm('当前有未保存的本地调整，继续操作会丢失这些修改，是否继续？', '提示', {
            type: 'warning'
        })
        .then(() => true)
        .catch(() => false)
}

async function handleSearch() {
    if (!(await confirmDiscardUnsavedChanges())) {
        return
    }

    loadData(true)
}

async function handleReset() {
    if (!(await confirmDiscardUnsavedChanges())) {
        return
    }

    queryForm.startDate = formatDateOnly(new Date())
    queryForm.workOrderNo = ''
    queryForm.machineCode = ''
    queryForm.machineName = ''
    queryForm.scheduleStatus = ''
    loadData()
}

async function handleRefresh() {
    if (!(await confirmDiscardUnsavedChanges())) {
        return
    }

    loadData(true)
}

function isDirtyTask(taskId) {
    return dirtyTaskIds.value.includes(taskId)
}

function markTaskDirty(taskId) {
    if (isDirtyTask(taskId)) {
        return
    }

    dirtyTaskIds.value = [...dirtyTaskIds.value, taskId]
}

function getLineById(lineId) {
    return lines.value.find((item) => item.id === lineId) || null
}

function getTaskById(taskId) {
    return tasks.value.find((item) => item.id === taskId) || null
}

function normalizeLineTasks(lineId, pinnedTaskId = null) {
    if (!lineId) {
        return { ok: true }
    }

    const totalLimit = dates.value.length * 24
    const lineTasks = tasks.value
        .filter((task) => task.lineId === lineId)
        .sort((prev, next) => {
            if (prev.start === next.start) {
                if (prev.id === pinnedTaskId) {
                    return -1
                }
                if (next.id === pinnedTaskId) {
                    return 1
                }
            }
            return prev.start - next.start
        })

    const totalDuration = lineTasks.reduce((sum, task) => sum + Number(task.duration || 0), 0)
    if (totalDuration > totalLimit) {
        return {
            ok: false,
            reason: 'totalDurationExceeded'
        }
    }

    let cursor = 0
    const nextPositions = []

    for (const task of lineTasks) {
        const maxStart = Math.max(0, totalLimit - task.duration)
        const desiredStart = Math.max(cursor, task.start)

        if (desiredStart > maxStart) {
            return {
                ok: false,
                reason: 'maxTimeExceeded'
            }
        }

        const nextStart = Math.max(0, Math.min(desiredStart, maxStart))
        nextPositions.push({
            task,
            start: nextStart
        })
        cursor = nextStart + task.duration
    }

    nextPositions.forEach((item) => {
        item.task.start = item.start
    })

    return { ok: true }
}

function getNormalizeFailureMessage(action) {
    return action === 'save'
        ? '本次调整会导致设备排程超出当前时间轴，无法保存'
        : '拖动后会导致设备排程超出当前时间轴，已回退原位置'
}

function cancelDragFrame() {
    if (dragState.frameId) {
        window.cancelAnimationFrame(dragState.frameId)
        dragState.frameId = null
    }
}

function removeDragListeners() {
    window.removeEventListener('mousemove', onDragMove)
    window.removeEventListener('mouseup', onDragEnd)
}

function closeAllOverlays() {
    resetContextMenu()
}

function onTaskMouseDown(event, task) {
    if (!ganttBody.value || !scrollWrapper.value) {
        return
    }

    const rect = event.currentTarget.getBoundingClientRect()
    const wrapperRect = scrollWrapper.value.getBoundingClientRect()
    const scrollLeft = scrollWrapper.value.scrollLeft
    const pointerContentX = event.clientX - wrapperRect.left + scrollLeft - FIXED_COLS_WIDTH

    takeSnapshot()
    closeAllOverlays()

    dragState.active = true
    dragState.taskId = task.id
    dragState.previewLineId = task.lineId
    dragState.previewStart = task.start
    dragState.duration = task.duration
    dragState.width = task.duration * HOUR_PX
    dragState.left = rect.left
    dragState.top = rect.top
    dragState.offsetX = pointerContentX - task.start * HOUR_PX
    dragState.moved = false
    dragState.label = task.label
    dragState.previewClassName = taskClassName(task)
    dragState.startClientX = event.clientX
    dragState.startClientY = event.clientY
    dragState.lastClientX = event.clientX
    dragState.lastClientY = event.clientY

    window.addEventListener('mousemove', onDragMove)
    window.addEventListener('mouseup', onDragEnd)
}

function onDragMove(event) {
    if (!dragState.active) {
        return
    }

    dragState.lastClientX = event.clientX
    dragState.lastClientY = event.clientY

    if (!dragState.moved) {
        const deltaX = Math.abs(event.clientX - dragState.startClientX)
        const deltaY = Math.abs(event.clientY - dragState.startClientY)
        dragState.moved = deltaX > 2 || deltaY > 2
    }

    if (dragState.frameId) {
        return
    }

    dragState.frameId = window.requestAnimationFrame(() => {
        dragState.frameId = null
        updateDragPreview()
    })
}

function updateDragPreview() {
    if (!dragState.active || !ganttBody.value || !scrollWrapper.value || !lines.value.length) {
        return
    }

    const wrapperRect = scrollWrapper.value.getBoundingClientRect()
    const bodyRect = ganttBody.value.getBoundingClientRect()
    const scrollLeft = scrollWrapper.value.scrollLeft
    const totalLimit = dates.value.length * 24
    const maxStart = Math.max(0, totalLimit - dragState.duration)
    const relativeX =
        dragState.lastClientX - wrapperRect.left - FIXED_COLS_WIDTH + scrollLeft - dragState.offsetX
    const rawLineIndex = Math.floor((dragState.lastClientY - bodyRect.top) / ROW_HEIGHT)
    const lineIndex = Math.max(0, Math.min(lines.value.length - 1, rawLineIndex))
    const previewLine = lines.value[lineIndex]
    const previewStart = Math.max(0, Math.min(maxStart, Math.round((relativeX / HOUR_PX) * 2) / 2))

    dragState.previewStart = previewStart
    dragState.previewLineId = previewLine.id
    dragState.left = wrapperRect.left + FIXED_COLS_WIDTH - scrollLeft + previewStart * HOUR_PX
    dragState.top = bodyRect.top + lineIndex * ROW_HEIGHT + TASK_TOP
}

async function onDragEnd() {
    if (!dragState.active) {
        return
    }

    cancelDragFrame()
    removeDragListeners()

    if (!dragState.moved) {
        snapshot.value = null
        resetDragState()
        return
    }

    const targetTask = getTaskById(dragState.taskId)
    const targetLine = getLineById(dragState.previewLineId)

    if (!targetTask || !targetLine) {
        tasks.value = snapshot.value?.tasks ? cloneData(snapshot.value.tasks) : tasks.value
        dirtyTaskIds.value = snapshot.value?.dirtyTaskIds ? [...snapshot.value.dirtyTaskIds] : dirtyTaskIds.value
        snapshot.value = null
        resetDragState()
        return
    }

    targetTask.lineId = dragState.previewLineId
    targetTask.machineCode = targetLine.code || ''
    targetTask.machineName = targetLine.name || ''
    targetTask.start = dragState.previewStart

    const normalizeResult = normalizeLineTasks(targetTask.lineId, targetTask.id)
    if (!normalizeResult.ok) {
        tasks.value = cloneData(snapshot.value.tasks)
        dirtyTaskIds.value = [...snapshot.value.dirtyTaskIds]
        snapshot.value = null
        proxy.$message.warning(getNormalizeFailureMessage('drag'))
        resetDragState()
        return
    }

    markTaskDirty(targetTask.id)
    resetDragState()
}

function handleUndo() {
    if (!snapshot.value) {
        return proxy.$message.warning('暂无可撤回的本地调整')
    }

    tasks.value = cloneData(snapshot.value.tasks)
    dirtyTaskIds.value = [...snapshot.value.dirtyTaskIds]
    snapshot.value = null
    resetDragState()
    resetContextMenu()
    proxy.$message.success('已撤回上一步调整')
}

function openContextMenu(event, task) {
    contextMenu.visible = true
    contextMenu.x = event.clientX
    contextMenu.y = event.clientY
    contextMenu.task = task
}

function handleEditClick() {
    if (!contextMenu.task || !baseDate.value) {
        return
    }

    const task = cloneData(contextMenu.task)
    Object.assign(editModal, {
        visible: true,
        form: {
            id: task.id,
            lineId: task.lineId,
            label: task.label,
            machineCode: task.machineCode,
            machineName: task.machineName,
            start: task.start,
            duration: task.duration,
            status: task.status,
            remark: task.remark
        },
        startTimeValue: addHours(baseDate.value, task.start),
        endTimeValue: addHours(baseDate.value, task.start + task.duration)
    })

    resetContextMenu()
}

function handleEditLineChange(lineId) {
    const targetLine = getLineById(lineId)
    if (!targetLine) {
        return
    }

    editModal.form.machineCode = targetLine.code || ''
    editModal.form.machineName = targetLine.name || ''
}

function handleStartTimeChange() {
    if (!baseDate.value || !editModal.startTimeValue) {
        return
    }

    const nextStart = (editModal.startTimeValue.getTime() - baseDate.value.getTime()) / 3600000
    editModal.form.start = Math.max(0, nextStart)
    syncEditEndTime()
}

function handleEndTimeChange() {
    if (!editModal.startTimeValue || !editModal.endTimeValue) {
        return
    }

    const duration = (editModal.endTimeValue.getTime() - editModal.startTimeValue.getTime()) / 3600000
    editModal.form.duration = Math.max(MIN_DURATION, Math.round(duration * 2) / 2)
    syncEditEndTime()
}

function handleDurationChange() {
    editModal.form.duration = Math.max(MIN_DURATION, Number(editModal.form.duration) || MIN_DURATION)
    syncEditEndTime()
}

function syncEditEndTime() {
    if (!baseDate.value || !editModal.startTimeValue) {
        return
    }

    const startMs = editModal.startTimeValue.getTime()
    editModal.form.start = Math.max(0, (startMs - baseDate.value.getTime()) / 3600000)
    editModal.endTimeValue = new Date(startMs + editModal.form.duration * 3600000)
}

function saveEdit() {
    if (!editModal.form.id) {
        return
    }

    const index = tasks.value.findIndex((item) => item.id === editModal.form.id)
    if (index === -1) {
        return
    }

    takeSnapshot()

    const targetLine = getLineById(editModal.form.lineId)
    tasks.value[index] = {
        ...tasks.value[index],
        lineId: editModal.form.lineId,
        machineCode: targetLine?.code || editModal.form.machineCode || '',
        machineName: targetLine?.name || editModal.form.machineName || '',
        workOrderNo: editModal.form.label,
        label: editModal.form.label,
        start: editModal.form.start,
        duration: Math.max(MIN_DURATION, Number(editModal.form.duration) || MIN_DURATION),
        status: editModal.form.status || '已排产',
        remark: editModal.form.remark || ''
    }

    const normalizeResult = normalizeLineTasks(editModal.form.lineId, editModal.form.id)
    if (!normalizeResult.ok) {
        tasks.value = cloneData(snapshot.value.tasks)
        dirtyTaskIds.value = [...snapshot.value.dirtyTaskIds]
        snapshot.value = null
        editModal.visible = false
        proxy.$message.warning(getNormalizeFailureMessage('save'))
        return
    }

    markTaskDirty(editModal.form.id)
    editModal.visible = false
}

async function handleDelete() {
    const task = contextMenu.task
    if (!task) {
        return
    }

    resetContextMenu()

    await proxy
        .$confirm(`确认删除工单【${task.label || '-'}】吗？`, '提示', {
            type: 'warning'
        })
        .then(async () => {
            const result = await proxy.http.post('api/Aps_Schedule_Result/Del', [task.id])
            if (!result?.status) {
                return proxy.$message.error(result?.message || '删除失败')
            }

            proxy.$message.success(result.message || '删除成功')
            await loadData()
        })
        .catch(() => {})
}

function buildUpdatePayload(task) {
    const sourceRow = sourceRowMap.value[task.id] || {}
    const startDateTime = addHours(baseDate.value, task.start)
    const endDateTime = addHours(baseDate.value, task.start + task.duration)

    return {
        ...sourceRow,
        Id: task.id,
        WorkOrderNo: task.label,
        MachineCode: task.machineCode,
        MachineName: task.machineName,
        PlanStartTime: formatDateTime(startDateTime),
        PlanEndTime: formatDateTime(endDateTime),
        PlanMinutes: Math.max(1, Math.round(task.duration * 60)),
        ScheduleStatus: task.status || sourceRow.ScheduleStatus || '已排产',
        Remark: task.remark || ''
    }
}

async function handleSaveChanges() {
    if (!dirtyTaskIds.value.length) {
        return proxy.$message.warning('当前没有待保存的调整')
    }

    if (!baseDate.value) {
        return proxy.$message.warning('当前没有可保存的排程数据')
    }

    saveLoading.value = true
    try {
        for (const taskId of dirtyTaskIds.value) {
            const task = getTaskById(taskId)
            if (!task) {
                continue
            }

            const payload = buildUpdatePayload(task)
            const result = await proxy.http.post('api/Aps_Schedule_Result/Update', {
                MainData: payload,
                DetailData: [],
                DelKeys: []
            })

            if (!result?.status) {
                throw new Error(result?.message || `工单 ${task.label || taskId} 保存失败`)
            }

            sourceRowMap.value[taskId] = cloneData(payload)
        }

        proxy.$message.success('排程调整已保存')
        snapshot.value = null
        dirtyTaskIds.value = []
        await loadData()
    } catch (error) {
        proxy.$message.error(error?.message || '保存失败')
    } finally {
        saveLoading.value = false
    }
}

function taskClassName(task) {
    const status = String(task.status || '')

    if (status.includes('延期')) {
        return 'status-delayed'
    }
    if (status.includes('完成')) {
        return 'status-completed'
    }
    if (status.includes('生产中') || status.includes('排产中')) {
        return 'status-running'
    }
    if (status.includes('待排产')) {
        return 'status-pending'
    }
    return 'status-scheduled'
}

function isDraggingTask(taskId) {
    return dragState.active && dragState.taskId === taskId
}

function computeTaskStyle(task) {
    return {
        left: `${task.start * HOUR_PX}px`,
        width: `${Math.max(task.duration * HOUR_PX, 18)}px`
    }
}

function toSimpleTime(value) {
    const normalized = ((value % 24) + 24) % 24
    let hours = Math.floor(normalized)
    let minutes = Math.round((normalized % 1) * 60)

    if (minutes === 60) {
        minutes = 0
        hours = (hours + 1) % 24
    }

    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
}

function formatTimeRange(task) {
    return `${toSimpleTime(task.start)}-${toSimpleTime(task.start + task.duration)}`
}

function formatDateTimeByHours(hours) {
    if (!baseDate.value) {
        return '-'
    }

    return formatDateTime(addHours(baseDate.value, hours))
}

onMounted(() => {
    loadData()
})

onBeforeUnmount(() => {
    removeDragListeners()
    cancelDragFrame()
})
</script>

<style scoped lang="less">
.aps-gannt-page {
    padding: 8px;
    background: #f5f7fa;
    min-height: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    gap: 8px;
    box-sizing: border-box;
}

.query-card {
    flex-shrink: 0;
}

.board-card {
    flex: 1;
    min-height: 0;

    :deep(.el-card__body) {
        height: 100%;
        min-height: 0;
        display: flex;
        flex-direction: column;
        padding-top: 14px;
    }
}

.card-title {
    font-size: 16px;
    font-weight: 600;
    color: #303133;
}

.query-card :deep(.el-card__body) {
    padding-bottom: 4px;
}

.query-card :deep(.el-form) {
    margin-bottom: -18px;
}

.board-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    flex-wrap: wrap;
}

.board-title-wrap {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.board-subtitle {
    font-size: 12px;
    color: #7a8797;
}

.board-summary {
    display: flex;
    gap: 10px;
    flex-wrap: wrap;
}

.summary-pill {
    padding: 6px 12px;
    border-radius: 999px;
    background: #eff6ff;
    color: #2b5fa8;
    font-size: 12px;
    font-weight: 600;
}

.summary-pill.is-dirty {
    background: #fff5e8;
    color: #c77712;
}

.legend-row {
    margin-top: 14px;
    display: flex;
    gap: 18px;
    flex-wrap: wrap;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    color: #606266;
}

.legend-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    display: inline-block;
}

.gantt-panel {
    flex: 1;
    min-height: 0;
}

.gantt-scroll-wrapper {
    width: 100%;
    height: 100%;
    overflow: auto;
    border-radius: 14px;
    border: 1px solid #e5eaf3;
    background: #fff;
}

.gantt-container {
    position: relative;
    min-height: 320px;
}

.gantt-header {
    display: flex;
    position: sticky;
    top: 0;
    z-index: 20;
    background: linear-gradient(90deg, #244a7c 0%, #3a6fb2 100%);
    color: #fff;
    box-shadow: 0 8px 18px rgba(36, 74, 124, 0.16);
}

.fixed-cols-header {
    width: 300px;
    display: flex;
    flex-shrink: 0;
    background: rgba(11, 34, 63, 0.3);
    backdrop-filter: blur(8px);
}

.time-axis {
    display: flex;
}

.date-group {
    border-right: 1px solid rgba(255, 255, 255, 0.18);
    text-align: center;
}

.date-label {
    height: 30px;
    line-height: 30px;
    font-size: 12px;
    font-weight: 600;
    background: rgba(255, 255, 255, 0.08);
}

.hour-labels {
    display: flex;
    height: 26px;
    line-height: 26px;
    font-size: 11px;
    color: rgba(255, 255, 255, 0.86);
}

.hour-item {
    flex: 1;
}

.gantt-body {
    position: relative;
}

.gantt-row {
    display: flex;
    min-height: 76px;
    border-bottom: 1px solid #eff3f8;
}

.fixed-cols {
    position: sticky;
    left: 0;
    width: 300px;
    display: flex;
    flex-shrink: 0;
    background: linear-gradient(180deg, #ffffff 0%, #f8fbff 100%);
    z-index: 10;
    border-right: 2px solid #d8e4f4;
}

.col-item {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0 10px;
    font-size: 13px;
    text-align: center;
    word-break: break-all;
    box-sizing: border-box;
}

.w-140 {
    width: 140px;
}

.w-160 {
    width: 160px;
}

.code-cell {
    color: #475569;
    font-weight: 600;
}

.name-cell {
    color: #1f2937;
    font-weight: 700;
}

.drop-zone {
    position: relative;
    height: 100%;
    user-select: none;
}

.grid-line-layer {
    position: absolute;
    inset: 0;
    pointer-events: none;
}

.task-block {
    position: absolute;
    top: 12px;
    height: 50px;
    border-radius: 10px;
    color: #fff;
    cursor: move;
    z-index: 5;
    transition: box-shadow 0.18s ease, transform 0.18s ease;
    box-sizing: border-box;
    overflow: hidden;
}

.task-block:hover {
    transform: translateY(-1px);
    box-shadow: 0 10px 26px rgba(15, 23, 42, 0.22);
}

.task-content {
    width: 100%;
    height: 100%;
    padding: 6px 8px;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    font-size: 11px;
    box-sizing: border-box;
}

.task-id,
.task-time {
    width: 100%;
    text-align: center;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.task-id {
    font-weight: 700;
}

.task-time {
    margin-top: 4px;
    opacity: 0.92;
}

.status-scheduled {
    background: linear-gradient(135deg, #4d78c9 0%, #79a0eb 100%);
    border: 1px solid #4c76c4;
}

.status-running {
    background: linear-gradient(135deg, #1f9d86 0%, #47c7ae 100%);
    border: 1px solid #228f7d;
}

.status-pending {
    background: linear-gradient(135deg, #d18916 0%, #efb44a 100%);
    border: 1px solid #c17d11;
}

.status-completed {
    background: linear-gradient(135deg, #4d8c57 0%, #75b884 100%);
    border: 1px solid #4a8453;
}

.status-delayed {
    background: linear-gradient(135deg, #c84d4d 0%, #e98585 100%);
    border: 1px solid #bb4747;
}

.is-drag-source {
    visibility: hidden;
}

.is-dirty-task {
    box-shadow: inset 0 0 0 2px rgba(255, 255, 255, 0.38);
}

.drag-preview {
    position: fixed;
    margin: 0;
    pointer-events: none;
    opacity: 0.95;
    z-index: 3000;
    box-shadow: 0 16px 34px rgba(15, 23, 42, 0.24);
}

.context-menu {
    position: fixed;
    z-index: 2200;
    min-width: 140px;
    padding: 6px 0;
    border-radius: 10px;
    border: 1px solid #e6eaf1;
    background: #fff;
    box-shadow: 0 18px 40px rgba(15, 23, 42, 0.14);
}

.menu-item {
    padding: 9px 16px;
    font-size: 13px;
    color: #303133;
    cursor: pointer;
}

.menu-item:hover {
    background: #f5f8fc;
    color: #409eff;
}

.text-red {
    color: #ef4444;
}

.tooltip-title {
    margin-bottom: 6px;
    font-weight: 700;
}

.time-unit {
    margin-left: 10px;
    color: #909399;
}

.empty-state {
    min-height: 320px;
    display: flex;
    align-items: center;
    justify-content: center;
}

@media (max-width: 768px) {
    .board-summary {
        width: 100%;
    }
}
</style>
