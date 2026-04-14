<template>
  <div class="page-container">
    <div class="page-header">
      <h2>Table → JSON</h2>
      <p>在表格中输入数据，一键转换为 JSON 格式</p>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-button type="primary" size="small" @click="addColumn">
          <el-icon><Plus /></el-icon> 添加列
        </el-button>
        <el-button type="success" size="small" @click="addRow">
          <el-icon><Plus /></el-icon> 添加行
        </el-button>
        <el-button type="danger" size="small" @click="clearAll">
          <el-icon><Delete /></el-icon> 清空
        </el-button>
        <el-button type="primary" @click="convert" style="margin-left: auto">
          <el-icon><Switch /></el-icon> 转换为 JSON
        </el-button>
      </div>

      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th class="row-num">#</th>
              <th v-for="(col, ci) in columns" :key="ci" class="header-cell">
                <el-input
                  v-model="columns[ci]"
                  size="small"
                  placeholder="列名"
                  class="header-input"
                />
                <el-icon class="remove-icon" @click="removeColumn(ci)"><Close /></el-icon>
              </th>
              <th class="action-col"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, ri) in rows" :key="ri">
              <td class="row-num">{{ ri + 1 }}</td>
              <td v-for="(col, ci) in columns" :key="ci">
                <el-input
                  v-model="rows[ri][col]"
                  size="small"
                  placeholder="值"
                />
              </td>
              <td class="action-col">
                <el-icon class="remove-icon" @click="removeRow(ri)"><Delete /></el-icon>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </el-card>

    <el-card v-if="jsonResult" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>JSON 结果</span>
          <el-button type="primary" size="small" @click="copyResult">
            <el-icon><CopyDocument /></el-icon> 复制
          </el-button>
        </div>
      </template>
      <pre class="code-block">{{ jsonResult }}</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'

const columns = ref<string[]>(['name', 'age', 'email'])
const rows = ref<Record<string, string>[]>([
  { name: '张三', age: '25', email: 'zhangsan@example.com' },
  { name: '李四', age: '30', email: 'lisi@example.com' },
])
const jsonResult = ref('')

const addColumn = () => {
  const name = `col_${columns.value.length + 1}`
  columns.value.push(name)
  rows.value.forEach(row => { row[name] = '' })
}

const removeColumn = (index: number) => {
  if (columns.value.length <= 1) return
  const col = columns.value[index]
  columns.value.splice(index, 1)
  rows.value.forEach(row => { delete row[col] })
}

const addRow = () => {
  const row: Record<string, string> = {}
  columns.value.forEach(col => { row[col] = '' })
  rows.value.push(row)
}

const removeRow = (index: number) => {
  rows.value.splice(index, 1)
}

const clearAll = () => {
  columns.value = ['col_1']
  rows.value = [{ col_1: '' }]
  jsonResult.value = ''
}

const convert = async () => {
  if (rows.value.length === 0) {
    ElMessage.warning('请先添加数据行')
    return
  }

  try {
    const res = await axios.post('/api/convert/table-to-json', {
      rows: rows.value
    })
    if (res.data.success) {
      jsonResult.value = res.data.data
      ElMessage.success('转换成功')
    } else {
      ElMessage.error(res.data.message || '转换失败')
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data?.message || '请求失败')
  }
}

const copyResult = () => {
  navigator.clipboard.writeText(jsonResult.value)
  ElMessage.success('已复制到剪贴板')
}
</script>

<style scoped>
.page-container {
  max-width: 1100px;
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

.toolbar {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
  align-items: center;
}

.table-wrapper {
  overflow-x: auto;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.data-table th,
.data-table td {
  border: 1px solid #ebeef5;
  padding: 8px;
  min-width: 120px;
}

.row-num {
  width: 40px;
  min-width: 40px;
  text-align: center;
  color: #909399;
  background: #fafafa;
}

.header-cell {
  background: #f5f7fa;
  position: relative;
}

.header-input {
  width: 100%;
}

.action-col {
  width: 40px;
  min-width: 40px;
  text-align: center;
}

.remove-icon {
  cursor: pointer;
  color: #f56c6c;
  font-size: 14px;
}

.remove-icon:hover {
  color: #e6363a;
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
}
</style>
