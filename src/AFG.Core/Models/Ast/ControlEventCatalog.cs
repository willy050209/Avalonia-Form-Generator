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
            [ControlType.Image] = ["Tapped", "DoubleTapped", "PointerPressed", "PointerReleased"],
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
            [ControlType.SerialPortService] = ["DataReceived", "ErrorReceived", "PinChanged"]
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
        ControlType.Button => "Click",
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
}
