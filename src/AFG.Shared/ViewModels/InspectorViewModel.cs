// filepath: src/AFG.Shared/ViewModels/InspectorViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AFG.Core.Models.Ast;
using AFG.Core.Validation;
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
    private readonly Services.IFileDialogService? _fileDialogService;
    private bool _isUpdating;
    private AstNode? _currentNode;

    public event Action<AstNode>? NodeUpdated;

    public InspectorViewModel(Services.IFileDialogService? fileDialogService = null)
    {
        _fileDialogService = fileDialogService;
    }

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
    private bool _autoSize;

    [ObservableProperty]
    private int? _interval = 1000;

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

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private Core.Enums.Stretch? _stretch = Core.Enums.Stretch.Uniform;

    [ObservableProperty]
    private bool _useRelativePath = true;

    [ObservableProperty]
    private bool _initBitmap;

    [ObservableProperty]
    private string? _bitmapBackgroundColor = "#F0F0F0";

    [ObservableProperty]
    private string? _imageFileInfo;

    [ObservableProperty]
    private bool _hasImageSource;

    // --- 控制項特性可見度旗標 (Property Visibility Capabilities) ---
    [ObservableProperty]
    private bool _isTextSupported = true;

    [ObservableProperty]
    private bool _isContentSupported = true;

    [ObservableProperty]
    private bool _isHeaderSupported;

    [ObservableProperty]
    private bool _isWatermarkSupported;

    [ObservableProperty]
    private bool _isImageSupported;

    [ObservableProperty]
    private bool _isTimerSupported;

    [ObservableProperty]
    private bool _isCheckableSupported;

    [ObservableProperty]
    private bool _isValueSupported;

    [ObservableProperty]
    private bool _isAutoSizeSupported;

    [ObservableProperty]
    private bool _isVisualControl = true;

    [ObservableProperty]
    private bool _isGeometrySupported = true;

    [ObservableProperty]
    private bool _isPositionManagedByParent;

    [ObservableProperty]
    private string _parentContainerType = string.Empty;

    [ObservableProperty]
    private bool _isCanvasPositionSupported = true;

    [ObservableProperty]
    private bool _isGridCellSupported;

    [ObservableProperty]
    private bool _isDockSupported;

    public ObservableCollection<BindingItemViewModel> Bindings { get; } = [];
    public ObservableCollection<EventItemViewModel> Events { get; } = [];
    public ObservableCollection<ValidationError> ValidationErrors { get; } = [];

    public IReadOnlyList<CoreHorizontalAlignment> HorizontalAlignmentOptions { get; } =
        Enum.GetValues<CoreHorizontalAlignment>();

    public IReadOnlyList<CoreVerticalAlignment> VerticalAlignmentOptions { get; } =
        Enum.GetValues<CoreVerticalAlignment>();

    public IReadOnlyList<CoreBindingMode> BindingModeOptions { get; } =
        Enum.GetValues<CoreBindingMode>();

    public IReadOnlyList<Core.Enums.Stretch?> StretchOptions { get; } =
    [
        null,
        Core.Enums.Stretch.None,
        Core.Enums.Stretch.Fill,
        Core.Enums.Stretch.Uniform,
        Core.Enums.Stretch.UniformToFill
    ];

    public InspectorViewModel()
    {
        Bindings.CollectionChanged += (s, e) =>
        {
            if (e.NewItems is not null)
            {
                foreach (BindingItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += (_, _) => ApplyChanges();
                }
            }
        };

        Events.CollectionChanged += (s, e) =>
        {
            if (e.NewItems is not null)
            {
                foreach (EventItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += (_, _) => ApplyChanges();
                    item.ParameterChanged += ApplyChanges;
                }
            }
        };
    }

    public void LoadNode(AstNode? node, AstNode? parentNode = null)
    {
        if (_isUpdating)
        {
            return;
        }

        var isSameNode = node is not null && _currentNode is not null && node.Id == _currentNode.Id;

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
            IsPositionManagedByParent = false;
            ParentContainerType = string.Empty;
            IsCanvasPositionSupported = true;
            IsGridCellSupported = false;
            IsDockSupported = false;
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
        AutoSize = node.AutoSize;
        Interval = node.Interval ?? 1000;
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
        Source = node.Source ?? string.Empty;
        Stretch = node.Stretch;
        UseRelativePath = node.UseRelativePath;
        InitBitmap = node.InitBitmap;
        BitmapBackgroundColor = string.IsNullOrWhiteSpace(node.BitmapBackgroundColor) ? "#F0F0F0" : node.BitmapBackgroundColor;
        UpdateImagePreviewInfo();

        // 計算控制項支援之特定屬性
        var type = node.Type;
        var isNonVisual = Generators.CSharpMarkup.CSharpMarkupViewGenerator.IsNonVisualComponent(type);
        IsVisualControl = !isNonVisual;
        IsGeometrySupported = !isNonVisual;
        IsTimerSupported = type == CoreControlType.DispatcherTimer;
        IsImageSupported = type == CoreControlType.PictureBox;
        IsTextSupported = type is CoreControlType.TextBlock or CoreControlType.TextBox or CoreControlType.Button or CoreControlType.CheckBox or CoreControlType.RadioButton or CoreControlType.ComboBox or CoreControlType.DatePicker;
        IsContentSupported = type is CoreControlType.Button or CoreControlType.CheckBox or CoreControlType.RadioButton or CoreControlType.Border;
        IsHeaderSupported = false;
        IsWatermarkSupported = type is CoreControlType.TextBox or CoreControlType.ComboBox;
        IsCheckableSupported = type is CoreControlType.CheckBox or CoreControlType.RadioButton;
        IsValueSupported = type is CoreControlType.Slider or CoreControlType.ProgressBar;
        IsAutoSizeSupported = !node.IsContainer && !isNonVisual;

        // 判定是否受非 Canvas 容器管理
        var isManagedByParent = parentNode is not null &&
            parentNode.Type is CoreControlType.StackPanel or CoreControlType.DockPanel or CoreControlType.WrapPanel or CoreControlType.Grid;

        IsPositionManagedByParent = isManagedByParent;
        ParentContainerType = isManagedByParent ? parentNode!.Type.ToString() : string.Empty;
        IsCanvasPositionSupported = !isNonVisual && !isManagedByParent;
        IsGridCellSupported = parentNode?.Type == CoreControlType.Grid;
        IsDockSupported = parentNode?.Type == CoreControlType.DockPanel;

        if (!isSameNode)
        {
            // 載入資料綁定
            Bindings.Clear();
            foreach (var b in node.Bindings)
            {
                Bindings.Add(BindingItemViewModel.FromDefinition(b));
            }

            // 載入事件
            Events.Clear();
            foreach (var e in node.Events)
            {
                Events.Add(EventItemViewModel.FromDefinition(e, node.Type));
            }
        }

        ValidateCurrentNode();
        _isUpdating = false;
    }

    [RelayCommand]
    public void AddBinding()
    {
        if (_currentNode is null) return;
        var availableProperties = GetAvailablePropertiesForCurrentControl();
        var targetProperty = availableProperties.Count > 0 ? availableProperties[0] : "Text";
        var defaultMode = targetProperty is "Text" or "IsChecked" or "Value"
            ? CoreBindingMode.TwoWay
            : CoreBindingMode.Default;

        Bindings.Add(new BindingItemViewModel
        {
            TargetProperty = targetProperty,
            ViewModelProperty = $"{NodeName}_{targetProperty}",
            Mode = defaultMode
        });
        ApplyChanges();
    }

    public IReadOnlyList<string> GetAvailablePropertiesForCurrentControl()
    {
        if (_currentNode is null) return ["Text", "Content"];
        return _currentNode.Type switch
        {
            CoreControlType.Button => ["Text", "Content", "IsEnabled", "IsVisible", "Background", "Foreground", "Width", "Height"],
            CoreControlType.TextBox => ["Text", "Watermark", "IsEnabled", "IsVisible", "FontSize", "Width", "Height"],
            CoreControlType.TextBlock => ["Text", "FontSize", "Foreground", "IsVisible", "Width", "Height"],
            CoreControlType.CheckBox or CoreControlType.RadioButton => ["IsChecked", "Text", "Content", "IsEnabled", "IsVisible"],
            CoreControlType.Slider or CoreControlType.ProgressBar => ["Value", "IsEnabled", "IsVisible", "Width", "Height"],
            CoreControlType.ComboBox => ["ItemsSource", "SelectedItem", "SelectedIndex", "IsEnabled", "IsVisible", "Width", "Height"],
            CoreControlType.ListBox => ["ItemsSource", "SelectedItem", "SelectedIndex", "IsEnabled", "IsVisible", "Width", "Height"],
            CoreControlType.PictureBox => ["Source", "Stretch", "IsEnabled", "IsVisible", "Width", "Height"],
            CoreControlType.Border => ["Background", "BorderBrush", "Padding", "Width", "Height", "IsVisible"],
            _ => ["Text", "Content", "IsEnabled", "IsVisible", "Width", "Height"]
        };
    }

    [RelayCommand]
    public void RemoveBinding(BindingItemViewModel item)
    {
        if (Bindings.Remove(item))
        {
            ApplyChanges();
        }
    }

    [RelayCommand]
    public void AddEvent()
    {
        if (_currentNode is null) return;
        var availableEvents = ControlEventCatalog.GetSupportedEvents(_currentNode.Type);
        var defaultEvent = ControlEventCatalog.GetDefaultEvent(_currentNode.Type) ?? (availableEvents.Count > 0 ? availableEvents[0] : "Click");
        var defaultCommand = $"{NodeName}_{defaultEvent}Command";

        var eventVm = new EventItemViewModel
        {
            EventName = defaultEvent,
            CommandProperty = defaultCommand,
            AvailableEvents = availableEvents
        };

        if (eventVm.Parameters.Count == 0)
        {
            foreach (var p in ControlEventCatalog.GetDefaultParameters(defaultEvent))
            {
                eventVm.Parameters.Add(EventParameterItemViewModel.FromDefinition(p, defaultEvent));
            }
        }

        Events.Add(eventVm);
        ApplyChanges();
    }

    [RelayCommand]
    public void RemoveEvent(EventItemViewModel item)
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
                AutoSize = AutoSize,
                Interval = IsTimerSupported ? (Interval.HasValue ? Math.Max(1, Interval.Value) : 1000) : null,
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
                Source = string.IsNullOrWhiteSpace(Source) ? null : Source.Trim(),
                Stretch = Stretch,
                UseRelativePath = UseRelativePath,
                InitBitmap = InitBitmap,
                BitmapBackgroundColor = string.IsNullOrWhiteSpace(BitmapBackgroundColor) ? "#F0F0F0" : BitmapBackgroundColor.Trim(),
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

    [RelayCommand]
    public async Task BrowseImageAsync()
    {
        if (_fileDialogService is null) return;
        var selectedFile = await _fileDialogService.OpenImageFileDialogAsync("選擇 PictureBox 圖片檔案");
        if (!string.IsNullOrWhiteSpace(selectedFile))
        {
            Source = selectedFile;
        }
    }

    private void UpdateImagePreviewInfo()
    {
        if (string.IsNullOrWhiteSpace(Source))
        {
            HasImageSource = false;
            ImageFileInfo = null;
            return;
        }

        HasImageSource = true;
        try
        {
            if (System.IO.File.Exists(Source))
            {
                var fi = new System.IO.FileInfo(Source);
                ImageFileInfo = $"{fi.Name} ({(fi.Length / 1024.0):F1} KB)";
            }
            else
            {
                ImageFileInfo = Source;
            }
        }
        catch
        {
            ImageFileInfo = Source;
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
    partial void OnSourceChanged(string value) { UpdateImagePreviewInfo(); ApplyChanges(); }
    partial void OnStretchChanged(Core.Enums.Stretch? value) => ApplyChanges();
    partial void OnUseRelativePathChanged(bool value) => ApplyChanges();
    partial void OnInitBitmapChanged(bool value) => ApplyChanges();
    partial void OnBitmapBackgroundColorChanged(string? value) => ApplyChanges();
    partial void OnWidthChanged(double? value) => ApplyChanges();
    partial void OnHeightChanged(double? value) => ApplyChanges();
    partial void OnAutoSizeChanged(bool value) => ApplyChanges();
    partial void OnIntervalChanged(int? value) => ApplyChanges();
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
