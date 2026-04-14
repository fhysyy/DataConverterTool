<template>
  <div class="page-container">
    <div class="page-header">
      <h2>JSON → SQL</h2>
      <p>将 JSON 数组转换为 SQL 语句，支持多数据库方言和列映射</p>
    </div>

    <el-card shadow="never" class="config-card">
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
    </el-card>

    <el-card shadow="never" class="input-card">
      <template #header>
        <div class="card-header">
          <span>JSON 输入</span>
          <div>
            <el-button size="small" @click="loadSample">加载示例</el-button>
            <el-button type="primary" size="small" @click="analyzeColumns" :loading="analyzing">
              <el-icon><Search /></el-icon>
              解析列
            </el-button>
          </div>
        </div>
      </template>
      <el-input
        v-model="form.json"
        type="textarea"
        :rows="8"
        placeholder='请输入 JSON 数组，例如: [{"name":"张三","age":28,"email":"zhangsan@example.com"}]'
        spellcheck="false"
      />
    </el-card>

    <el-card v-if="columnMappings.length > 0" shadow="never" class="mapping-card">
      <template #header>
        <div class="card-header">
          <span>列映射配置</span>
          <el-button-group>
            <el-button size="small" @click="selectAll">全选</el-button>
            <el-button size="small" @click="deselectAll">全不选</el-button>
            <el-button size="small" @click="resetMappings">重置</el-button>
          </el-button-group>
        </div>
      </template>
      <el-table :data="columnMappings" border size="small" style="width: 100%">
        <el-table-column label="包含" width="70" align="center">
          <template #default="{ row }">
            <el-checkbox v-model="row.include" />
          </template>
        </el-table-column>
        <el-table-column label="源字段名" prop="sourceName" width="200" />
        <el-table-column label="目标列名" width="200">
          <template #default="{ row }">
            <el-input v-model="row.targetName" size="small" placeholder="目标列名" />
          </template>
        </el-table-column>
        <el-table-column label="SQL 类型" min-width="200">
          <template #default="{ row }">
            <el-select v-model="row.sqlType" size="small" filterable allow-create style="width: 100%">
              <el-option-group label="整数">
                <el-option label="INT" value="INT" />
                <el-option label="BIGINT" value="BIGINT" />
              </el-option-group>
              <el-option-group label="小数">
                <el-option label="FLOAT" value="FLOAT" />
                <el-option label="DOUBLE" value="DOUBLE" />
                <el-option label="DECIMAL(18,2)" value="DECIMAL(18,2)" />
              </el-option-group>
              <el-option-group label="字符串">
                <el-option label="VARCHAR(255)" value="VARCHAR(255)" />
                <el-option label="NVARCHAR(255)" value="NVARCHAR(255)" />
                <el-option label="TEXT" value="TEXT" />
              </el-option-group>
              <el-option-group label="日期时间">
                <el-option label="DATETIME" value="DATETIME" />
                <el-option label="TIMESTAMP" value="TIMESTAMP" />
                <el-option label="DATE" value="DATE" />
              </el-option-group>
              <el-option-group label="其他">
                <el-option label="BOOLEAN" value="BOOLEAN" />
                <el-option label="BIT" value="BIT" />
                <el-option label="TINYINT(1)" value="TINYINT(1)" />
              </el-option-group>
            </el-select>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <div class="action-bar">
      <el-button type="primary" :loading="loading" @click="convert" :disabled="!form.json">
        <el-icon><Switch /></el-icon>
        生成 SQL
      </el-button>
    </div>

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
import axios from 'axios'

interface ColumnMapping {
  sourceName: string
  targetName: string
  sqlType: string
  include: boolean
}

const loading = ref(false)
const analyzing = ref(false)
const result = ref<any>(null)
const columnMappings = ref<ColumnMapping[]>([])

const form = reactive({
  json: '',
  tableName: 'MyTable',
  databaseType: 0,
  sqlType: 0,
  includeCreateTable: true,
})

const sampleJson = JSON.stringify([
  { id: 1, name: "张三", age: 28, email: "zhangsan@example.com", city: "北京", salary: 15000.50, is_active: true, created_at: "2024-01-15" },
  { id: 2, name: "李四", age: 35, email: "lisi@example.com", city: "上海", salary: 22000.00, is_active: false, created_at: "2024-02-20" },
  { id: 3, name: "王五", age: 22, email: "wangwu@example.com", city: "广州", salary: 8500.75, is_active: true, created_at: "2024-03-10" },
], null, 2)

const loadSample = () => {
  form.json = sampleJson
  columnMappings.value = []
  result.value = null
}

const analyzeColumns = async () => {
  if (!form.json.trim()) {
    ElMessage.warning('请先输入 JSON 数据')
    return
  }

  analyzing.value = true
  try {
    const res = await axios.post('/api/convert/json-analyze-columns', { json: form.json })
    if (res.data.success) {
      columnMappings.value = res.data.data.map((m: any) => ({
        sourceName: m.sourceName,
        targetName: m.targetName || m.sourceName,
        sqlType: m.sqlType || '',
        include: m.include !== false,
      }))
      ElMessage.success(`已解析 ${columnMappings.value.length} 个字段`)
    } else {
      ElMessage.error(res.data.message || '解析失败')
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data?.message || '解析失败')
  } finally {
    analyzing.value = false
  }
}

const selectAll = () => columnMappings.value.forEach(m => m.include = true)
const deselectAll = () => columnMappings.value.forEach(m => m.include = false)
const resetMappings = () => columnMappings.value.forEach(m => {
  m.targetName = m.sourceName
  m.sqlType = ''
  m.include = true
})

const convert = async () => {
  if (!form.json.trim()) {
    ElMessage.warning('请先输入 JSON 数据')
    return
  }

  loading.value = true
  result.value = null

  try {
    const payload: any = {
      json: form.json,
      tableName: form.tableName,
      databaseType: form.databaseType,
      sqlType: form.sqlType,
      includeCreateTable: form.includeCreateTable,
      columnMappings: columnMappings.value.map(m => ({
        sourceName: m.sourceName,
        targetName: m.targetName,
        sqlType: m.sqlType,
        include: m.include,
      })),
    }

    const res = await axios.post('/api/convert/json-to-sql', payload)

    if (res.data.success) {
      result.value = res.data
      ElMessage.success('转换成功')
    } else {
      ElMessage.error(res.data.message || '转换失败')
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
  max-width: 1000px;
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

.config-card {
  margin-bottom: 16px;
}

.input-card {
  margin-bottom: 16px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.mapping-card {
  margin-bottom: 16px;
}

.action-bar {
  margin-bottom: 16px;
}

.result-card {
  margin-top: 16px;
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
  max-height: 500px;
  overflow-y: auto;
}
</style>
