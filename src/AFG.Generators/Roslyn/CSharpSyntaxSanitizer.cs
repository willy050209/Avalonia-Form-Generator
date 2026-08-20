// filepath: src/AFG.Generators/Roslyn/CSharpSyntaxSanitizer.cs
using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;

namespace AFG.Generators.Roslyn;

/// <summary>
/// 提供 Roslyn 標準之 C# 字串常數轉義、識別字合法化與關鍵字碰撞逃逸輔助方法。
/// </summary>
public static partial class CSharpSyntaxSanitizer
{
    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex InvalidIdentifierCharRegex();

    /// <summary>
    /// 將任意文字安全轉義為合法的 C# 字串字面值（包含雙引號、反斜線、換行、定位字元與特殊控制字元）。
    /// </summary>
    /// <param name="input">欲轉義的原始字串內容</param>
    /// <param name="includeQuotes">是否包含外圍引號（預設為 false）</param>
    public static string EscapeStringLiteral(string? input, bool includeQuotes = false)
    {
        if (input is null)
        {
            return includeQuotes ? "\"\"" : string.Empty;
        }

        var literal = SymbolDisplay.FormatLiteral(input, quote: true);
        if (includeQuotes)
        {
            return literal;
        }

        // 移除 FormatLiteral 所產生的外圍雙引號
        if (literal.StartsWith('"') && literal.EndsWith('"') && literal.Length >= 2)
        {
            return literal[1..^1];
        }

        return literal;
    }

    /// <summary>
    /// 檢查名稱是否為 C# 保留關鍵字或內容關鍵字。
    /// </summary>
    public static bool IsKeyword(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return false;
        var kind = SyntaxFacts.GetKeywordKind(identifier);
        if (kind != SyntaxKind.None) return true;
        var contextualKind = SyntaxFacts.GetContextualKeywordKind(identifier);
        return contextualKind is SyntaxKind.VarKeyword or SyntaxKind.YieldKeyword or SyntaxKind.RecordKeyword
                                or SyntaxKind.InitKeyword or SyntaxKind.FileKeyword or SyntaxKind.ScopedKeyword;
    }

    /// <summary>
    /// 將識別字進行關鍵字安全轉義（若為關鍵字則加上 @ 前綴）。
    /// </summary>
    public static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return "value";
        var sanitized = SanitizeIdentifier(identifier);
        return IsKeyword(sanitized) ? $"@{sanitized}" : sanitized;
    }

    /// <summary>
    /// 清除非法字元，確保字串符合 C# 識別字規範。
    /// </summary>
    public static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Item";
        name = name.Trim();
        var cleaned = InvalidIdentifierCharRegex().Replace(name, "_");
        if (char.IsDigit(cleaned[0]))
        {
            cleaned = "_" + cleaned;
        }
        return cleaned;
    }
}
