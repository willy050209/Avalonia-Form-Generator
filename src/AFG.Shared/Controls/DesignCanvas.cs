using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Shared.Models;
using AFG.Shared.Services;
using AFG.Shared.ViewModels;

namespace AFG.Shared.Controls;

/// <summary>
/// 拖曳縮放模式列舉。
/// </summary>
public enum ResizeHandleType
{
    None,
    Move,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}

/// <summary>
/// 視覺化設計畫布，支援巢狀容器遞迴渲染、多選框 (Rubberband)、多節點選取裝飾器、8 點縮放調整與對齊輔助線渲染。
/// </summary>
public sealed class DesignCanvas : Grid
{
    public static readonly StyledProperty<CanvasViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<DesignCanvas, CanvasViewModel?>(nameof(ViewModel));

    public CanvasViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private readonly Canvas _elementsCanvas = new() { ClipToBounds = true };
    private readonly AdornerOverlay _overlay;

    private ResizeHandleType _activeHandle = ResizeHandleType.None;
    private Point _dragStartPoint;
    private Rect _initialNodeBounds;
    private bool _isDragging;
    private bool _isDragTransacted;
    private bool _isRubberbandActive;
    private Rect _rubberbandRect;

    // 容器內拖曳重新排序 (Container Drag-Reordering)
    private bool _isReorderingInContainer;
    private string? _reorderParentId;
    private string? _reorderChildId;
    private int _reorderTargetIndex;
    private (Point Start, Point End)? _insertionIndicator;

    public DesignCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
        Background = new SolidColorBrush(Color.FromRgb(24, 24, 27)); // 深色底板

        _overlay = new AdornerOverlay(this);

        Children.Add(_elementsCanvas);
        Children.Add(_overlay);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ViewModelProperty)
        {
            if (change.OldValue is CanvasViewModel oldVm)
            {
                oldVm.DocumentChanged -= OnDocumentChanged;
                oldVm.SelectionChanged -= OnSelectionChanged;
            }

            if (change.NewValue is CanvasViewModel newVm)
            {
                newVm.DocumentChanged += OnDocumentChanged;
                newVm.SelectionChanged += OnSelectionChanged;
                RebuildElements();
            }
        }
    }

    private void OnDocumentChanged(FormDocument doc)
    {
        if (!TryPatchElements(doc.RootNode))
        {
            RebuildElements();
        }
        else
        {
            _overlay.InvalidateVisual();
        }
    }

    private void OnSelectionChanged(AstNode? node) => _overlay.InvalidateVisual();

    public void RebuildElements()
    {
        _elementsCanvas.Children.Clear();
        if (ViewModel?.Document is null)
        {
            return;
        }

        foreach (var node in ViewModel.Document.RootNode.Children)
        {
            var element = CreateControlFromNode(node);
            _elementsCanvas.Children.Add(element);
        }

        _overlay.InvalidateVisual();
    }

    /// <summary>
    /// 高效差量更新現有視覺控制項之幾何座標與外觀，避免高頻率微調或屬性變更時全樹銷毀重建引發 GC 壓力與畫面閃爍。
    /// </summary>
    private bool TryPatchElements(AstNode rootNode)
    {
        var rootChildren = rootNode.Children;
        if (_elementsCanvas.Children.Count != rootChildren.Count)
        {
            return false;
        }

        for (var i = 0; i < rootChildren.Count; i++)
        {
            var node = rootChildren[i];
            var ctrl = _elementsCanvas.Children[i];

            if (!Equals(ctrl.Tag, node.Id))
            {
                return false;
            }

            // 若有巢狀子容器且子節點數量改變，觸發全樹重建
            if (node.IsContainer && node.Children.Count > 0)
            {
                return false;
            }

            // 尺寸更新
            if (node.AutoSize)
            {
                ctrl.Width = double.NaN;
                ctrl.Height = double.NaN;
            }
            else
            {
                if (node.Width.HasValue) ctrl.Width = node.Width.Value;
                if (node.Height.HasValue) ctrl.Height = node.Height.Value;
            }

            var left = node.CanvasLeft ?? 0;
            var top = node.CanvasTop ?? 0;
            Canvas.SetLeft(ctrl, left);
            Canvas.SetTop(ctrl, top);

            ctrl.Margin = new Thickness(node.Margin.Left, node.Margin.Top, node.Margin.Right, node.Margin.Bottom);
            ctrl.Opacity = node.Opacity;
            ctrl.IsVisible = node.IsVisible;
            ctrl.IsEnabled = node.IsEnabled;

            if (ctrl is Button btn)
            {
                btn.Content = node.Content ?? node.Text ?? "Button";
            }
            else if (ctrl is TextBlock tb)
            {
                tb.Text = node.Text ?? "TextBlock";
            }
            else if (ctrl is TextBox txt)
            {
                txt.Text = node.Text ?? string.Empty;
                txt.PlaceholderText = node.Watermark;
            }
            else if (ctrl is CheckBox cb)
            {
                cb.Content = node.Content ?? "CheckBox";
                cb.IsChecked = node.IsChecked;
            }
            else if (ctrl is RadioButton rb)
            {
                rb.Content = node.Content ?? "RadioButton";
                rb.IsChecked = node.IsChecked;
            }
            else if (ctrl is Slider sl)
            {
                if (node.Value.HasValue) sl.Value = node.Value.Value;
            }
            else if (ctrl is ProgressBar pb)
            {
                if (node.Value.HasValue) pb.Value = node.Value.Value;
            }
            else if (ctrl is Border cardBorder && cardBorder.Child is StackPanel cardSp && cardSp.Children.Count >= 2 && cardSp.Children[1] is TextBlock nameTb)
            {
                nameTb.Text = node.Name;
            }
        }

        return true;
    }

    /// <summary>
    /// 遞迴建立 AST 節點對應之 Avalonia 控制項階層。
    /// </summary>
    private static Control CreateControlFromNode(AstNode node)
    {
        Control control;

        switch (node.Type)
        {
            case ControlType.Button:
                control = new Button { Content = node.Content ?? node.Text ?? "Button" };
                break;
            case ControlType.TextBox:
                control = new TextBox { Text = node.Text ?? string.Empty, PlaceholderText = node.Watermark };
                break;
            case ControlType.TextBlock:
                control = new TextBlock { Text = node.Text ?? "TextBlock", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                break;
            case ControlType.CheckBox:
                control = new CheckBox { Content = node.Content ?? "CheckBox", IsChecked = node.IsChecked };
                break;
            case ControlType.RadioButton:
                control = new RadioButton { Content = node.Content ?? "RadioButton" };
                break;
            case ControlType.ComboBox:
                control = new ComboBox { PlaceholderText = "Select item...", Width = node.Width ?? 120 };
                break;
            case ControlType.DatePicker:
                control = new DatePicker();
                break;
            case ControlType.Slider:
                control = new Slider { Minimum = node.Minimum ?? 0, Maximum = node.Maximum ?? 100, Value = node.Value ?? 50 };
                break;
            case ControlType.ProgressBar:
                control = new ProgressBar { Minimum = 0, Maximum = 100, Value = 60 };
                break;
            case ControlType.PictureBox or ControlType.Image:
                var picBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4)
                };
                var picPanel = new StackPanel
                {
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Spacing = 4
                };
                picPanel.Children.Add(new TextBlock
                {
                    Text = "PictureBox",
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#A1A1AA")),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });
                var sourceText = !string.IsNullOrWhiteSpace(node.Source)
                    ? node.Source
                    : (!string.IsNullOrWhiteSpace(node.Text) ? node.Text : (!string.IsNullOrWhiteSpace(node.Content) ? node.Content : "(無影像來源)"));
                picPanel.Children.Add(new TextBlock
                {
                    Text = sourceText,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#71717A")),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });
                picBorder.Child = picPanel;
                control = picBorder;
                break;
            case ControlType.Border:
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1)
                };
                if (node.Children.Count > 0)
                {
                    border.Child = CreateControlFromNode(node.Children[0]);
                }
                control = border;
                break;
            case ControlType.StackPanel:
                var sp = new StackPanel
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    Orientation = (node.Orientation ?? Core.Enums.Orientation.Vertical) == Core.Enums.Orientation.Horizontal
                        ? Avalonia.Layout.Orientation.Horizontal
                        : Avalonia.Layout.Orientation.Vertical
                };
                foreach (var child in node.Children)
                {
                    sp.Children.Add(CreateControlFromNode(child));
                }
                control = sp;
                break;
            case ControlType.Grid:
                var grid = new Grid
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255))
                };
                if (node.RowDefinitions.Count > 0)
                {
                    grid.RowDefinitions = new RowDefinitions(string.Join(",", node.RowDefinitions.Select(r => r.ToString())));
                }
                if (node.ColumnDefinitions.Count > 0)
                {
                    grid.ColumnDefinitions = new ColumnDefinitions(string.Join(",", node.ColumnDefinitions.Select(c => c.ToString())));
                }
                foreach (var child in node.Children)
                {
                    var childControl = CreateControlFromNode(child);
                    if (child.GridRow > 0) Grid.SetRow(childControl, child.GridRow);
                    if (child.GridColumn > 0) Grid.SetColumn(childControl, child.GridColumn);
                    if (child.GridRowSpan > 1) Grid.SetRowSpan(childControl, child.GridRowSpan);
                    if (child.GridColumnSpan > 1) Grid.SetColumnSpan(childControl, child.GridColumnSpan);
                    grid.Children.Add(childControl);
                }
                control = grid;
                break;
            case ControlType.Canvas:
                var innerCanvas = new Canvas
                {
                    Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255))
                };
                foreach (var child in node.Children)
                {
                    innerCanvas.Children.Add(CreateControlFromNode(child));
                }
                control = innerCanvas;
                break;
            case ControlType.DockPanel:
                var dockPanel = new DockPanel
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255))
                };
                foreach (var child in node.Children)
                {
                    var childCtrl = CreateControlFromNode(child);
                    if (child.Dock.HasValue)
                    {
                        DockPanel.SetDock(childCtrl, (Avalonia.Controls.Dock)child.Dock.Value);
                    }
                    dockPanel.Children.Add(childCtrl);
                }
                control = dockPanel;
                break;
            case ControlType.WrapPanel:
                var wrapPanel = new WrapPanel
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    Orientation = (node.Orientation ?? Core.Enums.Orientation.Horizontal) == Core.Enums.Orientation.Vertical
                        ? Avalonia.Layout.Orientation.Vertical
                        : Avalonia.Layout.Orientation.Horizontal
                };
                foreach (var child in node.Children)
                {
                    wrapPanel.Children.Add(CreateControlFromNode(child));
                }
                control = wrapPanel;
                break;
            case ControlType.ScrollViewer:
                var scrollViewer = new ScrollViewer
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255))
                };
                if (node.Children.Count > 0)
                {
                    scrollViewer.Content = CreateControlFromNode(node.Children[0]);
                }
                control = scrollViewer;
                break;
            case ControlType.DispatcherTimer or ControlType.BackgroundWorker or ControlType.BluetoothClient or ControlType.SerialPortService
                 or ControlType.OpenFileDialog or ControlType.SaveFileDialog or ControlType.MessageBox:
                var iconTag = node.Type switch
                {
                    ControlType.DispatcherTimer => "[Timer]",
                    ControlType.BackgroundWorker => "[Worker]",
                    ControlType.BluetoothClient => "[BLE]",
                    ControlType.SerialPortService => "[COM]",
                    ControlType.OpenFileDialog => "[OpenFileDialog]",
                    ControlType.SaveFileDialog => "[SaveFileDialog]",
                    ControlType.MessageBox => "[MessageBox]",
                    _ => "[Service]"
                };
                var compBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(220, 30, 41, 59)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 4),
                    Child = new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 6,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = iconTag, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)), FontSize = 11 },
                            new TextBlock { Text = node.Name, Foreground = Brushes.White, FontSize = 11, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }
                        }
                    }
                };
                control = compBorder;
                break;
            default:
                control = new Button { Content = node.Name };
                break;
        }

        if (node.AutoSize)
        {
            control.Width = double.NaN;
            control.Height = double.NaN;
        }
        else
        {
            control.Width = node.Width ?? 120;
            control.Height = node.Height ?? 35;
        }
        control.Tag = node.Id;
        control.IsHitTestVisible = false; // 讓畫布統一捕捉滑鼠選取與拖曳

        control.Margin = new Thickness(node.Margin.Left, node.Margin.Top, node.Margin.Right, node.Margin.Bottom);

        var left = node.CanvasLeft ?? 0;
        var top = node.CanvasTop ?? 0;
        Canvas.SetLeft(control, left);
        Canvas.SetTop(control, top);

        return control;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        if (ViewModel is null)
        {
            return;
        }

        // 0. 若目前處於工具箱拖曳放置狀態，直接在滑鼠釋放點加入控制項
        if (ViewModel.ActiveDraggingItem is not null)
        {
            var dropPos = e.GetPosition(_elementsCanvas);
            var item = ViewModel.ActiveDraggingItem;
            ViewModel.ActiveDraggingItem = null;
            Cursor = Cursor.Default;
            ViewModel.AddControlFromToolbox(item, dropPos.X, dropPos.Y);
            e.Handled = true;
            return;
        }

        var pos = e.GetPosition(this);
        _dragStartPoint = pos;
        var isCtrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // 1. 檢查是否點擊在已選取主節點的 8 個縮放手柄上 (若節點位於容器內部，則禁用自由縮放手柄)
        if (ViewModel.SelectedNode is not null)
        {
            var parentNode = AstTreeOperations.FindParentNode(ViewModel.Document.RootNode, ViewModel.SelectedNode.Id);
            var isManagedByParent = parentNode is not null && parentNode.Type is ControlType.StackPanel or ControlType.DockPanel or ControlType.WrapPanel or ControlType.Grid;

            if (!isManagedByParent)
            {
                var selectedBounds = GetNodeBounds(ViewModel.SelectedNode);
                var handle = HitTestResizeHandle(selectedBounds, pos);
                if (handle != ResizeHandleType.None)
                {
                    _activeHandle = handle;
                    _initialNodeBounds = selectedBounds;
                    _isDragging = true;
                    _isDragTransacted = false;
                    e.Pointer.Capture(this);
                    e.Handled = true;
                    return;
                }
            }
        }

        // 2. 命中測試畫布中的控制項
        var hitNode = HitTestNode(pos);
        if (hitNode is not null)
        {
            ViewModel.SelectNode(hitNode.Id, isToggle: isCtrlPressed);
            _activeHandle = ResizeHandleType.Move;
            _initialNodeBounds = GetNodeBounds(hitNode);
            _isDragging = true;
            _isDragTransacted = false;
            _isRubberbandActive = false;
            e.Pointer.Capture(this);
        }
        else
        {
            if (!isCtrlPressed)
            {
                ViewModel.SelectNode(null);
            }
            _activeHandle = ResizeHandleType.None;
            _isDragging = false;
            _isDragTransacted = false;
            _isRubberbandActive = true;
            _rubberbandRect = new Rect(pos, new Size(0, 0));
            e.Pointer.Capture(this);
        }

        _overlay.InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (ViewModel is null) return;

        if (ViewModel.ActiveDraggingItem is not null)
        {
            Cursor = new Cursor(StandardCursorType.DragCopy);
            return;
        }

        var currentPos = e.GetPosition(this);

        if (_isRubberbandActive)
        {
            var x = Math.Min(_dragStartPoint.X, currentPos.X);
            var y = Math.Min(_dragStartPoint.Y, currentPos.Y);
            var w = Math.Abs(currentPos.X - _dragStartPoint.X);
            var h = Math.Abs(currentPos.Y - _dragStartPoint.Y);
            _rubberbandRect = new Rect(x, y, w, h);

            // 框選命中測試（支援容器內部所有子控制項）
            var selectedIds = new List<string>();
            foreach (var node in AstTreeOperations.Flatten(ViewModel.Document.RootNode).Where(n => n.Id != ViewModel.Document.RootNode.Id))
            {
                var bounds = GetNodeBounds(node);
                if (_rubberbandRect.Intersects(bounds))
                {
                    selectedIds.Add(node.Id);
                }
            }

            ViewModel.SelectNodes(selectedIds);
            _overlay.InvalidateVisual();
            return;
        }

        if (!_isDragging || ViewModel.SelectedNode is null)
        {
            return;
        }

        var deltaX = currentPos.X - _dragStartPoint.X;
        var deltaY = currentPos.Y - _dragStartPoint.Y;

        // 拖曳事務（Drag Transaction）：位移超過閾值時推入一次起始狀態至歷史記錄
        if (!_isDragTransacted && (Math.Abs(deltaX) > 1.0 || Math.Abs(deltaY) > 1.0))
        {
            ViewModel.PushHistory();
            _isDragTransacted = true;
        }

        var selectedId = ViewModel.SelectedNode.Id;

        if (_activeHandle == ResizeHandleType.Move)
        {
            var parent = AstTreeOperations.FindParentNode(ViewModel.Document.RootNode, selectedId);
            if (parent is not null && parent.Type is ControlType.StackPanel or ControlType.DockPanel or ControlType.WrapPanel)
            {
                // 容器內拖曳重新排序 (Container Drag-Reordering)
                _isReorderingInContainer = true;
                _reorderParentId = parent.Id;
                _reorderChildId = selectedId;

                var isVertical = parent.Type != ControlType.StackPanel || (parent.Orientation ?? Core.Enums.Orientation.Vertical) == Core.Enums.Orientation.Vertical;
                var children = parent.Children;
                var targetIndex = children.Count;
                (Point, Point)? indicator = null;

                for (var i = 0; i < children.Count; i++)
                {
                    var childBounds = GetNodeBounds(children[i]);
                    if (isVertical)
                    {
                        if (currentPos.Y < childBounds.Center.Y)
                        {
                            targetIndex = i;
                            indicator = (new Point(childBounds.Left, childBounds.Top), new Point(childBounds.Right, childBounds.Top));
                            break;
                        }
                    }
                    else
                    {
                        if (currentPos.X < childBounds.Center.X)
                        {
                            targetIndex = i;
                            indicator = (new Point(childBounds.Left, childBounds.Top), new Point(childBounds.Left, childBounds.Bottom));
                            break;
                        }
                    }
                }

                if (indicator is null && children.Count > 0)
                {
                    var lastBounds = GetNodeBounds(children[^1]);
                    if (isVertical)
                    {
                        indicator = (new Point(lastBounds.Left, lastBounds.Bottom), new Point(lastBounds.Right, lastBounds.Bottom));
                    }
                    else
                    {
                        indicator = (new Point(lastBounds.Right, lastBounds.Top), new Point(lastBounds.Right, lastBounds.Bottom));
                    }
                }

                _reorderTargetIndex = targetIndex;
                _insertionIndicator = indicator;
            }
            else
            {
                _isReorderingInContainer = false;
                _insertionIndicator = null;
                var newLeft = _initialNodeBounds.X + deltaX;
                var newTop = _initialNodeBounds.Y + deltaY;
                ViewModel.MoveNode(selectedId, newLeft, newTop);
            }
        }
        else
        {
            // 8 點縮放計算
            var newX = _initialNodeBounds.X;
            var newY = _initialNodeBounds.Y;
            var newW = _initialNodeBounds.Width;
            var newH = _initialNodeBounds.Height;

            switch (_activeHandle)
            {
                case ResizeHandleType.Right:
                    newW = Math.Max(20, _initialNodeBounds.Width + deltaX);
                    break;
                case ResizeHandleType.Bottom:
                    newH = Math.Max(15, _initialNodeBounds.Height + deltaY);
                    break;
                case ResizeHandleType.BottomRight:
                    newW = Math.Max(20, _initialNodeBounds.Width + deltaX);
                    newH = Math.Max(15, _initialNodeBounds.Height + deltaY);
                    break;
                case ResizeHandleType.Left:
                    newX = _initialNodeBounds.X + deltaX;
                    newW = Math.Max(20, _initialNodeBounds.Width - deltaX);
                    break;
                case ResizeHandleType.Top:
                    newY = _initialNodeBounds.Y + deltaY;
                    newH = Math.Max(15, _initialNodeBounds.Height - deltaY);
                    break;
                case ResizeHandleType.TopLeft:
                    newX = _initialNodeBounds.X + deltaX;
                    newY = _initialNodeBounds.Y + deltaY;
                    newW = Math.Max(20, _initialNodeBounds.Width - deltaX);
                    newH = Math.Max(15, _initialNodeBounds.Height - deltaY);
                    break;
                case ResizeHandleType.TopRight:
                    newY = _initialNodeBounds.Y + deltaY;
                    newW = Math.Max(20, _initialNodeBounds.Width + deltaX);
                    newH = Math.Max(15, _initialNodeBounds.Height - deltaY);
                    break;
                case ResizeHandleType.BottomLeft:
                    newX = _initialNodeBounds.X + deltaX;
                    newW = Math.Max(20, _initialNodeBounds.Width - deltaX);
                    newH = Math.Max(15, _initialNodeBounds.Height + deltaY);
                    break;
            }

            ViewModel.ResizeNode(selectedId, newW, newH, newX, newY);
        }

        _overlay.InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (ViewModel?.ActiveDraggingItem is not null)
        {
            var dropPos = e.GetPosition(_elementsCanvas);
            var item = ViewModel.ActiveDraggingItem;
            ViewModel.ActiveDraggingItem = null;
            Cursor = Cursor.Default;
            ViewModel.AddControlFromToolbox(item, dropPos.X, dropPos.Y);
            e.Handled = true;
            return;
        }

        if (_isReorderingInContainer && _reorderParentId is not null && _reorderChildId is not null && ViewModel is not null)
        {
            ViewModel.ReorderChild(_reorderParentId, _reorderChildId, _reorderTargetIndex);
            _isReorderingInContainer = false;
            _insertionIndicator = null;
            _reorderParentId = null;
            _reorderChildId = null;
        }

        _isDragging = false;
        _isDragTransacted = false;
        _isRubberbandActive = false;
        _activeHandle = ResizeHandleType.None;
        e.Pointer.Capture(null);

        if (ViewModel is not null)
        {
            ViewModel.ActiveGuideLines.Clear();
        }

        _overlay.InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && ViewModel is not null)
        {
            if (e.Delta.Y > 0)
            {
                ViewModel.ZoomLevel = Math.Min(3.0, ViewModel.ZoomLevel + 0.1);
            }
            else if (e.Delta.Y < 0)
            {
                ViewModel.ZoomLevel = Math.Max(0.2, ViewModel.ZoomLevel - 0.1);
            }
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (ViewModel is null) return;

        var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var isShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var step = isShift ? (ViewModel.GridSize > 0 ? ViewModel.GridSize : 8.0) : 1.0;

        if (isCtrl)
        {
            switch (e.Key)
            {
                case Key.Z:
                    if (isShift) ViewModel.Redo();
                    else ViewModel.Undo();
                    e.Handled = true;
                    break;
                case Key.Y:
                    ViewModel.Redo();
                    e.Handled = true;
                    break;
                case Key.C:
                    ViewModel.CopySelectedNodes();
                    e.Handled = true;
                    break;
                case Key.V:
                    ViewModel.PasteNodes();
                    e.Handled = true;
                    break;
            }
        }
        else
        {
            switch (e.Key)
            {
                case Key.Delete:
                case Key.Back:
                    ViewModel.DeleteSelectedNodes();
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                    // 若選取節點位於容器內部，禁用自由座標微調 (Nudge)，避免語意矛盾
                    var parentNode = (ViewModel?.Document is not null && ViewModel.SelectedNode is not null)
                        ? AstTreeOperations.FindParentNode(ViewModel.Document.RootNode, ViewModel.SelectedNode.Id)
                        : null;
                    var isManagedByParent = parentNode is not null && parentNode.Type is ControlType.StackPanel or ControlType.DockPanel or ControlType.WrapPanel or ControlType.Grid;
                    if (!isManagedByParent && ViewModel is not null)
                    {
                        var dx = e.Key == Key.Left ? -step : (e.Key == Key.Right ? step : 0);
                        var dy = e.Key == Key.Up ? -step : (e.Key == Key.Down ? step : 0);
                        ViewModel.NudgeSelectedNodes(dx, dy);
                        e.Handled = true;
                    }
                    break;
            }
        }

        _overlay.InvalidateVisual();
    }

    private AstNode? HitTestNode(Point pos)
    {
        if (ViewModel?.Document is null)
        {
            return null;
        }

        var hitControl = FindHitControl(_elementsCanvas, pos);
        if (hitControl?.Tag is string nodeId)
        {
            return AstTreeOperations.FindNodeById(ViewModel.Document.RootNode, nodeId);
        }

        return null;
    }

    private Control? FindHitControl(Visual parent, Point canvasPos)
    {
        Control? deepestHit = null;

        foreach (var child in parent.GetVisualChildren())
        {
            if (child is Control ctrl && ctrl.IsVisible)
            {
                var pt = ctrl.TranslatePoint(new Point(0, 0), this);
                if (pt.HasValue)
                {
                    var rect = new Rect(pt.Value.X, pt.Value.Y, ctrl.Bounds.Width, ctrl.Bounds.Height);
                    if (rect.Contains(canvasPos))
                    {
                        var deeper = FindHitControl(ctrl, canvasPos);
                        deepestHit = deeper ?? (ctrl.Tag is not null ? ctrl : deepestHit);
                    }
                }
            }
        }

        return deepestHit;
    }

    internal Rect GetNodeBounds(AstNode node)
    {
        var control = FindControlByNodeId(_elementsCanvas, node.Id);
        if (control is not null && control.IsVisible)
        {
            var relativePoint = control.TranslatePoint(new Point(0, 0), this);
            if (relativePoint.HasValue && control.Bounds.Width > 0 && control.Bounds.Height > 0)
            {
                return new Rect(relativePoint.Value.X, relativePoint.Value.Y, control.Bounds.Width, control.Bounds.Height);
            }
        }

        var x = node.CanvasLeft ?? 0;
        var y = node.CanvasTop ?? 0;
        var w = node.Width ?? 120;
        var h = node.Height ?? 35;
        return new Rect(x, y, w, h);
    }

    private static Control? FindControlByNodeId(Visual parent, string nodeId)
    {
        if (parent is Control ctrl && Equals(ctrl.Tag, nodeId))
        {
            return ctrl;
        }

        foreach (var child in parent.GetVisualChildren())
        {
            if (child is Visual visualChild)
            {
                var found = FindControlByNodeId(visualChild, nodeId);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static ResizeHandleType HitTestResizeHandle(Rect bounds, Point pos)
    {
        const double handleRadius = 6.0;

        if (new Rect(bounds.X - handleRadius, bounds.Y - handleRadius, handleRadius * 2, handleRadius * 2).Contains(pos))
            return ResizeHandleType.TopLeft;
        if (new Rect(bounds.X + (bounds.Width / 2) - handleRadius, bounds.Y - handleRadius, handleRadius * 2, handleRadius * 2).Contains(pos))
            return ResizeHandleType.Top;
        if (new Rect(bounds.Right - handleRadius, bounds.Y - handleRadius, handleRadius * 2, handleRadius * 2).Contains(pos))
            return ResizeHandleType.TopRight;
        if (new Rect(bounds.Right - handleRadius, bounds.Y + (bounds.Height / 2) - handleRadius, handleRadius * 2, handleRadius * 2).Contains(pos))
            return ResizeHandleType.Right;
        if (new Rect(bounds.Right - handleRadius, bounds.Bottom - handleRadius, handleRadius * 2, handleRadius * 2).Contains(pos))
            return ResizeHandleType.BottomRight;
        if (new Rect(bounds.X + (bounds.Width / 2) - handleRadius, bounds.Bottom - handleRadius, handleRadius * 2, handleRadius * 2).Contains(pos))
            return ResizeHandleType.Bottom;
        if (new Rect(bounds.X - handleRadius, bounds.Bottom - handleRadius, handleRadius * 2, handleRadius * 2).Contains(pos))
            return ResizeHandleType.BottomLeft;
        if (new Rect(bounds.X - handleRadius, bounds.Y + (bounds.Height / 2) - handleRadius, handleRadius * 2, handleRadius * 2).Contains(pos))
            return ResizeHandleType.Left;

        return ResizeHandleType.None;
    }

    /// <summary>
    /// 裝飾器、網格、橡皮筋框與多節點選取繪製覆蓋層。
    /// </summary>
    private sealed class AdornerOverlay(DesignCanvas parent) : Control
    {
        private readonly DesignCanvas _parent = parent;

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var vm = _parent.ViewModel;

            // 1. 繪製網格背景 (Grid Pattern)
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)), 1.0);
            var gridSize = vm?.GridSize ?? 8.0;

            for (var x = 0.0; x < Bounds.Width; x += gridSize * 2)
            {
                context.DrawLine(gridPen, new Point(x, 0), new Point(x, Bounds.Height));
            }

            for (var y = 0.0; y < Bounds.Height; y += gridSize * 2)
            {
                context.DrawLine(gridPen, new Point(0, y), new Point(Bounds.Width, y));
            }

            // 2. 繪製吸附輔助對齊線
            if (vm?.ActiveGuideLines.Count > 0)
            {
                var guidePen = new Pen(new SolidColorBrush(Color.FromRgb(56, 189, 248)), 1.5, new DashStyle([4, 4], 0));
                foreach (var guide in vm.ActiveGuideLines)
                {
                    if (guide.Orientation == GuideLineOrientation.Vertical)
                    {
                        context.DrawLine(guidePen, new Point(guide.Position, 0), new Point(guide.Position, Bounds.Height));
                    }
                    else
                    {
                        context.DrawLine(guidePen, new Point(0, guide.Position), new Point(Bounds.Width, guide.Position));
                    }
                }
            }

            // 3. 繪製橡皮筋框選外框 (Rubberband Box)
            if (_parent._isRubberbandActive && _parent._rubberbandRect.Width > 0 && _parent._rubberbandRect.Height > 0)
            {
                var rbPen = new Pen(new SolidColorBrush(Color.FromRgb(56, 189, 248)), 1.0, new DashStyle([3, 3], 0));
                var rbFill = new SolidColorBrush(Color.FromArgb(30, 56, 189, 248));
                context.DrawRectangle(rbFill, rbPen, _parent._rubberbandRect);
            }

            // 4. 繪製多選裝飾器 (Multi-selection Bounding Boxes)
            if (vm?.SelectedNodeIds.Count > 0)
            {
                var multiPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 56, 189, 248)), 1.0, new DashStyle([3, 2], 0));
                foreach (var id in vm.SelectedNodeIds)
                {
                    var node = AstTreeOperations.FindNodeById(vm.Document.RootNode, id);
                    if (node is not null && node.Id != vm.Document.RootNode.Id && node != vm.SelectedNode)
                    {
                        context.DrawRectangle(null, multiPen, _parent.GetNodeBounds(node));
                    }
                }
            }

            // 5. 繪製主選取裝飾器 (Primary Selection Adorner)
            if (vm?.SelectedNode is not null)
            {
                var bounds = _parent.GetNodeBounds(vm.SelectedNode);
                var parentNode = AstTreeOperations.FindParentNode(vm.Document.RootNode, vm.SelectedNode.Id);
                var isManagedByParent = parentNode is not null && parentNode.Type is ControlType.StackPanel or ControlType.DockPanel or ControlType.WrapPanel or ControlType.Grid;

                if (isManagedByParent)
                {
                    // 容器內部節點：繪製專屬虛線選取框（不繪製 8 點自由座標手柄，避免認知混淆）
                    var containerPen = new Pen(new SolidColorBrush(Color.FromRgb(56, 189, 248)), 1.5, new DashStyle([4, 3], 0));
                    context.DrawRectangle(null, containerPen, bounds);
                }
                else
                {
                    var adornerPen = new Pen(new SolidColorBrush(Color.FromRgb(14, 165, 233)), 1.5);
                    var handleFill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    var handlePen = new Pen(new SolidColorBrush(Color.FromRgb(14, 165, 233)), 1.5);

                    // 選取框外框
                    context.DrawRectangle(null, adornerPen, bounds);

                    // 8 個縮放手柄
                    const double handleSize = 6.0;
                    var handlePoints = new[]
                    {
                        new Point(bounds.X, bounds.Y),
                        new Point(bounds.X + (bounds.Width / 2), bounds.Y),
                        new Point(bounds.Right, bounds.Y),
                        new Point(bounds.Right, bounds.Y + (bounds.Height / 2)),
                        new Point(bounds.Right, bounds.Bottom),
                        new Point(bounds.X + (bounds.Width / 2), bounds.Bottom),
                        new Point(bounds.X, bounds.Bottom),
                        new Point(bounds.X, bounds.Y + (bounds.Height / 2))
                    };

                    foreach (var p in handlePoints)
                    {
                        context.DrawRectangle(handleFill, handlePen, new Rect(p.X - (handleSize / 2), p.Y - (handleSize / 2), handleSize, handleSize));
                    }
                }
            }

            // 6. 繪製容器拖曳重新排序藍色插入指示線 (Container Drag-Reordering Blue Insertion Indicator)
            if (_parent._isReorderingInContainer && _parent._insertionIndicator.HasValue)
            {
                var (p1, p2) = _parent._insertionIndicator.Value;
                var insertPen = new Pen(new SolidColorBrush(Color.FromRgb(56, 189, 248)), 3.0);
                var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 56, 189, 248)), 6.0);
                var dotBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));

                context.DrawLine(glowPen, p1, p2);
                context.DrawLine(insertPen, p1, p2);
                context.DrawEllipse(dotBrush, null, p1, 4.0, 4.0);
                context.DrawEllipse(dotBrush, null, p2, 4.0, 4.0);
            }
        }
    }
}
