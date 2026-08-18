// filepath: src/AFG.Shared/ViewModels/InspectorViewModels.cs
using CoreBindingMode = AFG.Core.Enums.BindingMode;

namespace AFG.Shared.ViewModels;

/// <summary>
/// 綁定項目編輯 ViewModel。
/// </summary>
public sealed partial class BindingItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _targetProperty = "Text";

    [ObservableProperty]
    private string _viewModelProperty = string.Empty;

    [ObservableProperty]
    private CoreBindingMode _mode = CoreBindingMode.Default;

    public BindingDefinition ToDefinition() => new(
        TargetProperty: TargetProperty.Trim(),
        ViewModelProperty: ViewModelProperty.Trim(),
        Mode: Mode);

    public static BindingItemViewModel FromDefinition(BindingDefinition def) => new()
    {
        TargetProperty = def.TargetProperty,
        ViewModelProperty = def.ViewModelProperty,
        Mode = def.Mode
    };
}

/// <summary>
/// 事件映射項目編輯 ViewModel（支援同步/非同步選項）。
/// </summary>
public sealed partial class EventItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _eventName = "Click";

    [ObservableProperty]
    private string _commandProperty = string.Empty;

    [ObservableProperty]
    private bool _isAsync = true;

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
