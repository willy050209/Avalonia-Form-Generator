// filepath: src/AFG.Core/Enums/ControlType.cs
namespace AFG.Core.Enums;

/// <summary>
/// 表示支援的 Avalonia 控制項與容器類型。
/// </summary>
public enum ControlType
{
    // 基本控制項
    Button,
    TextBox,
    TextBlock,
    CheckBox,
    RadioButton,
    ComboBox,
    ListBox,
    DatePicker,
    TimePicker,
    Slider,
    ProgressBar,
    DataGrid,
    Image,
    Border,

    // 佈局容器
    Canvas,
    Grid,
    StackPanel,
    DockPanel,
    WrapPanel,
    ScrollViewer,

    // 不可視元件與通訊服務
    DispatcherTimer,
    BackgroundWorker,
    BluetoothClient,
    SerialPortService
}
