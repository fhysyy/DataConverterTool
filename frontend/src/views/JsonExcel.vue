<template>
  <div class="page-container">
    <div class="page-header">
      <h2>JSON ↔ Excel 互转</h2>
      <p>JSON 数据与 Excel 文件互相转换</p>
    </div>

    <el-tabs v-model="activeTab" type="border-card">
      <el-tab-pane label="JSON → Excel" name="json-to-excel">
        <el-card shadow="never">
          <el-form inline style="margin-bottom: 16px">
            <el-form-item label="文件名">
              <el-input v-model="fileName" placeholder="export" style="width: 180px" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="loading" @click="jsonToExcel">
                <el-icon><Download /></el-icon> 导出 Excel
              </el-button>
            </el-form-item>
          </el-form>
          <el-input
            v-model="jsonInput"
            type="textarea"
            :rows="14"
            placeholder='请输入 JSON 数组，例如：
[
  {"name": "张三", "age": 25, "city": "北京"},
  {"name": "李四", "age": 30, "city": "上海"}
]'
            class="json-input"
          />
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="Excel → JSON" name="excel-to-json">
        <el-card shadow="never">
          <el-upload
            drag
            :auto-upload="false"
            :limit="1"
            accept=".xlsx,.xls"
            :on-change="handleFileChange"
            class="upload-area"
          >
            <el-icon :size="40" class="upload-icon"><UploadFilled /></el-icon>
            <div class="el-upload__text">拖拽 Excel 文件到此处，或 <em>点击上传</em></div>
            <template #tip><div class="el-upload__tip">支持 .xlsx / .xls 格式</div></template>
          </el-upload>
          <el-button type="primary" :loading="loading" @click="excelToJson" style="margin-top: 16px">
            <el-icon><Switch /></el-icon> 转换为 JSON
          </el-button>
        </el-card>
      </el-tab-pane>
    </el-tabs>

    <el-card v-if="jsonResult" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>JSON 结果</span>
          <div>
            <el-button type="primary" size="small" @click="copyResult"><el-icon><CopyDocument /></el-icon> 复制</el-button>
            <el-button type="success" size="small" @click="downloadJson"><el-icon><Download /></el-icon> 下载</el-button>
          </div>
        </div>
      </template>
      <pre class="code-block">{{ jsonResult }}</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { UploadFile } from 'element-plus'
import axios from 'axios'

const activeTab = ref('json-to-excel')
const loading = ref(false)
const jsonResult = ref('')
const fileName = ref('export')
const selectedFile = ref<File | null>(null)

const jsonInput = ref(`[
  {"name": "张三", "age": 25, "city": "北京"},
  {"name": "李四", "age": 30, "city": "上海"},
  {"name": "王五", "age": 28, "city": "广州"}
]`)

const handleFileChange = (file: UploadFile) => { selectedFile.value = file.raw || null }

const jsonToExcel = async () => {
  if (!jsonInput.value.trim()) { ElMessage.warning('请输入 JSON 内容'); return }
  loading.value = true
  try {
    const res = await axios.post('/api/convert/json-to-excel', {
      json: jsonInput.value,
      fileName: fileName.value
    })
    if (res.data.success) {
      const bytes = atob(res.data.data.content)
      const arr = new Uint8Array(bytes.length)
      for (let i = 0; i < bytes.length; i++) arr[i] = bytes.charCodeAt(i)
      const blob = new Blob([arr], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url; link.download = res.data.data.fileName; link.click()
      URL.revokeObjectURL(url)
      ElMessage.success('导出成功')
    } else { ElMessage.error(res.data.message) }
  } catch (err: any) { ElMessage.error(err.response?.data || '请求失败') }
  finally { loading.value = false }
}

const excelToJson = async () => {
  if (!selectedFile.value) { ElMessage.warning('请先上传 Excel 文件'); return }
  loading.value = true; jsonResult.value = ''
  try {
    const formData = new FormData()
    formData.append('file', selectedFile.value)
    const res = await axios.post('/api/convert/excel-to-json', formData, { headers: { 'Content-Type': 'multipart/form-data' } })
    if (res.data.success) {
      jsonResult.value = res.data.data.content
      ElMessage.success('转换成功')
    } else { ElMessage.error(res.data.message) }
  } catch (err: any) { ElMessage.error(err.response?.data || '请求失败') }
  finally { loading.value = false }
}

const copyResult = () => {
  navigator.clipboard.writeText(jsonResult.value)
  ElMessage.success('已复制到剪贴板')
}

const downloadJson = () => {
  const blob = new Blob([jsonResult.value], { type: 'application/json;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url; link.download = 'data.json'; link.click()
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
.json-input :deep(.el-textarea__inner) {
  font-family: 'Cascadia Code', 'Fira Code', monospace; font-size: 13px; line-height: 1.6;
}
.result-card { margin-top: 20px; }
.result-header { display: flex; justify-content: space-between; align-items: center; }
.code-block {
  background: #1e1e2e; color: #cdd6f4; padding: 16px; border-radius: 8px;
  font-family: 'Cascadia Code', 'Fira Code', monospace; font-size: 13px;
  line-height: 1.6; overflow-x: auto; white-space: pre-wrap; word-break: break-all;
  max-height: 500px; overflow-y: auto;
}
</style>
