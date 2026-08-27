// filepath: src/AFG.Core/Models/Ast/ControlBindingCatalog.cs
using System;
using System.Collections.Generic;
using System.Linq;
using AFG.Core.Enums;

namespace AFG.Core.Models.Ast;

/// <summary>
/// 控制項專屬資料綁定屬性白名單目錄、預設型別推斷與強型別型態約束服務。
/// </summary>
public static class ControlBindingCatalog
{
    private static readonly IReadOnlyList<string> s_commonVisualProperties =
        ["IsEnabled", "IsVisible", "Width", "Height", "Opacity"];

    /// <summary>
    /// 取得指定控制項類型所支援的所有可綁定屬性白名單清單。
    /// </summary>
    public static IReadOnlyList<string> GetSupportedProperties(ControlType controlType) => controlType switch
    {
        ControlType.Button => ["Text", "Content", "Background", "Foreground", .. s_commonVisualProperties],
        ControlType.TextBox => ["Text", "Watermark", "FontSize", "Background", "Foreground", .. s_commonVisualProperties],
        ControlType.TextBlock => ["Text", "FontSize", "Foreground", "Background", .. s_commonVisualProperties],
        ControlType.CheckBox or ControlType.RadioButton => ["IsChecked", "Text", "Content", "Foreground", "Background", .. s_commonVisualProperties],
        ControlType.Slider or ControlType.ProgressBar => ["Value", .. s_commonVisualProperties],
        ControlType.ComboBox or ControlType.ListBox or ControlType.DataGrid => ["ItemsSource", "SelectedItem", "SelectedIndex", .. s_commonVisualProperties],
        ControlType.DatePicker => ["SelectedDate", .. s_commonVisualProperties],
        ControlType.TimePicker => ["SelectedTime", .. s_commonVisualProperties],
        ControlType.Image or ControlType.PictureBox => ["Source", "Stretch", .. s_commonVisualProperties],
        ControlType.Border => ["Background", "BorderBrush", "Padding", .. s_commonVisualProperties],
        ControlType.MediaPlayer => ["Source", "AutoPlay", "IsLooping", "Volume", "Position", "Duration", "State", "CurrentFrame", "Stretch", "SpeedRatio", .. s_commonVisualProperties],
        ControlType.Canvas or ControlType.Grid or ControlType.StackPanel or ControlType.DockPanel or ControlType.WrapPanel or ControlType.ScrollViewer =>
            ["Background", .. s_commonVisualProperties],
        ControlType.DispatcherTimer => ["Interval", "IsEnabled"],
        ControlType.BackgroundWorker => ["WorkerReportsProgress", "WorkerSupportsCancellation", "IsBusy"],
        ControlType.BluetoothClient => ["DeviceName", "IsConnected", "ServiceUuid"],
        ControlType.SerialPortService => ["PortName", "BaudRate", "IsOpen"],
        ControlType.OpenFileDialog => ["Title", "Filter", "InitialDirectory", "SelectedFilePath"],
        ControlType.SaveFileDialog => ["Title", "Filter", "DefaultExtension", "SelectedFilePath"],
        ControlType.MessageBox => ["Title", "Message"],
        ControlType.DebugConsole => ["LogEntries", "MaxLines", "AutoScroll", .. s_commonVisualProperties],
        _ => ["Text", "Content", .. s_commonVisualProperties]
    };

    /// <summary>
    /// 檢查指定屬性是否為該控制項類型所合法支援。
    /// </summary>
    public static bool IsPropertySupported(ControlType controlType, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return false;
        var supported = GetSupportedProperties(controlType);
        return supported.Contains(propertyName.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 取得指定屬性於特定控制項類型下的預設 C# 資料型別。
    /// </summary>
    public static string GetDefaultDataType(string propertyName, ControlType controlType)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return "string";

        return propertyName.Trim() switch
        {
            "IsChecked" or "IsEnabled" or "IsVisible" or "AutoPlay" or "IsLooping" or
            "WorkerReportsProgress" or "WorkerSupportsCancellation" or "IsBusy" or
            "IsConnected" or "IsOpen" or "AutoScroll" => "bool",

            "Value" or "Volume" or "Opacity" or "SpeedRatio" or "FontSize" or "Width" or "Height" => "double",

            "SelectedIndex" or "MaxLines" or "BaudRate" or "Interval" => "int",

            "Position" or "Duration" or "SelectedTime" => "TimeSpan",

            "SelectedDate" => "DateTime?",

            "State" => "AFG.Core.Enums.MediaState",

            "Stretch" => "Avalonia.Media.Stretch",

            "CurrentFrame" => "Avalonia.Media.Imaging.Bitmap?",

            "Source" when controlType == ControlType.MediaPlayer => "string",
            "Source" => "Avalonia.Media.IImage?",

            "Background" or "Foreground" or "BorderBrush" => "string",

            "ItemsSource" => "ObservableCollection<string>",
            "LogEntries" => "ObservableCollection<LogEntry>",

            "SelectedItem" => "string?",

            _ => "string"
        };
    }

    /// <summary>
    /// 取得指定屬性於特定控制項類型下所有合法相容的 C# 資料型別選項清單。
    /// </summary>
    public static IReadOnlyList<string> GetCompatibleDataTypes(string propertyName, ControlType controlType)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return ["string"];

        return propertyName.Trim() switch
        {
            "IsChecked" or "IsEnabled" or "IsVisible" or "AutoPlay" or "IsLooping" or
            "WorkerReportsProgress" or "WorkerSupportsCancellation" or "IsBusy" or
            "IsConnected" or "IsOpen" or "AutoScroll" =>
                ["bool", "bool?"],

            "Value" or "Volume" or "Opacity" or "SpeedRatio" or "FontSize" =>
                ["double", "double?", "float", "float?", "int", "int?", "decimal", "decimal?"],

            "Width" or "Height" =>
                ["double", "double?", "int", "int?"],

            "SelectedIndex" or "MaxLines" or "BaudRate" or "Interval" =>
                ["int", "int?", "long", "double"],

            "Position" or "Duration" or "SelectedTime" =>
                ["TimeSpan", "TimeSpan?"],

            "SelectedDate" =>
                ["DateTime?", "DateTime", "DateTimeOffset?", "DateTimeOffset"],

            "State" =>
                ["AFG.Core.Enums.MediaState", "string"],

            "Stretch" =>
                ["Avalonia.Media.Stretch", "AFG.Core.Enums.Stretch"],

            "CurrentFrame" =>
                [
                    "Avalonia.Media.Imaging.Bitmap?",
                    "Avalonia.Media.Imaging.Bitmap",
                    "Avalonia.Media.IImage?",
                    "Avalonia.Media.IImage",
                    "Avalonia.Media.Imaging.WriteableBitmap?"
                ],

            "Source" when controlType == ControlType.MediaPlayer =>
                ["string", "string?", "Uri", "Uri?"],

            "Source" =>
                [
                    "Avalonia.Media.IImage?",
                    "Avalonia.Media.IImage",
                    "Avalonia.Media.Imaging.Bitmap?",
                    "Avalonia.Media.Imaging.Bitmap",
                    "Avalonia.Media.Imaging.WriteableBitmap?",
                    "string",
                    "string?",
                    "Uri"
                ],

            "Background" or "Foreground" or "BorderBrush" =>
                ["string", "string?", "Avalonia.Media.IBrush", "Avalonia.Media.IBrush?", "Avalonia.Media.SolidColorBrush?"],

            "ItemsSource" =>
                [
                    "ObservableCollection<string>",
                    "ObservableCollection<object>",
                    "List<string>",
                    "List<object>",
                    "IEnumerable<object>",
                    "IEnumerable<string>"
                ],

            "LogEntries" =>
                ["ObservableCollection<LogEntry>", "List<LogEntry>", "ObservableCollection<string>"],

            "SelectedItem" =>
                ["string?", "string", "object?", "object"],

            "Content" =>
                ["string", "string?", "object", "object?"],

            _ => ["string", "string?"]
        };
    }

    /// <summary>
    /// 檢查指定的資料型別名稱是否與目標屬性的型態約束相容。
    /// </summary>
    public static bool IsDataTypeCompatible(string propertyName, string dataTypeName, ControlType controlType)
    {
        if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(dataTypeName))
        {
            return false;
        }

        var normalizedProp = propertyName.Trim();
        var normalizedType = NormalizeTypeName(dataTypeName);

        // 1. CurrentFrame 嚴格禁止非影像型別 (例如 bool, int, double, string)
        if (normalizedProp.Equals("CurrentFrame", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedType is "iimage" or "bitmap" or "writeablebitmap" or "rendertargetbitmap" or "image";
        }

        // 2. 布林專屬屬性嚴格限制
        if (normalizedProp is "IsChecked" or "IsEnabled" or "IsVisible" or "AutoPlay" or "IsLooping" or
            "WorkerReportsProgress" or "WorkerSupportsCancellation" or "IsBusy" or "IsConnected" or "IsOpen" or "AutoScroll")
        {
            return normalizedType is "bool" or "boolean";
        }

        // 3. 數值屬性限制
        if (normalizedProp is "Value" or "Volume" or "Opacity" or "SpeedRatio" or "FontSize")
        {
            return normalizedType is "double" or "float" or "int" or "decimal" or "single" or "int32" or "int64";
        }

        // 4. 時間跨度屬性限制
        if (normalizedProp is "Position" or "Duration" or "SelectedTime")
        {
            return normalizedType is "timespan";
        }

        // 5. 日期屬性限制
        if (normalizedProp is "SelectedDate")
        {
            return normalizedType is "datetime" or "datetimeoffset";
        }

        // 6. 狀態列舉限制
        if (normalizedProp is "State")
        {
            return normalizedType is "mediastate" or "string";
        }

        // 7. 拉伸列舉限制
        if (normalizedProp is "Stretch")
        {
            return normalizedType is "stretch";
        }

        // 8. 集合屬性
        if (normalizedProp is "ItemsSource" or "LogEntries")
        {
            return normalizedType.StartsWith("observablecollection", StringComparison.OrdinalIgnoreCase) ||
                   normalizedType.StartsWith("list", StringComparison.OrdinalIgnoreCase) ||
                   normalizedType.StartsWith("ienumerable", StringComparison.OrdinalIgnoreCase) ||
                   normalizedType.StartsWith("icollection", StringComparison.OrdinalIgnoreCase);
        }

        // 9. 一般文字或自訂型別
        var allowed = GetCompatibleDataTypes(normalizedProp, controlType);
        return allowed.Any(a => NormalizeTypeName(a).Equals(normalizedType, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeTypeName(string typeName)
    {
        var raw = typeName.Trim();
        if (raw.EndsWith('?')) raw = raw[..^1].Trim();
        if (raw.StartsWith("global::", StringComparison.OrdinalIgnoreCase)) raw = raw[8..];

        var dotIndex = raw.LastIndexOf('.');
        if (dotIndex >= 0 && !raw.Contains('<'))
        {
            raw = raw[(dotIndex + 1)..];
        }

        return raw.ToLowerInvariant();
    }
}
