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
}
