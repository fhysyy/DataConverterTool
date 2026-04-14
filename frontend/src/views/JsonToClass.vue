<template>
  <div class="page-container">
    <div class="page-header">
      <h2>JSON → 实体类</h2>
      <p>输入 JSON 数据，自动生成 C# / Java / TypeScript 实体类代码</p>
    </div>

    <el-card shadow="never" class="input-card">
      <div class="config-bar">
        <el-form :model="form" inline>
          <el-form-item label="类名">
            <el-input v-model="form.className" placeholder="RootEntity" style="width: 180px" />
          </el-form-item>
          <el-form-item label="语言">
            <el-select v-model="form.language" style="width: 150px">
              <el-option label="C#" :value="0" />
              <el-option label="Java" :value="1" />
              <el-option label="TypeScript" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="convert" :loading="loading">
              <el-icon><Switch /></el-icon> 生成代码
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

    <el-card v-if="codeResult" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>生成结果 - {{ languageLabel }}</span>
          <el-button type="primary" size="small" @click="copyResult">
            <el-icon><CopyDocument /></el-icon> 复制
          </el-button>
        </div>
      </template>
      <pre class="code-block">{{ codeResult }}</pre>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue'
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

const codeResult = ref('')
const loading = ref(false)

const form = reactive({
  className: 'RootEntity',
  language: 0,
})

const languageLabel = computed(() => {
  const labels = ['C#', 'Java', 'TypeScript']
  return labels[form.language] || 'C#'
})

const convert = async () => {
  if (!jsonInput.value.trim()) {
    ElMessage.warning('请输入 JSON 内容')
    return
  }

  loading.value = true
  codeResult.value = ''

  try {
    const res = await axios.post('/api/convert/json-to-class', {
      json: jsonInput.value,
      className: form.className,
      language: form.language,
    })

    if (res.data.success) {
      codeResult.value = res.data.data.code
      ElMessage.success('生成成功')
    } else {
      ElMessage.error(res.data.message || '生成失败')
    }
  } catch (err: any) {
    ElMessage.error(err.response?.data?.message || '请求失败')
  } finally {
    loading.value = false
  }
}

const copyResult = () => {
  navigator.clipboard.writeText(codeResult.value)
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
