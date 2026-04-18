<template>
  <div class="page-container">
    <div class="page-header">
      <h2>Table → Excel</h2>
      <p>在表格中输入数据，导出为 Excel 文件下载</p>
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
        <el-input
          v-model="fileName"
          size="small"
          placeholder="文件名"
          style="width: 180px; margin-left: auto; margin-right: 12px"
        />
        <el-button type="primary" @click="exportExcel" :loading="loading">
          <el-icon><Download /></el-icon> 导出 Excel
        </el-button>
      </div>

      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th class="row-num">#</th>
              <th v-for="(ci) in columns.length" :key="ci" class="header-cell">
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
            <tr v-for="(ri) in rows.length" :key="ri">
              <td class="row-num">{{ ri }}</td>
              <td v-for="(ci) in columns.length" :key="ci">
                <el-input
                  v-model="rows[ri - 1][columns[ci - 1]]"
                  size="small"
                  placeholder="值"
                />
              </td>
              <td class="action-col">
                <el-icon class="remove-icon" @click="removeRow(ri - 1)"><Delete /></el-icon>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { api } from '@/api'

const columns = ref<string[]>(['姓名', '年龄', '城市'])
const rows = ref<Record<string, string>[]>([
  { 姓名: '张三', 年龄: '25', 城市: '北京' },
  { 姓名: '李四', 年龄: '30', 城市: '上海' },
])
const fileName = ref('export')
const loading = ref(false)

const addColumn = () => {
  const name = `列${columns.value.length + 1}`
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
  columns.value = ['列1']
  rows.value = [{ '列1': '' }]
}

const exportExcel = async () => {
  if (rows.value.length === 0) {
    ElMessage.warning('请先添加数据行')
    return
  }

  loading.value = true
  try {
    const res = await api.convert.tableToExcel({
      rows: rows.value.map(row => {
        const newRow: any = {};
        columns.value.forEach(col => {
          newRow[col] = row[col];
        });
        return newRow;
      }),
      columns: columns.value.map(col => ({ key: col, title: col }))
    })

    const blob = new Blob([res.data], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    })
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `${fileName.value}.xlsx`
    link.click()
    window.URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (err: any) {
    ElMessage.error('导出失败')
  } finally {
    loading.value = false
  }
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
</style>
