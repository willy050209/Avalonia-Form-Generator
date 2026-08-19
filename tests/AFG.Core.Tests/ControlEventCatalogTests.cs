// filepath: tests/AFG.Core.Tests/ControlEventCatalogTests.cs
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using FluentAssertions;

namespace AFG.Core.Tests;

public sealed class ControlEventCatalogTests
{
    [Fact]
    public void GetSupportedEvents_ForButton_ShouldContainClickAndNotContainValueChanged()
    {
        // Act
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.Button);

        // Assert
        events.Should().Contain("Click");
        events.Should().Contain("Tapped");
        events.Should().NotContain("ValueChanged");
        events.Should().NotContain("DoWork");
        events.Should().NotContain("DataReceived");
    }

    [Fact]
    public void GetSupportedEvents_ForSlider_ShouldContainValueChangedAndNotClick()
    {
        // Act
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.Slider);

        // Assert
        events.Should().Contain("ValueChanged");
        events.Should().NotContain("Click");
        events.Should().NotContain("TextChanged");
    }

    [Fact]
    public void GetSupportedEvents_ForTextBox_ShouldContainTextChanged()
    {
        // Act
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.TextBox);

        // Assert
        events.Should().Contain("TextChanged");
        events.Should().Contain("KeyDown");
        events.Should().NotContain("Click");
        events.Should().NotContain("ValueChanged");
    }

    [Fact]
    public void GetSupportedEvents_ForDispatcherTimer_ShouldOnlyContainTick()
    {
        // Act
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.DispatcherTimer);

        // Assert
        events.Should().Equal(["Tick"]);
    }

    [Fact]
    public void GetSupportedEvents_ForBackgroundWorker_ShouldContainWorkerCallbacks()
    {
        // Act
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.BackgroundWorker);

        // Assert
        events.Should().BeEquivalentTo(["DoWork", "ProgressChanged", "RunWorkerCompleted"]);
    }

    [Fact]
    public void GetSupportedEvents_ForBluetoothClient_ShouldContainBluetoothCallbacks()
    {
        // Act
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.BluetoothClient);

        // Assert
        events.Should().BeEquivalentTo(["DeviceDiscovered", "Connected", "Disconnected", "DataReceived"]);
    }

    [Fact]
    public void GetSupportedEvents_ForSerialPortService_ShouldContainSerialPortCallbacks()
    {
        // Act
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.SerialPortService);

        // Assert
        events.Should().BeEquivalentTo(["DataReceived", "ErrorReceived", "PinChanged"]);
    }

    [Theory]
    [InlineData(ControlType.Button, "Click")]
    [InlineData(ControlType.TextBox, "TextChanged")]
    [InlineData(ControlType.CheckBox, "IsCheckedChanged")]
    [InlineData(ControlType.ComboBox, "SelectionChanged")]
    [InlineData(ControlType.Slider, "ValueChanged")]
    [InlineData(ControlType.DispatcherTimer, "Tick")]
    [InlineData(ControlType.BackgroundWorker, "DoWork")]
    [InlineData(ControlType.BluetoothClient, "DataReceived")]
    [InlineData(ControlType.SerialPortService, "DataReceived")]
    public void GetDefaultEvent_ShouldReturnExpectedDefault(ControlType type, string expectedEvent)
    {
        // Act
        var defaultEvt = ControlEventCatalog.GetDefaultEvent(type);

        // Assert
        defaultEvt.Should().Be(expectedEvent);
    }

    [Fact]
    public void GetSupportedEvents_ForPictureBox_ShouldContainWinFormsPictureBoxEvents()
    {
        // Act
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.PictureBox);

        // Assert
        events.Should().Contain("Click");
        events.Should().Contain("DoubleClick");
        events.Should().Contain("DoubleTapped");
        events.Should().Contain("LoadCompleted");
        events.Should().Contain("SizeModeChanged");
        events.Should().NotContain("TextChanged");
        events.Should().NotContain("DoWork");
    }

    [Theory]
    [InlineData(ControlType.Button, "Click", true)]
    [InlineData(ControlType.Button, "ValueChanged", false)]
    [InlineData(ControlType.PictureBox, "Click", true)]
    [InlineData(ControlType.PictureBox, "LoadCompleted", true)]
    [InlineData(ControlType.PictureBox, "DoWork", false)]
    [InlineData(ControlType.BluetoothClient, "DataReceived", true)]
    [InlineData(ControlType.BluetoothClient, "Click", false)]
    [InlineData(ControlType.BackgroundWorker, "DoWork", true)]
    [InlineData(ControlType.BackgroundWorker, "Tick", false)]
    public void IsSupported_ShouldValidateEventCorrectly(ControlType type, string eventName, bool expected)
    {
        // Act
        var result = ControlEventCatalog.IsSupported(type, eventName);

        // Assert
        result.Should().Be(expected);
    }
}
