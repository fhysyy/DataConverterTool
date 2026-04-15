<template>
  <div class="page-container">
    <div class="page-header">
      <h2>数据库 → SQL</h2>
      <p>从数据库表结构生成建表语句，支持 MySQL、SQL Server、PostgreSQL</p>
    </div>

    <el-card shadow="never" class="config-card">
      <el-form :model="form" label-width="100px" inline>
        <el-form-item label="数据库类型">
          <el-select v-model="form.databaseType" style="width: 160px" @change="handleDbTypeChange">
            <el-option label="MySQL" :value="1" />
            <el-option label="SQL Server" :value="0" />
            <el-option label="PostgreSQL" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item label="主机">
          <el-input v-model="form.host" placeholder="localhost" style="width: 180px" />
        </el-form-item>
        <el-form-item label="端口">
          <el-input-number v-model="form.port" :min="1" :max="65535" style="width: 120px" />
        </el-form-item>
        <el-form-item label="数据库">
          <el-input v-model="form.database" placeholder="数据库名" style="width: 180px" />
        </el-form-item>
        <el-form-item label="Schema">
          <el-input v-model="form.schema" placeholder="默认或指定" style="width: 150px" />
        </el-form-item>
        <el-form-item label="用户名">
          <el-input v-model="form.username" placeholder="root" style="width: 150px" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input v-model="form.password" type="password" placeholder="密码" show-password style="width: 150px" />
        </el-form-item>
      </el-form>

      <el-divider />

      <el-form label-width="100px">
        <el-form-item label="表名">
          <el-input v-model="form.tableName" placeholder="表名" style="width: 200px" />
        </el-form-item>
        <el-form-item label="选项">
          <el-checkbox v-model="form.includeIndexes">包含索引</el-checkbox>
          <el-checkbox v-model="form.includeForeignKeys" style="margin-left: 20px">包含外键</el-checkbox>
        </el-form-item>
      </el-form>

      <div class="action-buttons">
        <el-button type="primary" @click="loadTables" :loading="loading">加载表列表</el-button>
        <el-button type="success" @click="generateSql" :loading="loading">生成建表语句</el-button>
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
    </el-card>

    <el-card v-if="sqlResult" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>生成结果</span>
          <el-button size="small" @click="sqlResult = null">关闭</el-button>
        </div>
      </template>

      <div v-if="sqlResult.createTableSql" class="sql-section">
        <h4>建表语句</h4>
        <pre class="code-block">{{ sqlResult.createTableSql }}</pre>
      </div>

      <div v-if="sqlResult.indexSqlList && sqlResult.indexSqlList.length > 0" class="sql-section">
        <h4>索引语句</h4>
        <pre class="code-block">{{ sqlResult.indexSqlList.join('\n') }}</pre>
      </div>

      <div v-if="sqlResult.foreignKeySqlList && sqlResult.foreignKeySqlList.length > 0" class="sql-section">
        <h4>外键语句</h4>
        <pre class="code-block">{{ sqlResult.foreignKeySqlList.join('\n') }}</pre>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'

const loading = ref(false)
const tables = ref<string[]>([])
const sqlResult = ref<any>(null)

const form = reactive({
  databaseType: 1,
  host: 'localhost',
  port: 3306,
  database: '',
  schema: '',
  username: 'root',
  password: '',
  tableName: '',
  includeIndexes: true,
  includeForeignKeys: true
})

const handleDbTypeChange = () => {
  if (form.databaseType === 1) {
    form.port = 3306
    form.username = 'root'
    form.schema = ''
  } else if (form.databaseType === 0) {
    form.port = 1433
    form.username = 'sa'
    form.schema = 'dbo'
  } else if (form.databaseType === 2) {
    form.port = 5432
    form.username = 'postgres'
    form.schema = 'public'
  }
}

const loadTables = async () => {
  loading.value = true
  try {
    const { data } = await axios.post('http://localhost:5077/api/convert/database-sql-tables', form)
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
  form.tableName = tableName
}

const generateSql = async () => {
  if (!form.tableName) {
    ElMessage.warning('请输入表名')
    return
  }
  loading.value = true
  try {
    const { data } = await axios.post('http://localhost:5077/api/convert/database-to-sql', form)
    if (data.success) {
      sqlResult.value = data.data
      ElMessage.success('生成建表语句成功')
    } else {
      ElMessage.error(data.message || '生成建表语句失败')
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '生成建表语句失败')
  } finally {
    loading.value = false
  }
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

.result-card {
  margin-top: 20px;
}

.result-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.sql-section {
  margin-bottom: 20px;
}

.sql-section h4 {
  margin: 0 0 8px 0;
  font-size: 14px;
  color: #1e1e2e;
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
