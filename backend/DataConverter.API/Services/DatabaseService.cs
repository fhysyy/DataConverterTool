using System.Data;
using System.Text;
using MiniExcelLibs;
using MySqlConnector;
using Microsoft.Data.SqlClient;
using Npgsql;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IDatabaseService
{
    Task<ConvertResponse<byte[]>> DatabaseToExcelAsync(DatabaseToExcelRequest request);
    Task<ConvertResponse<MysqlQueryResult>> DatabaseToPreviewAsync(DatabaseToExcelRequest request);
    Task<ConvertResponse<List<string>>> GetTableListAsync(DatabaseToExcelRequest request);
    Task<ConvertResponse<byte[]>> PasteDataToExcelAsync(PasteDataRequest request);
    Task<ConvertResponse<MysqlQueryResult>> PasteDataToPreviewAsync(PasteDataRequest request);
}

public class DatabaseService : IDatabaseService
{
    public async Task<ConvertResponse<byte[]>> DatabaseToExcelAsync(DatabaseToExcelRequest request)
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

    public async Task<ConvertResponse<MysqlQueryResult>> DatabaseToPreviewAsync(DatabaseToExcelRequest request)
    {
        return await ExecuteQueryAsync(request);
    }

    public async Task<ConvertResponse<List<string>>> GetTableListAsync(DatabaseToExcelRequest request)
    {
        try
        {
            var query = request.DatabaseType switch
            {
                DatabaseType.SqlServer => "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME",
                DatabaseType.PostgreSql => "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name",
                _ => "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME"
            };

            return request.DatabaseType switch
            {
                DatabaseType.SqlServer => await GetTablesSqlAsync(request, query),
                DatabaseType.PostgreSql => await GetTablesPostgreAsync(request, query),
                _ => await GetTablesMySqlAsync(request, query)
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<List<string>> { Success = false, Message = $"获取表列表失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<byte[]>> PasteDataToExcelAsync(PasteDataRequest request)
    {
        try
        {
            var parseResult = ParsePasteData(request);
            if (!parseResult.Success)
                return new ConvertResponse<byte[]> { Success = false, Message = parseResult.Message };

            var data = new List<Dictionary<string, object?>>();
            foreach (var row in parseResult.Data!.Rows)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var col in parseResult.Data.Columns)
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

    public Task<ConvertResponse<MysqlQueryResult>> PasteDataToPreviewAsync(PasteDataRequest request)
    {
        return Task.FromResult(ParsePasteData(request));
    }

    private static async Task<ConvertResponse<MysqlQueryResult>> ExecuteQueryAsync(DatabaseToExcelRequest request)
    {
        try
        {
            return request.DatabaseType switch
            {
                DatabaseType.SqlServer => await ExecuteQuerySqlAsync(request),
                DatabaseType.PostgreSql => await ExecuteQueryPostgreAsync(request),
                _ => await ExecuteQueryMySqlAsync(request)
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<MysqlQueryResult> { Success = false, Message = $"查询失败: {ex.Message}" };
        }
    }

    private static async Task<ConvertResponse<MysqlQueryResult>> ExecuteQueryMySqlAsync(DatabaseToExcelRequest request)
    {
        await using var connection = new MySqlConnection(
            $"Server={request.Host};Port={request.Port};Database={request.Database};User Id={request.Username};Password={request.Password};");
        await connection.OpenAsync();

        await using var command = new MySqlCommand(request.Query, connection);
        command.CommandTimeout = 120;

        await using var reader = await command.ExecuteReaderAsync();

        var columns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        var rows = new List<Dictionary<string, string>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString() ?? "";
                row[columns[i]] = value;
            }
            rows.Add(row);
        }

        return new ConvertResponse<MysqlQueryResult>
        {
            Success = true,
            Data = new MysqlQueryResult { Columns = columns, Rows = rows, TotalRows = rows.Count }
        };
    }

    private static async Task<ConvertResponse<MysqlQueryResult>> ExecuteQuerySqlAsync(DatabaseToExcelRequest request)
    {
        await using var connection = new SqlConnection(
            $"Server={request.Host},{request.Port};Database={request.Database};User Id={request.Username};Password={request.Password};TrustServerCertificate=True;");
        await connection.OpenAsync();

        await using var command = new SqlCommand(request.Query, connection);
        command.CommandTimeout = 120;

        await using var reader = await command.ExecuteReaderAsync();

        var columns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        var rows = new List<Dictionary<string, string>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString() ?? "";
                row[columns[i]] = value;
            }
            rows.Add(row);
        }

        return new ConvertResponse<MysqlQueryResult>
        {
            Success = true,
            Data = new MysqlQueryResult { Columns = columns, Rows = rows, TotalRows = rows.Count }
        };
    }

    private static async Task<ConvertResponse<MysqlQueryResult>> ExecuteQueryPostgreAsync(DatabaseToExcelRequest request)
    {
        await using var connection = new NpgsqlConnection(
            $"Host={request.Host};Port={request.Port};Database={request.Database};Username={request.Username};Password={request.Password};");
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(request.Query, connection);
        command.CommandTimeout = 120;

        await using var reader = await command.ExecuteReaderAsync();

        var columns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        var rows = new List<Dictionary<string, string>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString() ?? "";
                row[columns[i]] = value;
            }
            rows.Add(row);
        }

        return new ConvertResponse<MysqlQueryResult>
        {
            Success = true,
            Data = new MysqlQueryResult { Columns = columns, Rows = rows, TotalRows = rows.Count }
        };
    }

    private static async Task<ConvertResponse<List<string>>> GetTablesMySqlAsync(DatabaseToExcelRequest request, string query)
    {
        await using var connection = new MySqlConnection(
            $"Server={request.Host};Port={request.Port};Database={request.Database};User Id={request.Username};Password={request.Password};");
        await connection.OpenAsync();

        var tables = new List<string>();
        await using var command = new MySqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));

        return new ConvertResponse<List<string>> { Success = true, Data = tables };
    }

    private static async Task<ConvertResponse<List<string>>> GetTablesSqlAsync(DatabaseToExcelRequest request, string query)
    {
        await using var connection = new SqlConnection(
            $"Server={request.Host},{request.Port};Database={request.Database};User Id={request.Username};Password={request.Password};TrustServerCertificate=True;");
        await connection.OpenAsync();

        var tables = new List<string>();
        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));

        return new ConvertResponse<List<string>> { Success = true, Data = tables };
    }

    private static async Task<ConvertResponse<List<string>>> GetTablesPostgreAsync(DatabaseToExcelRequest request, string query)
    {
        await using var connection = new NpgsqlConnection(
            $"Host={request.Host};Port={request.Port};Database={request.Database};Username={request.Username};Password={request.Password};");
        await connection.OpenAsync();

        var tables = new List<string>();
        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));

        return new ConvertResponse<List<string>> { Success = true, Data = tables };
    }

    private static ConvertResponse<MysqlQueryResult> ParsePasteData(PasteDataRequest request)
    {
        try
        {
            var lines = request.Data.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return new ConvertResponse<MysqlQueryResult> { Success = false, Message = "数据为空" };

            var rows = new List<Dictionary<string, string>>();
            var columns = new List<string>();

            var delimiter = request.Delimiter == "\\t" ? "\t" : request.Delimiter;

            if (request.HasHeader && lines.Length > 0)
            {
                var headerLine = lines[0];
                columns = headerLine.Split(delimiter).Select(c => c.Trim()).ToList();
                lines = lines.Skip(1).ToArray();
            }

            foreach (var line in lines)
            {
                var values = line.Split(delimiter);
                var row = new Dictionary<string, string>();
                for (var i = 0; i < values.Length; i++)
                {
                    var colName = columns.Count > i ? columns[i] : $"Column{i + 1}";
                    row[colName] = values[i].Trim();
                }
                rows.Add(row);
            }

            if (columns.Count == 0 && rows.Count > 0)
            {
                var maxCols = rows.Max(r => r.Count);
                for (var i = 0; i < maxCols; i++)
                    columns.Add($"Column{i + 1}");
            }

            return new ConvertResponse<MysqlQueryResult>
            {
                Success = true,
                Data = new MysqlQueryResult
                {
                    Columns = columns,
                    Rows = rows,
                    TotalRows = rows.Count
                }
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<MysqlQueryResult> { Success = false, Message = $"解析失败: {ex.Message}" };
        }
    }
}
