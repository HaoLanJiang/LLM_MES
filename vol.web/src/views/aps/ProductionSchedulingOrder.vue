<template>
    <div class="production-scheduling-order">
        <el-card shadow="never" class="query-card">
            <template #header>
                <div class="card-title">查询条件</div>
            </template>

            <el-form :model="queryForm" inline label-width="100px">
                <el-form-item label="工单号">
                    <el-input v-model="queryForm.workOrderNo" clearable placeholder="请输入工单号"
                        @keyup.enter="handleSearch" />
                </el-form-item>

                <el-form-item label="客户名称">
                    <el-input v-model="queryForm.customerName" clearable placeholder="请输入客户名称"
                        @keyup.enter="handleSearch" />
                </el-form-item>

                <el-form-item label="排产状态">
                    <el-select v-model="queryForm.scheduleStatus" clearable placeholder="请选择排产状态" style="width: 180px">
                        <el-option label="待排产" value="待排产" />
                        <el-option label="排产中" value="排产中" />
                        <el-option label="已排产" value="已排产" />
                        <el-option label="已完成" value="已完成" />
                    </el-select>
                </el-form-item>

                <el-form-item>
                    <el-button type="primary" @click="handleSearch">查询</el-button>
                    <el-button @click="handleReset">重置</el-button>
                </el-form-item>
            </el-form>
        </el-card>

        <el-card shadow="never" class="table-card">
            <template #header>
                <div class="table-header">
                    <div class="card-title">排产订单列表</div>
                    <div class="table-actions">
                        <el-button type="primary" @click="openAddDialog">新增</el-button>
                        <el-button type="danger" @click="handleDelete">删除</el-button>
                    </div>
                </div>
            </template>

            <el-table v-loading="loading" :data="tableData" :height="tableHeight" border stripe style="width: 100%"
                @sort-change="handleSortChange" @selection-change="handleSelectionChange">
                <el-table-column type="selection" width="55" align="center" />
                <el-table-column type="index" label="序号" width="70" align="center" />
                <el-table-column prop="WorkOrderNo" label="工单号" min-width="160" sortable="custom"
                    show-overflow-tooltip />
                <el-table-column prop="CustomerName" label="客户名称" min-width="160" sortable="custom"
                    show-overflow-tooltip />
                <el-table-column prop="ProductCode" label="产品编码" min-width="140" show-overflow-tooltip />
                <el-table-column prop="ProductName" label="产品名称" min-width="160" show-overflow-tooltip />
                <el-table-column prop="OrderQty" label="订单数量" min-width="110" sortable="custom" align="right" />
                <el-table-column prop="EarliestStartTime" label="最早开始时间" min-width="170" sortable="custom" />
                <el-table-column prop="LatestDeliveryDate" label="最晚交付日期" min-width="170" sortable="custom" />
                <el-table-column prop="ProcessMinutes" label="加工分钟数" min-width="120" sortable="custom"
                    align="right" />
                <el-table-column prop="RequiredMachine" label="指定设备" min-width="180" show-overflow-tooltip />
                <el-table-column prop="ChangeoverGroup" label="换型分组" min-width="120" show-overflow-tooltip />
                <el-table-column prop="ScheduleStatus" label="排产状态" min-width="110" sortable="custom" />
                <el-table-column prop="Remark" label="备注" min-width="220" show-overflow-tooltip />
                <el-table-column label="操作" width="120" fixed="right" align="center">
                    <template #default="{ row }">
                        <el-button link type="primary" @click="openEditDialog(row)">修改</el-button>
                    </template>
                </el-table-column>
            </el-table>

            <div class="pagination-wrap">
                <el-pagination v-model:current-page="pageInfo.page" v-model:page-size="pageInfo.rows" background
                    layout="total, sizes, prev, pager, next, jumper" :page-sizes="[10, 20, 30, 50, 100]"
                    :total="pageInfo.total" @current-change="loadData" @size-change="handleSizeChange" />
            </div>
        </el-card>

        <el-dialog v-model="dialogVisible" :title="dialogMode === 'add' ? '新增排产订单' : '修改排产订单'" width="860px"
            destroy-on-close>
            <el-form ref="formRef" :model="editForm" :rules="formRules" label-width="120px" class="dialog-form">
                <el-row :gutter="16">
                    <el-col :span="12">
                        <el-form-item label="工单号" prop="WorkOrderNo">
                            <el-input v-model="editForm.WorkOrderNo" maxlength="50" placeholder="请输入工单号" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="客户名称" prop="CustomerName">
                            <el-input v-model="editForm.CustomerName" maxlength="100" placeholder="请输入客户名称" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="客户优先级" prop="CustomerPriority">
                            <el-input-number v-model="editForm.CustomerPriority" :min="1" :max="5" :precision="0"
                                controls-position="right" style="width: 100%" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="排产状态" prop="ScheduleStatus">
                            <el-select v-model="editForm.ScheduleStatus" clearable placeholder="请选择排产状态"
                                style="width: 100%">
                                <el-option label="待排产" value="待排产" />
                                <el-option label="排产中" value="排产中" />
                                <el-option label="已排产" value="已排产" />
                                <el-option label="已完成" value="已完成" />
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="产品编码" prop="ProductCode">
                            <el-input v-model="editForm.ProductCode" maxlength="50" placeholder="请输入产品编码" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="产品名称" prop="ProductName">
                            <el-input v-model="editForm.ProductName" maxlength="100" placeholder="请输入产品名称" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="订单数量" prop="OrderQty">
                            <el-input-number v-model="editForm.OrderQty" :min="0.01" :precision="2"
                                controls-position="right" style="width: 100%" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="加工分钟数" prop="ProcessMinutes">
                            <el-input-number v-model="editForm.ProcessMinutes" :min="1" :precision="0"
                                controls-position="right" style="width: 100%" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="最早开始时间" prop="EarliestStartTime">
                            <el-date-picker v-model="editForm.EarliestStartTime" type="datetime" value-format="YYYY-MM-DD HH:mm:ss"
                                placeholder="请选择最早开始时间" style="width: 100%" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="最晚交付日期" prop="LatestDeliveryDate">
                            <el-date-picker v-model="editForm.LatestDeliveryDate" type="datetime" value-format="YYYY-MM-DD HH:mm:ss"
                                placeholder="请选择最晚交付日期" style="width: 100%" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="指定设备" prop="RequiredMachine">
                            <el-select
                                v-model="editForm.RequiredMachineList"
                                multiple
                                clearable
                                collapse-tags
                                collapse-tags-tooltip
                                filterable
                                placeholder="不选择时表示可在任意设备生产"
                                style="width: 100%">
                                <el-option
                                    v-for="item in machineOptions"
                                    :key="item.value"
                                    :label="item.label"
                                    :value="item.value" />
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="换型分组" prop="ChangeoverGroup">
                            <el-input v-model="editForm.ChangeoverGroup" maxlength="50" placeholder="请输入换型分组" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="24">
                        <el-form-item label="备注" prop="Remark">
                            <el-input v-model="editForm.Remark" type="textarea" :rows="4" maxlength="500"
                                show-word-limit placeholder="请输入备注" />
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form>

            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" :loading="submitLoading" @click="handleSubmit">保存</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
import { getCurrentInstance, nextTick, onMounted, reactive, ref } from 'vue'
import { initDataSource as initDicDataSource } from '@/components/basic/VolForm/VolFormProvider.js'

const { proxy } = getCurrentInstance()

const loading = ref(false)
const submitLoading = ref(false)
const tableData = ref([])
const selectedRows = ref([])
const dialogVisible = ref(false)
const dialogMode = ref('add')
const formRef = ref()
const tableHeight = 'calc(100vh - 382px)'
const machineOptions = ref([])

const queryForm = reactive({
    workOrderNo: '',
    customerName: '',
    scheduleStatus: ''
})

const pageInfo = reactive({
    page: 1,
    rows: 20,
    total: 0,
    sort: '',
    order: ''
})

const createEditForm = () => ({
    Id: '',
    WorkOrderNo: '',
    CustomerName: '',
    CustomerPriority: 3,
    ProductCode: '',
    ProductName: '',
    OrderQty: 1,
    EarliestStartTime: '',
    LatestDeliveryDate: '',
    ProcessMinutes: 1,
    RequiredMachine: '',
    RequiredMachineId: '',
    RequiredMachineList: [],
    ChangeoverGroup: '',
    ScheduleStatus: '待排产',
    Remark: ''
})

const editForm = reactive(createEditForm())

const formRules = {
    WorkOrderNo: [{ required: true, message: '请输入工单号', trigger: 'blur' }],
    CustomerName: [{ required: true, message: '请输入客户名称', trigger: 'blur' }],
    OrderQty: [{ required: true, message: '请输入订单数量', trigger: 'change' }],
    EarliestStartTime: [{ required: true, message: '请选择最早开始时间', trigger: 'change' }],
    LatestDeliveryDate: [{ required: true, message: '请选择最晚交付日期', trigger: 'change' }],
    ProcessMinutes: [{ required: true, message: '请输入加工分钟数', trigger: 'change' }]
}

const loadMachineOptions = async () => {
    const dicData = await new Promise((resolve) => {
        initDicDataSource(
            [[{ dataKey: '生产设备', data: [] }]],
            proxy.http,
            proxy.$ts,
            Number.MAX_SAFE_INTEGER,
            true,
            (result) => resolve(result || [])
        )
    })

    const dic = Array.isArray(dicData) ? dicData.find((item) => item.dicNo === '生产设备') : null
    const rows = dic?.data || []

    machineOptions.value = rows.map((item) => ({
        label: item.value,
        value: item.key
    }))
}

const loadData = async () => {
    loading.value = true
    try {
        const result = await proxy.http.post('api/Aps_Work_Order/GetWorkOrderPageList', {
            Page: pageInfo.page,
            Rows: pageInfo.rows,
            Sort: pageInfo.sort,
            Order: pageInfo.order,
            WorkOrderNo: queryForm.workOrderNo,
            CustomerName: queryForm.customerName,
            ScheduleStatus: queryForm.scheduleStatus
        })

        tableData.value = result?.rows || []
        pageInfo.total = result?.total || 0
    } finally {
        loading.value = false
    }
}

const handleSearch = () => {
    pageInfo.page = 1
    loadData()
}

const handleReset = () => {
    queryForm.workOrderNo = ''
    queryForm.customerName = ''
    queryForm.scheduleStatus = ''
    pageInfo.page = 1
    pageInfo.sort = ''
    pageInfo.order = ''
    loadData()
}

const handleSizeChange = () => {
    pageInfo.page = 1
    loadData()
}

const handleSortChange = ({ prop, order }) => {
    pageInfo.sort = prop || ''
    pageInfo.order = order === 'descending' ? 'desc' : order === 'ascending' ? 'asc' : ''
    pageInfo.page = 1
    loadData()
}

const handleSelectionChange = (rows) => {
    selectedRows.value = rows
}

const resetEditForm = () => {
    Object.assign(editForm, createEditForm())
}

const openAddDialog = async () => {
    dialogMode.value = 'add'
    resetEditForm()
    if (!machineOptions.value.length) {
        await loadMachineOptions()
    }
    dialogVisible.value = true
    await nextTick()
    formRef.value?.clearValidate()
}

const openEditDialog = async (row) => {
    dialogMode.value = 'edit'
    Object.assign(editForm, {
        Id: row.Id || '',
        WorkOrderNo: row.WorkOrderNo || '',
        CustomerName: row.CustomerName || '',
        CustomerPriority: row.CustomerPriority ?? 3,
        ProductCode: row.ProductCode || '',
        ProductName: row.ProductName || '',
        OrderQty: row.OrderQty ?? 1,
        EarliestStartTime: row.EarliestStartTime || '',
        LatestDeliveryDate: row.LatestDeliveryDate || '',
        ProcessMinutes: row.ProcessMinutes ?? 1,
        RequiredMachine: row.RequiredMachine || '',
        RequiredMachineId: row.RequiredMachineId || '',
        RequiredMachineList: row.RequiredMachine
            ? row.RequiredMachine
                .split(',')
                .map((item) => item.trim())
                .filter((item) => !!item)
            : [],
        ChangeoverGroup: row.ChangeoverGroup || '',
        ScheduleStatus: row.ScheduleStatus || '待排产',
        Remark: row.Remark || ''
    })
    if (!machineOptions.value.length) {
        await loadMachineOptions()
    }
    dialogVisible.value = true
    await nextTick()
    formRef.value?.clearValidate()
}

const handleSubmit = async () => {
    const valid = await formRef.value?.validate().catch(() => false)
    if (!valid) {
        return
    }

    const mainData = {
        WorkOrderNo: editForm.WorkOrderNo,
        CustomerName: editForm.CustomerName,
        CustomerPriority: editForm.CustomerPriority,
        ProductCode: editForm.ProductCode,
        ProductName: editForm.ProductName,
        OrderQty: editForm.OrderQty,
        EarliestStartTime: editForm.EarliestStartTime,
        LatestDeliveryDate: editForm.LatestDeliveryDate,
        ProcessMinutes: editForm.ProcessMinutes,
        RequiredMachine: (editForm.RequiredMachineList || []).join(','),
        ChangeoverGroup: editForm.ChangeoverGroup,
        ScheduleStatus: editForm.ScheduleStatus,
        Remark: editForm.Remark
    }

    if (dialogMode.value === 'edit') {
        mainData.Id = editForm.Id
    }

    submitLoading.value = true
    try {
        const url = dialogMode.value === 'add' ? 'api/Aps_Work_Order/Add' : 'api/Aps_Work_Order/Update'
        const result = await proxy.http.post(url, {
            MainData: mainData,
            DetailData: [],
            DelKeys: []
        })

        if (!result?.status) {
            return proxy.$message.error(result?.message || '保存失败')
        }

        proxy.$message.success(result.message || '保存成功')
        dialogVisible.value = false
        await loadData()
    } finally {
        submitLoading.value = false
    }
}

const handleDelete = () => {
    if (!selectedRows.value.length) {
        return proxy.$message.error('请选择要删除的数据')
    }

    proxy
        .$confirm('确认要删除选中的数据吗？', '警告', {
            type: 'warning'
        })
        .then(async () => {
            const ids = selectedRows.value.map((item) => item.Id).filter((id) => !!id)

            if (!ids.length) {
                return proxy.$message.error('未获取到有效主键')
            }

            const result = await proxy.http.post('api/Aps_Work_Order/Del', ids)
            if (!result?.status) {
                return proxy.$message.error(result?.message || '删除失败')
            }

            proxy.$message.success(result.message || '删除成功')
            if (tableData.value.length === ids.length && pageInfo.page > 1) {
                pageInfo.page -= 1
            }
            selectedRows.value = []
            await loadData()
        })
        .catch(() => { })
}

onMounted(() => {
    loadMachineOptions()
    loadData()
})
</script>

<style scoped lang="less">
.production-scheduling-order {
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

.table-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
}

.table-actions {
    display: flex;
    gap: 12px;
}

.card-title {
    font-size: 16px;
    font-weight: 600;
    color: #303133;
}

.table-card {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;

    :deep(.el-card__body) {
        padding-bottom: 12px;
        height: 100%;
        display: flex;
        flex-direction: column;
        min-height: 0;
    }
}

.pagination-wrap {
    display: flex;
    justify-content: flex-end;
    margin-top: 12px;
    flex-shrink: 0;
}

.dialog-form {
    padding-right: 20px;
}

.query-card :deep(.el-card__body) {
    padding-bottom: 4px;
}

.query-card :deep(.el-form) {
    margin-bottom: -18px;
}
</style>