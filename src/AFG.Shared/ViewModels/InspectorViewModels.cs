// filepath: src/AFG.Shared/ViewModels/InspectorViewModels.cs
using System.Collections.Generic;
using CoreBindingMode = AFG.Core.Enums.BindingMode;

namespace AFG.Shared.ViewModels;

/// <summary>
/// 綁定項目編輯 ViewModel（支援自訂 C# 資料型別與下拉選擇目標屬性）。
/// </summary>
public sealed partial class BindingItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _targetProperty = "Text";

    [ObservableProperty]
    private string _viewModelProperty = string.Empty;

    [ObservableProperty]
    private string _customDataType = "string";

    [ObservableProperty]
    private CoreBindingMode _mode = CoreBindingMode.Default;

    public IReadOnlyList<string> AvailableProperties { get; } =
    [
        "Text",
        "Content",
        "IsChecked",
        "Value",
        "IsEnabled",
        "IsVisible",
        "Opacity",
        "Width",
        "Height",
        "FontSize",
        "ItemsSource",
        "SelectedItem",
        "SelectedIndex",
        "Header",
        "Watermark",
        "Background",
        "Foreground"
    ];

    public IReadOnlyList<string> CommonDataTypes { get; } =
    [
        "string",
        "int",
        "double",
        "decimal",
        "bool",
        "DateTime?",
        "ObservableCollection<string>",
        "ObservableCollection<object>",
        "List<string>"
    ];

    partial void OnTargetPropertyChanged(string value)
    {
        CustomDataType = value switch
        {
            "IsChecked" or "IsEnabled" or "IsVisible" => "bool",
            "Value" or "Opacity" or "Width" or "Height" or "FontSize" => "double",
            "SelectedIndex" => "int",
            "ItemsSource" => "ObservableCollection<string>",
            "SelectedItem" => "string?",
            _ => "string"
        };
    }

    public BindingDefinition ToDefinition() => new(
        TargetProperty: TargetProperty.Trim(),
        ViewModelProperty: ViewModelProperty.Trim(),
        Mode: Mode,
        CustomDataType: string.IsNullOrWhiteSpace(CustomDataType) ? null : CustomDataType.Trim());

    public static BindingItemViewModel FromDefinition(BindingDefinition def) => new()
    {
        TargetProperty = def.TargetProperty,
        ViewModelProperty = def.ViewModelProperty,
        CustomDataType = def.CustomDataType ?? "string",
        Mode = def.Mode
    };
}

/// <summary>
/// 事件映射項目編輯 ViewModel（支援事件名稱下拉選單與同步/非同步選項）。
/// </summary>
public sealed partial class EventItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _eventName = "Click";

    [ObservableProperty]
    private string _commandProperty = string.Empty;

    [ObservableProperty]
    private bool _isAsync = true;

    public IReadOnlyList<string> AvailableEvents { get; } =
    [
        "Click",
        "Tapped",
        "DoubleTapped",
        "PointerPressed",
        "PointerReleased",
        "PointerEntered",
        "PointerExited",
        "SelectionChanged",
        "ValueChanged",
        "TextChanged",
        "KeyDown",
        "KeyUp",
        "GotFocus",
        "LostFocus",
        "Checked",
        "Unchecked"
    ];

    public EventMappingDefinition ToDefinition() => new(
        EventName: EventName.Trim(),
        CommandProperty: CommandProperty.Trim(),
        IsAsync: IsAsync);

    public static EventItemViewModel FromDefinition(EventMappingDefinition def) => new()
    {
        EventName = def.EventName,
        CommandProperty = def.CommandProperty,
        IsAsync = def.IsAsync
    };
}
