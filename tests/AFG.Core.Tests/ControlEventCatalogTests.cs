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

    [Theory]
    [InlineData("Click", "RoutedEventArgs")]
    [InlineData("Tapped", "TappedEventArgs")]
    [InlineData("PointerPressed", "PointerPressedEventArgs")]
    [InlineData("KeyDown", "KeyEventArgs")]
    [InlineData("TextChanged", "TextChangedEventArgs")]
    [InlineData("SelectionChanged", "SelectionChangedEventArgs")]
    [InlineData("Tick", "EventArgs")]
    public void GetDefaultEventArgsType_ShouldReturnCorrectAvaloniaEventArgs(string eventName, string expectedType)
    {
        var type = ControlEventCatalog.GetDefaultEventArgsType(eventName);
        type.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("RoutedEventArgs", "e")]
    [InlineData("PointerPressedEventArgs", "e")]
    [InlineData("EventArgs", "e")]
    [InlineData("object?", "sender")]
    [InlineData("Control", "sender")]
    [InlineData("int", "parameter")]
    [InlineData("string", "parameter")]
    public void GetDefaultParameterName_ShouldInferStandardParameterNames(string paramType, string expectedName)
    {
        var name = ControlEventCatalog.GetDefaultParameterName(paramType);
        name.Should().Be(expectedName);
    }

    [Fact]
    public void GetDefaultParameters_ForClick_ShouldReturnSenderAndRoutedEventArgs()
    {
        var parameters = ControlEventCatalog.GetDefaultParameters("Click");
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("sender");
        parameters[0].Type.Should().Be("object?");
        parameters[1].Name.Should().Be("e");
        parameters[1].Type.Should().Be("RoutedEventArgs");
    }

    [Fact]
    public void GetDefaultParameters_ForTick_ShouldReturnSenderAndEventArgs()
    {
        var parameters = ControlEventCatalog.GetDefaultParameters("Tick");
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("sender");
        parameters[0].Type.Should().Be("object?");
        parameters[1].Name.Should().Be("e");
        parameters[1].Type.Should().Be("EventArgs");
    }

    [Fact]
    public void GetSupportedParameterTypes_ForClick_ShouldOnlyIncludeRelevantEventArgsAndExcludeOthers()
    {
        var types = ControlEventCatalog.GetSupportedParameterTypes("Click");
        types.Should().Contain("RoutedEventArgs");
        types.Should().Contain("object?");
        types.Should().Contain("string");
        types.Should().NotContain("TextChangedEventArgs");
        types.Should().NotContain("SelectionChangedEventArgs");
        types.Should().NotContain("KeyEventArgs");
        types.Should().NotContain("ScrollChangedEventArgs");
        types.Should().NotContain("PointerPressedEventArgs");
    }

    [Fact]
    public void GetSupportedParameterTypes_ForTextChanged_ShouldOnlyIncludeTextChangedEventArgs()
    {
        var types = ControlEventCatalog.GetSupportedParameterTypes("TextChanged");
        types.Should().Contain("TextChangedEventArgs");
        types.Should().Contain("RoutedEventArgs");
        types.Should().NotContain("PointerPressedEventArgs");
        types.Should().NotContain("SelectionChangedEventArgs");
        types.Should().NotContain("KeyEventArgs");
    }

    [Theory]
    [InlineData(ControlType.OpenFileDialog, "FileOk")]
    [InlineData(ControlType.SaveFileDialog, "FileOk")]
    [InlineData(ControlType.MessageBox, "Confirmed")]
    public void GetSupportedEvents_ForDialogs_ShouldReturnCorrectEvents(ControlType controlType, string expectedEvent)
    {
        var events = ControlEventCatalog.GetSupportedEvents(controlType);
        events.Should().Contain(expectedEvent);
    }

    [Fact]
    public void GetDefaultParameters_ForFileOk_ShouldReturnSenderAndFilePath()
    {
        var parameters = ControlEventCatalog.GetDefaultParameters("FileOk");
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("sender");
        parameters[0].Type.Should().Be("object?");
        parameters[1].Name.Should().Be("filePath");
        parameters[1].Type.Should().Be("string?");
    }

    [Fact]
    public void GetDefaultParameters_ForConfirmed_ShouldReturnSenderAndResult()
    {
        var parameters = ControlEventCatalog.GetDefaultParameters("Confirmed");
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("sender");
        parameters[0].Type.Should().Be("object?");
        parameters[1].Name.Should().Be("result");
        parameters[1].Type.Should().Be("bool?");
    }

    [Fact]
    public void GetSupportedEvents_ForDebugConsole_ShouldReturnClearedAndPointerEvents()
    {
        var events = ControlEventCatalog.GetSupportedEvents(ControlType.DebugConsole);
        events.Should().Contain("Cleared");
        events.Should().Contain("Tapped");
        events.Should().Contain("PointerPressed");

        var defaultEvt = ControlEventCatalog.GetDefaultEvent(ControlType.DebugConsole);
        defaultEvt.Should().Be("Cleared");
    }
}
