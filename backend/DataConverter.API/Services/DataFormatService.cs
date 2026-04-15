using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using MiniExcelLibs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IDataFormatService
{
    // XML 相关
    Task<ConvertResponse<byte[]>> XmlToExcelAsync(XmlConvertRequest request);
    Task<ConvertResponse<string>> ExcelToXmlAsync(IFormFile file);
    Task<ConvertResponse<string>> XmlToJsonAsync(XmlConvertRequest request);
    Task<ConvertResponse<string>> JsonToXmlAsync(XmlConvertRequest request);
    
    // YAML 相关
    Task<ConvertResponse<byte[]>> YamlToExcelAsync(YamlConvertRequest request);
    Task<ConvertResponse<string>> ExcelToYamlAsync(IFormFile file);
    Task<ConvertResponse<string>> YamlToJsonAsync(YamlConvertRequest request);
    Task<ConvertResponse<string>> JsonToYamlAsync(YamlConvertRequest request);
    
    // Markdown 相关
    Task<ConvertResponse<byte[]>> MarkdownToExcelAsync(MarkdownConvertRequest request);
    Task<ConvertResponse<string>> ExcelToMarkdownAsync(IFormFile file);
    
    // 数据清洗
    Task<ConvertResponse<List<Dictionary<string, string>>>> CleanDataAsync(DataCleanRequest request);
    
    // 加解密相关
    Task<ConvertResponse<string>> EncryptAsync(EncryptRequest request);
    Task<ConvertResponse<string>> DecryptAsync(DecryptRequest request);
}

public class DataFormatService : IDataFormatService
{
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public DataFormatService()
    {
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    // XML 相关方法
    public async Task<ConvertResponse<byte[]>> XmlToExcelAsync(XmlConvertRequest request)
    {
        try
        {
            var data = ParseXmlToDictionary(request.Content);
            using var stream = new MemoryStream();
            await stream.SaveAsAsync(data);
            return new ConvertResponse<byte[]> { Success = true, Data = stream.ToArray() };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<byte[]> { Success = false, Message = $"XML 转 Excel 失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<string>> ExcelToXmlAsync(IFormFile file)
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            var data = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>()
                .Select(row => row.ToDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty))
                .ToList<Dictionary<string, object>>();
            var xml = ConvertToXml(data);
            return new ConvertResponse<string> { Success = true, Data = xml };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<string> { Success = false, Message = $"Excel 转 XML 失败: {ex.Message}" };
        }
    }

    public Task<ConvertResponse<string>> XmlToJsonAsync(XmlConvertRequest request)
    {
        try
        {
            var data = ParseXmlToDictionary(request.Content);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(new ConvertResponse<string> { Success = true, Data = json });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConvertResponse<string> { Success = false, Message = $"XML 转 JSON 失败: {ex.Message}" });
        }
    }

    public Task<ConvertResponse<string>> JsonToXmlAsync(XmlConvertRequest request)
    {
        try
        {
            var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(request.Content);
            var xml = ConvertToXml(data);
            return Task.FromResult(new ConvertResponse<string> { Success = true, Data = xml });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConvertResponse<string> { Success = false, Message = $"JSON 转 XML 失败: {ex.Message}" });
        }
    }

    // YAML 相关方法
    public async Task<ConvertResponse<byte[]>> YamlToExcelAsync(YamlConvertRequest request)
    {
        try
        {
            var data = _yamlDeserializer.Deserialize<List<Dictionary<string, object>>>(request.Content);
            using var stream = new MemoryStream();
            await stream.SaveAsAsync(data);
            return new ConvertResponse<byte[]> { Success = true, Data = stream.ToArray() };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<byte[]> { Success = false, Message = $"YAML 转 Excel 失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<string>> ExcelToYamlAsync(IFormFile file)
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            var data = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>()
                .Select(row => row.ToDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty))
                .ToList<Dictionary<string, object>>();
            var yaml = _yamlSerializer.Serialize(data);
            return new ConvertResponse<string> { Success = true, Data = yaml };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<string> { Success = false, Message = $"Excel 转 YAML 失败: {ex.Message}" };
        }
    }

    public Task<ConvertResponse<string>> YamlToJsonAsync(YamlConvertRequest request)
    {
        try
        {
            var data = _yamlDeserializer.Deserialize<object>(request.Content);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(new ConvertResponse<string> { Success = true, Data = json });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConvertResponse<string> { Success = false, Message = $"YAML 转 JSON 失败: {ex.Message}" });
        }
    }

    public Task<ConvertResponse<string>> JsonToYamlAsync(YamlConvertRequest request)
    {
        try
        {
            var data = JsonSerializer.Deserialize<object>(request.Content);
            var yaml = _yamlSerializer.Serialize(data);
            return Task.FromResult(new ConvertResponse<string> { Success = true, Data = yaml });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConvertResponse<string> { Success = false, Message = $"JSON 转 YAML 失败: {ex.Message}" });
        }
    }

    // Markdown 相关方法
    public async Task<ConvertResponse<byte[]>> MarkdownToExcelAsync(MarkdownConvertRequest request)
    {
        try
        {
            var data = ParseMarkdownTable(request.Content, request.HasHeader);
            using var stream = new MemoryStream();
            await stream.SaveAsAsync(data);
            return new ConvertResponse<byte[]> { Success = true, Data = stream.ToArray() };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<byte[]> { Success = false, Message = $"Markdown 转 Excel 失败: {ex.Message}" };
        }
    }

    public async Task<ConvertResponse<string>> ExcelToMarkdownAsync(IFormFile file)
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            var data = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>()
                .Select(row => row.ToDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty))
                .ToList<Dictionary<string, object>>();
            var markdown = ConvertToMarkdownTable(data);
            return new ConvertResponse<string> { Success = true, Data = markdown };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<string> { Success = false, Message = $"Excel 转 Markdown 失败: {ex.Message}" };
        }
    }

    // 数据清洗方法
    public Task<ConvertResponse<List<Dictionary<string, string>>>> CleanDataAsync(DataCleanRequest request)
    {
        try
        {
            var cleanedRows = new List<Dictionary<string, string>>();

            foreach (var row in request.Rows)
            {
                // 移除空行
                if (request.RemoveEmptyRows && row.All(kv => string.IsNullOrWhiteSpace(kv.Value)))
                    continue;

                var cleanedRow = new Dictionary<string, string>();
                foreach (var (key, value) in row)
                {
                    var cleanedValue = value;
                    // 去除空格
                    if (request.TrimWhitespace)
                        cleanedValue = cleanedValue?.Trim();
                    cleanedRow[key] = cleanedValue;
                }
                cleanedRows.Add(cleanedRow);
            }

            // 去除重复行
            if (request.RemoveDuplicates)
            {
                var uniqueRows = new HashSet<string>();
                var distinctRows = new List<Dictionary<string, string>>();

                foreach (var row in cleanedRows)
                {
                    var rowString = JsonSerializer.Serialize(row);
                    if (uniqueRows.Add(rowString))
                        distinctRows.Add(row);
                }
                cleanedRows = distinctRows;
            }

            return Task.FromResult(new ConvertResponse<List<Dictionary<string, string>>> { Success = true, Data = cleanedRows });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConvertResponse<List<Dictionary<string, string>>> { Success = false, Message = $"数据清洗失败: {ex.Message}" });
        }
    }

    // 辅助方法
    private static List<Dictionary<string, object>> ParseXmlToDictionary(string xmlContent)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);
        var data = new List<Dictionary<string, object>>();

        var root = doc.DocumentElement;
        if (root != null)
        {
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    var row = new Dictionary<string, object>();
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        if (childNode.NodeType == XmlNodeType.Element)
                        {
                            row[childNode.Name] = childNode.InnerText;
                        }
                    }
                    data.Add(row);
                }
            }
        }

        return data;
    }

    private static string ConvertToXml(List<Dictionary<string, object>> data)
    {
        var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };
        var sb = new StringBuilder();

        using (var writer = XmlWriter.Create(sb, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("Root");

            foreach (var row in data)
            {
                writer.WriteStartElement("Row");
                foreach (var (key, value) in row)
                {
                    writer.WriteElementString(key, value?.ToString() ?? "");
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return sb.ToString();
    }

    private static List<Dictionary<string, object>> ParseMarkdownTable(string markdown, bool hasHeader)
    {
        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var data = new List<Dictionary<string, object>>();

        if (lines.Length < 2) return data;

        var headerLine = lines[0].Trim();
        var separatorLine = lines[1].Trim();
        var dataLines = lines.Skip(2).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

        var headers = headerLine.Split('|').Select(h => h.Trim()).Where(h => !string.IsNullOrWhiteSpace(h)).ToList();

        foreach (var line in dataLines)
        {
            var values = line.Split('|').Select(v => v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            var row = new Dictionary<string, object>();
            for (var i = 0; i < headers.Count && i < values.Count; i++)
            {
                row[headers[i]] = values[i];
            }
            data.Add(row);
        }

        return data;
    }

    private static string ConvertToMarkdownTable(List<Dictionary<string, object>> data)
    {
        if (data.Count == 0) return string.Empty;

        var headers = data[0].Keys.ToList();
        var sb = new StringBuilder();

        // 表头
        sb.Append('|').Append(string.Join("|", headers)).Append("|\n");
        // 分隔线
        sb.Append('|').Append(string.Join("|", headers.Select(h => "---".ToString()))).Append("|\n");
        // 数据行
        foreach (var row in data)
        {
            sb.Append('|').Append(string.Join("|", headers.Select(h => row.TryGetValue(h, out var v) ? v?.ToString() ?? "" : ""))).Append("|\n");
        }

        return sb.ToString();
    }

    // 加解密相关方法
    public Task<ConvertResponse<string>> EncryptAsync(EncryptRequest request)
    {
        try
        {
            string result;
            switch (request.EncryptionType)
            {
                case EncryptionType.Base64:
                    result = Base64Encrypt(request.Content);
                    break;
                case EncryptionType.MD5:
                    result = MD5Encrypt(request.Content);
                    break;
                case EncryptionType.SHA1:
                    result = SHA1Encrypt(request.Content);
                    break;
                case EncryptionType.SHA256:
                    result = SHA256Encrypt(request.Content);
                    break;
                case EncryptionType.SHA512:
                    result = SHA512Encrypt(request.Content);
                    break;
                default:
                    throw new ArgumentException("不支持的加密类型");
            }
            return Task.FromResult(new ConvertResponse<string> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConvertResponse<string> { Success = false, Message = $"加密失败: {ex.Message}" });
        }
    }

    public Task<ConvertResponse<string>> DecryptAsync(DecryptRequest request)
    {
        try
        {
            string result;
            switch (request.EncryptionType)
            {
                case EncryptionType.Base64:
                    result = Base64Decrypt(request.Content);
                    break;
                case EncryptionType.MD5:
                case EncryptionType.SHA1:
                case EncryptionType.SHA256:
                case EncryptionType.SHA512:
                    return Task.FromResult(new ConvertResponse<string> { Success = false, Message = "哈希算法不可逆，无法解密" });
                default:
                    throw new ArgumentException("不支持的解密类型");
            }
            return Task.FromResult(new ConvertResponse<string> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConvertResponse<string> { Success = false, Message = $"解密失败: {ex.Message}" });
        }
    }

    // 辅助方法：Base64加密
    private static string Base64Encrypt(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToBase64String(bytes);
    }

    // 辅助方法：Base64解密
    private static string Base64Decrypt(string content)
    {
        var bytes = Convert.FromBase64String(content);
        return Encoding.UTF8.GetString(bytes);
    }

    // 辅助方法：MD5加密
    private static string MD5Encrypt(string content)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    // 辅助方法：SHA1加密
    private static string SHA1Encrypt(string content)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(content));
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    // 辅助方法：SHA256加密
    private static string SHA256Encrypt(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    // 辅助方法：SHA512加密
    private static string SHA512Encrypt(string content)
    {
        using var sha512 = System.Security.Cryptography.SHA512.Create();
        var bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(content));
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
