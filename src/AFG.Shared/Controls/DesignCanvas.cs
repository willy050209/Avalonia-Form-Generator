// filepath: src/AFG.Shared/Controls/DesignCanvas.cs
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
/// 視覺化設計畫布，支援控制項動態渲染、選取框 (Adorner)、8 點縮放調整與對齊輔助線渲染。
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

    public DesignCanvas()
    {
        ClipToBounds = true;
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

    private static Control CreateControlFromNode(AstNode node)
    {
        Control control = node.Type switch
        {
            ControlType.Button => new Button { Content = node.Content ?? node.Text ?? "Button" },
            ControlType.TextBox => new TextBox { Text = node.Text ?? string.Empty, PlaceholderText = node.Watermark },
            ControlType.TextBlock => new TextBlock { Text = node.Text ?? "TextBlock", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
            ControlType.CheckBox => new CheckBox { Content = node.Content ?? "CheckBox", IsChecked = node.IsChecked },
            ControlType.RadioButton => new RadioButton { Content = node.Content ?? "RadioButton" },
            ControlType.ComboBox => new ComboBox { PlaceholderText = "Select item...", Width = node.Width ?? 120 },
            ControlType.DatePicker => new DatePicker(),
            ControlType.Slider => new Slider { Minimum = node.Minimum ?? 0, Maximum = node.Maximum ?? 100, Value = node.Value ?? 50 },
            ControlType.ProgressBar => new ProgressBar { Minimum = 0, Maximum = 100, Value = 60 },
            ControlType.Border => new Border { Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) },
            ControlType.StackPanel => new StackPanel { Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)) },
            ControlType.Grid => new Grid { Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)) },
            _ => new Button { Content = node.Name }
        };

        control.Width = node.Width ?? 120;
        control.Height = node.Height ?? 35;
        control.Tag = node.Id;
        control.IsHitTestVisible = false; // 讓畫布統一捕捉滑鼠選取與拖曳

        var left = node.CanvasLeft ?? 0;
        var top = node.CanvasTop ?? 0;
        Canvas.SetLeft(control, left);
        Canvas.SetTop(control, top);

        return control;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (ViewModel is null)
        {
            return;
        }

        var pos = e.GetPosition(this);
        _dragStartPoint = pos;

        // 1. 檢查是否點擊在已選取節點的 8 個縮放手柄上
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
            ViewModel.SelectNode(hitNode.Id);
            _activeHandle = ResizeHandleType.Move;
            _initialNodeBounds = GetNodeBounds(hitNode);
            _isDragging = true;
            e.Pointer.Capture(this);
        }
        else
        {
            ViewModel.SelectNode(null);
            _activeHandle = ResizeHandleType.None;
            _isDragging = false;
        }

        _overlay.InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDragging || ViewModel?.SelectedNode is null)
        {
            return;
        }

        var currentPos = e.GetPosition(this);
        var deltaX = currentPos.X - _dragStartPoint.X;
        var deltaY = currentPos.Y - _dragStartPoint.Y;

        var selectedId = ViewModel.SelectedNode.Id;

        if (_activeHandle == ResizeHandleType.Move)
        {
            var newLeft = _initialNodeBounds.X + deltaX;
            var newTop = _initialNodeBounds.Y + deltaY;
            ViewModel.MoveNode(selectedId, Math.Max(0, newLeft), Math.Max(0, newTop));
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
        _isDragging = false;
        _activeHandle = ResizeHandleType.None;
        e.Pointer.Capture(null);

        if (ViewModel is not null)
        {
            ViewModel.ActiveGuideLines.Clear();
        }

        _overlay.InvalidateVisual();
    }

    private AstNode? HitTestNode(Point pos)
    {
        if (ViewModel?.Document is null)
        {
            return null;
        }

        // 從上層 (後加入者) 優先測試
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
    /// 裝飾器與網格繪製覆蓋層。
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

            // 3. 繪製選取裝飾器 (Adorner Overlay)
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
