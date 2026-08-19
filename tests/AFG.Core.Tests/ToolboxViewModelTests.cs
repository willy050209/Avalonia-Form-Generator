// filepath: tests/AFG.Core.Tests/ToolboxViewModelTests.cs
using System;
using System.Linq;
using AFG.Core.Enums;
using AFG.Shared.Models;
using AFG.Shared.ViewModels;
using FluentAssertions;
using Xunit;

namespace AFG.Core.Tests;

public sealed class ToolboxViewModelTests
{
    [Fact]
    public void ToolboxViewModel_InitialState_ShouldLoadAvailableItems()
    {
        var vm = new ToolboxViewModel();
        vm.FilteredItems.Should().NotBeEmpty();
        vm.FilteredItems.Should().Contain(i => i.DisplayName == "Button");
        vm.SelectedItem.Should().BeNull();
    }

    [Fact]
    public void FilteredItems_WhenSearchTextSet_ShouldFilterByDisplayName()
    {
        var vm = new ToolboxViewModel();
        vm.SearchText = "Box";

        vm.FilteredItems.Should().NotBeEmpty();
        vm.FilteredItems.Should().OnlyContain(i => i.DisplayName.Contains("Box", StringComparison.OrdinalIgnoreCase));
        vm.FilteredItems.Should().Contain(i => i.DisplayName == "TextBox");
        vm.FilteredItems.Should().Contain(i => i.DisplayName == "CheckBox");
        vm.FilteredItems.Should().Contain(i => i.DisplayName == "PictureBox");
    }

    [Fact]
    public void SelectedItem_WhenUpdated_ShouldNotifyChange()
    {
        var vm = new ToolboxViewModel();
        var item = vm.FilteredItems.First(i => i.DisplayName == "Button");

        var changed = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.SelectedItem))
            {
                changed = true;
            }
        };

        vm.SelectedItem = item;

        changed.Should().BeTrue();
        vm.SelectedItem.Should().Be(item);
    }

    [Fact]
    public void DragAndDoubleClickEvents_ShouldFireProperCallbacks()
    {
        var vm = new ToolboxViewModel();
        var item = vm.FilteredItems.First();

        ToolboxItem? draggedItem = null;
        var dragEnded = false;
        ToolboxItem? doubleClickedItem = null;

        vm.ItemDragStarted += i => draggedItem = i;
        vm.ItemDragEnded += () => dragEnded = true;
        vm.ItemDoubleClicked += i => doubleClickedItem = i;

        vm.StartDrag(item);
        draggedItem.Should().Be(item);

        vm.EndDrag();
        dragEnded.Should().BeTrue();

        vm.TriggerDoubleClick(item);
        doubleClickedItem.Should().Be(item);
    }
}
