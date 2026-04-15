<template>
  <el-container class="app-container">
    <el-aside width="220px" class="app-aside">
      <div class="logo">
        <el-icon :size="28"><Switch /></el-icon>
        <span>DataConverter</span>
      </div>
      <el-menu
        :default-active="currentRoute"
        router
        class="app-menu"
        background-color="#1d1e2c"
        text-color="#a0a3bd"
        active-text-color="#409eff"
      >
        <div class="menu-group-title">基础转换</div>
        <el-menu-item index="/excel-to-sql">
          <el-icon><Document /></el-icon>
          <span>Excel → SQL</span>
        </el-menu-item>
        <el-menu-item index="/table-to-json">
          <el-icon><Grid /></el-icon>
          <span>Table → JSON</span>
        </el-menu-item>
        <el-menu-item index="/table-to-excel">
          <el-icon><Download /></el-icon>
          <span>Table → Excel</span>
        </el-menu-item>
        <el-menu-item index="/json-to-class">
          <el-icon><Cpu /></el-icon>
          <span>JSON → 实体类</span>
        </el-menu-item>
        <el-menu-item index="/json-to-sql">
          <el-icon><Coin /></el-icon>
          <span>JSON → SQL</span>
        </el-menu-item>
        <div class="menu-group-title">格式互转</div>
        <el-menu-item index="/excel-csv">
          <el-icon><DocumentCopy /></el-icon>
          <span>Excel ↔ CSV</span>
        </el-menu-item>
        <el-menu-item index="/json-excel">
          <el-icon><Files /></el-icon>
          <span>JSON ↔ Excel</span>
        </el-menu-item>
        <el-menu-item index="/database-excel">
          <el-icon><Coin /></el-icon>
          <span>数据库 → Excel</span>
        </el-menu-item>
        <el-menu-item index="/database-to-sql">
          <el-icon><Document /></el-icon>
          <span>数据库 → SQL</span>
        </el-menu-item>
        <div class="menu-group-title">数据格式</div>
        <el-menu-item index="/data-format">
          <el-icon><Grid /></el-icon>
          <span>格式转换</span>
        </el-menu-item>
        <div class="menu-group-title">工具</div>
        <el-menu-item index="/data-generator">
          <el-icon><MagicStick /></el-icon>
          <span>数据生成器</span>
        </el-menu-item>
        <el-menu-item index="/encryption">
          <el-icon><Lock /></el-icon>
          <span>加解密工具</span>
        </el-menu-item>
      </el-menu>
    </el-aside>
    <el-main class="app-main">
      <router-view />
    </el-main>
  </el-container>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const currentRoute = computed(() => route.path)
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html, body, #app {
  height: 100%;
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}

.app-container {
  height: 100vh;
}

.app-aside {
  background-color: #1d1e2c;
  border-right: 1px solid #2a2b3d;
  overflow-y: auto;
}

.logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px 16px;
  color: #e0e2f0;
  font-size: 18px;
  font-weight: 700;
  border-bottom: 1px solid #2a2b3d;
}

.menu-group-title {
  padding: 12px 16px 6px;
  font-size: 11px;
  color: #6b6e8a;
  text-transform: uppercase;
  letter-spacing: 1px;
  font-weight: 600;
}

.app-menu {
  border-right: none !important;
}

.app-menu .el-menu-item {
  height: 44px;
  line-height: 44px;
  font-size: 13px;
}

.app-menu .el-menu-item:hover {
  background-color: #2a2b3d !important;
}

.app-menu .el-menu-item.is-active {
  background-color: #2a2b3d !important;
  border-right: 3px solid #409eff;
}

.app-main {
  background-color: #f0f2f5;
  padding: 24px;
  overflow-y: auto;
}
</style>
