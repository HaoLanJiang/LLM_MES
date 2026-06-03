<template>
    <div class="production-scheduling-shifts">
        <el-card shadow="never" class="query-card">
            <template #header>
                <div class="card-title">查询条件</div>
            </template>

            <el-form :model="queryForm" inline label-width="100px">
                <el-form-item label="班次编码">
                    <el-input v-model="queryForm.shift_code" clearable placeholder="请输入班次编码"
                        @keyup.enter="handleSearch" />
                </el-form-item>

                <el-form-item label="班次名称">
                    <el-input v-model="queryForm.shift_name" clearable placeholder="请输入班次名称"
                        @keyup.enter="handleSearch" />
                </el-form-item>

                <el-form-item label="启用状态">
                    <el-select v-model="queryForm.enable_flag" clearable placeholder="请选择启用状态" style="width: 180px">
                        <el-option label="启用" :value="1" />
                        <el-option label="停用" :value="0" />
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
                    <div class="card-title">班次列表</div>
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
                <el-table-column prop="shift_code" label="班次编码" min-width="140" sortable="custom"
                    show-overflow-tooltip />
                <el-table-column prop="shift_name" label="班次名称" min-width="160" sortable="custom"
                    show-overflow-tooltip />
                <el-table-column prop="shift_type" label="班次类型" min-width="100" align="center">
                    <template #default="{ row }">
                        {{ row.shift_type === 1 ? '夜班' : '白班' }}
                    </template>
                </el-table-column>
                <el-table-column prop="start_time" label="开始时间" min-width="110" />
                <el-table-column prop="end_time" label="结束时间" min-width="110" />
                <el-table-column prop="cross_day_flag" label="是否跨天" min-width="100" align="center">
                    <template #default="{ row }">
                        {{ row.cross_day_flag === 1 ? '是' : '否' }}
                    </template>
                </el-table-column>
                <el-table-column prop="work_minutes" label="工作分钟数" min-width="110" sortable="custom"
                    align="right" />
                <el-table-column prop="enable_flag" label="启用状态" min-width="100" sortable="custom" align="center">
                    <template #default="{ row }">
                        {{ row.enable_flag === 1 ? '启用' : '停用' }}
                    </template>
                </el-table-column>
                <el-table-column prop="remark" label="备注" min-width="220" show-overflow-tooltip />
                <el-table-column prop="create_time" label="创建时间" min-width="170" sortable="custom" />
                <el-table-column prop="update_time" label="更新时间" min-width="170" />
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

        <el-dialog v-model="dialogVisible" :title="dialogMode === 'add' ? '新增班次' : '修改班次'" width="760px"
            destroy-on-close>
            <el-form ref="formRef" :model="editForm" :rules="formRules" label-width="120px" class="dialog-form">
                <el-row :gutter="16">
                    <el-col :span="12">
                        <el-form-item label="班次编码" prop="shift_code">
                            <el-input v-model="editForm.shift_code" maxlength="50" placeholder="请输入班次编码" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="班次名称" prop="shift_name">
                            <el-input v-model="editForm.shift_name" maxlength="100" placeholder="请输入班次名称" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="班次类型" prop="shift_type">
                            <el-select v-model="editForm.shift_type" placeholder="请选择班次类型" style="width: 100%">
                                <el-option label="白班" :value="0" />
                                <el-option label="夜班" :value="1" />
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="启用状态" prop="enable_flag">
                            <el-select v-model="editForm.enable_flag" placeholder="请选择启用状态" style="width: 100%">
                                <el-option label="启用" :value="1" />
                                <el-option label="停用" :value="0" />
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="开始时间" prop="start_time">
                            <el-time-picker v-model="editForm.start_time" value-format="HH:mm:ss" format="HH:mm:ss"
                                placeholder="请选择开始时间" style="width: 100%" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="结束时间" prop="end_time">
                            <el-time-picker v-model="editForm.end_time" value-format="HH:mm:ss" format="HH:mm:ss"
                                placeholder="请选择结束时间" style="width: 100%" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="是否跨天" prop="cross_day_flag">
                            <el-select v-model="editForm.cross_day_flag" placeholder="请选择是否跨天" style="width: 100%">
                                <el-option label="否" :value="0" />
                                <el-option label="是" :value="1" />
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="工作分钟数" prop="work_minutes">
                            <el-input-number v-model="editForm.work_minutes" :min="1" :precision="0"
                                controls-position="right" style="width: 100%" />
                        </el-form-item>
                    </el-col>
                    <el-col :span="24">
                        <el-form-item label="备注" prop="remark">
                            <el-input v-model="editForm.remark" type="textarea" :rows="4" maxlength="500"
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
    shift_code: '',
    shift_name: '',
    enable_flag: null
})

const pageInfo = reactive({
    page: 1,
    rows: 20,
    total: 0,
    sort: '',
    order: ''
})

const createEditForm = () => ({
    id: null,
    shift_code: '',
    shift_name: '',
    shift_type: 0,
    start_time: '',
    end_time: '',
    cross_day_flag: 0,
    work_minutes: 480,
    enable_flag: 1,
    remark: ''
})

const editForm = reactive(createEditForm())

const formRules = {
    shift_code: [{ required: true, message: '请输入班次编码', trigger: 'blur' }],
    shift_name: [{ required: true, message: '请输入班次名称', trigger: 'blur' }],
    shift_type: [{ required: true, message: '请选择班次类型', trigger: 'change' }],
    start_time: [{ required: true, message: '请选择开始时间', trigger: 'change' }],
    end_time: [{ required: true, message: '请选择结束时间', trigger: 'change' }],
    cross_day_flag: [{ required: true, message: '请选择是否跨天', trigger: 'change' }],
    work_minutes: [{ required: true, message: '请输入工作分钟数', trigger: 'change' }],
    enable_flag: [{ required: true, message: '请选择启用状态', trigger: 'change' }]
}

const loadData = async () => {
    loading.value = true
    try {
        const result = await proxy.http.post('api/Aps_Shift/GetShiftPageList', {
            Page: pageInfo.page,
            Rows: pageInfo.rows,
            Sort: pageInfo.sort,
            Order: pageInfo.order,
            shift_code: queryForm.shift_code,
            shift_name: queryForm.shift_name,
            enable_flag: queryForm.enable_flag
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
    queryForm.shift_code = ''
    queryForm.shift_name = ''
    queryForm.enable_flag = null
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
        id: row.id ?? null,
        shift_code: row.shift_code || '',
        shift_name: row.shift_name || '',
        shift_type: row.shift_type ?? 0,
        start_time: row.start_time || '',
        end_time: row.end_time || '',
        cross_day_flag: row.cross_day_flag ?? 0,
        work_minutes: row.work_minutes ?? 480,
        enable_flag: row.enable_flag ?? 1,
        remark: row.remark || ''
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
        shift_code: editForm.shift_code,
        shift_name: editForm.shift_name,
        shift_type: editForm.shift_type,
        start_time: editForm.start_time,
        end_time: editForm.end_time,
        cross_day_flag: editForm.cross_day_flag,
        work_minutes: editForm.work_minutes,
        enable_flag: editForm.enable_flag,
        remark: editForm.remark
    }

    if (dialogMode.value === 'edit') {
        mainData.id = editForm.id
    }

    submitLoading.value = true
    try {
        const url = dialogMode.value === 'add' ? 'api/Aps_Shift/Add' : 'api/Aps_Shift/Update'
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
            const ids = selectedRows.value.map((item) => item.id).filter((id) => !!id)

            if (!ids.length) {
                return proxy.$message.error('未获取到有效主键')
            }

            const result = await proxy.http.post('api/Aps_Shift/Del', ids)
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
.production-scheduling-shifts {
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
