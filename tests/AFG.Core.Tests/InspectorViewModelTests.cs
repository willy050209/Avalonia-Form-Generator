// filepath: tests/AFG.Core.Tests/InspectorViewModelTests.cs
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Shared.ViewModels;
using FluentAssertions;

namespace AFG.Core.Tests;

public sealed class InspectorViewModelTests
{
    [Fact]
    public void LoadNode_WhenButtonLoaded_AddEventShouldProvideButtonEventsOnly()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "btn1", Name = "SubmitBtn", Type = ControlType.Button };
        vm.LoadNode(node);

        // Act
        vm.AddEventCommand.Execute(null);

        // Assert
        vm.Events.Should().HaveCount(1);
        var item = vm.Events[0];
        item.EventName.Should().Be("Click");
        item.Parameters.Should().HaveCount(2);
        item.Parameters[0].Name.Should().Be("sender");
        item.Parameters[0].Type.Should().Be("object?");
        item.Parameters[1].Name.Should().Be("e");
        item.Parameters[1].Type.Should().Be("RoutedEventArgs");
        item.AvailableEvents.Should().Contain("Click");
        item.AvailableEvents.Should().Contain("Tapped");
        item.AvailableEvents.Should().NotContain("ValueChanged");
        item.AvailableEvents.Should().NotContain("DoWork");
    }

    [Fact]
    public void LoadNode_WhenSliderLoaded_AddEventShouldProvideSliderEventsOnly()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "sld1", Name = "VolumeSlider", Type = ControlType.Slider };
        vm.LoadNode(node);

        // Act
        vm.AddEventCommand.Execute(null);

        // Assert
        vm.Events.Should().HaveCount(1);
        var item = vm.Events[0];
        item.EventName.Should().Be("ValueChanged");
        item.AvailableEvents.Should().Equal(["ValueChanged"]);
    }

    [Fact]
    public void LoadNode_WhenBluetoothClientLoaded_AddEventShouldProvideBluetoothCallbacksOnly()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "ble1", Name = "BleClient", Type = ControlType.BluetoothClient };
        vm.LoadNode(node);

        // Act
        vm.AddEventCommand.Execute(null);

        // Assert
        vm.Events.Should().HaveCount(1);
        var item = vm.Events[0];
        item.EventName.Should().Be("DataReceived");
        item.AvailableEvents.Should().BeEquivalentTo(["DeviceDiscovered", "Connected", "Disconnected", "DataReceived"]);
        item.AvailableEvents.Should().NotContain("Click");
    }

    [Fact]
    public void LoadNode_WhenBackgroundWorkerLoaded_AddEventShouldProvideWorkerCallbacksOnly()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "bgw1", Name = "Worker", Type = ControlType.BackgroundWorker };
        vm.LoadNode(node);

        // Act
        vm.AddEventCommand.Execute(null);

        // Assert
        vm.Events.Should().HaveCount(1);
        var item = vm.Events[0];
        item.EventName.Should().Be("DoWork");
        item.AvailableEvents.Should().BeEquivalentTo(["DoWork", "ProgressChanged", "RunWorkerCompleted"]);
    }

    [Fact]
    public void LoadNode_WhenDispatcherTimerLoaded_AddEventShouldHaveExactlyOnePairOfSenderAndE()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "tmr1", Name = "PollTimer", Type = ControlType.DispatcherTimer };
        vm.LoadNode(node);

        // Act
        vm.AddEventCommand.Execute(null);

        // Assert
        vm.Events.Should().HaveCount(1);
        var item = vm.Events[0];
        item.EventName.Should().Be("Tick");
        item.Parameters.Should().HaveCount(2);
        item.Parameters[0].Name.Should().Be("sender");
        item.Parameters[0].Type.Should().Be("object?");
        item.Parameters[1].Name.Should().Be("e");
        item.Parameters[1].Type.Should().Be("EventArgs");
    }

    [Fact]
    public void LoadNode_WhenOpenFileDialogLoaded_AddEventShouldProvideFileOkEvent()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "ofd1", Name = "OpenFileDialog1", Type = ControlType.OpenFileDialog };
        vm.LoadNode(node);

        // Act
        vm.AddEventCommand.Execute(null);

        // Assert
        vm.Events.Should().HaveCount(1);
        var item = vm.Events[0];
        item.EventName.Should().Be("FileOk");
        item.Parameters.Should().HaveCount(2);
        item.Parameters[0].Name.Should().Be("sender");
        item.Parameters[1].Name.Should().Be("filePath");
        item.Parameters[1].Type.Should().Be("string?");
    }

    [Fact]
    public void LoadNode_WhenMessageBoxLoaded_AddEventShouldProvideConfirmedEvent()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "msg1", Name = "MessageBox1", Type = ControlType.MessageBox };
        vm.LoadNode(node);

        // Act
        vm.AddEventCommand.Execute(null);

        // Assert
        vm.Events.Should().HaveCount(1);
        var item = vm.Events[0];
        item.EventName.Should().Be("Confirmed");
        item.Parameters.Should().HaveCount(2);
        item.Parameters[0].Name.Should().Be("sender");
        item.Parameters[1].Name.Should().Be("result");
        item.Parameters[1].Type.Should().Be("bool?");
    }

    [Fact]
    public void LoadNode_WhenDebugConsoleLoaded_AddEventShouldProvideClearedEvent()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "dbg1", Name = "DebugConsole1", Type = ControlType.DebugConsole };
        vm.LoadNode(node);

        // Act
        vm.AddEventCommand.Execute(null);

        // Assert
        vm.Events.Should().HaveCount(1);
        var item = vm.Events[0];
        item.EventName.Should().Be("Cleared");
        item.AvailableEvents.Should().Contain("Cleared");
    }

    [Fact]
    public void LoadDocument_ShouldPopulateFormPropertiesAndTriggerFormUpdatedOnPropertyChange()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var doc = new FormDocument
        {
            Title = "銷售管理系統",
            BackgroundColor = "#1E293B",
            CanvasWidth = 1024,
            CanvasHeight = 768,
            MinWidth = 800,
            MinHeight = 600,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowState = WindowState.Maximized,
            CanResize = false,
            Topmost = true
        };

        FormDocument? updatedDoc = null;
        vm.FormUpdated += d => updatedDoc = d;

        // Act
        vm.LoadDocument(doc);

        // Assert
        vm.IsFormSelected.Should().BeTrue();
        vm.FormTitle.Should().Be("銷售管理系統");
        vm.FormBackgroundColor.Should().Be("#1E293B");
        vm.FormWidth.Should().Be(1024);
        vm.FormHeight.Should().Be(768);
        vm.FormMinWidth.Should().Be(800);
        vm.FormMinHeight.Should().Be(600);
        vm.FormWindowState.Should().Be(WindowState.Maximized);
        vm.FormCanResize.Should().BeFalse();
        vm.FormTopmost.Should().BeTrue();

        // Mutate form property
        vm.FormTitle = "銷售管理系統 v2";

        updatedDoc.Should().NotBeNull();
        updatedDoc!.Title.Should().Be("銷售管理系統 v2");
        updatedDoc.BackgroundColor.Should().Be("#1E293B");
    }

    [Fact]
    public void SelectForm_ShouldToggleIsFormSelected()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var node = new AstNode { Id = "btn1", Name = "SubmitBtn", Type = ControlType.Button };
        vm.LoadNode(node);
        vm.IsFormSelected.Should().BeFalse();
        vm.HasSelectedNode.Should().BeTrue();

        // Act
        vm.SelectFormCommand.Execute(null);

        // Assert
        vm.IsFormSelected.Should().BeTrue();
        vm.HasSelectedNode.Should().BeFalse();
    }

    [Fact]
    public void AddFormEvent_WhenFormSelected_ShouldProvideFormEventsAndTriggerFormUpdated()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var doc = new FormDocument { Title = "TestForm" };
        FormDocument? updatedDoc = null;
        vm.FormUpdated += d => updatedDoc = d;
        vm.LoadDocument(doc);

        // Act
        vm.AddFormEventCommand.Execute(null);

        // Assert
        vm.FormEvents.Should().HaveCount(1);
        var item = vm.FormEvents[0];
        item.EventName.Should().Be("Loaded");
        item.CommandProperty.Should().Be("Form_LoadedCommand");
        item.AvailableEvents.Should().Contain("Loaded");
        item.AvailableEvents.Should().Contain("PointerPressed");
        item.AvailableEvents.Should().Contain("SizeChanged");
        item.AvailableEvents.Should().Contain("KeyDown");

        updatedDoc.Should().NotBeNull();
        updatedDoc!.Events.Should().HaveCount(1);
        updatedDoc.Events[0].EventName.Should().Be("Loaded");
    }

    [Fact]
    public void LoadDocument_WithFormEvents_ShouldPopulateFormEventsCollection()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var doc = new FormDocument
        {
            Title = "TestForm",
            Events = [
                new EventMappingDefinition { EventName = "Loaded", CommandProperty = "OnLoadedCommand" },
                new EventMappingDefinition { EventName = "SizeChanged", CommandProperty = "OnSizeChangedCommand" }
            ]
        };

        // Act
        vm.LoadDocument(doc);

        // Assert
        vm.FormEvents.Should().HaveCount(2);
        vm.FormEvents[0].EventName.Should().Be("Loaded");
        vm.FormEvents[0].CommandProperty.Should().Be("OnLoadedCommand");
        vm.FormEvents[1].EventName.Should().Be("SizeChanged");
        vm.FormEvents[1].CommandProperty.Should().Be("OnSizeChangedCommand");
    }

    [Fact]
    public void RemoveFormEvent_ShouldRemoveFromCollectionAndTriggerFormUpdated()
    {
        // Arrange
        var vm = new InspectorViewModel();
        var doc = new FormDocument
        {
            Title = "TestForm",
            Events = [
                new EventMappingDefinition { EventName = "Loaded", CommandProperty = "OnLoadedCommand" }
            ]
        };
        FormDocument? updatedDoc = null;
        vm.FormUpdated += d => updatedDoc = d;
        vm.LoadDocument(doc);

        // Act
        vm.RemoveFormEventCommand.Execute(vm.FormEvents[0]);

        // Assert
        vm.FormEvents.Should().BeEmpty();
        updatedDoc.Should().NotBeNull();
        updatedDoc!.Events.Should().BeEmpty();
    }
}
