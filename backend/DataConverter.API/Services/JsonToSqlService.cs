using System.Text;
using System.Text.Json;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IJsonToSqlService
{
    ConvertResponse<JsonToSqlResult> JsonToSql(JsonToSqlRequest request);
    ConvertResponse<List<ColumnMapping>> AnalyzeJsonColumns(string json);
}

internal record JsonCellValue(JsonValueKind Kind, string? StringVal, string? RawVal)
{
    public bool IsNull => Kind == JsonValueKind.Null || Kind == JsonValueKind.Undefined;
    public bool IsNumber => Kind == JsonValueKind.Number;
    public bool IsBoolean => Kind is JsonValueKind.True or JsonValueKind.False;
    public bool IsString => Kind == JsonValueKind.String;
    public string AsString() => StringVal ?? RawVal ?? "";
}

public class JsonToSqlService : IJsonToSqlService
{
    public ConvertResponse<List<ColumnMapping>> AnalyzeJsonColumns(string json)
    {
        try
        {
            var rows = ParseJsonArray(json);
            if (rows.Count == 0)
                return new ConvertResponse<List<ColumnMapping>> { Success = false, Message = "JSON数组为空或格式不正确" };

            var allKeys = new HashSet<string>();
            foreach (var row in rows)
                foreach (var key in row.Keys)
                    allKeys.Add(key);

            var mappings = allKeys.Select(key =>
            {
                var inferredType = InferTypeFromValues(key, rows);
                return new ColumnMapping
                {
                    SourceName = key,
                    TargetName = key,
                    SqlType = inferredType,
                    Include = true
                };
            }).ToList();

            return new ConvertResponse<List<ColumnMapping>> { Success = true, Data = mappings };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<List<ColumnMapping>> { Success = false, Message = $"解析失败: {ex.Message}" };
        }
    }

    public ConvertResponse<JsonToSqlResult> JsonToSql(JsonToSqlRequest request)
    {
        try
        {
            var rows = ParseJsonArray(request.Json);
            if (rows.Count == 0)
                return new ConvertResponse<JsonToSqlResult> { Success = false, Message = "JSON数组为空或格式不正确" };

            var dialect = SqlDialectFactory.GetDialect(request.DatabaseType);
            var mappings = BuildEffectiveMappings(request.ColumnMappings, rows[0].Keys.ToList());
            var includedMappings = mappings.Where(m => m.Include).ToList();

            if (includedMappings.Count == 0)
                return new ConvertResponse<JsonToSqlResult> { Success = false, Message = "至少需要包含一列" };

            var result = new JsonToSqlResult { AppliedMappings = includedMappings };

            if (request.IncludeCreateTable || request.SqlType == SqlType.Create)
            {
                result.CreateTableSql = GenerateCreateTableSql(dialect, request.TableName, includedMappings);
            }

            if (request.SqlType == SqlType.Create)
                return new ConvertResponse<JsonToSqlResult> { Success = true, Data = result };

            foreach (var row in rows)
            {
                result.DataSqlList.Add(request.SqlType switch
                {
                    SqlType.Update => GenerateUpdateSql(dialect, request.TableName, includedMappings, row),
                    _ => GenerateInsertSql(dialect, request.TableName, includedMappings, row)
                });
            }

            return new ConvertResponse<JsonToSqlResult> { Success = true, Data = result };
        }
        catch (JsonException ex)
        {
            return new ConvertResponse<JsonToSqlResult> { Success = false, Message = $"JSON格式错误: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<JsonToSqlResult> { Success = false, Message = $"转换失败: {ex.Message}" };
        }
    }

    private static List<Dictionary<string, JsonCellValue>> ParseJsonArray(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("JSON必须是数组格式");

        var rows = new List<Dictionary<string, JsonCellValue>>();
        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object) continue;
            var dict = new Dictionary<string, JsonCellValue>();
            foreach (var prop in element.EnumerateObject())
            {
                var val = prop.Value;
                dict[prop.Name] = new JsonCellValue(
                    val.ValueKind,
                    val.ValueKind == JsonValueKind.String ? val.GetString() : null,
                    val.ValueKind != JsonValueKind.Null && val.ValueKind != JsonValueKind.Undefined
                        ? val.GetRawText() : null
                );
            }
            rows.Add(dict);
        }
        return rows;
    }

    private static List<ColumnMapping> BuildEffectiveMappings(List<ColumnMapping> userMappings, List<string> sourceKeys)
    {
        if (userMappings == null || userMappings.Count == 0)
        {
            return sourceKeys.Select(key => new ColumnMapping
            {
                SourceName = key,
                TargetName = key,
                SqlType = "",
                Include = true
            }).ToList();
        }

        var mappingDict = userMappings.ToDictionary(m => m.SourceName, m => m);
        var result = new List<ColumnMapping>();

        foreach (var key in sourceKeys)
        {
            if (mappingDict.TryGetValue(key, out var mapping))
            {
                result.Add(new ColumnMapping
                {
                    SourceName = mapping.SourceName,
                    TargetName = string.IsNullOrWhiteSpace(mapping.TargetName) ? mapping.SourceName : mapping.TargetName,
                    SqlType = mapping.SqlType,
                    Include = mapping.Include
                });
            }
            else
            {
                result.Add(new ColumnMapping
                {
                    SourceName = key,
                    TargetName = key,
                    SqlType = "",
                    Include = true
                });
            }
        }

        return result;
    }

    private static string InferTypeFromValues(string key, List<Dictionary<string, JsonCellValue>> rows)
    {
        foreach (var row in rows)
        {
            if (!row.TryGetValue(key, out var val)) continue;

            if (val.IsNumber)
            {
                if (int.TryParse(val.AsString(), out _)) return "INT";
                if (long.TryParse(val.AsString(), out _)) return "BIGINT";
                return "FLOAT";
            }
            if (val.IsBoolean) return "BOOLEAN";
            if (val.IsString)
            {
                var str = val.AsString();
                if (DateTime.TryParse(str, out _)) return "DATETIME";
                if (str.Length > 255) return "TEXT";
                return "VARCHAR(255)";
            }
        }
        return "VARCHAR(255)";
    }

    private static ColumnType InferColumnTypeFromMapping(string sqlType)
    {
        var upper = sqlType.ToUpperInvariant();
        if (upper.Contains("INT") && !upper.Contains("BIGINT")) return ColumnType.Int;
        if (upper.Contains("BIGINT")) return ColumnType.BigInt;
        if (upper.Contains("FLOAT") || upper.Contains("DOUBLE") || upper.Contains("REAL") || upper.Contains("PRECISION")) return ColumnType.Float;
        if (upper.Contains("DECIMAL") || upper.Contains("NUMERIC")) return ColumnType.Decimal;
        if (upper.Contains("DATE") || upper.Contains("TIME") || upper.Contains("TIMESTAMP")) return ColumnType.DateTime;
        if (upper.Contains("BOOL") || upper.Contains("BIT") || upper.Contains("TINYINT")) return ColumnType.Boolean;
        if (upper.Contains("TEXT") || upper.Contains("MAX")) return ColumnType.LongText;
        return ColumnType.ShortText;
    }

    private static string GenerateCreateTableSql(SqlDialect dialect, string tableName, List<ColumnMapping> mappings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{dialect.CreateTablePrefix}{dialect.QuoteIdentifier(tableName)} (");

        if (dialect.AutoIncrementColumn is not null)
            sb.AppendLine($"    {dialect.QuoteIdentifier(dialect.AutoIncrementColumn.Name)} {dialect.AutoIncrementColumn.Definition},");

        foreach (var mapping in mappings)
        {
            var colType = string.IsNullOrWhiteSpace(mapping.SqlType)
                ? dialect.MapType(ColumnType.ShortText)
                : (IsBuiltInType(mapping.SqlType)
                    ? dialect.MapType(InferColumnTypeFromMapping(mapping.SqlType))
                    : mapping.SqlType);
            sb.AppendLine($"    {dialect.QuoteIdentifier(mapping.TargetName)} {colType},");
        }

        var sql = sb.ToString().TrimEnd(',', '\r', '\n');
        sql += "\n)";
        if (dialect.CreateTableSuffix is not null)
            sql += $" {dialect.CreateTableSuffix}";
        sql += ";";
        return sql;
    }

    private static bool IsBuiltInType(string sqlType)
    {
        var upper = sqlType.ToUpperInvariant();
        var genericTypes = new[] { "INT", "BIGINT", "FLOAT", "DOUBLE", "DECIMAL", "NUMERIC", "DATETIME", "TIMESTAMP", "BOOLEAN", "BIT", "TINYINT", "VARCHAR", "NVARCHAR", "TEXT" };
        return genericTypes.Any(t => upper.Contains(t));
    }

    private static string GenerateInsertSql(SqlDialect dialect, string tableName, List<ColumnMapping> mappings, Dictionary<string, JsonCellValue> row)
    {
        var colNames = string.Join(", ", mappings.Select(m => dialect.QuoteIdentifier(m.TargetName)));
        var values = string.Join(", ", mappings.Select(m => GetSqlValue(dialect, m, row)));
        return $"INSERT INTO {dialect.QuoteIdentifier(tableName)} ({colNames}) VALUES ({values});";
    }

    private static string GenerateUpdateSql(SqlDialect dialect, string tableName, List<ColumnMapping> mappings, Dictionary<string, JsonCellValue> row)
    {
        var setClause = string.Join(", ", mappings.Select(m =>
        {
            var sqlVal = GetSqlValue(dialect, m, row);
            return $"{dialect.QuoteIdentifier(m.TargetName)} = {sqlVal}";
        }));

        var firstMapping = mappings[0];
        var whereVal = GetSqlValue(dialect, firstMapping, row);
        return $"UPDATE {dialect.QuoteIdentifier(tableName)} SET {setClause} WHERE {dialect.QuoteIdentifier(firstMapping.TargetName)} = {whereVal};";
    }

    private static string GetSqlValue(SqlDialect dialect, ColumnMapping mapping, Dictionary<string, JsonCellValue> row)
    {
        if (!row.TryGetValue(mapping.SourceName, out var val))
            return "NULL";

        if (val.IsNull) return "NULL";
        if (val.IsBoolean)
        {
            var isTrue = val.Kind == JsonValueKind.True;
            var boolType = dialect.MapType(ColumnType.Boolean);
            if (boolType == "BIT") return isTrue ? "1" : "0";
            if (boolType == "TINYINT(1)") return isTrue ? "1" : "0";
            return isTrue ? "TRUE" : "FALSE";
        }
        if (val.IsNumber) return val.AsString();
        return dialect.QuoteValue(val.AsString());
    }
}
