<template>
  <div class="page-container">
    <div class="page-header">
      <h2>JSON 校验工具</h2>
      <p>输入 JSON 数据，验证其格式是否正确</p>
    </div>

    <el-card shadow="never" class="input-card">
      <div class="config-bar">
        <el-form :model="form" inline>
          <el-form-item>
            <el-button type="primary" @click="validateJson" :loading="loading">
              <el-icon><Check /></el-icon> 校验 JSON
            </el-button>
          </el-form-item>
          <el-form-item>
            <el-button @click="addEscape">
              <el-icon><Edit /></el-icon> 添加转义
            </el-button>
          </el-form-item>
          <el-form-item>
            <el-button @click="removeEscape">
              <el-icon><Edit /></el-icon> 去转义
            </el-button>
          </el-form-item>
          <el-form-item>
            <el-button @click="clearInput">
              <el-icon><Delete /></el-icon> 清空
            </el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-input
        v-model="jsonInput"
        type="textarea"
        :rows="12"
        placeholder='请输入 JSON，例如：
{
  "name": "张三",
  "age": 25,
  "email": "zhangsan@example.com",
  "address": {
    "city": "北京",
    "street": "朝阳路"
  },
  "tags": ["developer", "csharp"]
}'
        class="json-input"
      />
    </el-card>

    <el-card v-if="validationResult" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>校验结果</span>
        </div>
      </template>
      <div v-if="validationResult.valid" class="valid-result">
        <el-icon class="valid-icon"><Success /></el-icon>
        <p>JSON 格式正确！</p>
        <div v-if="validationResult.data" class="json-info">
          <h4>JSON 信息：</h4>
          <ul>
            <li>类型：{{ validationResult.data.type }}</li>
            <li v-if="validationResult.data.properties">属性数量：{{ validationResult.data.properties }}</li>
            <li v-if="validationResult.data.items">数组元素数量：{{ validationResult.data.items }}</li>
          </ul>
        </div>
      </div>
      <div v-else class="invalid-result">
        <el-icon class="invalid-icon"><Warning /></el-icon>
        <p>{{ validationResult.error }}</p>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'

const jsonInput = ref(`{
  "name": "张三",
  "age": 25,
  "email": "zhangsan@example.com",
  "isActive": true,
  "address": {
    "city": "北京",
    "street": "朝阳路"
  },
  "tags": ["developer", "csharp"]
}`)

const validationResult = ref<any>(null)
const loading = ref(false)

const form = reactive({
  // 可以添加其他配置项
})

const validateJson = async () => {
  if (!jsonInput.value.trim()) {
    ElMessage.warning('请输入 JSON 内容')
    return
  }

  loading.value = true
  validationResult.value = null

  try {
    const res = await axios.post('/api/validate/json', {
      json: jsonInput.value
    })

    if (res.data.success) {
      validationResult.value = res.data.data
      ElMessage.success('校验完成')
    } else {
      ElMessage.error(res.data.message || '校验失败')
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data?.message || '请求失败')
  } finally {
    loading.value = false
  }
}

const clearInput = () => {
  jsonInput.value = ''
  validationResult.value = null
}

const addEscape = () => {
  if (!jsonInput.value.trim()) {
    ElMessage.warning('请输入 JSON 内容')
    return
  }

  try {
    // 将字符串转换为JSON字符串，添加转义
    jsonInput.value = JSON.stringify(jsonInput.value)
    ElMessage.success('已添加转义')
  } catch (error) {
    ElMessage.error('操作失败')
  }
}

const removeEscape = () => {
  if (!jsonInput.value.trim()) {
    ElMessage.warning('请输入 JSON 内容')
    return
  }

  try {
    // 解析JSON字符串，去除转义
    jsonInput.value = JSON.parse(jsonInput.value)
    ElMessage.success('已去除转义')
  } catch (error) {
    ElMessage.error('JSON 格式错误，请确保输入的是转义后的 JSON 字符串')
  }
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

.input-card {
  margin-bottom: 20px;
}

.config-bar {
  margin-bottom: 16px;
}

.json-input :deep(.el-textarea__inner) {
  font-family: 'Cascadia Code', 'Fira Code', monospace;
  font-size: 13px;
  line-height: 1.6;
}

.result-card {
  margin-top: 20px;
}

.result-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.valid-result {
  background-color: #f0f9eb;
  border: 1px solid #e1f3d8;
  border-radius: 8px;
  padding: 20px;
  color: #67c23a;
}

.valid-icon {
  font-size: 24px;
  margin-bottom: 12px;
}

.invalid-result {
  background-color: #fef0f0;
  border: 1px solid #fbc4c4;
  border-radius: 8px;
  padding: 20px;
  color: #f56c6c;
}

.invalid-icon {
  font-size: 24px;
  margin-bottom: 12px;
}

.json-info {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #e1f3d8;
}

.json-info h4 {
  margin-bottom: 8px;
  font-size: 14px;
}

.json-info ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

.json-info li {
  margin-bottom: 4px;
  font-size: 13px;
}
</style>