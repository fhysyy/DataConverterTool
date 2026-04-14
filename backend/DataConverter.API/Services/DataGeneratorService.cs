using System.Text;
using MiniExcelLibs;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IDataGeneratorService
{
    ConvertResponse<DataGeneratorResult> Generate(DataGeneratorRequest request);
}

public class DataGeneratorService : IDataGeneratorService
{
    private static readonly Random _random = new();

    private static readonly string[] FirstNames = ["张", "李", "王", "刘", "陈", "杨", "赵", "黄", "周", "吴", "徐", "孙", "马", "朱", "胡", "郭", "林", "何", "高", "罗"];
    private static readonly string[] LastNames = ["伟", "芳", "娜", "敏", "静", "丽", "强", "磊", "洋", "勇", "艳", "杰", "军", "秀英", "明", "华", "超", "慧", "建国", "志强"];
    private static readonly string[] Cities = ["北京", "上海", "广州", "深圳", "杭州", "成都", "武汉", "南京", "重庆", "西安", "苏州", "天津", "长沙", "郑州", "东莞"];
    private static readonly string[] Companies = ["阿里巴巴", "腾讯", "百度", "字节跳动", "华为", "小米", "京东", "美团", "网易", "滴滴", "快手", "拼多多", "携程", "蚂蚁集团", "微众银行"];
    private static readonly string[] JobTitles = ["软件工程师", "产品经理", "UI设计师", "数据分析师", "运维工程师", "测试工程师", "项目经理", "架构师", "前端开发", "后端开发", "全栈工程师", "DevOps工程师", "算法工程师", "技术总监", "CTO"];
    private static readonly string[] Domains = ["qq.com", "163.com", "gmail.com", "outlook.com", "company.com", "example.com", "foxmail.com", "126.com"];
    private static readonly string[] Streets = ["朝阳路", "中山大道", "人民路", "解放路", "建设路", "和平路", "长安街", "南京路", "淮海路", "复兴路"];

    public ConvertResponse<DataGeneratorResult> Generate(DataGeneratorRequest request)
    {
        try
        {
            if (request.Columns == null || request.Columns.Count == 0)
                return new ConvertResponse<DataGeneratorResult> { Success = false, Message = "请定义至少一列" };

            if (request.RowCount <= 0 || request.RowCount > 10000)
                return new ConvertResponse<DataGeneratorResult> { Success = false, Message = "行数范围: 1-10000" };

            var rows = new List<Dictionary<string, object?>>();

            for (int i = 0; i < request.RowCount; i++)
            {
                var row = new Dictionary<string, object?>();
                foreach (var col in request.Columns)
                    row[col.Name] = GenerateValue(col, i + 1);
                rows.Add(row);
            }

            return request.OutputType switch
            {
                GeneratorOutputType.Sql => GenerateSql(request, rows),
                GeneratorOutputType.Json => GenerateJson(request, rows),
                GeneratorOutputType.Csv => GenerateCsv(request, rows),
                GeneratorOutputType.Excel => GenerateExcel(request, rows),
                _ => new ConvertResponse<DataGeneratorResult> { Success = false, Message = "不支持的输出类型" }
            };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<DataGeneratorResult> { Success = false, Message = $"生成失败: {ex.Message}" };
        }
    }

    private object? GenerateValue(ColumnDefinition col, int rowIndex)
    {
        return col.DataType switch
        {
            ColumnDataType.AutoIncrement => rowIndex,
            ColumnDataType.FullName => $"{Pick(FirstNames)}{Pick(LastNames)}",
            ColumnDataType.FirstName => Pick(FirstNames),
            ColumnDataType.LastName => Pick(LastNames),
            ColumnDataType.Email => $"{Pinyin(Pick(FirstNames))}{Pinyin(Pick(LastNames))}{_random.Next(100, 999)}@{Pick(Domains)}",
            ColumnDataType.Phone => $"1{Pick(new[] { "3", "5", "7", "8", "9" })}{RandomDigits(9)}",
            ColumnDataType.Address => $"{Pick(Cities)}市{Pick(Streets)}{_random.Next(1, 200)}号",
            ColumnDataType.City => Pick(Cities),
            ColumnDataType.Company => Pick(Companies),
            ColumnDataType.JobTitle => Pick(JobTitles),
            ColumnDataType.Age => _random.Next(18, 65),
            ColumnDataType.Birthday => new DateTime(_random.Next(1970, 2005), _random.Next(1, 13), _random.Next(1, 29)).ToString("yyyy-MM-dd"),
            ColumnDataType.DateTime => DateTime.Now.AddDays(-_random.Next(0, 365)).ToString("yyyy-MM-dd HH:mm:ss"),
            ColumnDataType.Number => _random.Next(1, 10000),
            ColumnDataType.Decimal => Math.Round(_random.NextDouble() * 100000, 2),
            ColumnDataType.Boolean => _random.Next(2) == 1,
            ColumnDataType.Uuid => Guid.NewGuid().ToString(),
            ColumnDataType.Url => $"https://www.{Pick(Domains)}/{Guid.NewGuid():N}",
            ColumnDataType.IpAddress => $"{_random.Next(1, 255)}.{_random.Next(0, 256)}.{_random.Next(0, 256)}.{_random.Next(1, 255)}",
            ColumnDataType.Custom => PickCustom(col.CustomValues),
            _ => ""
        };
    }

    private static string Pick(string[] arr) => arr[_random.Next(arr.Length)];

    private static string RandomDigits(int count)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
            sb.Append(_random.Next(10));
        return sb.ToString();
    }

    private static string Pinyin(string chinese) => chinese switch
    {
        "张" => "zhang", "李" => "li", "王" => "wang", "刘" => "liu", "陈" => "chen",
        "杨" => "yang", "赵" => "zhao", "黄" => "huang", "周" => "zhou", "吴" => "wu",
        "徐" => "xu", "孙" => "sun", "马" => "ma", "朱" => "zhu", "胡" => "hu",
        "郭" => "guo", "林" => "lin", "何" => "he", "高" => "gao", "罗" => "luo",
        _ => "user"
    };

    private static string PickCustom(string? customValues)
    {
        if (string.IsNullOrWhiteSpace(customValues)) return "";
        var parts = customValues.Split(',', ';', '，', '；');
        return parts[_random.Next(parts.Length)].Trim();
    }

    private ConvertResponse<DataGeneratorResult> GenerateSql(DataGeneratorRequest request, List<Dictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        var columns = request.Columns.Select(c => c.Name).ToList();

        sb.AppendLine($"INSERT INTO `{request.TableName}` ({string.Join(", ", columns.Select(c => $"`{c}`"))}) VALUES");

        for (int i = 0; i < rows.Count; i++)
        {
            var values = columns.Select(col =>
            {
                var val = rows[i].TryGetValue(col, out var v) ? v : "";
                return val switch
                {
                    null => "NULL",
                    bool b => b ? "1" : "0",
                    int or long => val.ToString(),
                    double d => d.ToString(),
                    _ => $"'{val.ToString()!.Replace("'", "''")}'"
                };
            });
            sb.Append($"  ({string.Join(", ", values)})");
            sb.AppendLine(i < rows.Count - 1 ? "," : ";");
        }

        return new ConvertResponse<DataGeneratorResult>
        {
            Success = true,
            Data = new DataGeneratorResult { Content = sb.ToString(), FileName = $"{request.TableName}.sql", ContentType = "text/plain" }
        };
    }

    private ConvertResponse<DataGeneratorResult> GenerateJson(DataGeneratorRequest request, List<Dictionary<string, object?>> rows)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(rows, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        return new ConvertResponse<DataGeneratorResult>
        {
            Success = true,
            Data = new DataGeneratorResult { Content = json, FileName = $"{request.TableName}.json", ContentType = "application/json" }
        };
    }

    private ConvertResponse<DataGeneratorResult> GenerateCsv(DataGeneratorRequest request, List<Dictionary<string, object?>> rows)
    {
        var columns = request.Columns.Select(c => c.Name).ToList();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns));

        foreach (var row in rows)
        {
            var values = columns.Select(col =>
            {
                var val = row.TryGetValue(col, out var v) ? v?.ToString() ?? "" : "";
                if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                    return $"\"{val.Replace("\"", "\"\"")}\"";
                return val;
            });
            sb.AppendLine(string.Join(",", values));
        }

        return new ConvertResponse<DataGeneratorResult>
        {
            Success = true,
            Data = new DataGeneratorResult { Content = sb.ToString(), FileName = $"{request.TableName}.csv", ContentType = "text/csv" }
        };
    }

    private ConvertResponse<DataGeneratorResult> GenerateExcel(DataGeneratorRequest request, List<Dictionary<string, object?>> rows)
    {
        using var stream = new MemoryStream();
        stream.SaveAs(rows);
        var bytes = stream.ToArray();

        return new ConvertResponse<DataGeneratorResult>
        {
            Success = true,
            Data = new DataGeneratorResult
            {
                Content = Convert.ToBase64String(bytes),
                FileName = $"{request.TableName}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            }
        };
    }
}
