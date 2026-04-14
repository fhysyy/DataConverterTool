using System.Text;
using System.Text.Json;
using DataConverter.API.Models;

namespace DataConverter.API.Services;

public interface IJsonToClassService
{
    ConvertResponse<JsonToClassResult> ConvertJsonToClass(JsonToClassRequest request);
}

public class JsonToClassService : IJsonToClassService
{
    public ConvertResponse<JsonToClassResult> ConvertJsonToClass(JsonToClassRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Json))
                return new ConvertResponse<JsonToClassResult> { Success = false, Message = "JSON内容不能为空" };

            using var doc = JsonDocument.Parse(request.Json);
            var root = doc.RootElement;

            var sb = new StringBuilder();
            var generatedClasses = new List<string>();

            GenerateClass(request.ClassName, root, sb, generatedClasses);

            return new ConvertResponse<JsonToClassResult>
            {
                Success = true,
                Data = new JsonToClassResult
                {
                    Code = sb.ToString(),
                    Language = request.Language.ToString()
                }
            };
        }
        catch (JsonException ex)
        {
            return new ConvertResponse<JsonToClassResult> { Success = false, Message = $"JSON解析失败: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new ConvertResponse<JsonToClassResult> { Success = false, Message = $"转换失败: {ex.Message}" };
        }
    }

    private void GenerateClass(string className, JsonElement element, StringBuilder sb, List<string> generatedClasses)
    {
        if (element.ValueKind != JsonValueKind.Object) return;

        var properties = element.EnumerateObject().ToList();
        if (generatedClasses.Contains(className)) return;
        generatedClasses.Add(className);

        switch (GetCurrentLanguage())
        {
            case LanguageType.CSharp:
                GenerateCSharpClass(className, properties, sb, generatedClasses);
                break;
            case LanguageType.Java:
                GenerateJavaClass(className, properties, sb, generatedClasses);
                break;
            case LanguageType.TypeScript:
                GenerateTypeScriptInterface(className, properties, sb, generatedClasses);
                break;
        }
    }

    private LanguageType GetCurrentLanguage() =>
        System.Threading.Thread.CurrentThread.CurrentCulture.Name.Contains("zh")
            ? LanguageType.CSharp
            : LanguageType.CSharp;

    private void GenerateCSharpClass(string className, List<JsonProperty> properties, StringBuilder sb, List<string> generatedClasses)
    {
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        foreach (var prop in properties)
        {
            var (type, isNested) = MapJsonToCSharpType(prop.Value, prop.Name, generatedClasses);
            sb.AppendLine($"    public {type} {PascalCase(prop.Name)} {{ get; set; }}");

            if (isNested && prop.Value.ValueKind == JsonValueKind.Object)
            {
                var nestedSb = new StringBuilder();
                GenerateCSharpClass(PascalCase(prop.Name), prop.Value.EnumerateObject().ToList(), nestedSb, generatedClasses);
                sb.Insert(0, nestedSb.ToString() + "\n");
            }
            else if (isNested && prop.Value.ValueKind == JsonValueKind.Array)
            {
                var arrItem = prop.Value.EnumerateArray().FirstOrDefault();
                if (arrItem.ValueKind == JsonValueKind.Object)
                {
                    var nestedSb = new StringBuilder();
                    GenerateCSharpClass(PascalCase(prop.Name) + "Item", arrItem.EnumerateObject().ToList(), nestedSb, generatedClasses);
                    sb.Insert(0, nestedSb.ToString() + "\n");
                }
            }
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateJavaClass(string className, List<JsonProperty> properties, StringBuilder sb, List<string> generatedClasses)
    {
        sb.AppendLine($"public class {className} {{");

        foreach (var prop in properties)
        {
            var (type, isNested) = MapJsonToJavaType(prop.Value, prop.Name, generatedClasses);
            sb.AppendLine($"    private {type} {CamelCase(prop.Name)};");
        }

        sb.AppendLine();

        foreach (var prop in properties)
        {
            var (type, _) = MapJsonToJavaType(prop.Value, prop.Name, generatedClasses);
            var fieldName = CamelCase(prop.Name);
            var pascalName = PascalCase(prop.Name);
            sb.AppendLine($"    public {type} get{pascalName}() {{ return {fieldName}; }}");
            sb.AppendLine($"    public void set{pascalName}({type} {fieldName}) {{ this.{fieldName} = {fieldName}; }}");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        foreach (var prop in properties)
        {
            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                GenerateJavaClass(PascalCase(prop.Name), prop.Value.EnumerateObject().ToList(), sb, generatedClasses);
            }
            else if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                var arrItem = prop.Value.EnumerateArray().FirstOrDefault();
                if (arrItem.ValueKind == JsonValueKind.Object)
                    GenerateJavaClass(PascalCase(prop.Name) + "Item", arrItem.EnumerateObject().ToList(), sb, generatedClasses);
            }
        }
    }

    private void GenerateTypeScriptInterface(string className, List<JsonProperty> properties, StringBuilder sb, List<string> generatedClasses)
    {
        sb.AppendLine($"export interface {className} {{");

        foreach (var prop in properties)
        {
            var (type, isNested) = MapJsonToTypeScriptType(prop.Value, prop.Name, generatedClasses);
            var optional = prop.Value.ValueKind == JsonValueKind.Null ? "?" : "";
            sb.AppendLine($"  {CamelCase(prop.Name)}{optional}: {type};");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        foreach (var prop in properties)
        {
            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                GenerateTypeScriptInterface(PascalCase(prop.Name), prop.Value.EnumerateObject().ToList(), sb, generatedClasses);
            }
            else if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                var arrItem = prop.Value.EnumerateArray().FirstOrDefault();
                if (arrItem.ValueKind == JsonValueKind.Object)
                    GenerateTypeScriptInterface(PascalCase(prop.Name) + "Item", arrItem.EnumerateObject().ToList(), sb, generatedClasses);
            }
        }
    }

    private (string type, bool isNested) MapJsonToCSharpType(JsonElement element, string propName, List<string> generatedClasses)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => TryParseDateString(element.GetString(), out var dateType) ? (dateType, false) : ("string", false),
            JsonValueKind.Number => element.TryGetInt64(out _) ? ("long", false) : ("double", false),
            JsonValueKind.True or JsonValueKind.False => ("bool", false),
            JsonValueKind.Null => ("object?", false),
            JsonValueKind.Array => MapArrayType(element, propName, "List<", ">", generatedClasses),
            JsonValueKind.Object => (PascalCase(propName), true),
            _ => ("object", false)
        };
    }

    private (string type, bool isNested) MapJsonToJavaType(JsonElement element, string propName, List<string> generatedClasses)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => TryParseDateString(element.GetString(), out var dateType) ? ("LocalDateTime", false) : ("String", false),
            JsonValueKind.Number => element.TryGetInt64(out _) ? ("Long", false) : ("Double", false),
            JsonValueKind.True or JsonValueKind.False => ("Boolean", false),
            JsonValueKind.Null => ("Object", false),
            JsonValueKind.Array => MapArrayType(element, propName, "List<", ">", generatedClasses),
            JsonValueKind.Object => (PascalCase(propName), true),
            _ => ("Object", false)
        };
    }

    private (string type, bool isNested) MapJsonToTypeScriptType(JsonElement element, string propName, List<string> generatedClasses)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => TryParseDateString(element.GetString(), out _) ? ("Date", false) : ("string", false),
            JsonValueKind.Number => element.TryGetInt64(out _) ? ("number", false) : ("number", false),
            JsonValueKind.True or JsonValueKind.False => ("boolean", false),
            JsonValueKind.Null => ("any", false),
            JsonValueKind.Array => MapArrayType(element, propName, "", "[]", generatedClasses),
            JsonValueKind.Object => (PascalCase(propName), true),
            _ => ("any", false)
        };
    }

    private (string type, bool isNested) MapArrayType(JsonElement element, string propName, string prefix, string suffix, List<string> generatedClasses)
    {
        if (element.GetArrayLength() == 0) return ($"{prefix}object{suffix}", false);
        var firstItem = element.EnumerateArray().First();
        var itemType = firstItem.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => firstItem.TryGetInt64(out _) ? "long" : "double",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Object => PascalCase(propName) + "Item",
            _ => "object"
        };
        var isNested = firstItem.ValueKind == JsonValueKind.Object;
        return ($"{prefix}{itemType}{suffix}", isNested);
    }

    private static bool TryParseDateString(string? value, out string dateType)
    {
        dateType = "DateTime";
        if (value == null) return false;
        return DateTime.TryParse(value, out _);
    }

    private static string PascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var parts = name.Split('_', '-', ' ');
        return string.Join("", parts.Select(p => char.ToUpper(p[0]) + p[1..].ToLower()));
    }

    private static string CamelCase(string name)
    {
        var pascal = PascalCase(name);
        return char.ToLower(pascal[0]) + pascal[1..];
    }
}
