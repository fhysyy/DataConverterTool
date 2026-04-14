<template>
  <div class="page-container">
    <div class="page-header">
      <h2>数据库 → Excel</h2>
      <p>从数据库查询或粘贴数据导出为 Excel 文件，支持 MySQL、SQL Server、PostgreSQL</p>
    </div>

    <el-card shadow="never" class="config-card">
      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <el-tab-pane label="数据库连接" name="database">
          <el-form :model="dbForm" label-width="100px" inline>
            <el-form-item label="数据库类型">
              <el-select v-model="dbForm.databaseType" style="width: 160px" @change="handleDbTypeChange">
                <el-option label="MySQL" :value="1" />
                <el-option label="SQL Server" :value="0" />
                <el-option label="PostgreSQL" :value="2" />
              </el-select>
            </el-form-item>
            <el-form-item label="主机">
              <el-input v-model="dbForm.host" placeholder="localhost" style="width: 180px" />
            </el-form-item>
            <el-form-item label="端口">
              <el-input-number v-model="dbForm.port" :min="1" :max="65535" style="width: 120px" />
            </el-form-item>
            <el-form-item label="数据库">
              <el-input v-model="dbForm.database" placeholder="数据库名" style="width: 180px" />
            </el-form-item>
            <el-form-item label="用户名">
              <el-input v-model="dbForm.username" placeholder="root" style="width: 150px" />
            </el-form-item>
            <el-form-item label="密码">
              <el-input v-model="dbForm.password" type="password" placeholder="密码" show-password style="width: 150px" />
            </el-form-item>
          </el-form>

          <el-divider />

          <el-form label-width="100px">
            <el-form-item label="查询语句">
              <el-input
                v-model="dbForm.query"
                type="textarea"
                :rows="4"
                placeholder="SELECT * FROM users WHERE status = 1"
                style="font-family: 'Cascadia Code', monospace"
              />
            </el-form-item>
            <el-form-item label="文件名">
              <el-input v-model="dbForm.fileName" placeholder="export" style="width: 200px" />
            </el-form-item>
          </el-form>

          <div class="action-buttons">
            <el-button type="primary" @click="loadTables" :loading="loading">加载表列表</el-button>
            <el-button type="success" @click="previewData" :loading="loading">预览数据</el-button>
            <el-button type="warning" @click="downloadExcel" :loading="downloading">导出 Excel</el-button>
          </div>

          <div v-if="tables.length > 0" class="tables-section">
            <el-divider />
            <h4>表列表</h4>
            <el-tag
              v-for="table in tables"
              :key="table"
              class="table-tag"
              @click="selectTable(table)"
              style="cursor: pointer"
            >
              {{ table }}
            </el-tag>
          </div>
        </el-tab-pane>

        <el-tab-pane label="粘贴数据" name="paste">
          <el-form :model="pasteForm" label-width="100px">
            <el-form-item label="数据分隔符">
              <el-select v-model="pasteForm.delimiter" style="width: 150px">
                <el-option label="制表符 (Tab)" value="\t" />
                <el-option label="逗号 (,)" value="," />
                <el-option label="分号 (;)" value=";" />
                <el-option label="竖线 (|)" value="|" />
              </el-select>
            </el-form-item>
            <el-form-item label="包含表头">
              <el-switch v-model="pasteForm.hasHeader" />
            </el-form-item>
            <el-form-item label="文件名">
              <el-input v-model="pasteForm.fileName" placeholder="export" style="width: 200px" />
            </el-form-item>
            <el-form-item label="粘贴数据">
              <el-input
                v-model="pasteForm.data"
                type="textarea"
                :rows="12"
                placeholder="从 Excel 或其他来源复制数据并粘贴到这里&#10;例如：&#10;ID	Name	Age	Email&#10;1	Zhang	28	zhang@example.com&#10;2	Li	35	li@example.com"
                style="font-family: 'Cascadia Code', monospace"
              />
            </el-form-item>
          </el-form>

          <div class="action-buttons">
            <el-button type="primary" @click="previewPasteData" :loading="loading">预览数据</el-button>
            <el-button type="success" @click="downloadPasteExcel" :loading="downloading">导出 Excel</el-button>
            <el-button @click="loadPasteSample">加载示例</el-button>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-card v-if="previewDataResult" shadow="never" class="preview-card">
      <template #header>
        <div class="preview-header">
          <span>数据预览 (共 {{ previewDataResult.totalRows }} 行)</span>
          <el-button size="small" @click="previewDataResult = null">关闭</el-button>
        </div>
      </template>
      <el-table :data="previewDataResult.rows.slice(0, 100)" stripe border max-height="400">
        <el-table-column
          v-for="col in previewDataResult.columns"
          :key="col"
          :prop="col"
          :label="col"
          min-width="120"
        />
      </el-table>
      <div v-if="previewDataResult.totalRows > 100" class="preview-footer">
        仅显示前 100 行，共 {{ previewDataResult.totalRows }} 行
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'

const activeTab = ref('database')
const loading = ref(false)
const downloading = ref(false)
const tables = ref<string[]>([])
const previewDataResult = ref<any>(null)

const dbForm = reactive({
  databaseType: 1,
  host: 'localhost',
  port: 3306,
  database: '',
  username: 'root',
  password: '',
  query: '',
  fileName: 'export'
})

const pasteForm = reactive({
  data: '',
  delimiter: '\t',
  hasHeader: true,
  fileName: 'export'
})

const handleTabChange = () => {
  previewDataResult.value = null
}

const handleDbTypeChange = () => {
  if (dbForm.databaseType === 1) {
    dbForm.port = 3306
    dbForm.username = 'root'
  } else if (dbForm.databaseType === 0) {
    dbForm.port = 1433
    dbForm.username = 'sa'
  } else if (dbForm.databaseType === 2) {
    dbForm.port = 5432
    dbForm.username = 'postgres'
  }
}

const loadTables = async () => {
  loading.value = true
  try {
    const { data } = await axios.post('http://localhost:5077/api/convert/database-tables', dbForm)
    if (data.success) {
      tables.value = data.data
      ElMessage.success(`加载了 ${tables.value.length} 个表`)
    } else {
      ElMessage.error(data.message || '加载表列表失败')
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '加载表列表失败')
  } finally {
    loading.value = false
  }
}

const selectTable = (tableName: string) => {
  dbForm.query = `SELECT * FROM ${tableName}`
}

const previewData = async () => {
  if (!dbForm.query.trim()) {
    ElMessage.warning('请输入查询语句')
    return
  }
  loading.value = true
  try {
    const { data } = await axios.post('http://localhost:5077/api/convert/database-preview', dbForm)
    if (data.success) {
      previewDataResult.value = data.data
      ElMessage.success(`查询成功，共 ${data.data.totalRows} 行`)
    } else {
      ElMessage.error(data.message || '查询失败')
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '查询失败')
  } finally {
    loading.value = false
  }
}

const downloadExcel = async () => {
  if (!dbForm.query.trim()) {
    ElMessage.warning('请输入查询语句')
    return
  }
  downloading.value = true
  try {
    const { data } = await axios.post('http://localhost:5077/api/convert/database-to-excel', dbForm, {
      responseType: 'blob'
    })
    const url = window.URL.createObjectURL(new Blob([data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', `${dbForm.fileName}.xlsx`)
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error: any) {
    ElMessage.error(error.response?.data || '导出失败')
  } finally {
    downloading.value = false
  }
}

const previewPasteData = async () => {
  if (!pasteForm.data.trim()) {
    ElMessage.warning('请粘贴数据')
    return
  }
  loading.value = true
  try {
    const { data } = await axios.post('http://localhost:5077/api/convert/paste-data-preview', pasteForm)
    if (data.success) {
      previewDataResult.value = data.data
      ElMessage.success(`解析成功，共 ${data.data.totalRows} 行`)
    } else {
      ElMessage.error(data.message || '解析失败')
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '解析失败')
  } finally {
    loading.value = false
  }
}

const downloadPasteExcel = async () => {
  if (!pasteForm.data.trim()) {
    ElMessage.warning('请粘贴数据')
    return
  }
  downloading.value = true
  try {
    const { data } = await axios.post('http://localhost:5077/api/convert/paste-data-to-excel', pasteForm, {
      responseType: 'blob'
    })
    const url = window.URL.createObjectURL(new Blob([data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', `${pasteForm.fileName}.xlsx`)
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error: any) {
    ElMessage.error(error.response?.data || '导出失败')
  } finally {
    downloading.value = false
  }
}

const loadPasteSample = () => {
  pasteForm.data = `ID	Name	Age	Email	City	Salary	IsActive
1	Zhang	28	zhang@example.com	Beijing	15000.5	true
2	Li	35	li@example.com	Shanghai	22000.0	false
3	Wang	42	wang@example.com	Shenzhen	28000.0	true
4	Zhao	31	zhao@example.com	Guangzhou	19500.5	false
5	Liu	27	liu@example.com	Chengdu	17500.0	true`
}
</script>

<style scoped>
.page-container {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 20px;
}

.page-header h2 {
  margin: 0 0 8px 0;
  font-size: 24px;
  color: #1e1e2e;
}

.page-header p {
  margin: 0;
  color: #6c7086;
  font-size: 14px;
}

.config-card {
  margin-bottom: 20px;
}

.action-buttons {
  display: flex;
  gap: 12px;
  margin-top: 16px;
}

.tables-section {
  margin-top: 16px;
}

.tables-section h4 {
  margin: 0 0 12px 0;
  font-size: 14px;
  color: #1e1e2e;
}

.table-tag {
  margin-right: 8px;
  margin-bottom: 8px;
}

.preview-card {
  margin-top: 20px;
}

.preview-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.preview-footer {
  text-align: center;
  padding: 12px;
  color: #6c7086;
  font-size: 13px;
}
</style>
