namespace DataConverter.API.Models;

public class ExcelToSqlRequest
{
    public string TableName { get; set; } = "MyTable";
    public SqlType SqlType { get; set; } = SqlType.Insert;
    public bool IncludeCreateTable { get; set; } = true;
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
}

public enum SqlType
{
    Insert,
    Update,
    Create
}

public enum DatabaseType
{
    SqlServer,
    MySql,
    PostgreSql
}

public class TableToJsonRequest
{
    public List<Dictionary<string, string>> Rows { get; set; } = [];
}

public class TableToExcelRequest
{
    public List<string> Headers { get; set; } = [];
    public List<Dictionary<string, string>> Rows { get; set; } = [];
    public string FileName { get; set; } = "export";
}

public class JsonToClassRequest
{
    public string Json { get; set; } = "";
    public string ClassName { get; set; } = "RootEntity";
    public LanguageType Language { get; set; } = LanguageType.CSharp;
}

public enum LanguageType
{
    CSharp,
    Java,
    TypeScript
}

public class ConvertResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }
}

public class ExcelToSqlResult
{
    public string CreateTableSql { get; set; } = "";
    public List<string> DataSqlList { get; set; } = [];
}

public class JsonToClassResult
{
    public string Code { get; set; } = "";
    public string Language { get; set; } = "";
}

public class ExcelCsvConvertResult
{
    public string Content { get; set; } = "";
    public string FileName { get; set; } = "";
}

public class JsonExcelConvertRequest
{
    public string Json { get; set; } = "";
    public string FileName { get; set; } = "export";
}

public class MysqlConnectionRequest
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "";
    public string Username { get; set; } = "root";
    public string Password { get; set; } = "";
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;
}

public class DatabaseToExcelRequest : MysqlConnectionRequest
{
    public string Query { get; set; } = "";
    public string FileName { get; set; } = "export";
}

public class PasteDataRequest
{
    public string Data { get; set; } = "";
    public string FileName { get; set; } = "export";
    public string Delimiter { get; set; } = "\t";
    public bool HasHeader { get; set; } = true;
}

public class ExcelToMysqlRequest : MysqlConnectionRequest
{
    public string TableName { get; set; } = "imported_table";
    public bool CreateIfNotExists { get; set; } = true;
}

public class MysqlQueryResult
{
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, string>> Rows { get; set; } = [];
    public int TotalRows { get; set; }
}

public class DataGeneratorRequest
{
    public string TableName { get; set; } = "users";
    public int RowCount { get; set; } = 10;
    public List<ColumnDefinition> Columns { get; set; } = [];
    public GeneratorOutputType OutputType { get; set; } = GeneratorOutputType.Sql;
}

public class ColumnDefinition
{
    public string Name { get; set; } = "";
    public ColumnDataType DataType { get; set; } = ColumnDataType.AutoIncrement;
    public string? CustomValues { get; set; }
}

public enum ColumnDataType
{
    AutoIncrement,
    FullName,
    FirstName,
    LastName,
    Email,
    Phone,
    Address,
    City,
    Company,
    JobTitle,
    Age,
    Birthday,
    DateTime,
    Number,
    Decimal,
    Boolean,
    Uuid,
    Url,
    IpAddress,
    Custom
}

public enum GeneratorOutputType
{
    Sql,
    Json,
    Csv,
    Excel
}

public class DataGeneratorResult
{
    public string Content { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "text/plain";
}

public class JsonToSqlRequest
{
    public string Json { get; set; } = "";
    public string TableName { get; set; } = "MyTable";
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
    public SqlType SqlType { get; set; } = SqlType.Insert;
    public bool IncludeCreateTable { get; set; } = true;
    public List<ColumnMapping> ColumnMappings { get; set; } = [];
}

public class ColumnMapping
{
    public string SourceName { get; set; } = "";
    public string TargetName { get; set; } = "";
    public string SqlType { get; set; } = "";
    public bool Include { get; set; } = true;
}

public class JsonToSqlResult
{
    public string CreateTableSql { get; set; } = "";
    public List<string> DataSqlList { get; set; } = [];
    public List<ColumnMapping> AppliedMappings { get; set; } = [];
}

// 数据库相关模型
public class DatabaseToSqlRequest : MysqlConnectionRequest
{
    public string TableName { get; set; } = "";
    public bool IncludeIndexes { get; set; } = true;
    public bool IncludeForeignKeys { get; set; } = true;
}

public class DatabaseToSqlResult
{
    public string CreateTableSql { get; set; } = "";
    public List<string> IndexSqlList { get; set; } = [];
    public List<string> ForeignKeySqlList { get; set; } = [];
}

public class DatabaseToJsonRequest : MysqlConnectionRequest
{
    public string Query { get; set; } = "";
    public bool FormatOutput { get; set; } = true;
}

public class DatabaseToJsonResult
{
    public string Json { get; set; } = "";
    public int TotalRows { get; set; }
}

// 数据格式相关模型
public class XmlConvertRequest
{
    public string Content { get; set; } = "";
    public string FileName { get; set; } = "export";
}

public class YamlConvertRequest
{
    public string Content { get; set; } = "";
    public string FileName { get; set; } = "export";
}

public class MarkdownConvertRequest
{
    public string Content { get; set; } = "";
    public string FileName { get; set; } = "export";
    public bool HasHeader { get; set; } = true;
}

public class DataCleanRequest
{
    public List<Dictionary<string, string>> Rows { get; set; } = [];
    public bool RemoveDuplicates { get; set; } = true;
    public bool TrimWhitespace { get; set; } = true;
    public bool RemoveEmptyRows { get; set; } = true;
}

// 数据库连接管理模型
public class ConnectionConfig
{
    public string Name { get; set; } = "";
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ConnectionConfigRequest
{
    public List<ConnectionConfig> Configs { get; set; } = [];
}

