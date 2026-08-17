// filepath: src/AFG.Generators/Abstractions/ICodeGenerator.cs
namespace AFG.Generators.Abstractions;

/// <summary>
/// 程式碼生成器介面。
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// 根據傳入的 AST 表單文件生成對應的 C# 檔案。
    /// </summary>
    /// <param name="document">UI AST 中介模型。</param>
    /// <returns>生成的原始碼檔案結果。</returns>
    GeneratedSourceFile Generate(FormDocument document);
}

/// <summary>
/// Roslyn 記憶體編譯診斷服務介面。
/// </summary>
public interface IRoslynCompilerService
{
    /// <summary>
    /// 格式化 C# 原始程式碼。
    /// </summary>
    /// <param name="sourceCode">原始未格式化程式碼。</param>
    /// <returns>格式化後的 C# 程式碼。</returns>
    string FormatCode(string sourceCode);

    /// <summary>
    /// 進行記憶體內編譯以診斷語法或型別錯誤。
    /// </summary>
    /// <param name="sources">要編譯的一組原始碼。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>編譯與診斷結果。</returns>
    Task<CompilationResult> CompileInMemoryAsync(
        IReadOnlyList<GeneratedSourceFile> sources,
        CancellationToken cancellationToken = default);
}
