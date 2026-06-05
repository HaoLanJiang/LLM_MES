<template>
    <div class="production-equipment-management">
        <el-card shadow="never" class="query-card">
            <template #header>
                <div class="card-title">查询条件</div>
            </template>

            <el-form :model="queryForm" inline label-width="100px">
                <el-form-item label="设备编码">
                    <el-input v-model="queryForm.machineCode" clearable placeholder="请输入设备编码"
                        @keyup.enter="handleSearch" />
                </el-form-item>

                <el-form-item label="设备名称">
                    <el-input v-model="queryForm.machineName" clearable placeholder="请输入设备名称"
                        @keyup.enter="handleSearch" />
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
                    <div class="card-title">设备列表</div>
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
                <el-table-column prop="MachineCode" label="设备编码" min-width="180" sortable="custom"
                    show-overflow-tooltip />
                <el-table-column prop="MachineName" label="设备名称" min-width="220" sortable="custom"
                    show-overflow-tooltip />
                <el-table-column prop="CapacityMinutesPerDay" label="每日产能" min-width="150" sortable="custom"
                    align="right" />
                <el-table-column prop="Remark" label="备注" min-width="260" sortable="custom" show-overflow-tooltip />
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

        <el-dialog v-model="dialogVisible" :title="dialogMode === 'add' ? '新增设备' : '修改设备'" width="620px"
            destroy-on-close>
            <el-form ref="formRef" :model="editForm" :rules="formRules" label-width="130px" class="dialog-form">
                <el-form-item label="设备编码" prop="MachineCode">
                    <el-input v-model="editForm.MachineCode" maxlength="50" placeholder="请输入设备编码" />
                </el-form-item>

                <el-form-item label="设备名称" prop="MachineName">
                    <el-input v-model="editForm.MachineName" maxlength="100" placeholder="请输入设备名称" />
                </el-form-item>

                <el-form-item label="每日产能" prop="CapacityMinutesPerDay">
                    <el-input-number v-model="editForm.CapacityMinutesPerDay" :min="0" :precision="0"
                        controls-position="right" style="width: 100%" />
                </el-form-item>

                <el-form-item label="备注" prop="Remark">
                    <el-input v-model="editForm.Remark" type="textarea" :rows="4" maxlength="500" show-word-limit
                        placeholder="请输入备注" />
                </el-form-item>
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

const { proxy } = getCurrentInstance()

const loading = ref(false)
const submitLoading = ref(false)
const tableData = ref([])
const selectedRows = ref([])
const dialogVisible = ref(false)
const dialogMode = ref('add')
const formRef = ref()
const tableHeight = 'calc(100vh - 382px)'

const queryForm = reactive({
    machineCode: '',
    machineName: ''
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
    MachineCode: '',
    MachineName: '',
    CapacityMinutesPerDay: null,
    Remark: ''
})

const editForm = reactive(createEditForm())

const formRules = {
    MachineCode: [{ required: true, message: '请输入设备编码', trigger: 'blur' }],
    MachineName: [{ required: true, message: '请输入设备名称', trigger: 'blur' }]
}

const loadData = async () => {
    loading.value = true
    try {
        const result = await proxy.http.post('api/Aps_Machine/GetMachinePageList', {
            Page: pageInfo.page,
            Rows: pageInfo.rows,
            Sort: pageInfo.sort,
            Order: pageInfo.order,
            MachineCode: queryForm.machineCode,
            MachineName: queryForm.machineName
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
    queryForm.machineCode = ''
    queryForm.machineName = ''
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
    dialogVisible.value = true
    await nextTick()
    formRef.value?.clearValidate()
}

const openEditDialog = async (row) => {
    dialogMode.value = 'edit'
    Object.assign(editForm, {
        Id: row.Id || '',
        MachineCode: row.MachineCode || '',
        MachineName: row.MachineName || '',
        CapacityMinutesPerDay: row.CapacityMinutesPerDay,
        Remark: row.Remark || ''
    })
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
        MachineCode: editForm.MachineCode,
        MachineName: editForm.MachineName,
        CapacityMinutesPerDay: editForm.CapacityMinutesPerDay,
        Remark: editForm.Remark
    }

    if (dialogMode.value === 'edit') {
        mainData.Id = editForm.Id
    }

    submitLoading.value = true
    try {
        const url = dialogMode.value === 'add' ? 'api/Aps_Machine/Add' : 'api/Aps_Machine/Update'
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

            const result = await proxy.http.post('api/Aps_Machine/Del', ids)
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
    loadData()
})
</script>

<style scoped lang="less">
.production-equipment-management {
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
