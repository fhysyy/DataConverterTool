import axios from 'axios'

// 响应类型定义
export interface ApiResponse<T = any> {
  success: boolean
  data: T
  message?: string
}

const request = axios.create({
  baseURL: '/api',
  timeout: 30000,
}) as any

request.interceptors.response.use(
  (response: any) => response.data,
  (error: any) => {
    console.error('API Error:', error)
    return Promise.reject(error)
  }
)

// API方法封装
export const api = {
  // 数据转换相关
  convert: {
    // Excel相关
    excelToXml: (formData: FormData) => request.post('/convert/excel-to-xml', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
    xmlToExcel: (xmlForm: any) => request.post('/convert/xml-to-excel', xmlForm),
    xmlToJson: (xmlForm: any) => request.post('/convert/xml-to-json', xmlForm),
    jsonToXml: (xmlForm: any) => request.post('/convert/json-to-xml', xmlForm),
    excelToYaml: (formData: FormData) => request.post('/convert/excel-to-yaml', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
    yamlToExcel: (yamlForm: any) => request.post('/convert/yaml-to-excel', yamlForm),
    yamlToJson: (yamlForm: any) => request.post('/convert/yaml-to-json', yamlForm),
    jsonToYaml: (yamlForm: any) => request.post('/convert/json-to-yaml', yamlForm),
    excelToMarkdown: (formData: FormData) => request.post('/convert/excel-to-markdown', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
    markdownToExcel: (markdownForm: any) => request.post('/convert/markdown-to-excel', markdownForm),
    excelToSql: (formData: FormData) => request.post('/convert/excel-to-sql', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
    excelToCsv: (formData: FormData) => request.post('/convert/excel-to-csv', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
    csvToExcel: (formData: FormData) => request.post('/convert/csv-to-excel', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
    
    // JSON相关
    tableToJson: (data: any) => request.post('/convert/table-to-json', data),
    jsonToExcel: (data: any) => request.post('/convert/json-to-excel', data),
    excelToJson: (formData: FormData) => request.post('/convert/excel-to-json', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
    jsonToSql: (payload: any) => request.post('/convert/json-to-sql', payload),
    jsonAnalyzeColumns: (data: { json: string }) => request.post('/convert/json-analyze-columns', data),
    jsonToClass: (data: any) => request.post('/convert/json-to-class', data),
    
    // 数据库相关
    databaseTables: (dbForm: any) => request.post('/convert/database-tables', dbForm),
    databasePreview: (dbForm: any) => request.post('/convert/database-preview', dbForm),
    databaseToExcel: (dbForm: any) => request.post('/convert/database-to-excel', dbForm),
    databaseToSql: (form: any) => request.post('/convert/database-to-sql', form),
    databaseSqlTables: (form: any) => request.post('/convert/database-sql-tables', form),
    mysqlPreview: (mysqlForm: any) => request.post('/convert/mysql-preview', mysqlForm),
    mysqlToExcel: (mysqlForm: any) => request.post('/convert/mysql-to-excel', mysqlForm),
    excelToMysql: (formData: FormData) => request.post('/convert/excel-to-mysql', formData, { headers: { 'Content-Type': 'multipart/form-data' } }),
    
    // 其他转换
    tableToExcel: (data: any) => request.post('/convert/table-to-excel', data),
    cleanData: (data: any) => request.post('/convert/clean-data', data),
    dataGenerator: (form: any) => request.post('/convert/data-generator', form),
    dataGeneratorDownload: (form: any) => request.post('/convert/data-generator-download', form),
    pasteDataPreview: (pasteForm: any) => request.post('/convert/paste-data-preview', pasteForm),
    pasteDataToExcel: (pasteForm: any) => request.post('/convert/paste-data-to-excel', pasteForm),
  },
  
  // 验证相关
  validate: {
    json: (data: { json: string }) => request.post('/validate/json', data),
  },
  
  // 加密相关
  encrypt: {
    encrypt: (encryptForm: any) => request.post('/Convert/encrypt', encryptForm),
    decrypt: (decryptForm: any) => request.post('/Convert/decrypt', decryptForm),
  },
}

export default request
