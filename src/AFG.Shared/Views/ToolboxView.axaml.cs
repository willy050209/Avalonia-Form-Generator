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
    private Point _pointerDownPos;
    private bool _isPointerDown;
    private ToolboxItem? _dragItem;

    public ToolboxView()
    {
        InitializeComponent();

        var listBox = this.FindControl<ListBox>("ToolboxListBox");
        if (listBox is not null)
        {
            listBox.AddHandler(InputElement.PointerPressedEvent, OnListBoxPointerPressed, RoutingStrategies.Tunnel);
            listBox.AddHandler(InputElement.PointerMovedEvent, OnListBoxPointerMoved, RoutingStrategies.Tunnel);
            listBox.AddHandler(InputElement.PointerReleasedEvent, OnListBoxPointerReleased, RoutingStrategies.Tunnel);
            listBox.DoubleTapped += OnListBoxDoubleTapped;
        }
    }

    private void OnListBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _pointerDownPos = e.GetPosition(this);
            _isPointerDown = true;

            var visual = e.Source as Visual;
            var border = visual?.FindAncestorOfType<Border>();
            _dragItem = border?.DataContext as ToolboxItem ?? (DataContext as ToolboxViewModel)?.SelectedItem;

            if (_dragItem is not null && DataContext is ToolboxViewModel vm)
            {
                vm.StartDrag(_dragItem);
            }
        }
    }

    private void OnListBoxPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPointerDown || _dragItem is null)
        {
            return;
        }

        var currentPos = e.GetPosition(this);
        var delta = currentPos - _pointerDownPos;
        if (Math.Abs(delta.X) > 4 || Math.Abs(delta.Y) > 4)
        {
            if (DataContext is ToolboxViewModel vm)
            {
                vm.StartDrag(_dragItem);
            }
        }
    }

    private void OnListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPointerDown = false;
    }

    private void OnListBoxDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ToolboxViewModel vm && vm.SelectedItem is not null)
        {
            vm.TriggerDoubleClick(vm.SelectedItem);
        }
    }
}
