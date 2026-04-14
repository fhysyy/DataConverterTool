using System.Data;
using System.Globalization;
using System.Text;
using MiniExcelLibs;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IExcelCsvService
{
    Task<ConvertResponse<ExcelCsvConvertResult>> ExcelToCsvAsync(IFormFile file, char delimiter = ',');
    Task<ConvertResponse<ExcelCsvConvertResult>> CsvToExcelAsync(IFormFile file, char delimiter = ',');
    Task<ConvertResponse<ExcelCsvConvertResult>> JsonToExcelAsync(JsonExcelConvertRequest request);
    Task<ConvertResponse<ExcelCsvConvertResult>> ExcelToJsonAsync(IFormFile file);
}

public class ExcelCsvService : IExcelCsvService
{
    public async Task<ConvertResponse<ExcelCsvConvertResult>> ExcelToCsvAsync(IFormFile file, char delimiter = ',')
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var rows = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>().ToList();
            if (rows.Count == 0)
                return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = "Excel文件中没有数据" };

            var columns = rows[0].Keys.ToList();
            var sb = new StringBuilder();

            sb.AppendLine(string.Join(delimiter, columns.Select(EscapeCsvField)));

            foreach (var row in rows)
            {
                var values = columns.Select(col =>
                {
                    var val = row.TryGetValue(col, out var v) ? v?.ToString() ?? "" : "";
                    return EscapeCsvField(val);
                });
                sb.AppendLine(string.Join(delimiter, values));
            }

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            return new ConvertResponse<ExcelCsvConvertResult>
            {
                Success = true,
                Data = new ExcelCsvConvertResult
                {
                    Content = sb.ToString(),
                    FileName = $"{baseName}.csv"
                }
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = $"转换失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<ExcelCsvConvertResult>> CsvToExcelAsync(IFormFile file, char delimiter = ',')
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var lines = new List<string>();
            while (await reader.ReadLineAsync() is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }

            if (lines.Count < 2)
                return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = "CSV文件数据不足" };

            var headers = ParseCsvLine(lines[0], delimiter);
            var data = new List<Dictionary<string, object?>>();

            for (int i = 1; i < lines.Count; i++)
            {
                var values = ParseCsvLine(lines[i], delimiter);
                var row = new Dictionary<string, object?>();
                for (int j = 0; j < headers.Count; j++)
                {
                    row[headers[j]] = j < values.Count ? values[j] : "";
                }
                data.Add(row);
            }

            using var outStream = new MemoryStream();
            await outStream.SaveAsAsync(data);
            var bytes = outStream.ToArray();

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            return new ConvertResponse<ExcelCsvConvertResult>
            {
                Success = true,
                Data = new ExcelCsvConvertResult
                {
                    Content = Convert.ToBase64String(bytes),
                    FileName = $"{baseName}.xlsx"
                }
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = $"转换失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<ExcelCsvConvertResult>> JsonToExcelAsync(JsonExcelConvertRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = "JSON内容不能为空" };

            var jsonDoc = System.Text.Json.JsonDocument.Parse(request.Json);
            var root = jsonDoc.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Array)
                return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = "JSON必须是数组格式" };

            var data = new List<Dictionary<string, object?>>();
            HashSet<string> allKeys = [];

            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                foreach (var prop in item.EnumerateObject())
                    allKeys.Add(prop.Name);
            }

            var columns = allKeys.ToList();

            foreach (var item in root.EnumerateArray())
            {
                var row = new Dictionary<string, object?>();
                foreach (var col in columns)
                {
                    if (item.TryGetProperty(col, out var val))
                        row[col] = ExtractJsonValue(val);
                    else
                        row[col] = "";
                }
                data.Add(row);
            }

            using var stream = new MemoryStream();
            await stream.SaveAsAsync(data);
            var bytes = stream.ToArray();

            return new ConvertResponse<ExcelCsvConvertResult>
            {
                Success = true,
                Data = new ExcelCsvConvertResult
                {
                    Content = Convert.ToBase64String(bytes),
                    FileName = $"{request.FileName}.xlsx"
                }
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = $"转换失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<ExcelCsvConvertResult>> ExcelToJsonAsync(IFormFile file)
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var rows = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>().ToList();
            if (rows.Count == 0)
                return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = "Excel文件中没有数据" };

            var result = rows.Select(row =>
            {
                var dict = new Dictionary<string, object?>();
                foreach (var kv in row)
                {
                    dict[kv.Key] = kv.Value;
                }
                return dict;
            }).ToList();

            var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            return new ConvertResponse<ExcelCsvConvertResult>
            {
                Success = true,
                Data = new ExcelCsvConvertResult
                {
                    Content = json,
                    FileName = $"{baseName}.json"
                }
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<ExcelCsvConvertResult> { Success = false, Message = $"转换失败: {ex.Message}" };
        }
    }

    private static object? ExtractJsonValue(System.Text.Json.JsonElement val)
    {
        return val.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => val.GetString(),
            System.Text.Json.JsonValueKind.Number => val.TryGetInt64(out var l) ? l : val.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            _ => val.ToString()
        };
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }
        fields.Add(current.ToString());
        return fields;
    }
}
