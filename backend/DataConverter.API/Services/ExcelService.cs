using System.Data;
using System.Text;
using MiniExcelLibs;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IExcelService
{
    Task<ConvertResponse<ExcelToSqlResult>> ExcelToSqlAsync(IFormFile file, ExcelToSqlRequest request);
    Task<byte[]> TableToExcelAsync(TableToExcelRequest request);
}

public class ExcelService : IExcelService
{
    public async Task<ConvertResponse<ExcelToSqlResult>> ExcelToSqlAsync(IFormFile file, ExcelToSqlRequest request)
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var rows = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>().ToList();

            if (rows.Count == 0)
                return new ConvertResponse<ExcelToSqlResult> { Success = false, Message = "Excel文件中没有数据" };

            var columns = rows[0].Keys.ToList();
            var result = new ExcelToSqlResult();
            var dialect = SqlDialectFactory.GetDialect(request.DatabaseType);

            if (request.IncludeCreateTable || request.SqlType == SqlType.Create)
            {
                result.CreateTableSql = GenerateCreateTableSql(dialect, request.TableName, columns, rows);
            }

            if (request.SqlType == SqlType.Create)
                return new ConvertResponse<ExcelToSqlResult> { Success = true, Data = result };

            foreach (var row in rows)
            {
                result.DataSqlList.Add(request.SqlType switch
                {
                    SqlType.Update => GenerateUpdateSql(dialect, request.TableName, columns, row),
                    _ => GenerateInsertSql(dialect, request.TableName, columns, row)
                });
            }

            return new ConvertResponse<ExcelToSqlResult> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<ExcelToSqlResult> { Success = false, Message = $"转换失败: {ex.Message}" };
        }
    }

    private static string GenerateCreateTableSql(SqlDialect dialect, string tableName, List<string> columns, List<IDictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{dialect.CreateTablePrefix}{dialect.QuoteIdentifier(tableName)} (");

        if (dialect.AutoIncrementColumn is not null)
            sb.AppendLine($"    {dialect.QuoteIdentifier(dialect.AutoIncrementColumn.Name)} {dialect.AutoIncrementColumn.Definition},");

        foreach (var col in columns)
        {
            var sqlType = dialect.MapType(InferColumnType(col, rows));
            sb.AppendLine($"    {dialect.QuoteIdentifier(col)} {sqlType},");
        }

        var sql = sb.ToString().TrimEnd(',', '\r', '\n');
        sql += "\n)";
        if (dialect.CreateTableSuffix is not null)
            sql += $" {dialect.CreateTableSuffix}";
        sql += ";";
        return sql;
    }

    internal static ColumnType InferColumnType(string columnName, List<IDictionary<string, object?>> rows)
    {
        foreach (var row in rows)
        {
            if (row.TryGetValue(columnName, out var value) && value != null)
            {
                var strVal = value.ToString() ?? "";
                if (int.TryParse(strVal, out _)) return ColumnType.Int;
                if (long.TryParse(strVal, out _)) return ColumnType.BigInt;
                if (double.TryParse(strVal, out _)) return ColumnType.Float;
                if (decimal.TryParse(strVal, out _)) return ColumnType.Decimal;
                if (DateTime.TryParse(strVal, out _)) return ColumnType.DateTime;
                if (bool.TryParse(strVal, out _)) return ColumnType.Boolean;
                if (strVal.Length > 255) return ColumnType.LongText;
                return ColumnType.ShortText;
            }
        }
        return ColumnType.ShortText;
    }

    private static string GenerateInsertSql(SqlDialect dialect, string tableName, List<string> columns, IDictionary<string, object?> row)
    {
        var colNames = string.Join(", ", columns.Select(c => dialect.QuoteIdentifier(c)));
        var values = string.Join(", ", columns.Select(c =>
        {
            var val = row.TryGetValue(c, out var v) ? v?.ToString() ?? "NULL" : "NULL";
            if (val == "NULL") return "NULL";
            return $"{dialect.QuoteValue(val)}";
        }));
        return $"INSERT INTO {dialect.QuoteIdentifier(tableName)} ({colNames}) VALUES ({values});";
    }

    private static string GenerateUpdateSql(SqlDialect dialect, string tableName, List<string> columns, IDictionary<string, object?> row)
    {
        var setClause = string.Join(", ", columns.Select(c =>
        {
            var val = row.TryGetValue(c, out var v) ? v?.ToString() ?? "NULL" : "NULL";
            var sqlVal = val == "NULL" ? "NULL" : dialect.QuoteValue(val);
            return $"{dialect.QuoteIdentifier(c)} = {sqlVal}";
        }));

        var firstCol = columns.FirstOrDefault() ?? "id";
        var firstVal = row.TryGetValue(firstCol, out var v2) ? v2?.ToString() ?? "NULL" : "NULL";
        var whereVal = firstVal == "NULL" ? "NULL" : dialect.QuoteValue(firstVal);

        return $"UPDATE {dialect.QuoteIdentifier(tableName)} SET {setClause} WHERE {dialect.QuoteIdentifier(firstCol)} = {whereVal};";
    }

    public async Task<byte[]> TableToExcelAsync(TableToExcelRequest request)
    {
        using var stream = new MemoryStream();
        var data = new List<Dictionary<string, object?>>();

        foreach (var row in request.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var header in request.Headers)
            {
                dict[header] = row.TryGetValue(header, out var val) ? val : "";
            }
            data.Add(dict);
        }

        await stream.SaveAsAsync(data);
        return stream.ToArray();
    }
}
