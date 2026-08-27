using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Validation;
using CoreBindingMode = AFG.Core.Enums.BindingMode;
using CoreHorizontalAlignment = AFG.Core.Enums.HorizontalAlignment;
using CoreVerticalAlignment = AFG.Core.Enums.VerticalAlignment;
using CoreControlType = AFG.Core.Enums.ControlType;
using CoreWindowStartupLocation = AFG.Core.Enums.WindowStartupLocation;
using CoreWindowState = AFG.Core.Enums.WindowState;
using CoreSystemDecorations = AFG.Core.Enums.SystemDecorations;

namespace AFG.Shared.ViewModels;

/// <summary>
/// 控制項與表單屬性檢查器 ViewModel，支援表單/視窗外觀、幾何佈局、MVVM 強型別資料綁定與事件轉命令配置。
/// </summary>
public sealed partial class InspectorViewModel : ObservableObject
{
    private readonly Services.IFileDialogService? _fileDialogService;
    private bool _isUpdating;
    private AstNode? _currentNode;
    private FormDocument? _currentDocument;

    public event Action<AstNode>? NodeUpdated;
    public event Action<FormDocument>? FormUpdated;

    [ObservableProperty]
    private bool _isFormSelected = true;

    // --- 表單與視窗控制屬性 (Form & Window Properties) ---
    [ObservableProperty]
    private string _formTitle = "Avalonia Form";

    [ObservableProperty]
    private string _formBackgroundColor = "#FFFFFF";

    [ObservableProperty]
    private double _formWidth = 800;

    [ObservableProperty]
    private double _formHeight = 600;

    [ObservableProperty]
    private double? _formMinWidth;

    [ObservableProperty]
    private double? _formMinHeight;

    [ObservableProperty]
    private double? _formMaxWidth;

    [ObservableProperty]
    private double? _formMaxHeight;

    [ObservableProperty]
    private CoreWindowStartupLocation _formWindowStartupLocation = CoreWindowStartupLocation.CenterScreen;

    [ObservableProperty]
    private CoreWindowState _formWindowState = CoreWindowState.Normal;

    [ObservableProperty]
    private bool _formCanResize = true;

    [ObservableProperty]
    private bool _formTopmost;

    [ObservableProperty]
    private bool _formShowInTaskbar = true;

    [ObservableProperty]
    private string _formIcon = string.Empty;

    [ObservableProperty]
    private CoreSystemDecorations _formSystemDecorations = CoreSystemDecorations.Full;

    [ObservableProperty]
    private string _formViewClassName = "MainFormView";

    [ObservableProperty]
    private string _formViewModelClassName = "MainFormViewModel";

    [ObservableProperty]
    private string _formRootNamespace = "GeneratedApp.Views";

    [ObservableProperty]
    private ArchitectureMode _formArchitectureMode = ArchitectureMode.Hybrid;

    [ObservableProperty]
    private bool _formGenerateCodeBehindFields = true;

    public IReadOnlyList<ArchitectureMode> ArchitectureModeOptions { get; } =
        Enum.GetValues<ArchitectureMode>();

    public IReadOnlyList<CoreWindowStartupLocation> WindowStartupLocationOptions { get; } =
        Enum.GetValues<CoreWindowStartupLocation>();

    public IReadOnlyList<CoreWindowState> WindowStateOptions { get; } =
        Enum.GetValues<CoreWindowState>();

    public IReadOnlyList<CoreSystemDecorations> SystemDecorationsOptions { get; } =
        Enum.GetValues<CoreSystemDecorations>();

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
    private bool? _autoPlay;

    [ObservableProperty]
    private bool? _isLooping;

    [ObservableProperty]
    private double? _volume = 1.0;

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
    private bool _isMediaPlayerSupported;

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
    public ObservableCollection<EventItemViewModel> FormEvents { get; } = [];
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

    public InspectorViewModel() : this(null) { }

    public InspectorViewModel(Services.IFileDialogService? fileDialogService = null)
    {
        _fileDialogService = fileDialogService;

        Bindings.CollectionChanged += (s, e) =>
        {
            if (e.OldItems is not null)
            {
                foreach (BindingItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= OnBindingItemPropertyChanged;
                }
            }
            if (e.NewItems is not null)
            {
                foreach (BindingItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += OnBindingItemPropertyChanged;
                }
            }
            if (!_isUpdating)
            {
                ApplyChanges();
            }
        };

        Events.CollectionChanged += (s, e) =>
        {
            if (e.OldItems is not null)
            {
                foreach (EventItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= OnEventItemPropertyChanged;
                    item.ParameterChanged -= OnEventItemParameterChanged;
                }
            }
            if (e.NewItems is not null)
            {
                foreach (EventItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += OnEventItemPropertyChanged;
                    item.ParameterChanged += OnEventItemParameterChanged;
                }
            }
            if (!_isUpdating)
            {
                ApplyChanges();
            }
        };

        FormEvents.CollectionChanged += (s, e) =>
        {
            if (e.OldItems is not null)
            {
                foreach (EventItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= OnFormEventItemPropertyChanged;
                    item.ParameterChanged -= OnFormEventItemParameterChanged;
                }
            }
            if (e.NewItems is not null)
            {
                foreach (EventItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += OnFormEventItemPropertyChanged;
                    item.ParameterChanged += OnFormEventItemParameterChanged;
                }
            }
            if (!_isUpdating)
            {
                ApplyFormChanges();
            }
        };
    }

    private void OnBindingItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_isUpdating)
        {
            ApplyChanges();
        }
    }

    private void OnEventItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_isUpdating)
        {
            ApplyChanges();
        }
    }

    private void OnEventItemParameterChanged()
    {
        if (!_isUpdating)
        {
            ApplyChanges();
        }
    }

    private void OnFormEventItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_isUpdating)
        {
            ApplyFormChanges();
        }
    }

    private void OnFormEventItemParameterChanged()
    {
        if (!_isUpdating)
        {
            ApplyFormChanges();
        }
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
            IsFormSelected = true;
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
        IsFormSelected = false;
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
        BitmapBackgroundColor = node.BitmapBackgroundColor ?? "#F0F0F0";
        AutoPlay = node.AutoPlay;
        IsLooping = node.IsLooping;
        Volume = node.Volume ?? 1.0;
        UpdateImagePreviewInfo();

        var isNonVisual = node.Type is CoreControlType.DispatcherTimer
                                    or CoreControlType.BackgroundWorker
                                    or CoreControlType.BluetoothClient
                                    or CoreControlType.SerialPortService
                                    or CoreControlType.OpenFileDialog
                                    or CoreControlType.SaveFileDialog
                                    or CoreControlType.MessageBox;

        IsVisualControl = !isNonVisual;
        IsTextSupported = node.Type is CoreControlType.Button or CoreControlType.TextBox or CoreControlType.TextBlock or CoreControlType.CheckBox or CoreControlType.RadioButton;
        IsContentSupported = node.Type is CoreControlType.Button or CoreControlType.CheckBox or CoreControlType.RadioButton or CoreControlType.Border;
        IsHeaderSupported = false;
        IsWatermarkSupported = node.Type is CoreControlType.TextBox;
        IsImageSupported = node.Type is CoreControlType.PictureBox or CoreControlType.Image;
        IsMediaPlayerSupported = node.Type is CoreControlType.MediaPlayer;
        IsTimerSupported = node.Type is CoreControlType.DispatcherTimer;
        IsCheckableSupported = node.Type is CoreControlType.CheckBox or CoreControlType.RadioButton;
        IsValueSupported = node.Type is CoreControlType.Slider or CoreControlType.ProgressBar;
        IsAutoSizeSupported = !isNonVisual && node.Type is not CoreControlType.Canvas;

        var isManagedByParent = parentNode is not null && parentNode.Type != CoreControlType.Canvas;
        IsPositionManagedByParent = isManagedByParent;
        ParentContainerType = parentNode?.Type.ToString() ?? string.Empty;

        IsCanvasPositionSupported = !isNonVisual && !isManagedByParent;
        IsGridCellSupported = parentNode?.Type == CoreControlType.Grid;
        IsDockSupported = parentNode?.Type == CoreControlType.DockPanel;

        var targetBindings = node.Bindings ?? [];
        var currentBindings = Bindings.Select(b => b.ToDefinition()).ToImmutableList();
        if (!targetBindings.SequenceEqual(currentBindings))
        {
            Bindings.Clear();
            foreach (var b in targetBindings)
            {
                Bindings.Add(BindingItemViewModel.FromDefinition(b, node.Type));
            }
        }

        var targetEvents = node.Events ?? [];
        var currentEvents = Events.Select(e => e.ToDefinition()).ToImmutableList();
        if (!targetEvents.SequenceEqual(currentEvents))
        {
            Events.Clear();
            foreach (var e in targetEvents)
            {
                Events.Add(EventItemViewModel.FromDefinition(e, node.Type));
            }
        }

        ValidateCurrentNode();
        _isUpdating = false;
    }

    /// <summary>
    /// 載入整份表單與視窗之控制屬性。
    /// </summary>
    public void LoadDocument(FormDocument? doc)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        _currentDocument = doc;

        if (doc is null)
        {
            _isUpdating = false;
            return;
        }

        FormTitle = doc.Title;
        FormBackgroundColor = doc.BackgroundColor ?? "#FFFFFF";
        FormWidth = doc.CanvasWidth;
        FormHeight = doc.CanvasHeight;
        FormMinWidth = doc.MinWidth;
        FormMinHeight = doc.MinHeight;
        FormMaxWidth = doc.MaxWidth;
        FormMaxHeight = doc.MaxHeight;
        FormWindowStartupLocation = doc.WindowStartupLocation;
        FormWindowState = doc.WindowState;
        FormCanResize = doc.CanResize;
        FormTopmost = doc.Topmost;
        FormShowInTaskbar = doc.ShowInTaskbar;
        FormIcon = doc.Icon ?? string.Empty;
        FormSystemDecorations = doc.SystemDecorations;
        FormViewClassName = doc.ViewClassName;
        FormViewModelClassName = doc.ViewModelClassName;
        FormRootNamespace = doc.RootNamespace;
        FormArchitectureMode = doc.ArchitectureMode;
        FormGenerateCodeBehindFields = doc.GenerateCodeBehindFields;

        var docEvents = doc.Events ?? [];
        var currentFormEvents = FormEvents.Select(e => e.ToDefinition()).ToImmutableList();
        if (!docEvents.SequenceEqual(currentFormEvents))
        {
            FormEvents.Clear();
            foreach (var e in docEvents)
            {
                FormEvents.Add(EventItemViewModel.FromFormEventDefinition(e));
            }
        }

        if (_currentNode is null)
        {
            IsFormSelected = true;
            HasSelectedNode = false;
        }

        _isUpdating = false;
    }

    [RelayCommand]
    public void SelectForm()
    {
        _currentNode = null;
        HasSelectedNode = false;
        IsFormSelected = true;
    }

    [RelayCommand]
    public async Task BrowseFormIconAsync()
    {
        if (_fileDialogService is null) return;
        var selectedFile = await _fileDialogService.OpenImageFileDialogAsync("選擇視窗圖示檔案");
        if (!string.IsNullOrWhiteSpace(selectedFile))
        {
            FormIcon = selectedFile;
        }
    }

    [RelayCommand]
    public void SetFormBackgroundColor(string? hexColor)
    {
        if (!string.IsNullOrWhiteSpace(hexColor))
        {
            FormBackgroundColor = hexColor;
        }
    }

    [RelayCommand]
    public void ApplyFormPreset(string? sizeStr)
    {
        if (string.IsNullOrWhiteSpace(sizeStr)) return;
        var parts = sizeStr.Split('x', 'X');
        if (parts.Length == 2 && double.TryParse(parts[0], System.Globalization.CultureInfo.InvariantCulture, out var w) && double.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var h))
        {
            FormWidth = w;
            FormHeight = h;
        }
    }

    private void ApplyFormChanges()
    {
        if (_isUpdating || _currentDocument is null) return;

        try
        {
            var updatedDoc = _currentDocument with
            {
                Title = FormTitle?.Trim() ?? "Avalonia Form",
                BackgroundColor = string.IsNullOrWhiteSpace(FormBackgroundColor) ? null : FormBackgroundColor.Trim(),
                CanvasWidth = Math.Max(100, FormWidth),
                CanvasHeight = Math.Max(100, FormHeight),
                MinWidth = FormMinWidth.HasValue ? Math.Max(0, FormMinWidth.Value) : null,
                MinHeight = FormMinHeight.HasValue ? Math.Max(0, FormMinHeight.Value) : null,
                MaxWidth = FormMaxWidth.HasValue ? Math.Max(0, FormMaxWidth.Value) : null,
                MaxHeight = FormMaxHeight.HasValue ? Math.Max(0, FormMaxHeight.Value) : null,
                WindowStartupLocation = FormWindowStartupLocation,
                WindowState = FormWindowState,
                CanResize = FormCanResize,
                Topmost = FormTopmost,
                ShowInTaskbar = FormShowInTaskbar,
                Icon = string.IsNullOrWhiteSpace(FormIcon) ? null : FormIcon.Trim(),
                SystemDecorations = FormSystemDecorations,
                ViewClassName = string.IsNullOrWhiteSpace(FormViewClassName) ? "MainFormView" : FormViewClassName.Trim(),
                ViewModelClassName = string.IsNullOrWhiteSpace(FormViewModelClassName) ? "MainFormViewModel" : FormViewModelClassName.Trim(),
                RootNamespace = string.IsNullOrWhiteSpace(FormRootNamespace) ? "GeneratedApp.Views" : FormRootNamespace.Trim(),
                ArchitectureMode = FormArchitectureMode,
                Events = FormEvents.Select(e => e.ToDefinition()).ToImmutableList()
            };

            _currentDocument = updatedDoc;
            FormUpdated?.Invoke(updatedDoc);
        }
        catch
        {
            // 防護任何異常
        }
    }

    [RelayCommand]
    public void AddBinding()
    {
        if (_currentNode is null) return;
        var availableProperties = ControlBindingCatalog.GetSupportedProperties(_currentNode.Type);
        var targetProperty = availableProperties.Count > 0 ? availableProperties[0] : "Text";
        var defaultMode = targetProperty is "Text" or "IsChecked" or "Value" or "Position" or "Volume" or "AutoPlay" or "IsLooping"
            ? CoreBindingMode.TwoWay
            : CoreBindingMode.Default;

        Bindings.Add(new BindingItemViewModel
        {
            TargetControlType = _currentNode.Type,
            AvailableProperties = availableProperties,
            TargetProperty = targetProperty,
            ViewModelProperty = $"{NodeName}_{targetProperty}",
            CustomDataType = ControlBindingCatalog.GetDefaultDataType(targetProperty, _currentNode.Type),
            Mode = defaultMode
        });
        ApplyChanges();
    }

    public IReadOnlyList<string> GetAvailablePropertiesForCurrentControl()
    {
        if (_currentNode is null) return ["Text", "Content"];
        return ControlBindingCatalog.GetSupportedProperties(_currentNode.Type);
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
    public void AddFormEvent()
    {
        var availableEvents = ControlEventCatalog.GetSupportedFormEvents();
        var defaultEvent = availableEvents.Count > 0 ? availableEvents[0] : "Loaded";
        var defaultCommand = $"Form_{defaultEvent}Command";

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

        FormEvents.Add(eventVm);
        ApplyFormChanges();
    }

    [RelayCommand]
    public void RemoveFormEvent(EventItemViewModel item)
    {
        if (FormEvents.Remove(item))
        {
            ApplyFormChanges();
        }
    }

    [RelayCommand]
    public void AddEvent()
    {
        if (_currentNode is null)
        {
            if (IsFormSelected)
            {
                AddFormEvent();
            }
            return;
        }
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
        else if (FormEvents.Remove(item))
        {
            ApplyFormChanges();
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
                AutoPlay = IsMediaPlayerSupported ? AutoPlay : null,
                IsLooping = IsMediaPlayerSupported ? IsLooping : null,
                Volume = IsMediaPlayerSupported ? (Volume.HasValue ? Math.Clamp(Volume.Value, 0.0, 1.0) : 1.0) : null,
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

    partial void OnFormTitleChanged(string value) => ApplyFormChanges();
    partial void OnFormBackgroundColorChanged(string value) => ApplyFormChanges();
    partial void OnFormWidthChanged(double value) => ApplyFormChanges();
    partial void OnFormHeightChanged(double value) => ApplyFormChanges();
    partial void OnFormMinWidthChanged(double? value) => ApplyFormChanges();
    partial void OnFormMinHeightChanged(double? value) => ApplyFormChanges();
    partial void OnFormMaxWidthChanged(double? value) => ApplyFormChanges();
    partial void OnFormMaxHeightChanged(double? value) => ApplyFormChanges();
    partial void OnFormWindowStartupLocationChanged(CoreWindowStartupLocation value) => ApplyFormChanges();
    partial void OnFormWindowStateChanged(CoreWindowState value) => ApplyFormChanges();
    partial void OnFormCanResizeChanged(bool value) => ApplyFormChanges();
    partial void OnFormTopmostChanged(bool value) => ApplyFormChanges();
    partial void OnFormShowInTaskbarChanged(bool value) => ApplyFormChanges();
    partial void OnFormIconChanged(string value) => ApplyFormChanges();
    partial void OnFormSystemDecorationsChanged(CoreSystemDecorations value) => ApplyFormChanges();
    partial void OnFormViewClassNameChanged(string value) => ApplyFormChanges();
    partial void OnFormViewModelClassNameChanged(string value) => ApplyFormChanges();
    partial void OnFormRootNamespaceChanged(string value) => ApplyFormChanges();
    partial void OnFormArchitectureModeChanged(ArchitectureMode value)
    {
        _formGenerateCodeBehindFields = value is ArchitectureMode.Hybrid or ArchitectureMode.CodeBehind;
        OnPropertyChanged(nameof(FormGenerateCodeBehindFields));
        ApplyFormChanges();
    }
    partial void OnFormGenerateCodeBehindFieldsChanged(bool value)
    {
        if (value && _formArchitectureMode == ArchitectureMode.PureMvvm)
        {
            _formArchitectureMode = ArchitectureMode.Hybrid;
            OnPropertyChanged(nameof(FormArchitectureMode));
        }
        else if (!value && _formArchitectureMode != ArchitectureMode.PureMvvm)
        {
            _formArchitectureMode = ArchitectureMode.PureMvvm;
            OnPropertyChanged(nameof(FormArchitectureMode));
        }
        ApplyFormChanges();
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
    partial void OnAutoPlayChanged(bool? value) => ApplyChanges();
    partial void OnIsLoopingChanged(bool? value) => ApplyChanges();
    partial void OnVolumeChanged(double? value) => ApplyChanges();
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
