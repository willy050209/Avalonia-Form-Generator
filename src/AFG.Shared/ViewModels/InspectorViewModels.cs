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
        "Source",
        "Stretch",
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
        "Avalonia.Media.IImage",
        "Avalonia.Media.Stretch",
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
            "Source" => "Avalonia.Media.IImage?",
            "Stretch" => "Avalonia.Media.Stretch",
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
/// 事件參數項目編輯 ViewModel（支援參數名稱、專屬型別選單與常數/綁定設定）。
/// </summary>
public sealed partial class EventParameterItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "e";

    [ObservableProperty]
    private string _type = "RoutedEventArgs";

    [ObservableProperty]
    private string? _valueOrPath;

    [ObservableProperty]
    private bool _isConstant;

    [ObservableProperty]
    private IReadOnlyList<string> _availableParameterTypes = ControlEventCatalog.GetSupportedParameterTypes("Click");

    public EventParameterDefinition ToDefinition() => new(
        Name: string.IsNullOrWhiteSpace(Name) ? "parameter" : Name.Trim(),
        Type: string.IsNullOrWhiteSpace(Type) ? "object?" : Type.Trim(),
        ValueOrPath: string.IsNullOrWhiteSpace(ValueOrPath) ? null : ValueOrPath.Trim(),
        IsConstant: IsConstant);

    public static EventParameterItemViewModel FromDefinition(EventParameterDefinition def, string? eventName = null) => new()
    {
        Name = def.Name,
        Type = def.Type,
        ValueOrPath = def.ValueOrPath,
        IsConstant = def.IsConstant,
        AvailableParameterTypes = ControlEventCatalog.GetSupportedParameterTypes(eventName)
    };
}

/// <summary>
/// 事件映射項目編輯 ViewModel（支援多參數清單、事件名稱下拉選單與同步/非同步選項）。
/// </summary>
public sealed partial class EventItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _eventName = "Click";

    [ObservableProperty]
    private string _commandProperty = string.Empty;

    [ObservableProperty]
    private bool _isAsync = true;

    [ObservableProperty]
    private IReadOnlyList<string> _availableEvents = ControlEventCatalog.GetSupportedEvents(ControlType.Button);

    public ObservableCollection<EventParameterItemViewModel> Parameters { get; } = [];

    public event Action? ParameterChanged;

    public EventItemViewModel()
    {
        Parameters.CollectionChanged += (s, e) =>
        {
            if (e.NewItems is not null)
            {
                foreach (EventParameterItemViewModel p in e.NewItems)
                {
                    p.PropertyChanged += (_, _) => ParameterChanged?.Invoke();
                }
            }
            ParameterChanged?.Invoke();
        };
    }

    [RelayCommand]
    public void AddParameter()
    {
        var nextIndex = Parameters.Count + 1;
        var availableTypes = ControlEventCatalog.GetSupportedParameterTypes(EventName);
        var defaultType = availableTypes.Count > 0 ? availableTypes[0] : "string";

        Parameters.Add(new EventParameterItemViewModel
        {
            Name = $"param{nextIndex}",
            Type = defaultType,
            AvailableParameterTypes = availableTypes
        });
    }

    [RelayCommand]
    public void RemoveParameter(EventParameterItemViewModel param)
    {
        Parameters.Remove(param);
    }

    partial void OnEventNameChanged(string value)
    {
        var defaultParams = ControlEventCatalog.GetDefaultParameters(value);
        var availableTypes = ControlEventCatalog.GetSupportedParameterTypes(value);

        if (Parameters.Count == 0 || (Parameters.Count == 2 && Parameters[0].Name == "sender" && Parameters[1].Name == "e") || (Parameters.Count == 1 && Parameters[0].Name == "e"))
        {
            Parameters.Clear();
            foreach (var p in defaultParams)
            {
                Parameters.Add(EventParameterItemViewModel.FromDefinition(p, value));
            }
        }
        else
        {
            foreach (var param in Parameters)
            {
                param.AvailableParameterTypes = availableTypes;
            }
        }
    }

    public EventMappingDefinition ToDefinition() => new(
        EventName: EventName.Trim(),
        CommandProperty: CommandProperty.Trim(),
        IsAsync: IsAsync,
        Parameters: Parameters.Select(p => p.ToDefinition()).ToImmutableList());

    public static EventItemViewModel FromDefinition(EventMappingDefinition def, ControlType controlType = ControlType.Button)
    {
        var vm = new EventItemViewModel
        {
            EventName = def.EventName,
            CommandProperty = def.CommandProperty,
            IsAsync = def.IsAsync,
            AvailableEvents = ControlEventCatalog.GetSupportedEvents(controlType)
        };

        var effectiveParams = def.GetEffectiveParameters();
        if (effectiveParams.Count > 0)
        {
            foreach (var p in effectiveParams)
            {
                vm.Parameters.Add(EventParameterItemViewModel.FromDefinition(p, def.EventName));
            }
        }
        else
        {
            foreach (var p in ControlEventCatalog.GetDefaultParameters(def.EventName))
            {
                vm.Parameters.Add(EventParameterItemViewModel.FromDefinition(p, def.EventName));
            }
        }

        return vm;
    }
}
