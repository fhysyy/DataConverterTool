using System.Data;
using System.Text;
using System.Text.Json;
using MySqlConnector;
using Microsoft.Data.SqlClient;
using Npgsql;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IDatabaseToJsonService
{
    Task<ConvertResponse<DatabaseToJsonResult>> DatabaseToJsonAsync(DatabaseToJsonRequest request);
}

public class DatabaseToJsonService : IDatabaseToJsonService
{
    public async Task<ConvertResponse<DatabaseToJsonResult>> DatabaseToJsonAsync(DatabaseToJsonRequest request)
    {
        try
        {
            var result = await ExecuteQueryAsync(request);
            if (!result.Success)
                return new ConvertResponse<DatabaseToJsonResult> { Success = false, Message = result.Message };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = request.FormatOutput,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(result.Data, jsonOptions);

            return new ConvertResponse<DatabaseToJsonResult>
            {
                Success = true,
                Data = new DatabaseToJsonResult
                {
                    Json = json,
                    TotalRows = result.Data?.Count ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<DatabaseToJsonResult> { Success = false, Message = $"转换失败: {ex.Message}" };
        }
    }

    private static async Task<ConvertResponse<List<Dictionary<string, object>>>> ExecuteQueryAsync(DatabaseToJsonRequest request)
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
            return new ConvertResponse<List<Dictionary<string, object>>> { Success = false, Message = $"查询失败: {ex.Message}" };
        }
    }

    private static async Task<ConvertResponse<List<Dictionary<string, object>>>> ExecuteQueryMySqlAsync(DatabaseToJsonRequest request)
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

        var results = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columns[i]] = value;
            }
            results.Add(row);
        }

        return new ConvertResponse<List<Dictionary<string, object>>> { Success = true, Data = results };
    }

    private static async Task<ConvertResponse<List<Dictionary<string, object>>>> ExecuteQuerySqlAsync(DatabaseToJsonRequest request)
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

        var results = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columns[i]] = value;
            }
            results.Add(row);
        }

        return new ConvertResponse<List<Dictionary<string, object>>> { Success = true, Data = results };
    }

    private static async Task<ConvertResponse<List<Dictionary<string, object>>>> ExecuteQueryPostgreAsync(DatabaseToJsonRequest request)
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

        var results = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columns[i]] = value;
            }
            results.Add(row);
        }

        return new ConvertResponse<List<Dictionary<string, object>>> { Success = true, Data = results };
    }
}
