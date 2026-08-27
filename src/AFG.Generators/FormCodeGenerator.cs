// filepath: src/AFG.Generators/FormCodeGenerator.cs
using AFG.Generators.CSharpMarkup;
using AFG.Generators.Mvvm;
using AFG.Generators.Roslyn;

namespace AFG.Generators;

/// <summary>
/// 整合 C# Markup View 與 CommunityToolkit.Mvvm ViewModel 之核心程式碼生成外觀服務。
/// </summary>
public sealed class FormCodeGenerator(
    ICodeGenerator? viewGenerator = null,
    ICodeGenerator? viewModelGenerator = null,
    IRoslynCompilerService? compilerService = null)
{
    private readonly ICodeGenerator _viewGenerator = viewGenerator ?? new CSharpMarkupViewGenerator();
    private readonly ICodeGenerator _viewModelGenerator = viewModelGenerator ?? new MvvmViewModelGenerator();
    private readonly IRoslynCompilerService _compilerService = compilerService ?? new RoslynCompilerService();

    /// <summary>
    /// 生成整套 Form 程式碼檔案（包含格式化後的 View.cs 與 ViewModel.cs）。
    /// </summary>
    /// <param name="document">UI AST 中介模型。</param>
    /// <returns>生成結果集。</returns>
    public GenerationResult GenerateAll(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var rawViewFile = _viewGenerator.Generate(document);
        var formattedViewCode = _compilerService.FormatCode(rawViewFile.Content);

        if (document.ArchitectureMode == ArchitectureMode.CodeBehind)
        {
            var codeBehindFiles = ImmutableList.Create(
                rawViewFile with { Content = formattedViewCode }
            );
            return GenerationResult.Success(codeBehindFiles);
        }

        var rawVmFile = _viewModelGenerator.Generate(document);
        var formattedVmCode = _compilerService.FormatCode(rawVmFile.Content);

        var formattedFiles = ImmutableList.Create(
            rawViewFile with { Content = formattedViewCode },
            rawVmFile with { Content = formattedVmCode }
        );

        return GenerationResult.Success(formattedFiles);
    }
}
