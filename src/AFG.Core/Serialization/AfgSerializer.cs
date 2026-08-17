// filepath: src/AFG.Core/Serialization/AfgSerializer.cs
using System.Text.Encodings.Web;

namespace AFG.Core.Serialization;

/// <summary>
/// 提供 AST 語意樹與 FormDocument 的 JSON 序列化與反序列化純函數服務。
/// </summary>
public static class AfgSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    /// <summary>
    /// 將 FormDocument 序列化為 JSON 字串。
    /// </summary>
    /// <param name="document">要序列化的表單文件。</param>
    /// <returns>JSON 字串。</returns>
    /// <exception cref="ArgumentNullException">當 document 為 null 時擲出。</exception>
    public static string SerializeDocument(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, DefaultOptions);
    }

    /// <summary>
    /// 從 JSON 字串反序列化為 FormDocument。
    /// </summary>
    /// <param name="json">JSON 字串。</param>
    /// <returns>反序列化後的 FormDocument。</returns>
    /// <exception cref="ArgumentNullException">當 json 為 null 時擲出。</exception>
    /// <exception cref="JsonException">當 JSON 格式不合法時擲出。</exception>
    public static FormDocument DeserializeDocument(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var doc = JsonSerializer.Deserialize<FormDocument>(json, DefaultOptions)
            ?? throw new JsonException("無法反序列化 FormDocument：結果為 null。");

        return doc;
    }

    /// <summary>
    /// 將單一 AstNode 序列化為 JSON 字串。
    /// </summary>
    public static string SerializeNode(AstNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return JsonSerializer.Serialize(node, DefaultOptions);
    }

    /// <summary>
    /// 從 JSON 字串反序列化為 AstNode。
    /// </summary>
    public static AstNode DeserializeNode(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var node = JsonSerializer.Deserialize<AstNode>(json, DefaultOptions)
            ?? throw new JsonException("無法反序列化 AstNode：結果為 null。");

        return node;
    }
}
