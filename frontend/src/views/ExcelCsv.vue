<template>
  <div class="page-container">
    <div class="page-header">
      <h2>Excel ↔ CSV 互转</h2>
      <p>Excel 与 CSV 格式互相转换</p>
    </div>

    <el-tabs v-model="activeTab" type="border-card">
      <el-tab-pane label="Excel → CSV" name="excel-to-csv">
        <el-card shadow="never">
          <el-upload
            drag
            :auto-upload="false"
            :limit="1"
            accept=".xlsx,.xls"
            :on-change="handleExcelFileChange"
            class="upload-area"
          >
            <el-icon :size="40" class="upload-icon"><UploadFilled /></el-icon>
            <div class="el-upload__text">拖拽 Excel 文件到此处，或 <em>点击上传</em></div>
            <template #tip><div class="el-upload__tip">支持 .xlsx / .xls 格式</div></template>
          </el-upload>

          <el-form inline style="margin-top: 16px">
            <el-form-item label="分隔符">
              <el-select v-model="csvDelimiter" style="width: 120px">
                <el-option label="逗号 (,)" value="," />
                <el-option label="分号 (;)" value=";" />
                <el-option label="制表符 (Tab)" value="&#9;" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="loading" @click="excelToCsv">
                <el-icon><Switch /></el-icon> 转换为 CSV
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="CSV → Excel" name="csv-to-excel">
        <el-card shadow="never">
          <el-upload
            drag
            :auto-upload="false"
            :limit="1"
            accept=".csv"
            :on-change="handleCsvFileChange"
            class="upload-area"
          >
            <el-icon :size="40" class="upload-icon"><UploadFilled /></el-icon>
            <div class="el-upload__text">拖拽 CSV 文件到此处，或 <em>点击上传</em></div>
            <template #tip><div class="el-upload__tip">支持 .csv 格式</div></template>
          </el-upload>

          <el-form inline style="margin-top: 16px">
            <el-form-item label="分隔符">
              <el-select v-model="csvDelimiter" style="width: 120px">
                <el-option label="逗号 (,)" value="," />
                <el-option label="分号 (;)" value=";" />
                <el-option label="制表符 (Tab)" value="&#9;" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="loading" @click="csvToExcel">
                <el-icon><Switch /></el-icon> 转换为 Excel
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>
    </el-tabs>

    <el-card v-if="textResult" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>转换结果</span>
          <div>
            <el-button type="primary" size="small" @click="copyResult"><el-icon><CopyDocument /></el-icon> 复制</el-button>
            <el-button type="success" size="small" @click="downloadResult"><el-icon><Download /></el-icon> 下载</el-button>
          </div>
        </div>
      </template>
      <pre class="code-block">{{ textResult }}</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { UploadFile } from 'element-plus'
import { api } from '@/api'

const activeTab = ref('excel-to-csv')
const loading = ref(false)
const textResult = ref('')
const resultFileName = ref('')
const csvDelimiter = ref(',')
const excelFile = ref<File | null>(null)
const csvFile = ref<File | null>(null)

const handleExcelFileChange = (file: UploadFile) => { excelFile.value = file.raw || null }
const handleCsvFileChange = (file: UploadFile) => { csvFile.value = file.raw || null }

const excelToCsv = async () => {
  if (!excelFile.value) { ElMessage.warning('请先上传 Excel 文件'); return }
  loading.value = true; textResult.value = ''
  try {
    const formData = new FormData()
    formData.append('file', excelFile.value)
    formData.append('delimiter', csvDelimiter.value)
    const res = await api.convert.excelToCsv(formData)
    if (res.success) {
      textResult.value = res.data.content
      resultFileName.value = res.data.fileName
      ElMessage.success('转换成功')
    } else {
      ElMessage.error(res.message || '转换失败')
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data || '请求失败')
  }
  finally { loading.value = false }
}

const csvToExcel = async () => {
  if (!csvFile.value) { ElMessage.warning('请先上传 CSV 文件'); return }
  loading.value = true; textResult.value = ''
  try {
    const formData = new FormData()
    formData.append('file', csvFile.value)
    formData.append('delimiter', csvDelimiter.value)
    const res = await api.convert.csvToExcel(formData)
    if (res.success) {
      const bytes = atob(res.data.content)
      const arr = new Uint8Array(bytes.length)
      for (let i = 0; i < bytes.length; i++) arr[i] = bytes.charCodeAt(i)
      const blob = new Blob([arr], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url; link.download = res.data.fileName; link.click()
      URL.revokeObjectURL(url)
      ElMessage.success('转换成功')
    } else {
      ElMessage.error(res.message || '转换失败')
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data || '请求失败')
  }
  finally { loading.value = false }
}

const copyResult = () => {
  navigator.clipboard.writeText(textResult.value)
  ElMessage.success('已复制到剪贴板')
}

const downloadResult = () => {
  const blob = new Blob([textResult.value], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url; link.download = resultFileName.value; link.click()
  URL.revokeObjectURL(url)
}
</script>

<style scoped>
.page-container { max-width: 900px; margin: 0 auto; }
.page-header { margin-bottom: 24px; }
.page-header h2 { font-size: 24px; color: #303133; margin-bottom: 8px; }
.page-header p { color: #909399; font-size: 14px; }
.upload-area { width: 100%; }
.upload-icon { color: #c0c4cc; margin-bottom: 8px; }
.result-card { margin-top: 20px; }
.result-header { display: flex; justify-content: space-between; align-items: center; }
.code-block {
  background: #1e1e2e; color: #cdd6f4; padding: 16px; border-radius: 8px;
  font-family: 'Cascadia Code', 'Fira Code', monospace; font-size: 13px;
  line-height: 1.6; overflow-x: auto; white-space: pre-wrap; word-break: break-all;
  max-height: 500px; overflow-y: auto;
}
</style>
