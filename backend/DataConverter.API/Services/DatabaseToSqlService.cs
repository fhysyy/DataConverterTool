using System.Data;
using System.Text;
using MySqlConnector;
using Microsoft.Data.SqlClient;
using Npgsql;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IDatabaseToSqlService
{
    Task<ConvertResponse<DatabaseToSqlResult>> DatabaseToSqlAsync(DatabaseToSqlRequest request);
    Task<ConvertResponse<List<string>>> GetTableListAsync(DatabaseToSqlRequest request);
}

public class DatabaseToSqlService : IDatabaseToSqlService
{
    public async Task<ConvertResponse<DatabaseToSqlResult>> DatabaseToSqlAsync(DatabaseToSqlRequest request)
    {
        try
        {
            var result = new DatabaseToSqlResult();

            switch (request.DatabaseType)
            {
                case DatabaseType.MySql:
                    result = await GenerateMySqlSchemaAsync(request);
                    break;
                case DatabaseType.SqlServer:
                    result = await GenerateSqlServerSchemaAsync(request);
                    break;
                case DatabaseType.PostgreSql:
                    result = await GeneratePostgreSqlSchemaAsync(request);
                    break;
            }

            return new ConvertResponse<DatabaseToSqlResult> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<DatabaseToSqlResult> { Success = false, Message = $"生成建表语句失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<List<string>>> GetTableListAsync(DatabaseToSqlRequest request)
    {
        try
        {
            var query = request.DatabaseType switch
            {
                DatabaseType.SqlServer => string.IsNullOrEmpty(request.Schema) 
                    ? "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME"
                    : $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = '{request.Schema}' ORDER BY TABLE_NAME",
                DatabaseType.PostgreSql => string.IsNullOrEmpty(request.Schema) 
                    ? "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name"
                    : $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{request.Schema}' ORDER BY table_name",
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

    private static async Task<DatabaseToSqlResult> GenerateMySqlSchemaAsync(DatabaseToSqlRequest request)
    {
        var databaseName = string.IsNullOrEmpty(request.Schema) ? request.Database : request.Schema;
        await using var connection = new MySqlConnection(
            $"Server={request.Host};Port={request.Port};Database={databaseName};User Id={request.Username};Password={request.Password};");
        await connection.OpenAsync();

        var result = new DatabaseToSqlResult();

        // 获取表结构
        var columnsSql = $"SHOW CREATE TABLE `{request.TableName}`";
        await using var cmd = new MySqlCommand(columnsSql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            result.CreateTableSql = reader.GetString(1);
        }
        await reader.CloseAsync();

        // 获取索引
        if (request.IncludeIndexes)
        {
            var indexesSql = $"SHOW INDEX FROM `{request.TableName}`";
            await using var indexCmd = new MySqlCommand(indexesSql, connection);
            await using var indexReader = await indexCmd.ExecuteReaderAsync();
            var indexes = new Dictionary<string, List<string>>();
            while (await indexReader.ReadAsync())
            {
                var indexName = indexReader.GetString(2);
                var columnName = indexReader.GetString(4);
                if (!indexes.ContainsKey(indexName))
                    indexes[indexName] = new List<string>();
                indexes[indexName].Add(columnName);
            }
            await indexReader.CloseAsync();

            foreach (var (indexName, columns) in indexes)
            {
                var indexSql = $"CREATE INDEX `{indexName}` ON `{request.TableName}` ({string.Join(", ", columns.Select(c => $"`{c}`"))});";
                result.IndexSqlList.Add(indexSql);
            }
        }

        return result;
    }

    private static async Task<DatabaseToSqlResult> GenerateSqlServerSchemaAsync(DatabaseToSqlRequest request)
    {
        await using var connection = new SqlConnection(
            $"Server={request.Host},{request.Port};Database={request.Database};User Id={request.Username};Password={request.Password};TrustServerCertificate=True;");
        await connection.OpenAsync();

        var result = new DatabaseToSqlResult();

        // 获取表结构
        var tableSchemaCondition = string.IsNullOrEmpty(request.Schema) ? "" : $"AND t.TABLE_SCHEMA = '{request.Schema}' AND c.TABLE_SCHEMA = '{request.Schema}'";
        var tableSql = $@"
            SELECT 
                t.TABLE_NAME, 
                c.COLUMN_NAME, 
                c.DATA_TYPE, 
                c.CHARACTER_MAXIMUM_LENGTH, 
                c.IS_NULLABLE, 
                c.COLUMN_DEFAULT
            FROM 
                INFORMATION_SCHEMA.TABLES t
            JOIN 
                INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
            WHERE 
                t.TABLE_NAME = '{request.TableName}'
                {tableSchemaCondition}
            ORDER BY 
                c.ORDINAL_POSITION
        ";

        await using var cmd = new SqlCommand(tableSql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(1);
            var dataType = reader.GetString(2);
            var maxLength = reader.IsDBNull(3) ? "" : $"({reader.GetInt32(3)})";
            var isNullable = reader.GetString(4) == "YES" ? "NULL" : "NOT NULL";
            var defaultValue = reader.IsDBNull(5) ? "" : $"DEFAULT {reader.GetString(5)}";

            columns.Add($"    [{columnName}] {dataType}{maxLength} {isNullable} {defaultValue}".Trim());
        }
        await reader.CloseAsync();

        // 构建 CREATE TABLE 语句
        var tableNameWithSchema = string.IsNullOrEmpty(request.Schema) 
            ? $"[{request.TableName}]" 
            : $"[{request.Schema}].[{request.TableName}]";
        var createTableSql = $"CREATE TABLE {tableNameWithSchema} (\n";
        createTableSql += string.Join(",\n", columns);
        createTableSql += "\n);";
        result.CreateTableSql = createTableSql;

        // 获取索引
        if (request.IncludeIndexes)
        {
            var indexSchemaCondition = string.IsNullOrEmpty(request.Schema) ? "" : $"AND s.name = '{request.Schema}'";
            var indexSql = $@"
                SELECT 
                    i.name, 
                    c.name as column_name
                FROM 
                    sys.indexes i
                JOIN 
                    sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                JOIN 
                    sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                JOIN 
                    sys.tables t ON i.object_id = t.object_id
                JOIN
                    sys.schemas s ON t.schema_id = s.schema_id
                WHERE 
                    t.name = '{request.TableName}' AND i.type > 0
                    {indexSchemaCondition}
                ORDER BY 
                    i.name, ic.key_ordinal
            ";

            await using var indexCmd = new SqlCommand(indexSql, connection);
            await using var indexReader = await indexCmd.ExecuteReaderAsync();
            var indexes = new Dictionary<string, List<string>>();
            while (await indexReader.ReadAsync())
            {
                var indexName = indexReader.GetString(0);
                var columnName = indexReader.GetString(1);
                if (!indexes.ContainsKey(indexName))
                    indexes[indexName] = new List<string>();
                indexes[indexName].Add(columnName);
            }
            await indexReader.CloseAsync();

            foreach (var (indexName, indexColumns) in indexes)
            {
                var indexTableNameWithSchema = string.IsNullOrEmpty(request.Schema) 
                    ? $"[{request.TableName}]" 
                    : $"[{request.Schema}].[{request.TableName}]";
                var indexCreateSql = $"CREATE INDEX [{indexName}] ON {indexTableNameWithSchema} ({string.Join(", ", indexColumns.Select(c => $"[{c}]".ToString()))});";
                result.IndexSqlList.Add(indexCreateSql);
            }
        }

        return result;
    }

    private static async Task<DatabaseToSqlResult> GeneratePostgreSqlSchemaAsync(DatabaseToSqlRequest request)
    {
        await using var connection = new NpgsqlConnection(
            $"Host={request.Host};Port={request.Port};Database={request.Database};Username={request.Username};Password={request.Password};");
        await connection.OpenAsync();

        var result = new DatabaseToSqlResult();

        // 获取表结构
        var schema = string.IsNullOrEmpty(request.Schema) ? "public" : request.Schema;
        var tableSql = $@"
            SELECT 
                column_name, 
                data_type, 
                character_maximum_length, 
                is_nullable, 
                column_default
            FROM 
                information_schema.columns
            WHERE 
                table_name = '{request.TableName}'
                AND table_schema = '{schema}'
            ORDER BY 
                ordinal_position
        ";

        await using var cmd = new NpgsqlCommand(tableSql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var maxLength = reader.IsDBNull(2) ? "" : $"({reader.GetInt32(2)})";
            var isNullable = reader.GetString(3) == "YES" ? "NULL" : "NOT NULL";
            var defaultValue = reader.IsDBNull(4) ? "" : $"DEFAULT {reader.GetString(4)}";

            columns.Add($"    \"{columnName}\" {dataType}{maxLength} {isNullable} {defaultValue}".Trim());
        }
        await reader.CloseAsync();

        // 构建 CREATE TABLE 语句
        var tableNameWithSchema = string.IsNullOrEmpty(request.Schema) 
            ? $"\"{request.TableName}\"" 
            : $"\"{schema}\".\"{request.TableName}\"";
        var createTableSql = $"CREATE TABLE {tableNameWithSchema} (\n";
        createTableSql += string.Join(",\n", columns);
        createTableSql += "\n);";
        result.CreateTableSql = createTableSql;

        // 获取索引
        if (request.IncludeIndexes)
        {
            var indexSql = $@"
                SELECT 
                    i.relname as index_name, 
                    a.attname as column_name
                FROM 
                    pg_index ix
                JOIN 
                    pg_class i ON i.oid = ix.indexrelid
                JOIN 
                    pg_class t ON t.oid = ix.indrelid
                JOIN 
                    pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
                JOIN
                    pg_namespace n ON t.relnamespace = n.oid
                WHERE 
                    t.relname = '{request.TableName}'
                    AND n.nspname = '{schema}'
                ORDER BY 
                    i.relname, ix.indkey
            ";

            await using var indexCmd = new NpgsqlCommand(indexSql, connection);
            await using var indexReader = await indexCmd.ExecuteReaderAsync();
            var indexes = new Dictionary<string, List<string>>();
            while (await indexReader.ReadAsync())
            {
                var indexName = indexReader.GetString(0);
                var columnName = indexReader.GetString(1);
                if (!indexes.ContainsKey(indexName))
                    indexes[indexName] = new List<string>();
                indexes[indexName].Add(columnName);
            }
            await indexReader.CloseAsync();

            foreach (var (indexName, indexColumns) in indexes)
            {
                var columnsStr = string.Join(", ", indexColumns.Select(c => $"\"{c}\""));
                var indexTableNameWithSchema = string.IsNullOrEmpty(request.Schema) 
                    ? $"\"{request.TableName}\"" 
                    : $"\"{schema}\".\"{request.TableName}\"";
                var indexCreateSql = $"CREATE INDEX \"{indexName}\" ON {indexTableNameWithSchema} ({columnsStr});";
                result.IndexSqlList.Add(indexCreateSql);
            }
        }

        return result;
    }

    private static async Task<ConvertResponse<List<string>>> GetTablesMySqlAsync(DatabaseToSqlRequest request, string query)
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

    private static async Task<ConvertResponse<List<string>>> GetTablesSqlAsync(DatabaseToSqlRequest request, string query)
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

    private static async Task<ConvertResponse<List<string>>> GetTablesPostgreAsync(DatabaseToSqlRequest request, string query)
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
}
