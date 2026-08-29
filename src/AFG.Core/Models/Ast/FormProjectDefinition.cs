// filepath: src/AFG.Core/Models/Ast/FormProjectDefinition.cs
using System.Collections.Immutable;

namespace AFG.Core.Models.Ast;

/// <summary>
/// 多表單專案定義，包含專案名稱、預設起始表單與所有子表單文檔清單。
/// </summary>
public sealed record FormProjectDefinition
{
    /// <summary>
    /// 專案名稱（例如 "MainFormApp"）。
    /// </summary>
    public string ProjectName { get; init; } = "MainFormApp";

    /// <summary>
    /// 根命名空間。
    /// </summary>
    public string RootNamespace { get; init; } = "MainFormApp";

    /// <summary>
    /// 專案視窗標題。
    /// </summary>
    public string Title { get; init; } = "Avalonia Application";

    /// <summary>
    /// 起始預設載入的表單 View 名稱。
    /// </summary>
    public string InitialFormName { get; init; } = "MainFormView";

    /// <summary>
    /// 包含於此專案的所有表單文件清單。
    /// </summary>
    public ImmutableList<FormDocument> Documents { get; init; } = [];

    /// <summary>
    /// 包含於此專案的全域/跨表單獨立業務邏輯服務清單。
    /// </summary>
    public ImmutableList<AFG.Core.Models.Logic.LogicServiceDefinition> LogicServices { get; init; } = [];

    /// <summary>
    /// 目標程式語言（預設為 CSharp）。
    /// </summary>
    public TargetLanguage TargetLanguage { get; init; } = TargetLanguage.CSharp;

    /// <summary>
    /// 從單一表單文檔建立預設多表單專案定義。
    /// </summary>
    public static FormProjectDefinition FromSingleDocument(FormDocument doc) => new()
    {
        ProjectName = !string.IsNullOrWhiteSpace(doc.ProjectName)
            ? doc.ProjectName
            : (doc.ViewClassName.EndsWith("View", StringComparison.OrdinalIgnoreCase)
                ? doc.ViewClassName[..^4] + "App"
                : doc.ViewClassName + "App"),
        RootNamespace = doc.RootNamespace,
        Title = doc.Title,
        TargetLanguage = doc.TargetLanguage,
        InitialFormName = doc.ViewClassName,
        Documents = [doc],
        LogicServices = doc.LogicServices
    };
}
