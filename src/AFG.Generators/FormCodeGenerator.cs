// filepath: src/AFG.Generators/FormCodeGenerator.cs
using System;
using System.Collections.Immutable;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Generators.Abstractions;
using AFG.Generators.CSharpMarkup;
using AFG.Generators.FSharp;
using AFG.Generators.Mvvm;
using AFG.Generators.Roslyn;
using AFG.Generators.VisualBasic;

namespace AFG.Generators;

/// <summary>
/// 整合多語言 (C#, F#, Visual Basic) View 與 ViewModel 之核心程式碼生成外觀服務。
/// </summary>
public sealed class FormCodeGenerator(
    ICodeGenerator? viewGenerator = null,
    ICodeGenerator? viewModelGenerator = null,
    IRoslynCompilerService? compilerService = null)
{
    private readonly ICodeGenerator _csharpViewGenerator = viewGenerator ?? new CSharpMarkupViewGenerator();
    private readonly ICodeGenerator _csharpViewModelGenerator = viewModelGenerator ?? new MvvmViewModelGenerator();
    private readonly ICodeGenerator _fsharpViewGenerator = new FSharpViewGenerator();
    private readonly ICodeGenerator _fsharpViewModelGenerator = new FSharpViewModelGenerator();
    private readonly ICodeGenerator _vbViewGenerator = new VisualBasicViewGenerator();
    private readonly ICodeGenerator _vbViewModelGenerator = new VisualBasicViewModelGenerator();
    private readonly IRoslynCompilerService _compilerService = compilerService ?? new RoslynCompilerService();

    /// <summary>
    /// 生成整套 Form 程式碼檔案（包含 View 與 ViewModel）。
    /// </summary>
    /// <param name="document">UI AST 中介模型。</param>
    /// <returns>生成結果集。</returns>
    public GenerationResult GenerateAll(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var (vGen, vmGen, isCSharp) = document.TargetLanguage switch
        {
            TargetLanguage.FSharp => (_fsharpViewGenerator, _fsharpViewModelGenerator, false),
            TargetLanguage.VisualBasic => (_vbViewGenerator, _vbViewModelGenerator, false),
            _ => (_csharpViewGenerator, _csharpViewModelGenerator, true)
        };

        var rawViewFile = vGen.Generate(document);
        var finalViewCode = isCSharp ? _compilerService.FormatCode(rawViewFile.Content) : rawViewFile.Content;

        if (document.ArchitectureMode == ArchitectureMode.CodeBehind)
        {
            var codeBehindFiles = ImmutableList.Create(
                rawViewFile with { Content = finalViewCode }
            );
            return GenerationResult.Success(codeBehindFiles);
        }

        var rawVmFile = vmGen.Generate(document);
        var finalVmCode = isCSharp ? _compilerService.FormatCode(rawVmFile.Content) : rawVmFile.Content;

        var formattedFiles = ImmutableList.Create(
            rawViewFile with { Content = finalViewCode },
            rawVmFile with { Content = finalVmCode }
        );

        return GenerationResult.Success(formattedFiles);
    }
}
