// filepath: src/AFG.Shared/Views/ToolboxView.axaml.cs
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AFG.Shared.Controls;
using AFG.Shared.Models;
using AFG.Shared.ViewModels;

namespace AFG.Shared.Views;

public partial class ToolboxView : UserControl
{
    private const double DragThreshold = 6.0;
    private Point _pointerDownPos;
    private bool _isPointerDown;
    private bool _isDragging;
    private ToolboxItem? _dragCandidateItem;

    public ToolboxView()
    {
        InitializeComponent();

        var listBox = this.FindControl<ListBox>("ToolboxListBox");
        if (listBox is not null)
        {
            // 使用 Tunnel 策略及時捕獲滑鼠交互
            listBox.AddHandler(InputElement.PointerPressedEvent, OnListBoxPointerPressed, RoutingStrategies.Tunnel);
            listBox.AddHandler(InputElement.PointerMovedEvent, OnListBoxPointerMoved, RoutingStrategies.Tunnel);
            listBox.AddHandler(InputElement.PointerReleasedEvent, OnListBoxPointerReleased, RoutingStrategies.Tunnel);
            listBox.AddHandler(InputElement.PointerCaptureLostEvent, OnListBoxPointerCaptureLost, RoutingStrategies.Tunnel);
            listBox.DoubleTapped += OnListBoxDoubleTapped;
        }
    }

    private void OnListBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _pointerDownPos = e.GetPosition(this);
            _isPointerDown = true;
            _isDragging = false;

            var visual = e.Source as Visual;
            var listBoxItem = visual?.FindAncestorOfType<ListBoxItem>();
            _dragCandidateItem = listBoxItem?.DataContext as ToolboxItem;

            if (_dragCandidateItem is not null)
            {
                if (DataContext is ToolboxViewModel vm && vm.SelectedItem != _dragCandidateItem)
                {
                    vm.SelectedItem = _dragCandidateItem;
                }

                // 捕獲滑鼠指標，確保拖曳跨出工具箱以及在畫布上釋放時，能完整接收 PointerMoved 與 PointerReleased
                e.Pointer.Capture(this);
            }
        }
    }

    private void OnListBoxPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPointerDown || _dragCandidateItem is null)
        {
            return;
        }

        var currentPos = e.GetPosition(this);
        var delta = currentPos - _pointerDownPos;

        // 僅當移動距離超過門檻值時才判定為開始拖曳
        if (!_isDragging && (Math.Abs(delta.X) > DragThreshold || Math.Abs(delta.Y) > DragThreshold))
        {
            _isDragging = true;
            if (DataContext is ToolboxViewModel vm)
            {
                vm.StartDrag(_dragCandidateItem);
            }
        }

        if (_isDragging)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var canvas = topLevel?.FindControl<DesignCanvas>("MainDesignCanvas")
                         ?? this.FindAncestorOfType<MainView>()?.FindControl<DesignCanvas>("MainDesignCanvas");

            if (canvas is not null)
            {
                var canvasPos = e.GetPosition(canvas);
                var canvasBounds = new Rect(0, 0, canvas.Bounds.Width, canvas.Bounds.Height);
                if (canvasBounds.Contains(canvasPos))
                {
                    Cursor = new Cursor(StandardCursorType.DragCopy);
                }
                else
                {
                    Cursor = new Cursor(StandardCursorType.Hand);
                }
            }
        }
    }

    private void OnListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPointerDown)
        {
            e.Pointer.Capture(null);
            Cursor = Cursor.Default;

            if (_isDragging && _dragCandidateItem is not null)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                var canvas = topLevel?.FindControl<DesignCanvas>("MainDesignCanvas")
                             ?? this.FindAncestorOfType<MainView>()?.FindControl<DesignCanvas>("MainDesignCanvas");

                if (canvas is not null && canvas.ViewModel is not null)
                {
                    var canvasPos = e.GetPosition(canvas);
                    var canvasBounds = new Rect(0, 0, canvas.Bounds.Width, canvas.Bounds.Height);

                    // 若釋放在畫布內部或周邊範圍內，精確加入控制項
                    if (canvasBounds.Contains(canvasPos) ||
                        (canvasPos.X >= 0 && canvasPos.Y >= 0 && canvasPos.X <= canvas.Bounds.Width + 60 && canvasPos.Y <= canvas.Bounds.Height + 60))
                    {
                        var dropX = Math.Clamp(canvasPos.X, 0, Math.Max(0, canvas.Bounds.Width - 100));
                        var dropY = Math.Clamp(canvasPos.Y, 0, Math.Max(0, canvas.Bounds.Height - 40));
                        canvas.ViewModel.AddControlFromToolbox(_dragCandidateItem, dropX, dropY);
                    }
                }

                if (DataContext is ToolboxViewModel vm)
                {
                    vm.EndDrag();
                }
                _isDragging = false;
            }
            else if (_dragCandidateItem is not null && DataContext is ToolboxViewModel vm)
            {
                vm.SelectedItem = _dragCandidateItem;
            }

            _isPointerDown = false;
            _dragCandidateItem = null;
        }
    }

    private void OnListBoxPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isDragging && DataContext is ToolboxViewModel vm)
        {
            vm.EndDrag();
        }
        _isPointerDown = false;
        _dragCandidateItem = null;
        _isDragging = false;
        Cursor = Cursor.Default;
    }

    private void OnListBoxDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ToolboxViewModel vm && vm.SelectedItem is not null)
        {
            vm.TriggerDoubleClick(vm.SelectedItem);
        }
    }
}
