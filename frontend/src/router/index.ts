import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/excel-to-sql' },
    { path: '/excel-to-sql', name: 'ExcelToSql', component: () => import('../views/ExcelToSql.vue') },
    { path: '/table-to-json', name: 'TableToJson', component: () => import('../views/TableToJson.vue') },
    { path: '/table-to-excel', name: 'TableToExcel', component: () => import('../views/TableToExcel.vue') },
    { path: '/json-to-class', name: 'JsonToClass', component: () => import('../views/JsonToClass.vue') },
    { path: '/json-to-sql', name: 'JsonToSql', component: () => import('../views/JsonToSql.vue') },
    { path: '/json-validate', name: 'JsonValidate', component: () => import('../views/JsonValidate.vue') },
    { path: '/excel-csv', name: 'ExcelCsv', component: () => import('../views/ExcelCsv.vue') },
    { path: '/json-excel', name: 'JsonExcel', component: () => import('../views/JsonExcel.vue') },
    { path: '/database-excel', name: 'DatabaseExcel', component: () => import('../views/DatabaseToExcel.vue') },
    { path: '/database-to-sql', name: 'DatabaseToSql', component: () => import('../views/DatabaseToSql.vue') },
    { path: '/data-format', name: 'DataFormat', component: () => import('../views/DataFormat.vue') },
    { path: '/data-generator', name: 'DataGenerator', component: () => import('../views/DataGenerator.vue') },
    { path: '/encryption', name: 'Encryption', component: () => import('../views/Encryption.vue') },
  ]
})

export default router
