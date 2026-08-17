// filepath: src/AFG.Generators/Models/GenerationModels.cs
namespace AFG.Generators.Models;

/// <summary>
/// 生成的檔案類型。
/// </summary>
public enum SourceFileType
{
    View,
    ViewModel,
    ProjectFile
}

/// <summary>
/// 表示單一產生的原始程式碼檔案。
/// </summary>
/// <param name="FileName">檔案名稱（例如 LoginFormView.cs）。</param>
/// <param name="Content">程式碼內容。</param>
/// <param name="FileType">檔案類型。</param>
public sealed record GeneratedSourceFile(
    string FileName,
    string Content,
    SourceFileType FileType);

/// <summary>
/// 程式碼生成總體結果。
/// </summary>
public sealed record GenerationResult(
    bool IsSuccess,
    ImmutableList<GeneratedSourceFile> Files,
    ImmutableList<string> Errors)
{
    public static GenerationResult Success(IEnumerable<GeneratedSourceFile> files) =>
        new(true, files.ToImmutableList(), []);

    public static GenerationResult Failure(IEnumerable<string> errors) =>
        new(false, [], errors.ToImmutableList());
}

/// <summary>
/// Roslyn 編譯診斷項目。
/// </summary>
public sealed record RoslynDiagnosticItem(
    string Id,
    string Message,
    DiagnosticSeverity Severity,
    int LineNumber,
    int ColumnNumber);

/// <summary>
/// 記憶體內 Roslyn 編譯結果。
/// </summary>
public sealed record CompilationResult(
    bool IsSuccess,
    ImmutableList<RoslynDiagnosticItem> Diagnostics,
    byte[]? AssemblyBytes = null)
{
    public static CompilationResult Success(byte[] assemblyBytes) =>
        new(true, [], assemblyBytes);

    public static CompilationResult Failed(IEnumerable<RoslynDiagnosticItem> diagnostics) =>
        new(false, diagnostics.ToImmutableList());
}
