// filepath: src/AFG.Generators/Roslyn/RoslynCompilerService.cs
using System.Reflection;

namespace AFG.Generators.Roslyn;

/// <summary>
/// 提供 Roslyn 記憶體內語法分析、格式化與編譯診斷服務。
/// </summary>
public sealed class RoslynCompilerService : IRoslynCompilerService
{
    private static readonly Lazy<ImmutableList<MetadataReference>> CoreReferences = new(() =>
    {
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location)
        };

        return refs.ToImmutableList();
    });

    public string FormatCode(string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);
        return RoslynCodeFormatter.Format(sourceCode);
    }

    public Task<CompilationResult> CompileInMemoryAsync(
        IReadOnlyList<GeneratedSourceFile> sources,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var syntaxTrees = sources
            .Select(s => CSharpSyntaxTree.ParseText(s.Content, cancellationToken: cancellationToken))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: $"AFG_Dynamic_{Guid.NewGuid():N}",
            syntaxTrees: syntaxTrees,
            references: CoreReferences.Value,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms, cancellationToken: cancellationToken);

        if (emitResult.Success)
        {
            return Task.FromResult(CompilationResult.Success(ms.ToArray()));
        }

        var diagnostics = emitResult.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
            .Select(d =>
            {
                var lineSpan = d.Location.GetLineSpan();
                return new RoslynDiagnosticItem(
                    Id: d.Id,
                    Message: d.GetMessage(),
                    Severity: d.Severity,
                    LineNumber: lineSpan.StartLinePosition.Line + 1,
                    ColumnNumber: lineSpan.StartLinePosition.Character + 1);
            })
            .ToImmutableList();

        return Task.FromResult(CompilationResult.Failed(diagnostics));
    }

    /// <summary>
    /// 快速檢查單一 C# 程式碼字串的語法樹正確性。
    /// </summary>
    public static IReadOnlyList<RoslynDiagnosticItem> CheckSyntaxDiagnostics(string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        return syntaxTree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d =>
            {
                var lineSpan = d.Location.GetLineSpan();
                return new RoslynDiagnosticItem(
                    Id: d.Id,
                    Message: d.GetMessage(),
                    Severity: d.Severity,
                    LineNumber: lineSpan.StartLinePosition.Line + 1,
                    ColumnNumber: lineSpan.StartLinePosition.Character + 1);
            })
            .ToImmutableList();
    }
}
