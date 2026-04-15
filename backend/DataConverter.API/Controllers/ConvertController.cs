using DataConverter.API.Models;
using DataConverter.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataConverter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConvertController : ControllerBase
{
    private readonly IExcelService _excelService;
    private readonly IJsonToClassService _jsonToClassService;
    private readonly IExcelCsvService _excelCsvService;
    private readonly IMysqlService _mysqlService;
    private readonly IDataGeneratorService _dataGeneratorService;
    private readonly IJsonToSqlService _jsonToSqlService;
    private readonly IDatabaseService _databaseService;
    private readonly IDatabaseToSqlService _databaseToSqlService;
    private readonly IDatabaseToJsonService _databaseToJsonService;
    private readonly IDataFormatService _dataFormatService;

    public ConvertController(
        IExcelService excelService,
        IJsonToClassService jsonToClassService,
        IExcelCsvService excelCsvService,
        IMysqlService mysqlService,
        IDataGeneratorService dataGeneratorService,
        IJsonToSqlService jsonToSqlService,
        IDatabaseService databaseService,
        IDatabaseToSqlService databaseToSqlService,
        IDatabaseToJsonService databaseToJsonService,
        IDataFormatService dataFormatService)
    {
        _excelService = excelService;
        _jsonToClassService = jsonToClassService;
        _excelCsvService = excelCsvService;
        _mysqlService = mysqlService;
        _dataGeneratorService = dataGeneratorService;
        _jsonToSqlService = jsonToSqlService;
        _databaseService = databaseService;
        _databaseToSqlService = databaseToSqlService;
        _databaseToJsonService = databaseToJsonService;
        _dataFormatService = dataFormatService;
    }

    [HttpPost("excel-to-sql")]
    public async Task<ActionResult<ConvertResponse<ExcelToSqlResult>>> ExcelToSql(
        IFormFile file,
        [FromForm] string tableName = "MyTable",
        [FromForm] SqlType sqlType = SqlType.Insert,
        [FromForm] bool includeCreateTable = true,
        [FromForm] DatabaseType databaseType = DatabaseType.SqlServer)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ConvertResponse<ExcelToSqlResult> { Success = false, Message = "请上传Excel文件" });

        var request = new ExcelToSqlRequest
        {
            TableName = tableName,
            SqlType = sqlType,
            IncludeCreateTable = includeCreateTable,
            DatabaseType = databaseType
        };

        var result = await _excelService.ExcelToSqlAsync(file, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("table-to-json")]
    public ActionResult<ConvertResponse<string>> TableToJson([FromBody] TableToJsonRequest request)
    {
        try
        {
            if (request.Rows == null || request.Rows.Count == 0)
                return BadRequest(new ConvertResponse<string> { Success = false, Message = "表格数据不能为空" });

            var json = System.Text.Json.JsonSerializer.Serialize(request.Rows, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            return Ok(new ConvertResponse<string> { Success = true, Data = json });
        }
        catch (Exception ex)
        {
            return BadRequest(new ConvertResponse<string> { Success = false, Message = $"转换失败: {ex.Message}" });
        }
    }

    [HttpPost("table-to-excel")]
    public async Task<IActionResult> TableToExcel([FromBody] TableToExcelRequest request)
    {
        try
        {
            if (request.Headers == null || request.Headers.Count == 0)
                return BadRequest("表头不能为空");

            var bytes = await _excelService.TableToExcelAsync(request);
            var fileName = $"{request.FileName}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"转换失败: {ex.Message}");
        }
    }

    [HttpPost("json-to-class")]
    public ActionResult<ConvertResponse<JsonToClassResult>> JsonToClass([FromBody] JsonToClassRequest request)
    {
        var result = _jsonToClassService.ConvertJsonToClass(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("excel-to-csv")]
    public async Task<ActionResult<ConvertResponse<ExcelCsvConvertResult>>> ExcelToCsv(
        IFormFile file, [FromForm] char delimiter = ',')
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = "请上传Excel文件" });

        var result = await _excelCsvService.ExcelToCsvAsync(file, delimiter);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("csv-to-excel")]
    public async Task<ActionResult<ConvertResponse<ExcelCsvConvertResult>>> CsvToExcel(
        IFormFile file, [FromForm] char delimiter = ',')
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = "请上传CSV文件" });

        var result = await _excelCsvService.CsvToExcelAsync(file, delimiter);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("json-to-excel")]
    public async Task<ActionResult<ConvertResponse<ExcelCsvConvertResult>>> JsonToExcel([FromBody] JsonExcelConvertRequest request)
    {
        var result = await _excelCsvService.JsonToExcelAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("excel-to-json")]
    public async Task<ActionResult<ConvertResponse<ExcelCsvConvertResult>>> ExcelToJson(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = "请上传Excel文件" });

        var result = await _excelCsvService.ExcelToJsonAsync(file);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("mysql-to-excel")]
    public async Task<IActionResult> MysqlToExcel([FromBody] DatabaseToExcelRequest request)
    {
        var result = await _mysqlService.MysqlToExcelAsync(request);
        if (!result.Success)
            return BadRequest(result.Message);

        var fileName = $"{request.FileName}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("mysql-preview")]
    public async Task<ActionResult<ConvertResponse<MysqlQueryResult>>> MysqlPreview([FromBody] DatabaseToExcelRequest request)
    {
        var result = await _mysqlService.MysqlToPreviewAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("excel-to-mysql")]
    public async Task<ActionResult<ConvertResponse<string>>> ExcelToMysql(
        IFormFile file,
        [FromForm] string host = "localhost",
        [FromForm] int port = 3306,
        [FromForm] string database = "",
        [FromForm] string username = "root",
        [FromForm] string password = "",
        [FromForm] string tableName = "imported_table",
        [FromForm] bool createIfNotExists = true)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ConvertResponse<string> { Success = false, Message = "请上传Excel文件" });

        var request = new ExcelToMysqlRequest
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            TableName = tableName,
            CreateIfNotExists = createIfNotExists
        };

        var result = await _mysqlService.ExcelToMysqlAsync(file, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("mysql-tables")]
    public async Task<ActionResult<ConvertResponse<List<string>>>> GetMysqlTables([FromBody] DatabaseToExcelRequest request)
    {
        var result = await _mysqlService.GetTableListAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("data-generator")]
    public ActionResult<ConvertResponse<DataGeneratorResult>> GenerateData([FromBody] DataGeneratorRequest request)
    {
        var result = _dataGeneratorService.Generate(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("data-generator-download")]
    public IActionResult GenerateDataDownload([FromBody] DataGeneratorRequest request)
    {
        var result = _dataGeneratorService.Generate(request);
        if (!result.Success || result.Data == null)
            return BadRequest(result.Message);

        var data = result.Data;

        if (data.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            var bytes = Convert.FromBase64String(data.Content);
            return File(bytes, data.ContentType, data.FileName);
        }

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(data.Content);
        return File(contentBytes, data.ContentType, data.FileName);
    }

    [HttpPost("json-to-sql")]
    public ActionResult<ConvertResponse<JsonToSqlResult>> JsonToSql([FromBody] JsonToSqlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Json))
            return BadRequest(new ConvertResponse<JsonToSqlResult> { Success = false, Message = "JSON内容不能为空" });

        var result = _jsonToSqlService.JsonToSql(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("json-analyze-columns")]
    public ActionResult<ConvertResponse<List<ColumnMapping>>> AnalyzeJsonColumns([FromBody] JsonToSqlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Json))
            return BadRequest(new ConvertResponse<List<ColumnMapping>> { Success = false, Message = "JSON内容不能为空" });

        var result = _jsonToSqlService.AnalyzeJsonColumns(request.Json);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("database-to-excel")]
    public async Task<ActionResult> DatabaseToExcel([FromBody] DatabaseToExcelRequest request)
    {
        var result = await _databaseService.DatabaseToExcelAsync(request);
        if (!result.Success || result.Data == null)
            return BadRequest(result.Message);

        return File(result.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{request.FileName}.xlsx");
    }

    [HttpPost("database-preview")]
    public async Task<ActionResult<ConvertResponse<MysqlQueryResult>>> DatabasePreview([FromBody] DatabaseToExcelRequest request)
    {
        var result = await _databaseService.DatabaseToPreviewAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("database-tables")]
    public async Task<ActionResult<ConvertResponse<List<string>>>> GetDatabaseTables([FromBody] DatabaseToExcelRequest request)
    {
        var result = await _databaseService.GetTableListAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("paste-data-to-excel")]
    public async Task<ActionResult> PasteDataToExcel([FromBody] PasteDataRequest request)
    {
        var result = await _databaseService.PasteDataToExcelAsync(request);
        if (!result.Success || result.Data == null)
            return BadRequest(result.Message);

        return File(result.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{request.FileName}.xlsx");
    }

    [HttpPost("paste-data-preview")]
    public async Task<ActionResult<ConvertResponse<MysqlQueryResult>>> PasteDataPreview([FromBody] PasteDataRequest request)
    {
        var result = await _databaseService.PasteDataToPreviewAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // 数据库转 SQL 相关端点
    [HttpPost("database-to-sql")]
    public async Task<ActionResult<ConvertResponse<DatabaseToSqlResult>>> DatabaseToSql([FromBody] DatabaseToSqlRequest request)
    {
        var result = await _databaseToSqlService.DatabaseToSqlAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("database-sql-tables")]
    public async Task<ActionResult<ConvertResponse<List<string>>>> GetDatabaseTables([FromBody] DatabaseToSqlRequest request)
    {
        var result = await _databaseToSqlService.GetTableListAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // 数据库转 JSON 相关端点
    [HttpPost("database-to-json")]
    public async Task<ActionResult<ConvertResponse<DatabaseToJsonResult>>> DatabaseToJson([FromBody] DatabaseToJsonRequest request)
    {
        var result = await _databaseToJsonService.DatabaseToJsonAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // XML 相关端点
    [HttpPost("xml-to-excel")]
    public async Task<ActionResult> XmlToExcel([FromBody] XmlConvertRequest request)
    {
        var result = await _dataFormatService.XmlToExcelAsync(request);
        if (!result.Success || result.Data == null)
            return BadRequest(result.Message);

        return File(result.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{request.FileName}.xlsx");
    }

    [HttpPost("excel-to-xml")]
    public async Task<ActionResult<ConvertResponse<string>>> ExcelToXml(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ConvertResponse<string> { Success = false, Message = "请上传Excel文件" });

        var result = await _dataFormatService.ExcelToXmlAsync(file);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("xml-to-json")]
    public async Task<ActionResult<ConvertResponse<string>>> XmlToJson([FromBody] XmlConvertRequest request)
    {
        var result = await _dataFormatService.XmlToJsonAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("json-to-xml")]
    public async Task<ActionResult<ConvertResponse<string>>> JsonToXml([FromBody] XmlConvertRequest request)
    {
        var result = await _dataFormatService.JsonToXmlAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // YAML 相关端点
    [HttpPost("yaml-to-excel")]
    public async Task<ActionResult> YamlToExcel([FromBody] YamlConvertRequest request)
    {
        var result = await _dataFormatService.YamlToExcelAsync(request);
        if (!result.Success || result.Data == null)
            return BadRequest(result.Message);

        return File(result.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{request.FileName}.xlsx");
    }

    [HttpPost("excel-to-yaml")]
    public async Task<ActionResult<ConvertResponse<string>>> ExcelToYaml(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ConvertResponse<string> { Success = false, Message = "请上传Excel文件" });

        var result = await _dataFormatService.ExcelToYamlAsync(file);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("yaml-to-json")]
    public async Task<ActionResult<ConvertResponse<string>>> YamlToJson([FromBody] YamlConvertRequest request)
    {
        var result = await _dataFormatService.YamlToJsonAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("json-to-yaml")]
    public async Task<ActionResult<ConvertResponse<string>>> JsonToYaml([FromBody] YamlConvertRequest request)
    {
        var result = await _dataFormatService.JsonToYamlAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Markdown 相关端点
    [HttpPost("markdown-to-excel")]
    public async Task<ActionResult> MarkdownToExcel([FromBody] MarkdownConvertRequest request)
    {
        var result = await _dataFormatService.MarkdownToExcelAsync(request);
        if (!result.Success || result.Data == null)
            return BadRequest(result.Message);

        return File(result.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{request.FileName}.xlsx");
    }

    [HttpPost("excel-to-markdown")]
    public async Task<ActionResult<ConvertResponse<string>>> ExcelToMarkdown(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ConvertResponse<string> { Success = false, Message = "请上传Excel文件" });

        var result = await _dataFormatService.ExcelToMarkdownAsync(file);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // 数据清洗端点
    [HttpPost("clean-data")]
    public async Task<ActionResult<ConvertResponse<List<Dictionary<string, string>> >>> CleanData([FromBody] DataCleanRequest request)
    {
        var result = await _dataFormatService.CleanDataAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
