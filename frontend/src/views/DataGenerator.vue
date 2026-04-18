<template>
  <div class="page-container">
    <div class="page-header">
      <h2>数据生成器</h2>
      <p>快速生成模拟数据，支持多种数据类型和输出格式</p>
    </div>

    <el-card shadow="never" class="config-card">
      <el-form :model="form" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="表名">
              <el-input v-model="form.tableName" placeholder="users" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="行数">
              <el-input-number v-model="form.rowCount" :min="1" :max="10000" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="输出格式">
              <el-select v-model="form.outputType" style="width: 100%">
                <el-option label="SQL" :value="0" />
                <el-option label="JSON" :value="1" />
                <el-option label="CSV" :value="2" />
                <el-option label="Excel" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <div class="columns-section">
        <div class="columns-header">
          <h4>列定义</h4>
          <el-button type="primary" size="small" @click="addColumn"><el-icon><Plus /></el-icon> 添加列</el-button>
        </div>

        <el-table :data="form.columns" border size="small" style="margin-top: 12px">
          <el-table-column label="列名" min-width="150">
            <template #default="{ row }">
              <el-input v-model="row.name" size="small" placeholder="列名" />
            </template>
          </el-table-column>
          <el-table-column label="数据类型" min-width="160">
            <template #default="{ row }">
              <el-select v-model="row.dataType" size="small" style="width: 100%">
                <el-option-group label="标识">
                  <el-option label="自增ID" :value="0" />
                  <el-option label="UUID" :value="16" />
                </el-option-group>
                <el-option-group label="个人信息">
                  <el-option label="姓名" :value="1" />
                  <el-option label="姓" :value="2" />
                  <el-option label="名" :value="3" />
                  <el-option label="邮箱" :value="4" />
                  <el-option label="手机号" :value="5" />
                </el-option-group>
                <el-option-group label="地址">
                  <el-option label="地址" :value="6" />
                  <el-option label="城市" :value="7" />
                </el-option-group>
                <el-option-group label="职业">
                  <el-option label="公司" :value="8" />
                  <el-option label="职位" :value="9" />
                </el-option-group>
                <el-option-group label="数值/日期">
                  <el-option label="年龄" :value="10" />
                  <el-option label="生日" :value="11" />
                  <el-option label="日期时间" :value="12" />
                  <el-option label="整数" :value="13" />
                  <el-option label="小数" :value="14" />
                  <el-option label="布尔值" :value="15" />
                </el-option-group>
                <el-option-group label="网络">
                  <el-option label="URL" :value="17" />
                  <el-option label="IP地址" :value="18" />
                </el-option-group>
                <el-option-group label="其他">
                  <el-option label="自定义" :value="19" />
                </el-option-group>
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="自定义值(逗号分隔)" min-width="200">
            <template #default="{ row }">
              <el-input
                v-model="row.customValues"
                size="small"
                placeholder="值1,值2,值3"
                :disabled="row.dataType !== 19"
              />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80" align="center">
            <template #default="{ $index }">
              <el-button type="danger" size="small" link @click="removeColumn($index)">
                <el-icon><Delete /></el-icon>
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <div class="action-bar">
        <el-button type="primary" size="small" @click="loadPreset('users')">用户表预设</el-button>
        <el-button type="primary" size="small" @click="loadPreset('products')">商品表预设</el-button>
        <el-button type="primary" size="small" @click="loadPreset('orders')">订单表预设</el-button>
        <el-button type="success" @click="generate" :loading="loading" style="margin-left: auto">
          <el-icon><MagicStick /></el-icon> 生成数据
        </el-button>
        <el-button type="warning" @click="generateAndDownload" :loading="loading">
          <el-icon><Download /></el-icon> 生成并下载
        </el-button>
      </div>
    </el-card>

    <el-card v-if="result" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>生成结果</span>
          <div>
            <el-button type="primary" size="small" @click="copyResult"><el-icon><CopyDocument /></el-icon> 复制</el-button>
            <el-button type="success" size="small" @click="downloadResult"><el-icon><Download /></el-icon> 下载</el-button>
          </div>
        </div>
      </template>
      <pre class="code-block">{{ result }}</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { api } from '@/api'

const loading = ref(false)
const result = ref('')
const resultFileName = ref('')

const form = reactive({
  tableName: 'users',
  rowCount: 10,
  outputType: 0,
  columns: [
    { name: 'id', dataType: 0, customValues: '' },
    { name: 'name', dataType: 1, customValues: '' },
    { name: 'email', dataType: 4, customValues: '' },
    { name: 'phone', dataType: 5, customValues: '' },
    { name: 'city', dataType: 7, customValues: '' },
    { name: 'age', dataType: 10, customValues: '' },
  ] as Array<{ name: string; dataType: number; customValues: string }>
})

const addColumn = () => {
  form.columns.push({ name: `col_${form.columns.length + 1}`, dataType: 13, customValues: '' })
}

const removeColumn = (index: number) => {
  form.columns.splice(index, 1)
}

const presets: Record<string, Array<{ name: string; dataType: number; customValues: string }>> = {
  users: [
    { name: 'id', dataType: 0, customValues: '' },
    { name: 'name', dataType: 1, customValues: '' },
    { name: 'email', dataType: 4, customValues: '' },
    { name: 'phone', dataType: 5, customValues: '' },
    { name: 'city', dataType: 7, customValues: '' },
    { name: 'age', dataType: 10, customValues: '' },
    { name: 'company', dataType: 8, customValues: '' },
    { name: 'created_at', dataType: 12, customValues: '' },
  ],
  products: [
    { name: 'id', dataType: 0, customValues: '' },
    { name: 'name', dataType: 19, customValues: '笔记本电脑,手机,平板,耳机,键盘,鼠标,显示器,摄像头' },
    { name: 'price', dataType: 14, customValues: '' },
    { name: 'stock', dataType: 13, customValues: '' },
    { name: 'is_active', dataType: 15, customValues: '' },
  ],
  orders: [
    { name: 'id', dataType: 0, customValues: '' },
    { name: 'order_no', dataType: 16, customValues: '' },
    { name: 'customer', dataType: 1, customValues: '' },
    { name: 'amount', dataType: 14, customValues: '' },
    { name: 'status', dataType: 19, customValues: '待付款,已付款,已发货,已完成,已取消' },
    { name: 'created_at', dataType: 12, customValues: '' },
  ],
}

const loadPreset = (name: string) => {
  const preset = presets[name]
  if (preset) {
    form.columns = JSON.parse(JSON.stringify(preset))
    form.tableName = name
    ElMessage.success(`已加载 ${name} 预设`)
  }
}

const generate = async () => {
  if (form.columns.length === 0) { ElMessage.warning('请至少定义一列'); return }
  loading.value = true; result.value = ''
  try {
    const res = await api.convert.dataGenerator(form)
    if (res.success) {
      result.value = res.data.content
      resultFileName.value = res.data.fileName
      ElMessage.success('生成成功')
    } else {
      ElMessage.error(res.message || '生成失败')
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data || '请求失败')
  }
  finally { loading.value = false }
}

const generateAndDownload = async () => {
  if (form.columns.length === 0) { ElMessage.warning('请至少定义一列'); return }
  loading.value = true
  try {
    const res = await api.convert.dataGeneratorDownload(form)
    const blob = new Blob([res], { type: 'application/octet-stream' })
    const url = URL.createObjectURL(blob)
    const contentDisposition = 'attachment; filename=data.csv'
    let filename = 'data.txt'
    if (contentDisposition) {
      const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
      if (match) filename = match[1].replace(/['"]/g, '')
    }
    const link = document.createElement('a')
    link.href = url; link.download = filename; link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('下载成功')
  } catch (err: any) { ElMessage.error('下载失败') }
  finally { loading.value = false }
}

const copyResult = () => {
  navigator.clipboard.writeText(result.value)
  ElMessage.success('已复制到剪贴板')
}

const downloadResult = () => {
  const blob = new Blob([result.value], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url; link.download = resultFileName.value || 'data.txt'; link.click()
  URL.revokeObjectURL(url)
}
</script>

<style scoped>
.page-container { max-width: 1100px; margin: 0 auto; }
.page-header { margin-bottom: 24px; }
.page-header h2 { font-size: 24px; color: #303133; margin-bottom: 8px; }
.page-header p { color: #909399; font-size: 14px; }
.config-card { margin-bottom: 20px; }
.columns-section { margin-top: 16px; }
.columns-header { display: flex; justify-content: space-between; align-items: center; }
.columns-header h4 { color: #606266; font-size: 14px; }
.action-bar { display: flex; gap: 8px; margin-top: 20px; align-items: center; }
.result-card { margin-top: 20px; }
.result-header { display: flex; justify-content: space-between; align-items: center; }
.code-block {
  background: #1e1e2e; color: #cdd6f4; padding: 16px; border-radius: 8px;
  font-family: 'Cascadia Code', 'Fira Code', monospace; font-size: 13px;
  line-height: 1.6; overflow-x: auto; white-space: pre-wrap; word-break: break-all;
  max-height: 500px; overflow-y: auto;
}
</style>
