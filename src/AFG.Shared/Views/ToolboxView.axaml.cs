// filepath: src/AFG.Shared/Views/ToolboxView.axaml.cs
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
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
            // 使用 Bubble 策略監聽，確保不干擾 ListBox 原生的選擇處理流程
            listBox.AddHandler(InputElement.PointerPressedEvent, OnListBoxPointerPressed, RoutingStrategies.Bubble);
            listBox.AddHandler(InputElement.PointerMovedEvent, OnListBoxPointerMoved, RoutingStrategies.Bubble);
            listBox.AddHandler(InputElement.PointerReleasedEvent, OnListBoxPointerReleased, RoutingStrategies.Bubble);
            listBox.AddHandler(InputElement.PointerCaptureLostEvent, OnListBoxPointerCaptureLost, RoutingStrategies.Bubble);
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

            if (_dragCandidateItem is not null && DataContext is ToolboxViewModel vm)
            {
                // 立即切換選取項目，確保 ViewModel 與 UI 狀態即時同步
                if (vm.SelectedItem != _dragCandidateItem)
                {
                    vm.SelectedItem = _dragCandidateItem;
                }
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
    }

    private void OnListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPointerDown)
        {
            if (_isDragging)
            {
                // 若是在工具箱範圍內釋放，取消拖曳狀態以防 ActiveDraggingItem 滯留
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
        _isPointerDown = false;
        _dragCandidateItem = null;
        _isDragging = false;
    }

    private void OnListBoxDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ToolboxViewModel vm && vm.SelectedItem is not null)
        {
            vm.TriggerDoubleClick(vm.SelectedItem);
        }
    }
}
