// filepath: src/AFG.Shared/Services/ToolboxService.cs
namespace AFG.Shared.Services;

/// <summary>
/// 提供標準 Avalonia 控制項工具箱清單服務。
/// </summary>
public static class ToolboxService
{
    private static readonly ImmutableList<ToolboxItem> Items =
    [
        // 基本控制項
        new("Button", "常用控制項", ControlType.Button, "🔘", 120, 35, "Button"),
        new("TextBox", "常用控制項", ControlType.TextBox, "📝", 180, 32, "TextBox"),
        new("TextBlock", "常用控制項", ControlType.TextBlock, "🔤", 120, 24, "TextBlock"),
        new("CheckBox", "常用控制項", ControlType.CheckBox, "☑️", 120, 28, "CheckBox"),
        new("RadioButton", "常用控制項", ControlType.RadioButton, "🔘", 120, 28, "RadioButton"),
        new("ComboBox", "常用控制項", ControlType.ComboBox, "🔽", 150, 32, "Option 1"),
        new("DatePicker", "常用控制項", ControlType.DatePicker, "📅", 200, 32),
        new("Slider", "常用控制項", ControlType.Slider, "🎚️", 180, 30),
        new("ProgressBar", "常用控制項", ControlType.ProgressBar, "📊", 180, 20),
        new("Border", "常用控制項", ControlType.Border, "🔲", 200, 150),

        // 佈局容器
        new("Grid", "佈局容器", ControlType.Grid, "▦", 400, 300),
        new("StackPanel", "佈局容器", ControlType.StackPanel, "📑", 300, 200),
        new("Canvas", "佈局容器", ControlType.Canvas, "🎨", 400, 300),
        new("DockPanel", "佈局容器", ControlType.DockPanel, "⚓", 400, 300),
        new("ScrollViewer", "佈局容器", ControlType.ScrollViewer, "📜", 300, 200)
    ];

    public static IReadOnlyList<ToolboxItem> GetAvailableItems() => Items;
}
