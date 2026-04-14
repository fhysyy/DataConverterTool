<template>
  <div class="page-container">
    <div class="page-header">
      <h2>数据格式转换</h2>
      <p>支持 XML、YAML、Markdown 格式转换和数据清洗</p>
    </div>

    <el-card shadow="never" class="config-card">
      <el-tabs v-model="activeTab">
        <el-tab-pane label="XML 转换" name="xml">
          <el-form :model="xmlForm" label-width="100px">
            <el-form-item label="操作类型">
              <el-select v-model="xmlForm.operation" style="width: 200px">
                <el-option label="XML → Excel" value="xml-to-excel" />
                <el-option label="Excel → XML" value="excel-to-xml" />
                <el-option label="XML → JSON" value="xml-to-json" />
                <el-option label="JSON → XML" value="json-to-xml" />
              </el-select>
            </el-form-item>
            
            <el-form-item v-if="xmlForm.operation !== 'excel-to-xml'" label="内容">
              <el-input
                v-model="xmlForm.content"
                type="textarea"
                :rows="8"
                placeholder="输入 XML 或 JSON 内容"
                style="font-family: 'Cascadia Code', monospace"
              />
            </el-form-item>
            
            <el-form-item v-if="xmlForm.operation === 'excel-to-xml'" label="上传文件">
              <el-upload
                class="upload-demo"
                action=""
                :auto-upload="false"
                :on-change="handleExcelUpload"
                :limit="1"
                accept=".xlsx,.xls"
              >
                <el-button type="primary">选择 Excel 文件</el-button>
                <template #tip>
                  <div class="el-upload__tip">只支持 .xlsx, .xls 文件</div>
                </template>
              </el-upload>
            </el-form-item>
            
            <el-form-item v-if="xmlForm.operation === 'xml-to-excel'" label="文件名">
              <el-input v-model="xmlForm.fileName" placeholder="export" style="width: 200px" />
            </el-form-item>
          </el-form>

          <div class="action-buttons">
            <el-button type="primary" @click="convertXml" :loading="loading">转换</el-button>
            <el-button @click="loadXmlSample">加载示例</el-button>
          </div>
        </el-tab-pane>

        <el-tab-pane label="YAML 转换" name="yaml">
          <el-form :model="yamlForm" label-width="100px">
            <el-form-item label="操作类型">
              <el-select v-model="yamlForm.operation" style="width: 200px">
                <el-option label="YAML → Excel" value="yaml-to-excel" />
                <el-option label="Excel → YAML" value="excel-to-yaml" />
                <el-option label="YAML → JSON" value="yaml-to-json" />
                <el-option label="JSON → YAML" value="json-to-yaml" />
              </el-select>
            </el-form-item>
            
            <el-form-item v-if="yamlForm.operation !== 'excel-to-yaml'" label="内容">
              <el-input
                v-model="yamlForm.content"
                type="textarea"
                :rows="8"
                placeholder="输入 YAML 或 JSON 内容"
                style="font-family: 'Cascadia Code', monospace"
              />
            </el-form-item>
            
            <el-form-item v-if="yamlForm.operation === 'excel-to-yaml'" label="上传文件">
              <el-upload
                class="upload-demo"
                action=""
                :auto-upload="false"
                :on-change="handleExcelUpload"
                :limit="1"
                accept=".xlsx,.xls"
              >
                <el-button type="primary">选择 Excel 文件</el-button>
                <template #tip>
                  <div class="el-upload__tip">只支持 .xlsx, .xls 文件</div>
                </template>
              </el-upload>
            </el-form-item>
            
            <el-form-item v-if="yamlForm.operation === 'yaml-to-excel'" label="文件名">
              <el-input v-model="yamlForm.fileName" placeholder="export" style="width: 200px" />
            </el-form-item>
          </el-form>

          <div class="action-buttons">
            <el-button type="primary" @click="convertYaml" :loading="loading">转换</el-button>
            <el-button @click="loadYamlSample">加载示例</el-button>
          </div>
        </el-tab-pane>

        <el-tab-pane label="Markdown 转换" name="markdown">
          <el-form :model="markdownForm" label-width="100px">
            <el-form-item label="操作类型">
              <el-select v-model="markdownForm.operation" style="width: 200px">
                <el-option label="Markdown → Excel" value="markdown-to-excel" />
                <el-option label="Excel → Markdown" value="excel-to-markdown" />
              </el-select>
            </el-form-item>
            
            <el-form-item v-if="markdownForm.operation === 'markdown-to-excel'" label="内容">
              <el-input
                v-model="markdownForm.content"
                type="textarea"
                :rows="8"
                placeholder="输入 Markdown 表格内容"
                style="font-family: 'Cascadia Code', monospace"
              />
            </el-form-item>
            
            <el-form-item v-if="markdownForm.operation === 'excel-to-markdown'" label="上传文件">
              <el-upload
                class="upload-demo"
                action=""
                :auto-upload="false"
                :on-change="handleExcelUpload"
                :limit="1"
                accept=".xlsx,.xls"
              >
                <el-button type="primary">选择 Excel 文件</el-button>
                <template #tip>
                  <div class="el-upload__tip">只支持 .xlsx, .xls 文件</div>
                </template>
              </el-upload>
            </el-form-item>
            
            <el-form-item v-if="markdownForm.operation === 'markdown-to-excel'">
              <el-checkbox v-model="markdownForm.hasHeader">包含表头</el-checkbox>
            </el-form-item>
            
            <el-form-item v-if="markdownForm.operation === 'markdown-to-excel'" label="文件名">
              <el-input v-model="markdownForm.fileName" placeholder="export" style="width: 200px" />
            </el-form-item>
          </el-form>

          <div class="action-buttons">
            <el-button type="primary" @click="convertMarkdown" :loading="loading">转换</el-button>
            <el-button @click="loadMarkdownSample">加载示例</el-button>
          </div>
        </el-tab-pane>

        <el-tab-pane label="数据清洗" name="clean">
          <el-form :model="cleanForm" label-width="100px">
            <el-form-item label="选项">
              <el-checkbox v-model="cleanForm.removeDuplicates">移除重复行</el-checkbox>
              <el-checkbox v-model="cleanForm.trimWhitespace" style="margin-left: 20px">去除空格</el-checkbox>
              <el-checkbox v-model="cleanForm.removeEmptyRows" style="margin-left: 20px">移除空行</el-checkbox>
            </el-form-item>
            <el-form-item label="数据（JSON）">
              <el-input
                v-model="cleanForm.data"
                type="textarea"
                :rows="8"
                placeholder='输入 JSON 格式的数据，例如：[{"name": "Zhang", "age": "28"}, {"name": "Li", "age": "35"}]'
                style="font-family: 'Cascadia Code', monospace"
              />
            </el-form-item>
          </el-form>

          <div class="action-buttons">
            <el-button type="primary" @click="cleanData" :loading="loading">清洗数据</el-button>
            <el-button @click="loadCleanSample">加载示例</el-button>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-card v-if="result" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>转换结果</span>
          <el-button size="small" @click="result = null">关闭</el-button>
        </div>
      </template>
      <pre class="code-block">{{ result }}</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'

const activeTab = ref('xml')
const loading = ref(false)
const result = ref<string | null>(null)
const excelFile = ref<File | null>(null)

const xmlForm = reactive({
  operation: 'xml-to-excel',
  content: '',
  fileName: 'export'
})

const yamlForm = reactive({
  operation: 'yaml-to-excel',
  content: '',
  fileName: 'export'
})

const markdownForm = reactive({
  operation: 'markdown-to-excel',
  content: '',
  hasHeader: true,
  fileName: 'export'
})

const cleanForm = reactive({
  data: '',
  removeDuplicates: true,
  trimWhitespace: true,
  removeEmptyRows: true
})

const handleExcelUpload = (file: any) => {
  excelFile.value = file.raw
}

const convertXml = async () => {
  loading.value = true
  try {
    if (xmlForm.operation === 'excel-to-xml') {
      if (!excelFile.value) {
        ElMessage.warning('请选择 Excel 文件')
        return
      }
      const formData = new FormData()
      formData.append('file', excelFile.value)
      const { data } = await axios.post('http://localhost:5077/api/convert/excel-to-xml', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      if (data.success) {
        result.value = data.data
        ElMessage.success('转换成功')
      } else {
        ElMessage.error(data.message || '转换失败')
      }
    } else if (xmlForm.operation === 'xml-to-excel') {
      const { data } = await axios.post('http://localhost:5077/api/convert/xml-to-excel', xmlForm, {
        responseType: 'blob'
      })
      const url = window.URL.createObjectURL(new Blob([data]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `${xmlForm.fileName}.xlsx`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
      ElMessage.success('导出成功')
    } else {
      const endpoint = xmlForm.operation === 'xml-to-json' ? 'xml-to-json' : 'json-to-xml'
      const { data } = await axios.post(`http://localhost:5077/api/convert/${endpoint}`, xmlForm)
      if (data.success) {
        result.value = data.data
        ElMessage.success('转换成功')
      } else {
        ElMessage.error(data.message || '转换失败')
      }
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '转换失败')
  } finally {
    loading.value = false
  }
}

const convertYaml = async () => {
  loading.value = true
  try {
    if (yamlForm.operation === 'excel-to-yaml') {
      if (!excelFile.value) {
        ElMessage.warning('请选择 Excel 文件')
        return
      }
      const formData = new FormData()
      formData.append('file', excelFile.value)
      const { data } = await axios.post('http://localhost:5077/api/convert/excel-to-yaml', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      if (data.success) {
        result.value = data.data
        ElMessage.success('转换成功')
      } else {
        ElMessage.error(data.message || '转换失败')
      }
    } else if (yamlForm.operation === 'yaml-to-excel') {
      const { data } = await axios.post('http://localhost:5077/api/convert/yaml-to-excel', yamlForm, {
        responseType: 'blob'
      })
      const url = window.URL.createObjectURL(new Blob([data]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `${yamlForm.fileName}.xlsx`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
      ElMessage.success('导出成功')
    } else {
      const endpoint = yamlForm.operation === 'yaml-to-json' ? 'yaml-to-json' : 'json-to-yaml'
      const { data } = await axios.post(`http://localhost:5077/api/convert/${endpoint}`, yamlForm)
      if (data.success) {
        result.value = data.data
        ElMessage.success('转换成功')
      } else {
        ElMessage.error(data.message || '转换失败')
      }
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '转换失败')
  } finally {
    loading.value = false
  }
}

const convertMarkdown = async () => {
  loading.value = true
  try {
    if (markdownForm.operation === 'excel-to-markdown') {
      if (!excelFile.value) {
        ElMessage.warning('请选择 Excel 文件')
        return
      }
      const formData = new FormData()
      formData.append('file', excelFile.value)
      const { data } = await axios.post('http://localhost:5077/api/convert/excel-to-markdown', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      if (data.success) {
        result.value = data.data
        ElMessage.success('转换成功')
      } else {
        ElMessage.error(data.message || '转换失败')
      }
    } else {
      const { data } = await axios.post('http://localhost:5077/api/convert/markdown-to-excel', markdownForm, {
        responseType: 'blob'
      })
      const url = window.URL.createObjectURL(new Blob([data]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `${markdownForm.fileName}.xlsx`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
      ElMessage.success('导出成功')
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '转换失败')
  } finally {
    loading.value = false
  }
}

const cleanData = async () => {
  loading.value = true
  try {
    const data = JSON.parse(cleanForm.data)
    const request = {
      rows: data,
      removeDuplicates: cleanForm.removeDuplicates,
      trimWhitespace: cleanForm.trimWhitespace,
      removeEmptyRows: cleanForm.removeEmptyRows
    }
    const { data: response } = await axios.post('http://localhost:5077/api/convert/clean-data', request)
    if (response.success) {
      result.value = JSON.stringify(response.data, null, 2)
      ElMessage.success('数据清洗成功')
    } else {
      ElMessage.error(response.message || '数据清洗失败')
    }
  } catch (error: any) {
    ElMessage.error('数据格式错误，请输入有效的 JSON 格式')
  } finally {
    loading.value = false
  }
}

const loadXmlSample = () => {
  xmlForm.content = `<Root>
  <Row>
    <ID>1</ID>
    <Name>Zhang</Name>
    <Age>28</Age>
    <Email>zhang@example.com</Email>
  </Row>
  <Row>
    <ID>2</ID>
    <Name>Li</Name>
    <Age>35</Age>
    <Email>li@example.com</Email>
  </Row>
</Root>`
}

const loadYamlSample = () => {
  yamlForm.content = `- id: 1
  name: Zhang
  age: 28
  email: zhang@example.com
- id: 2
  name: Li
  age: 35
  email: li@example.com`
}

const loadMarkdownSample = () => {
  markdownForm.content = `| ID | Name | Age | Email |
| --- | --- | --- | --- |
| 1 | Zhang | 28 | zhang@example.com |
| 2 | Li | 35 | li@example.com |`
}

const loadCleanSample = () => {
  cleanForm.data = `[
  {"name": "  Zhang  ", "age": "28"},
  {"name": "Li", "age": "35"},
  {"name": "  Zhang  ", "age": "28"},
  {"name": "", "age": ""}
]`
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

.result-card {
  margin-top: 20px;
}

.result-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
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
  max-height: 400px;
  overflow-y: auto;
}
</style>
