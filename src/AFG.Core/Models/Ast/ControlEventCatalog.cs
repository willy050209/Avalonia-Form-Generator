// filepath: src/AFG.Core/Models/Ast/ControlEventCatalog.cs
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using AFG.Core.Enums;

namespace AFG.Core.Models.Ast;

/// <summary>
/// 提供每個控制項與不可視硬體/通訊元件之專屬支援事件與回呼 (Callbacks) 清單目錄。
/// </summary>
public static class ControlEventCatalog
{
    private static readonly ImmutableDictionary<ControlType, ImmutableList<string>> EventMap =
        new Dictionary<ControlType, ImmutableList<string>>
        {
            [ControlType.Button] = ["Click", "Tapped", "DoubleTapped", "PointerPressed", "PointerReleased", "KeyDown", "KeyUp"],
            [ControlType.TextBox] = ["TextChanged", "KeyDown", "KeyUp", "GotFocus", "LostFocus", "PointerPressed"],
            [ControlType.TextBlock] = ["Tapped", "DoubleTapped", "PointerPressed", "PointerReleased"],
            [ControlType.CheckBox] = ["IsCheckedChanged", "Checked", "Unchecked", "Click"],
            [ControlType.RadioButton] = ["IsCheckedChanged", "Checked", "Unchecked", "Click"],
            [ControlType.ComboBox] = ["SelectionChanged", "DropDownOpened", "DropDownClosed"],
            [ControlType.ListBox] = ["SelectionChanged", "DoubleTapped", "Tapped"],
            [ControlType.DatePicker] = ["SelectedDateChanged"],
            [ControlType.TimePicker] = ["SelectedTimeChanged"],
            [ControlType.Slider] = ["ValueChanged"],
            [ControlType.ProgressBar] = ["ValueChanged"],
            [ControlType.DataGrid] = ["SelectionChanged", "DoubleTapped", "CellEditEnded"],
            [ControlType.Image] = ["Click", "DoubleClick", "Tapped", "DoubleTapped", "PointerPressed", "PointerReleased", "LoadCompleted"],
            [ControlType.PictureBox] = ["Click", "DoubleClick", "Tapped", "DoubleTapped", "PointerPressed", "PointerReleased", "LoadCompleted", "SizeModeChanged"],
            [ControlType.Border] = ["PointerPressed", "PointerReleased", "Tapped", "DoubleTapped"],

            // 佈局容器
            [ControlType.Canvas] = ["PointerPressed", "PointerReleased", "Tapped"],
            [ControlType.Grid] = ["PointerPressed", "PointerReleased", "Tapped"],
            [ControlType.StackPanel] = ["PointerPressed", "PointerReleased", "Tapped"],
            [ControlType.DockPanel] = ["PointerPressed", "PointerReleased", "Tapped"],
            [ControlType.WrapPanel] = ["PointerPressed", "PointerReleased", "Tapped"],
            [ControlType.ScrollViewer] = ["ScrollChanged", "PointerPressed", "PointerReleased"],

            // 不可視元件與通訊硬體服務專屬回呼事件
            [ControlType.DispatcherTimer] = ["Tick"],
            [ControlType.BackgroundWorker] = ["DoWork", "ProgressChanged", "RunWorkerCompleted"],
            [ControlType.BluetoothClient] = ["DeviceDiscovered", "Connected", "Disconnected", "DataReceived"],
            [ControlType.SerialPortService] = ["DataReceived", "ErrorReceived", "PinChanged"],

            // 對話方塊與除錯控制項
            [ControlType.OpenFileDialog] = ["FileOk"],
            [ControlType.SaveFileDialog] = ["FileOk"],
            [ControlType.MessageBox] = ["Confirmed"],
            [ControlType.DebugConsole] = ["Cleared", "Tapped", "PointerPressed"]
        }.ToImmutableDictionary();

    private static readonly ImmutableList<string> FallbackEvents = ["Tapped", "PointerPressed"];

    /// <summary>
    /// 取得特定控制項或元件所支援的專屬事件清單。
    /// </summary>
    public static IReadOnlyList<string> GetSupportedEvents(ControlType controlType)
    {
        if (EventMap.TryGetValue(controlType, out var list))
        {
            return list;
        }

        return FallbackEvents;
    }

    /// <summary>
    /// 取得特定控制項或元件在建立事件映射時的預設事件名稱。
    /// </summary>
    public static string GetDefaultEvent(ControlType controlType) => controlType switch
    {
        ControlType.Button or ControlType.PictureBox or ControlType.Image => "Click",
        ControlType.TextBox => "TextChanged",
        ControlType.CheckBox or ControlType.RadioButton => "IsCheckedChanged",
        ControlType.ComboBox or ControlType.ListBox or ControlType.DataGrid => "SelectionChanged",
        ControlType.Slider or ControlType.ProgressBar => "ValueChanged",
        ControlType.DatePicker => "SelectedDateChanged",
        ControlType.TimePicker => "SelectedTimeChanged",
        ControlType.ScrollViewer => "ScrollChanged",
        ControlType.DispatcherTimer => "Tick",
        ControlType.BackgroundWorker => "DoWork",
        ControlType.BluetoothClient => "DataReceived",
        ControlType.SerialPortService => "DataReceived",
        ControlType.OpenFileDialog => "FileOk",
        ControlType.SaveFileDialog => "FileOk",
        ControlType.MessageBox => "Confirmed",
        ControlType.DebugConsole => "Cleared",
        _ => "Tapped"
    };

    /// <summary>
    /// 驗證特定控制項是否支援該事件名稱。
    /// </summary>
    public static bool IsSupported(ControlType controlType, string? eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return false;
        }

        var supported = GetSupportedEvents(controlType);
        return supported.Contains(eventName.Trim());
    }

    /// <summary>
    /// 取得特定事件對應之預設 C# 事件參數型別（例如 RoutedEventArgs, PointerPressedEventArgs 等）。
    /// </summary>
    public static string GetDefaultEventArgsType(string? eventName) => eventName switch
    {
        "Click" or "Checked" or "Unchecked" or "IsCheckedChanged" => "RoutedEventArgs",
        "Tapped" or "DoubleTapped" => "TappedEventArgs",
        "PointerPressed" => "PointerPressedEventArgs",
        "PointerReleased" => "PointerReleasedEventArgs",
        "PointerMoved" => "PointerEventArgs",
        "KeyDown" or "KeyUp" => "KeyEventArgs",
        "TextChanged" => "TextChangedEventArgs",
        "SelectionChanged" => "SelectionChangedEventArgs",
        "ScrollChanged" => "ScrollChangedEventArgs",
        "SelectedDateChanged" => "DatePickerSelectedValueChangedEventArgs",
        "SelectedTimeChanged" => "TimePickerSelectedValueChangedEventArgs",
        "ValueChanged" => "RangeBaseValueChangedEventArgs",
        "DoWork" => "DoWorkEventArgs",
        "ProgressChanged" => "ProgressChangedEventArgs",
        "RunWorkerCompleted" => "RunWorkerCompletedEventArgs",
        "DataReceived" => "string",
        "Tick" => "EventArgs",
        "FileOk" => "string?",
        "Confirmed" => "bool?",
        "Cleared" => "EventArgs",
        _ => "RoutedEventArgs"
    };

    /// <summary>
    /// 依據參數型別推斷標準參數名稱（如 e, sender, parameter）。
    /// </summary>
    public static string GetDefaultParameterName(string? parameterType, string? fallbackName = null)
    {
        if (string.IsNullOrWhiteSpace(parameterType))
        {
            return fallbackName ?? "parameter";
        }

        parameterType = parameterType.Trim();
        if (parameterType.EndsWith("EventArgs", StringComparison.Ordinal) || parameterType == "EventArgs")
        {
            return "e";
        }

        if (parameterType is "object" or "object?" or "Control" or "Control?")
        {
            return "sender";
        }

        return fallbackName ?? "parameter";
    }

    /// <summary>
    /// 取得特定事件之預設參數清單（預設包含 sender 與專屬 EventArgs/資料參數）。
    /// </summary>
    public static ImmutableList<EventParameterDefinition> GetDefaultParameters(string? eventName)
    {
        var eventArgsType = GetDefaultEventArgsType(eventName);

        if (eventName is "Tick")
        {
            return
            [
                new EventParameterDefinition("sender", "object?", null, false),
                new EventParameterDefinition("e", "EventArgs", null, false)
            ];
        }

        if (eventName is "FileOk")
        {
            return
            [
                new EventParameterDefinition("sender", "object?", null, false),
                new EventParameterDefinition("filePath", "string?", null, false)
            ];
        }

        if (eventName is "Confirmed")
        {
            return
            [
                new EventParameterDefinition("sender", "object?", null, false),
                new EventParameterDefinition("result", "bool?", null, false)
            ];
        }

        if (eventName is "DataReceived")
        {
            return
            [
                new EventParameterDefinition("sender", "object?", null, false),
                new EventParameterDefinition("data", "string", null, false)
            ];
        }

        if (eventName is "DoWork")
        {
            return
            [
                new EventParameterDefinition("sender", "object?", null, false),
                new EventParameterDefinition("e", "DoWorkEventArgs", null, false)
            ];
        }

        return
        [
            new EventParameterDefinition("sender", "object?", null, false),
            new EventParameterDefinition("e", eventArgsType, null, false)
        ];
    }

    /// <summary>
    /// 取得特定事件專屬支援的 C# 參數型別清單（僅包含該事件專屬 EventArgs 與通用基底型別，排除其他事件的無關 EventArgs）。
    /// </summary>
    public static IReadOnlyList<string> GetSupportedParameterTypes(string? eventName)
    {
        var specificArgs = eventName switch
        {
            "Click" or "Checked" or "Unchecked" or "IsCheckedChanged" => (IReadOnlyList<string>)["RoutedEventArgs"],
            "Tapped" or "DoubleTapped" => ["TappedEventArgs", "RoutedEventArgs"],
            "PointerPressed" or "PointerReleased" or "PointerMoved" => ["PointerPressedEventArgs", "PointerReleasedEventArgs", "PointerEventArgs", "RoutedEventArgs"],
            "KeyDown" or "KeyUp" => ["KeyEventArgs", "RoutedEventArgs"],
            "TextChanged" => ["TextChangedEventArgs", "RoutedEventArgs"],
            "SelectionChanged" => ["SelectionChangedEventArgs", "RoutedEventArgs"],
            "ScrollChanged" => ["ScrollChangedEventArgs", "RoutedEventArgs"],
            "SelectedDateChanged" => ["DatePickerSelectedValueChangedEventArgs", "RoutedEventArgs"],
            "SelectedTimeChanged" => ["TimePickerSelectedValueChangedEventArgs", "RoutedEventArgs"],
            "ValueChanged" => ["RangeBaseValueChangedEventArgs", "RoutedEventArgs"],
            "DoWork" => ["DoWorkEventArgs", "EventArgs"],
            "ProgressChanged" => ["ProgressChangedEventArgs", "EventArgs"],
            "RunWorkerCompleted" => ["RunWorkerCompletedEventArgs", "EventArgs"],
            "Tick" => ["EventArgs"],
            "FileOk" => ["string?", "string", "CancelEventArgs", "EventArgs"],
            "Confirmed" => ["bool?", "bool", "string?", "EventArgs"],
            "DataReceived" => ["string", "byte[]", "EventArgs"],
            _ => ["RoutedEventArgs", "EventArgs"]
        };

        var commonPrimitives = new string[]
        {
            "object?",
            "Control?",
            "string",
            "int",
            "double",
            "bool",
            "Guid"
        };

        return specificArgs.Concat(commonPrimitives).Distinct().ToList();
    }
}
