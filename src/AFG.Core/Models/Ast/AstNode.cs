// filepath: src/AFG.Core/Models/Ast/AstNode.cs
namespace AFG.Core.Models.Ast;

/// <summary>
/// 表示 UI 中介語意樹 (UI Metadata AST) 的節點模型。
/// </summary>
public sealed record AstNode
{
    /// <summary>
    /// 唯一節點識別碼。
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 控制項名稱（變數名 / x:Name）。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 控制項或容器類型。
    /// </summary>
    public ControlType Type { get; init; } = ControlType.Button;

    // --- 幾何與佈局屬性 ---
    public double? Width { get; init; }
    public double? Height { get; init; }
    public double? MinWidth { get; init; }
    public double? MinHeight { get; init; }
    public double? MaxWidth { get; init; }
    public double? MaxHeight { get; init; }
    public ThicknessModel Margin { get; init; } = ThicknessModel.Zero;
    public ThicknessModel Padding { get; init; } = ThicknessModel.Zero;
    public HorizontalAlignment HorizontalAlignment { get; init; } = HorizontalAlignment.Stretch;
    public VerticalAlignment VerticalAlignment { get; init; } = VerticalAlignment.Stretch;
    public double Opacity { get; init; } = 1.0;
    public bool IsEnabled { get; init; } = true;
    public bool IsVisible { get; init; } = true;
    public int ZIndex { get; init; }

    // --- 容器附屬屬性 (Attached Properties) ---
    public double? CanvasLeft { get; init; }
    public double? CanvasTop { get; init; }
    public double? CanvasRight { get; init; }
    public double? CanvasBottom { get; init; }
    public int GridRow { get; init; }
    public int GridColumn { get; init; }
    public int GridRowSpan { get; init; } = 1;
    public int GridColumnSpan { get; init; } = 1;
    public DockPosition? Dock { get; init; }

    // --- 容器專屬配置 ---
    public ImmutableList<GridLengthModel> RowDefinitions { get; init; } = [];
    public ImmutableList<GridLengthModel> ColumnDefinitions { get; init; } = [];
    public Orientation? Orientation { get; init; }

    // --- 控制項專屬外觀與內容屬性 ---
    public string? Text { get; init; }
    public string? Content { get; init; }
    public string? Header { get; init; }
    public string? Watermark { get; init; }
    public bool? IsChecked { get; init; }
    public double? Value { get; init; }
    public double? Minimum { get; init; }
    public double? Maximum { get; init; }
    public string? Background { get; init; }
    public string? Foreground { get; init; }
    public double? FontSize { get; init; }
    public string? FontWeight { get; init; }
    public string? BorderBrush { get; init; }
    public ThicknessModel? BorderThickness { get; init; }
    public CornerRadiusModel? CornerRadius { get; init; }
    public BoxShadowModel? BoxShadow { get; init; }
    public string? ItemsSource { get; init; }
    public string? SelectedItem { get; init; }
    public string? Source { get; init; }
    public Stretch? Stretch { get; init; }
    public bool UseRelativePath { get; init; } = true;
    public bool InitBitmap { get; init; }
    public string? BitmapBackgroundColor { get; init; } = "#F0F0F0";
    public bool AutoSize { get; init; }
    public int? Interval { get; init; }
    public bool? AutoPlay { get; init; }
    public bool? IsLooping { get; init; }
    public double? Volume { get; init; }
    public double? Position { get; init; }
    public double? SpeedRatio { get; init; }

    // --- 擴充自訂屬性 ---
    public ImmutableDictionary<string, string> CustomProperties { get; init; } = ImmutableDictionary<string, string>.Empty;

    // --- MVVM 資料綁定與事件 ---
    public ImmutableList<BindingDefinition> Bindings { get; init; } = [];
    public ImmutableList<EventMappingDefinition> Events { get; init; } = [];

    // --- 子節點 (階層關係) ---
    public ImmutableList<AstNode> Children { get; init; } = [];

    /// <summary>
    /// 檢查此節點是否為佈局容器。
    /// </summary>
    [JsonIgnore]
    public bool IsContainer => Type switch
    {
        ControlType.Canvas or ControlType.Grid or ControlType.StackPanel or
        ControlType.DockPanel or ControlType.WrapPanel or ControlType.ScrollViewer or
        ControlType.Border => true,
        _ => false
    };

    /// <summary>
    /// 建立節點的深層複製（具有新的 Id）。
    /// </summary>
    public AstNode CloneWithNewId()
    {
        var clonedChildren = Children
            .Select(child => child.CloneWithNewId())
            .ToImmutableList();

        return this with
        {
            Id = Guid.NewGuid().ToString("N"),
            Children = clonedChildren
        };
    }
}
