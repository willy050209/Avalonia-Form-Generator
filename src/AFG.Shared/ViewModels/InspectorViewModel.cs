// filepath: src/AFG.Shared/ViewModels/InspectorViewModel.cs
using CoreBindingMode = AFG.Core.Enums.BindingMode;
using CoreHorizontalAlignment = AFG.Core.Enums.HorizontalAlignment;
using CoreVerticalAlignment = AFG.Core.Enums.VerticalAlignment;
using CoreControlType = AFG.Core.Enums.ControlType;

namespace AFG.Shared.ViewModels;

/// <summary>
/// 控制項屬性與事件檢查器 ViewModel，支援外觀、幾何佈局、MVVM 強型別資料綁定與事件轉命令配置。
/// </summary>
public sealed partial class InspectorViewModel : ObservableObject
{
    private bool _isUpdating;
    private AstNode? _currentNode;

    public event Action<AstNode>? NodeUpdated;

    [ObservableProperty]
    private bool _hasSelectedNode;

    [ObservableProperty]
    private string _nodeId = string.Empty;

    [ObservableProperty]
    private string _controlType = string.Empty;

    [ObservableProperty]
    private string _nodeName = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private string _watermark = string.Empty;

    [ObservableProperty]
    private double? _width;

    [ObservableProperty]
    private double? _height;

    [ObservableProperty]
    private double? _canvasLeft;

    [ObservableProperty]
    private double? _canvasTop;

    [ObservableProperty]
    private int _gridRow;

    [ObservableProperty]
    private int _gridColumn;

    [ObservableProperty]
    private int _gridRowSpan = 1;

    [ObservableProperty]
    private int _gridColumnSpan = 1;

    [ObservableProperty]
    private double _marginLeft;

    [ObservableProperty]
    private double _marginTop;

    [ObservableProperty]
    private double _marginRight;

    [ObservableProperty]
    private double _marginBottom;

    [ObservableProperty]
    private CoreHorizontalAlignment _horizontalAlignment = CoreHorizontalAlignment.Stretch;

    [ObservableProperty]
    private CoreVerticalAlignment _verticalAlignment = CoreVerticalAlignment.Stretch;

    [ObservableProperty]
    private double _opacity = 1.0;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private double? _fontSize;

    [ObservableProperty]
    private string? _background;

    [ObservableProperty]
    private string? _foreground;

    [ObservableProperty]
    private bool? _isChecked;

    [ObservableProperty]
    private double? _value;

    public ObservableCollection<BindingItemViewModel> Bindings { get; } = [];
    public ObservableCollection<EventItemViewModel> Events { get; } = [];
    public ObservableCollection<ValidationError> ValidationErrors { get; } = [];

    public IReadOnlyList<CoreHorizontalAlignment> HorizontalAlignmentOptions { get; } =
        Enum.GetValues<CoreHorizontalAlignment>();

    public IReadOnlyList<CoreVerticalAlignment> VerticalAlignmentOptions { get; } =
        Enum.GetValues<CoreVerticalAlignment>();

    public IReadOnlyList<CoreBindingMode> BindingModeOptions { get; } =
        Enum.GetValues<CoreBindingMode>();

    public void LoadNode(AstNode? node)
    {
        if (_isUpdating)
        {
            return;
        }

        if (node is not null && _currentNode is not null && node.Id == _currentNode.Id)
        {
            _currentNode = node;
            return;
        }

        _isUpdating = true;
        _currentNode = node;

        if (node is null)
        {
            HasSelectedNode = false;
            NodeId = string.Empty;
            ControlType = string.Empty;
            NodeName = string.Empty;
            Background = null;
            Foreground = null;
            Bindings.Clear();
            Events.Clear();
            ValidationErrors.Clear();
            _isUpdating = false;
            return;
        }

        HasSelectedNode = true;
        NodeId = node.Id;
        ControlType = node.Type.ToString();
        NodeName = node.Name;
        Text = node.Text ?? string.Empty;
        Content = node.Content ?? string.Empty;
        Header = node.Header ?? string.Empty;
        Watermark = node.Watermark ?? string.Empty;
        Width = node.Width;
        Height = node.Height;
        CanvasLeft = node.CanvasLeft;
        CanvasTop = node.CanvasTop;
        GridRow = node.GridRow;
        GridColumn = node.GridColumn;
        GridRowSpan = node.GridRowSpan;
        GridColumnSpan = node.GridColumnSpan;
        MarginLeft = node.Margin.Left;
        MarginTop = node.Margin.Top;
        MarginRight = node.Margin.Right;
        MarginBottom = node.Margin.Bottom;
        HorizontalAlignment = node.HorizontalAlignment;
        VerticalAlignment = node.VerticalAlignment;
        Opacity = node.Opacity;
        IsEnabled = node.IsEnabled;
        IsVisible = node.IsVisible;
        FontSize = node.FontSize;
        Background = node.Background;
        Foreground = node.Foreground;
        IsChecked = node.IsChecked;
        Value = node.Value;

        Bindings.Clear();
        foreach (var b in node.Bindings)
        {
            var item = BindingItemViewModel.FromDefinition(b);
            item.PropertyChanged += (_, _) => ApplyChanges();
            Bindings.Add(item);
        }

        Events.Clear();
        foreach (var e in node.Events)
        {
            var item = EventItemViewModel.FromDefinition(e, node.Type);
            item.PropertyChanged += (_, _) => ApplyChanges();
            Events.Add(item);
        }

        ValidateCurrentNode();
        _isUpdating = false;
    }

    [RelayCommand]
    private void AddBinding()
    {
        var item = new BindingItemViewModel
        {
            TargetProperty = ControlType switch
            {
                "TextBox" => "Text",
                "CheckBox" or "RadioButton" => "IsChecked",
                "Slider" or "ProgressBar" => "Value",
                "ComboBox" or "ListBox" or "DataGrid" => "ItemsSource",
                _ => "IsEnabled"
            },
            ViewModelProperty = $"{NodeName}Property"
        };
        item.PropertyChanged += (_, _) => ApplyChanges();
        Bindings.Add(item);
        ApplyChanges();
    }

    [RelayCommand]
    private void RemoveBinding(BindingItemViewModel item)
    {
        if (Bindings.Remove(item))
        {
            ApplyChanges();
        }
    }

    [RelayCommand]
    private void AddEvent()
    {
        var nodeType = _currentNode?.Type ?? CoreControlType.Button;
        var supported = ControlEventCatalog.GetSupportedEvents(nodeType);
        var defaultEvt = ControlEventCatalog.GetDefaultEvent(nodeType);

        var item = new EventItemViewModel
        {
            AvailableEvents = supported,
            EventName = defaultEvt,
            CommandProperty = $"{NodeName}Command"
        };
        item.PropertyChanged += (_, _) => ApplyChanges();
        Events.Add(item);
        ApplyChanges();
    }

    [RelayCommand]
    private void RemoveEvent(EventItemViewModel item)
    {
        if (Events.Remove(item))
        {
            ApplyChanges();
        }
    }

    private void ApplyChanges()
    {
        if (_isUpdating || _currentNode is null)
        {
            return;
        }

        try
        {
            var updatedNode = _currentNode with
            {
                Name = NodeName?.Trim() ?? string.Empty,
                Text = string.IsNullOrEmpty(Text) ? null : Text,
                Content = string.IsNullOrEmpty(Content) ? null : Content,
                Header = string.IsNullOrEmpty(Header) ? null : Header,
                Watermark = string.IsNullOrEmpty(Watermark) ? null : Watermark,
                Width = Width.HasValue ? Math.Max(0, Width.Value) : null,
                Height = Height.HasValue ? Math.Max(0, Height.Value) : null,
                CanvasLeft = CanvasLeft,
                CanvasTop = CanvasTop,
                GridRow = Math.Max(0, GridRow),
                GridColumn = Math.Max(0, GridColumn),
                GridRowSpan = Math.Max(1, GridRowSpan),
                GridColumnSpan = Math.Max(1, GridColumnSpan),
                Margin = new ThicknessModel(
                    Math.Max(0, MarginLeft),
                    Math.Max(0, MarginTop),
                    Math.Max(0, MarginRight),
                    Math.Max(0, MarginBottom)),
                HorizontalAlignment = HorizontalAlignment,
                VerticalAlignment = VerticalAlignment,
                Opacity = Math.Clamp(Opacity, 0.0, 1.0),
                IsEnabled = IsEnabled,
                IsVisible = IsVisible,
                FontSize = FontSize.HasValue ? Math.Clamp(FontSize.Value, 1.0, 200.0) : null,
                Background = string.IsNullOrWhiteSpace(Background) ? null : Background.Trim(),
                Foreground = string.IsNullOrWhiteSpace(Foreground) ? null : Foreground.Trim(),
                IsChecked = IsChecked,
                Value = Value,
                Bindings = Bindings.Select(b => b.ToDefinition()).ToImmutableList(),
                Events = Events.Select(e => e.ToDefinition()).ToImmutableList()
            };

            _currentNode = updatedNode;
            ValidateCurrentNode();
            NodeUpdated?.Invoke(updatedNode);
        }
        catch
        {
            // 防護任何異常輸入與格式轉換錯誤，保持 UI 穩定
        }
    }

    private void ValidateCurrentNode()
    {
        ValidationErrors.Clear();
        if (_currentNode is null) return;

        var result = AstValidator.ValidateTree(_currentNode);
        foreach (var err in result.Items)
        {
            ValidationErrors.Add(err);
        }
    }

    partial void OnNodeNameChanged(string value) => ApplyChanges();
    partial void OnTextChanged(string value) => ApplyChanges();
    partial void OnContentChanged(string value) => ApplyChanges();
    partial void OnHeaderChanged(string value) => ApplyChanges();
    partial void OnWatermarkChanged(string value) => ApplyChanges();
    partial void OnWidthChanged(double? value) => ApplyChanges();
    partial void OnHeightChanged(double? value) => ApplyChanges();
    partial void OnCanvasLeftChanged(double? value) => ApplyChanges();
    partial void OnCanvasTopChanged(double? value) => ApplyChanges();
    partial void OnGridRowChanged(int value) => ApplyChanges();
    partial void OnGridColumnChanged(int value) => ApplyChanges();
    partial void OnGridRowSpanChanged(int value) => ApplyChanges();
    partial void OnGridColumnSpanChanged(int value) => ApplyChanges();
    partial void OnMarginLeftChanged(double value) => ApplyChanges();
    partial void OnMarginTopChanged(double value) => ApplyChanges();
    partial void OnMarginRightChanged(double value) => ApplyChanges();
    partial void OnMarginBottomChanged(double value) => ApplyChanges();
    partial void OnHorizontalAlignmentChanged(CoreHorizontalAlignment value) => ApplyChanges();
    partial void OnVerticalAlignmentChanged(CoreVerticalAlignment value) => ApplyChanges();
    partial void OnOpacityChanged(double value) => ApplyChanges();
    partial void OnIsEnabledChanged(bool value) => ApplyChanges();
    partial void OnIsVisibleChanged(bool value) => ApplyChanges();
    partial void OnFontSizeChanged(double? value) => ApplyChanges();
    partial void OnBackgroundChanged(string? value) => ApplyChanges();
    partial void OnForegroundChanged(string? value) => ApplyChanges();
    partial void OnIsCheckedChanged(bool? value) => ApplyChanges();
    partial void OnValueChanged(double? value) => ApplyChanges();
}
