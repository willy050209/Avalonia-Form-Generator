using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
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
    private bool _isRubberbandActive;
    private Rect _rubberbandRect;

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

    private void OnDocumentChanged(FormDocument doc) => RebuildElements();
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
            case ControlType.DispatcherTimer or ControlType.BackgroundWorker or ControlType.BluetoothClient or ControlType.SerialPortService:
                var iconTag = node.Type switch
                {
                    ControlType.DispatcherTimer => "[Timer]",
                    ControlType.BackgroundWorker => "[Worker]",
                    ControlType.BluetoothClient => "[BLE]",
                    ControlType.SerialPortService => "[COM]",
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

        control.Width = node.Width ?? 120;
        control.Height = node.Height ?? 35;
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

        // 1. 檢查是否點擊在已選取主節點的 8 個縮放手柄上
        if (ViewModel.SelectedNode is not null)
        {
            var selectedBounds = GetNodeBounds(ViewModel.SelectedNode);
            var handle = HitTestResizeHandle(selectedBounds, pos);
            if (handle != ResizeHandleType.None)
            {
                _activeHandle = handle;
                _initialNodeBounds = selectedBounds;
                _isDragging = true;
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
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

            // 框選命中測試
            var selectedIds = new List<string>();
            foreach (var node in ViewModel.Document.RootNode.Children)
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

        var selectedId = ViewModel.SelectedNode.Id;

        if (_activeHandle == ResizeHandleType.Move)
        {
            var newLeft = _initialNodeBounds.X + deltaX;
            var newTop = _initialNodeBounds.Y + deltaY;
            ViewModel.MoveNode(selectedId, newLeft, newTop);
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

        if (_isDragging && ViewModel is not null)
        {
            ViewModel.PushHistory();
        }

        _isDragging = false;
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
                    ViewModel.NudgeSelectedNodes(-step, 0);
                    e.Handled = true;
                    break;
                case Key.Right:
                    ViewModel.NudgeSelectedNodes(step, 0);
                    e.Handled = true;
                    break;
                case Key.Up:
                    ViewModel.NudgeSelectedNodes(0, -step);
                    e.Handled = true;
                    break;
                case Key.Down:
                    ViewModel.NudgeSelectedNodes(0, step);
                    e.Handled = true;
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

        // 從上層 (後加入者) 優先測試直屬與遞迴子節點
        for (var i = ViewModel.Document.RootNode.Children.Count - 1; i >= 0; i--)
        {
            var node = ViewModel.Document.RootNode.Children[i];
            var bounds = GetNodeBounds(node);
            if (bounds.Contains(pos))
            {
                return node;
            }
        }

        return null;
    }

    internal static Rect GetNodeBounds(AstNode node)
    {
        var x = node.CanvasLeft ?? 0;
        var y = node.CanvasTop ?? 0;
        var w = node.Width ?? 120;
        var h = node.Height ?? 35;
        return new Rect(x, y, w, h);
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
                        context.DrawRectangle(null, multiPen, GetNodeBounds(node));
                    }
                }
            }

            // 5. 繪製主選取裝飾器 (Primary Selection 8-Handle Adorner)
            if (vm?.SelectedNode is not null)
            {
                var bounds = GetNodeBounds(vm.SelectedNode);
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
    }
}
