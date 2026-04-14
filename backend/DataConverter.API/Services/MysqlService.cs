using System.Data;
using System.Text;
using MiniExcelLibs;
using MySqlConnector;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IMysqlService
{
    Task<ConvertResponse<byte[]>> MysqlToExcelAsync(DatabaseToExcelRequest request);
    Task<ConvertResponse<MysqlQueryResult>> MysqlToPreviewAsync(DatabaseToExcelRequest request);
    Task<ConvertResponse<string>> ExcelToMysqlAsync(IFormFile file, ExcelToMysqlRequest request);
    Task<ConvertResponse<List<string>>> GetTableListAsync(DatabaseToExcelRequest request);
}

public class MysqlService : IMysqlService
{
    public async Task<ConvertResponse<byte[]>> MysqlToExcelAsync(DatabaseToExcelRequest request)
    {
        try
        {
            var queryResult = await ExecuteQueryAsync(request);
            if (!queryResult.Success)
                return new ConvertResponse<byte[]> { Success = false, Message = queryResult.Message };

            var data = new List<Dictionary<string, object?>>();
            foreach (var row in queryResult.Data!.Rows)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var col in queryResult.Data.Columns)
                    dict[col] = row.TryGetValue(col, out var val) ? val : "";
                data.Add(dict);
            }

            using var stream = new MemoryStream();
            await stream.SaveAsAsync(data);
            return new ConvertResponse<byte[]> { Success = true, Data = stream.ToArray() };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<byte[]> { Success = false, Message = $"导出失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<MysqlQueryResult>> MysqlToPreviewAsync(DatabaseToExcelRequest request)
    {
        return await ExecuteQueryAsync(request);
    }

    public async Task<ConvertResponse<string>> ExcelToMysqlAsync(IFormFile file, ExcelToMysqlRequest request)
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var rows = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>().ToList();
            if (rows.Count == 0)
                return new ConvertResponse<string> { Success = false, Message = "Excel文件中没有数据" };

            var columns = rows[0].Keys.ToList();
            var connStr = BuildConnectionString(request);

            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            if (request.CreateIfNotExists)
            {
                var createSql = BuildCreateTableSql(request.TableName, columns, rows);
                await using var createCmd = new MySqlCommand(createSql, conn);
                await createCmd.ExecuteNonQueryAsync();
            }

            foreach (var row in rows)
            {
                var insertSql = BuildMySqlInsert(request.TableName, columns, row);
                await using var insertCmd = new MySqlCommand(insertSql, conn);
                await insertCmd.ExecuteNonQueryAsync();
            }

            return new ConvertResponse<string> { Success = true, Data = $"成功导入 {rows.Count} 条数据到表 {request.TableName}" };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<string> { Success = false, Message = $"导入失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<List<string>>> GetTableListAsync(DatabaseToExcelRequest request)
    {
        try
        {
            var connStr = BuildConnectionString(request);
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var tables = new List<string>();
            await using var cmd = new MySqlCommand("SHOW TABLES", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));

            return new ConvertResponse<List<string>> { Success = true, Data = tables };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<List<string>> { Success = false, Message = $"连接失败: {ex.Message}" };
        }
    }

    private async Task<ConvertResponse<MysqlQueryResult>> ExecuteQueryAsync(DatabaseToExcelRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return new ConvertResponse<MysqlQueryResult> { Success = false, Message = "SQL查询语句不能为空" };

            var connStr = BuildConnectionString(request);
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            await using var cmd = new MySqlCommand(request.Query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));

            var rows = new List<Dictionary<string, string>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, string>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.GetValue(i)?.ToString() ?? "";
                rows.Add(row);
            }

            return new ConvertResponse<MysqlQueryResult>
            {
                Success = true,
                Data = new MysqlQueryResult { Columns = columns, Rows = rows, TotalRows = rows.Count }
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<MysqlQueryResult> { Success = false, Message = $"查询失败: {ex.Message}" };
        }
    }

    private static string BuildConnectionString(DatabaseToExcelRequest request)
    {
        return $"Server={request.Host};Port={request.Port};Database={request.Database};User={request.Username};Password={request.Password};Charset=utf8mb4;";
    }

    private static string BuildConnectionString(ExcelToMysqlRequest request)
    {
        return $"Server={request.Host};Port={request.Port};Database={request.Database};User={request.Username};Password={request.Password};Charset=utf8mb4;";
    }

    private static string BuildCreateTableSql(string tableName, List<string> columns, List<IDictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS `{tableName}` (");
        sb.AppendLine("    `id` INT AUTO_INCREMENT PRIMARY KEY,");

        foreach (var col in columns)
        {
            var mysqlType = InferMySqlType(col, rows);
            sb.AppendLine($"    `{col}` {mysqlType},");
        }

        var sql = sb.ToString().TrimEnd(',', '\r', '\n');
        return sql + "\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
    }

    private static string InferMySqlType(string columnName, List<IDictionary<string, object?>> rows)
    {
        foreach (var row in rows)
        {
            if (row.TryGetValue(columnName, out var value) && value != null)
            {
                var strVal = value.ToString() ?? "";
                if (int.TryParse(strVal, out _)) return "INT";
                if (long.TryParse(strVal, out _)) return "BIGINT";
                if (double.TryParse(strVal, out _)) return "DOUBLE";
                if (decimal.TryParse(strVal, out _)) return "DECIMAL(18,2)";
                if (DateTime.TryParse(strVal, out _)) return "DATETIME";
                if (bool.TryParse(strVal, out _)) return "TINYINT(1)";
                if (strVal.Length > 255) return "TEXT";
            }
        }
        return "VARCHAR(255)";
    }

    private static string BuildMySqlInsert(string tableName, List<string> columns, IDictionary<string, object?> row)
    {
        var colNames = string.Join(", ", columns.Select(c => $"`{c}`"));
        var values = string.Join(", ", columns.Select(c =>
        {
            var val = row.TryGetValue(c, out var v) ? v?.ToString() ?? "NULL" : "NULL";
            if (val == "NULL") return "NULL";
            return $"'{val.Replace("'", "''")}'";
        }));
        return $"INSERT INTO `{tableName}` ({colNames}) VALUES ({values});";
    }
}
