<template>
  <div class="page-container">
    <div class="page-header">
      <h2>加解密工具</h2>
      <p>支持 Base64、MD5、SHA1、SHA256、SHA512 等常用加解密算法</p>
    </div>

    <el-card shadow="never" class="config-card">
      <el-tabs v-model="activeTab">
        <el-tab-pane label="加密" name="encrypt">
          <el-form :model="encryptForm" label-width="100px">
            <el-form-item label="加密算法">
              <el-select v-model="encryptForm.encryptionType" style="width: 200px">
                <el-option label="Base64" :value="0" />
                <el-option label="MD5" :value="1" />
                <el-option label="SHA1" :value="2" />
                <el-option label="SHA256" :value="3" />
                <el-option label="SHA512" :value="4" />
              </el-select>
            </el-form-item>
            
            <el-form-item label="输入内容">
              <el-input
                v-model="encryptForm.content"
                type="textarea"
                :rows="8"
                placeholder="请输入需要加密的内容"
                style="font-family: 'Cascadia Code', monospace"
              />
            </el-form-item>
          </el-form>

          <div class="action-buttons">
            <el-button type="primary" @click="encrypt" :loading="loading">加密</el-button>
            <el-button @click="loadEncryptSample">加载示例</el-button>
            <el-button @click="clearEncryptForm">清空</el-button>
          </div>
        </el-tab-pane>

        <el-tab-pane label="解密" name="decrypt">
          <el-form :model="decryptForm" label-width="100px">
            <el-form-item label="解密算法">
              <el-select v-model="decryptForm.encryptionType" style="width: 200px">
                <el-option label="Base64" :value="0" />
                <el-option label="MD5" :value="1" disabled />
                <el-option label="SHA1" :value="2" disabled />
                <el-option label="SHA256" :value="3" disabled />
                <el-option label="SHA512" :value="4" disabled />
              </el-select>
            </el-form-item>
            
            <el-form-item label="输入内容">
              <el-input
                v-model="decryptForm.content"
                type="textarea"
                :rows="8"
                placeholder="请输入需要解密的内容"
                style="font-family: 'Cascadia Code', monospace"
              />
            </el-form-item>
          </el-form>

          <div class="action-buttons">
            <el-button type="primary" @click="decrypt" :loading="loading">解密</el-button>
            <el-button @click="loadDecryptSample">加载示例</el-button>
            <el-button @click="clearDecryptForm">清空</el-button>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <el-card v-if="result" shadow="never" class="result-card">
      <template #header>
        <div class="result-header">
          <span>{{ activeTab === 'encrypt' ? '加密结果' : '解密结果' }}</span>
          <div class="result-actions">
            <el-button size="small" @click="copyResult">复制</el-button>
            <el-button size="small" @click="result = null">关闭</el-button>
          </div>
        </div>
      </template>
      <pre class="code-block">{{ result }}</pre>
    </el-card>

    <el-card shadow="never" class="info-card">
      <template #header>
        <span>算法说明</span>
      </template>
      <div class="algorithm-info">
        <div class="info-item">
          <strong>Base64：</strong>可逆编码算法，用于数据传输和存储，支持加密和解密
        </div>
        <div class="info-item">
          <strong>MD5：</strong>单向哈希算法，生成128位哈希值，不可逆，主要用于数据校验
        </div>
        <div class="info-item">
          <strong>SHA1：</strong>单向哈希算法，生成160位哈希值，不可逆，安全性高于MD5
        </div>
        <div class="info-item">
          <strong>SHA256：</strong>单向哈希算法，生成256位哈希值，不可逆，安全性更高
        </div>
        <div class="info-item">
          <strong>SHA512：</strong>单向哈希算法，生成512位哈希值，不可逆，安全性最高
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { api } from '@/api'

const activeTab = ref('encrypt')
const loading = ref(false)
const result = ref<string | null>(null)

const encryptForm = reactive({
  content: '',
  encryptionType: 0,
  key: ''
})

const decryptForm = reactive({
  content: '',
  encryptionType: 0,
  key: ''
})

const encrypt = async () => {
  if (!encryptForm.content.trim()) {
    ElMessage.warning('请输入需要加密的内容')
    return
  }

  loading.value = true
  try {
    const data = await api.encrypt.encrypt(encryptForm)
    if (data.success) {
      result.value = data.data
      ElMessage.success('加密成功')
    } else {
      ElMessage.error(data.message || '加密失败')
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '加密失败')
  } finally {
    loading.value = false
  }
}

const decrypt = async () => {
  if (!decryptForm.content.trim()) {
    ElMessage.warning('请输入需要解密的内容')
    return
  }

  loading.value = true
  try {
    const data = await api.encrypt.decrypt(decryptForm)
    if (data.success) {
      result.value = data.data
      ElMessage.success('解密成功')
    } else {
      ElMessage.error(data.message || '解密失败')
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '解密失败')
  } finally {
    loading.value = false
  }
}

const copyResult = () => {
  if (result.value) {
    navigator.clipboard.writeText(result.value)
    ElMessage.success('已复制到剪贴板')
  }
}

const loadEncryptSample = () => {
  encryptForm.content = 'Hello World'
  encryptForm.encryptionType = 0
}

const loadDecryptSample = () => {
  decryptForm.content = 'SGVsbG8gV29ybGQ='
  decryptForm.encryptionType = 0
}

const clearEncryptForm = () => {
  encryptForm.content = ''
  encryptForm.encryptionType = 0
  result.value = null
}

const clearDecryptForm = () => {
  decryptForm.content = ''
  decryptForm.encryptionType = 0
  result.value = null
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

.result-actions {
  display: flex;
  gap: 8px;
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

.info-card {
  margin-top: 20px;
}

.algorithm-info {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.info-item {
  padding: 12px;
  background: #f5f5f5;
  border-radius: 6px;
  line-height: 1.6;
  color: #333;
}

.info-item strong {
  color: #1e1e2e;
  margin-right: 8px;
}
</style>