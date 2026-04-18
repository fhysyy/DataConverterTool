<template>
  <div class="page-container">
    <div class="page-header">
      <h2>Excel → SQL</h2>
      <p>上传 Excel 文件，自动生成 SQL 建表和插入语句</p>
    </div>

    <el-card shadow="never" class="upload-card">
      <el-form :model="form" label-width="100px" inline>
        <el-form-item label="数据库类型">
          <el-select v-model="form.databaseType" style="width: 160px">
            <el-option label="SQL Server" :value="0" />
            <el-option label="MySQL" :value="1" />
            <el-option label="PostgreSQL" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="表名">
          <el-input v-model="form.tableName" placeholder="请输入数据库表名" style="width: 200px" />
        </el-form-item>
        <el-form-item label="SQL类型">
          <el-select v-model="form.sqlType" style="width: 140px">
            <el-option label="INSERT" :value="0" />
            <el-option label="UPDATE" :value="1" />
            <el-option label="CREATE" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="建表语句">
          <el-switch v-model="form.includeCreateTable" />
        </el-form-item>
      </el-form>

      <el-upload
        drag
        :auto-upload="false"
        :limit="1"
        accept=".xlsx,.xls"
        :on-change="handleFileChange"
        class="upload-area"
      >
        <el-icon :size="48" class="upload-icon"><UploadFilled /></el-icon>
        <div class="el-upload__text">拖拽文件到此处，或 <em>点击上传</em></div>
        <template #tip>
          <div class="el-upload__tip">仅支持 .xlsx / .xls 格式文件</div>
        </template>
      </el-upload>

      <el-button type="primary" :loading="loading" @click="convert" style="margin-top: 16px">
        <el-icon><Switch /></el-icon>
        开始转换
      </el-button>
    </el-card>

    <el-card v-if="result" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>转换结果</span>
          <el-button type="primary" size="small" @click="copyResult">
            <el-icon><CopyDocument /></el-icon>
            复制
          </el-button>
        </div>
      </template>
      <div v-if="result.data?.createTableSql" class="sql-section">
        <h4>建表语句</h4>
        <pre class="code-block">{{ result.data.createTableSql }}</pre>
      </div>
      <div v-if="result.data?.dataSqlList?.length" class="sql-section">
        <h4>数据语句 ({{ result.data.dataSqlList.length }} 条)</h4>
        <pre class="code-block">{{ result.data.dataSqlList.join('\n') }}</pre>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import type { UploadFile } from 'element-plus'
import { api } from '@/api'

const loading = ref(false)
const result = ref<any>(null)
const selectedFile = ref<File | null>(null)

const form = reactive({
  tableName: 'MyTable',
  sqlType: 0,
  includeCreateTable: true,
  databaseType: 0,
})

const handleFileChange = (file: UploadFile) => {
  selectedFile.value = file.raw || null
}

const convert = async () => {
  if (!selectedFile.value) {
    ElMessage.warning('请先上传 Excel 文件')
    return
  }

  loading.value = true
  result.value = null

  try {
    const formData = new FormData()
    formData.append('file', selectedFile.value)
    formData.append('tableName', form.tableName)
    formData.append('sqlType', String(form.sqlType))
    formData.append('includeCreateTable', String(form.includeCreateTable))
    formData.append('databaseType', String(form.databaseType))

    const res = await api.convert.excelToSql(formData)

    if (res.success) {
      result.value = res.data
      ElMessage.success('转换成功')
    } else {
      ElMessage.error(res.message || '转换失败')
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data?.message || '请求失败')
  } finally {
    loading.value = false
  }
}

const copyResult = () => {
  if (!result.value?.data) return
  const text = [
    result.value.data.createTableSql || '',
    ...(result.value.data.dataSqlList || [])
  ].filter(Boolean).join('\n')

  navigator.clipboard.writeText(text)
  ElMessage.success('已复制到剪贴板')
}
</script>

<style scoped>
.page-container {
  max-width: 900px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 24px;
}

.page-header h2 {
  font-size: 24px;
  color: #303133;
  margin-bottom: 8px;
}

.page-header p {
  color: #909399;
  font-size: 14px;
}

.upload-card {
  margin-bottom: 20px;
}

.upload-area {
  width: 100%;
}

.upload-icon {
  color: #c0c4cc;
  margin-bottom: 8px;
}

.result-card {
  margin-top: 20px;
}

.result-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.sql-section {
  margin-bottom: 16px;
}

.sql-section h4 {
  color: #606266;
  margin-bottom: 8px;
  font-size: 14px;
}

.code-block {
  background: #1e1e2e;
  color: #cdd6f4;
  padding: 16px;
  border-radius: 8px;
  font-family: 'Cascadia Code', 'Fira Code', monospace;
  font-size: 13px;
  line-height: 1.6;
  overflow-x: auto;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
