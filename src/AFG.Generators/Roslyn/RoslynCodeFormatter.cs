// filepath: src/AFG.Generators/Roslyn/RoslynCodeFormatter.cs
namespace AFG.Generators.Roslyn;

/// <summary>
/// 使用 Roslyn CSharpSyntaxTree 進行語法標準化與縮排格式化的純函數服務。
/// </summary>
public static class RoslynCodeFormatter
{
    /// <summary>
    /// 格式化 C# 原始程式碼字串。
    /// </summary>
    /// <param name="rawCode">未格式化的 C# 程式碼。</param>
    /// <returns>標準化格式後的 C# 程式碼。</returns>
    public static string Format(string rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return rawCode;
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(rawCode);
        var root = syntaxTree.GetRoot();
        var formattedNode = root.NormalizeWhitespace();

        return formattedNode.ToFullString();
    }
}
