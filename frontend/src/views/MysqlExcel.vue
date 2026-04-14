<template>
  <div class="page-container">
    <div class="page-header">
      <h2>MySQL ↔ Excel 互转</h2>
      <p>MySQL 数据库与 Excel 文件互相导入导出</p>
    </div>

    <el-tabs v-model="activeTab" type="border-card">
      <el-tab-pane label="MySQL → Excel" name="mysql-to-excel">
        <el-card shadow="never">
          <el-form :model="mysqlForm" label-width="100px">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="主机">
                  <el-input v-model="mysqlForm.host" placeholder="localhost" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="端口">
                  <el-input-number v-model="mysqlForm.port" :min="1" :max="65535" style="width: 100%" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="数据库">
                  <el-input v-model="mysqlForm.database" placeholder="mydb" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="用户名">
                  <el-input v-model="mysqlForm.username" placeholder="root" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-form-item label="密码">
              <el-input v-model="mysqlForm.password" type="password" show-password placeholder="密码" />
            </el-form-item>
            <el-form-item label="SQL查询">
              <el-input
                v-model="mysqlForm.query"
                type="textarea"
                :rows="4"
                placeholder="SELECT * FROM users LIMIT 100"
              />
            </el-form-item>
            <el-form-item label="文件名">
              <el-input v-model="mysqlForm.fileName" placeholder="mysql_export" style="width: 250px" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="previewMysql" :loading="previewLoading">
                <el-icon><View /></el-icon> 预览数据
              </el-button>
              <el-button type="success" @click="exportMysqlToExcel" :loading="exportLoading">
                <el-icon><Download /></el-icon> 导出 Excel
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>

        <el-card v-if="previewData" shadow="never" class="result-card">
          <template #header>
            <span>预览结果 ({{ previewData.totalRows }} 行)</span>
          </template>
          <el-table :data="previewData.rows" border stripe size="small" max-height="400">
            <el-table-column v-for="col in previewData.columns" :key="col" :prop="col" :label="col" min-width="120" />
          </el-table>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="Excel → MySQL" name="excel-to-mysql">
        <el-card shadow="never">
          <el-form :model="importForm" label-width="100px">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="主机">
                  <el-input v-model="importForm.host" placeholder="localhost" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="端口">
                  <el-input-number v-model="importForm.port" :min="1" :max="65535" style="width: 100%" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="数据库">
                  <el-input v-model="importForm.database" placeholder="mydb" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="用户名">
                  <el-input v-model="importForm.username" placeholder="root" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-form-item label="密码">
              <el-input v-model="importForm.password" type="password" show-password placeholder="密码" />
            </el-form-item>
            <el-form-item label="表名">
              <el-input v-model="importForm.tableName" placeholder="imported_table" style="width: 250px" />
            </el-form-item>
            <el-form-item label="自动建表">
              <el-switch v-model="importForm.createIfNotExists" />
            </el-form-item>
            <el-form-item label="Excel文件">
              <el-upload
                :auto-upload="false"
                :limit="1"
                accept=".xlsx,.xls"
                :on-change="handleImportFileChange"
              >
                <el-button type="primary"><el-icon><Upload /></el-icon> 选择文件</el-button>
              </el-upload>
            </el-form-item>
            <el-form-item>
              <el-button type="success" @click="importExcelToMysql" :loading="importLoading">
                <el-icon><Upload /></el-icon> 导入到 MySQL
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import type { UploadFile } from 'element-plus'
import axios from 'axios'

const activeTab = ref('mysql-to-excel')
const previewLoading = ref(false)
const exportLoading = ref(false)
const importLoading = ref(false)
const previewData = ref<any>(null)
const importFile = ref<File | null>(null)

const mysqlForm = reactive({
  host: 'localhost', port: 3306, database: '', username: 'root', password: '',
  query: 'SELECT * FROM users LIMIT 100', fileName: 'mysql_export'
})

const importForm = reactive({
  host: 'localhost', port: 3306, database: '', username: 'root', password: '',
  tableName: 'imported_table', createIfNotExists: true
})

const handleImportFileChange = (file: UploadFile) => { importFile.value = file.raw || null }

const previewMysql = async () => {
  previewLoading.value = true
  try {
    const res = await axios.post('/api/convert/mysql-preview', mysqlForm)
    if (res.data.success) {
      previewData.value = res.data.data
      ElMessage.success(`查询到 ${res.data.data.totalRows} 条数据`)
    } else { ElMessage.error(res.data.message) }
  } catch (err: any) { ElMessage.error(err.response?.data || '连接失败') }
  finally { previewLoading.value = false }
}

const exportMysqlToExcel = async () => {
  exportLoading.value = true
  try {
    const res = await axios.post('/api/convert/mysql-to-excel', mysqlForm, { responseType: 'blob' })
    const blob = new Blob([res.data], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url; link.download = `${mysqlForm.fileName}.xlsx`; link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (err: any) { ElMessage.error('导出失败，请检查连接信息') }
  finally { exportLoading.value = false }
}

const importExcelToMysql = async () => {
  if (!importFile.value) { ElMessage.warning('请先选择 Excel 文件'); return }
  importLoading.value = true
  try {
    const formData = new FormData()
    formData.append('file', importFile.value)
    formData.append('host', importForm.host)
    formData.append('port', String(importForm.port))
    formData.append('database', importForm.database)
    formData.append('username', importForm.username)
    formData.append('password', importForm.password)
    formData.append('tableName', importForm.tableName)
    formData.append('createIfNotExists', String(importForm.createIfNotExists))
    const res = await axios.post('/api/convert/excel-to-mysql', formData, { headers: { 'Content-Type': 'multipart/form-data' } })
    if (res.data.success) { ElMessage.success(res.data.data) }
    else { ElMessage.error(res.data.message) }
  } catch (err: any) { ElMessage.error(err.response?.data || '导入失败') }
  finally { importLoading.value = false }
}
</script>

<style scoped>
.page-container { max-width: 1000px; margin: 0 auto; }
.page-header { margin-bottom: 24px; }
.page-header h2 { font-size: 24px; color: #303133; margin-bottom: 8px; }
.page-header p { color: #909399; font-size: 14px; }
.result-card { margin-top: 20px; }
</style>
