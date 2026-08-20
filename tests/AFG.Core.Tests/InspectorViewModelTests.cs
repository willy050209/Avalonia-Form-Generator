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
        item.Parameters.Should().HaveCount(1);
        item.Parameters[0].Name.Should().Be("e");
        item.Parameters[0].Type.Should().Be("RoutedEventArgs");
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
}
