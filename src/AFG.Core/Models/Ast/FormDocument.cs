// filepath: src/AFG.Core/Models/Ast/FormDocument.cs
namespace AFG.Core.Models.Ast;

/// <summary>
/// 表示整份表單設計的中介根文件模型。
/// </summary>
public sealed record FormDocument
{
    /// <summary>
    /// 文件規格版本。
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>
    /// 匯出之方案與專案名稱（若為 null 則自動依 ViewClassName 推斷，例如 MainFormApp）。
    /// </summary>
    public string? ProjectName { get; init; }

    /// <summary>
    /// 產生的 C# 類別命名空間。
    /// </summary>
    public string RootNamespace { get; init; } = "GeneratedApp.Views";

    /// <summary>
    /// View 類別名稱（例如 LoginFormView）。
    /// </summary>
    public string ViewClassName { get; init; } = "MainFormView";

    /// <summary>
    /// ViewModel 類別名稱（例如 LoginFormViewModel）。
    /// </summary>
    public string ViewModelClassName { get; init; } = "MainFormViewModel";

    /// <summary>
    /// 視窗或表單標題。
    /// </summary>
    public string Title { get; init; } = "Avalonia Form";

    /// <summary>
    /// 預設設計畫布寬度。
    /// </summary>
    public double CanvasWidth { get; init; } = 800;

    /// <summary>
    /// 預設設計畫布高度。
    /// </summary>
    public double CanvasHeight { get; init; } = 600;

    /// <summary>
    /// 是否在此表單啟用相依性注入架構配置。
    /// </summary>
    public bool EnableDependencyInjection { get; init; } = true;

    /// <summary>
    /// 是否使用強型別編譯綁定 (Compiled / Lambda Bindings) 語法。
    /// </summary>
    public bool UseCompiledBindings { get; init; }

    /// <summary>
    /// 注入至此 ViewModel 的自訂服務相依性清單（為空時產出乾淨無參數 ViewModel）。
    /// </summary>
    public ImmutableList<ServiceDependencyDefinition> InjectedServices { get; init; } = [];

    /// <summary>
    /// 根佈局節點（通常為 Grid 或 Canvas）。
    /// </summary>
    public AstNode RootNode { get; init; } = new()
    {
        Name = "RootCanvas",
        Type = ControlType.Canvas,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch
    };

    /// <summary>
    /// 建立預設空表單文件。
    /// </summary>
    public static FormDocument CreateDefault(string viewName = "MainFormView")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewName);

        var viewModelName = viewName.EndsWith("View", StringComparison.Ordinal)
            ? $"{viewName}Model"
            : $"{viewName}ViewModel";

        return new FormDocument
        {
            ViewClassName = viewName,
            ViewModelClassName = viewModelName,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas
            }
        };
    }
}
