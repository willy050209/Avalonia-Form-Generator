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
        new("Button", "常用控制項", ControlType.Button, "Btn", 120, 35, "Button"),
        new("TextBox", "常用控制項", ControlType.TextBox, "Txt", 180, 32, "TextBox"),
        new("TextBlock", "常用控制項", ControlType.TextBlock, "Lbl", 120, 24, "TextBlock"),
        new("CheckBox", "常用控制項", ControlType.CheckBox, "Chk", 120, 28, "CheckBox"),
        new("RadioButton", "常用控制項", ControlType.RadioButton, "Rad", 120, 28, "RadioButton"),
        new("ComboBox", "常用控制項", ControlType.ComboBox, "Cbo", 150, 32, "Option 1"),
        new("DatePicker", "常用控制項", ControlType.DatePicker, "Dtp", 200, 32),
        new("Slider", "常用控制項", ControlType.Slider, "Sld", 180, 30),
        new("ProgressBar", "常用控制項", ControlType.ProgressBar, "Prg", 180, 20),
        new("PictureBox", "常用控制項", ControlType.PictureBox, "Pic", 200, 150, "PictureBox"),
        new("Border", "常用控制項", ControlType.Border, "Brd", 200, 150),
        new("MediaPlayer", "多媒體元件", ControlType.MediaPlayer, "Player", 320, 240, "MediaPlayer"),

        // 佈局容器
        new("Grid", "佈局容器", ControlType.Grid, "Grd", 400, 300),
        new("StackPanel", "佈局容器", ControlType.StackPanel, "Stk", 300, 200),
        new("Canvas", "佈局容器", ControlType.Canvas, "Cvs", 400, 300),
        new("DockPanel", "佈局容器", ControlType.DockPanel, "Dck", 400, 300),
        new("ScrollViewer", "佈局容器", ControlType.ScrollViewer, "Scr", 300, 200),

        // 不可視元件與硬體通訊
        new("DispatcherTimer", "不可視元件", ControlType.DispatcherTimer, "Tmr", 150, 40, "計時器 (Timer)"),
        new("BackgroundWorker", "不可視元件", ControlType.BackgroundWorker, "Bgw", 150, 40, "背景工作 (Worker)"),
        new("BluetoothClient", "硬體通訊", ControlType.BluetoothClient, "Ble", 150, 40, "藍牙通訊 (BLE)"),
        new("SerialPortService", "硬體通訊", ControlType.SerialPortService, "Com", 150, 40, "序列埠 (COM)"),

        // 對話方塊元件
        new("OpenFileDialog", "對話方塊", ControlType.OpenFileDialog, "Ofd", 150, 40, "開啟檔案 (OpenFileDialog)"),
        new("SaveFileDialog", "對話方塊", ControlType.SaveFileDialog, "Sfd", 150, 40, "儲存檔案 (SaveFileDialog)"),
        new("MessageBox", "對話方塊", ControlType.MessageBox, "Msg", 150, 40, "訊息方塊 (MessageBox)"),

        // 除錯控制項
        new("DebugConsole", "除錯工具", ControlType.DebugConsole, "Dbg", 400, 180, "Debug Console"),

        // 業務邏輯原型
        new("LogicFunction", "業務邏輯", ControlType.LogicFunction, "Fn", 180, 50, "業務邏輯 (LogicFunction)")
    ];

    public static IReadOnlyList<ToolboxItem> GetAvailableItems() => Items;
}
