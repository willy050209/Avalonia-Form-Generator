// filepath: tests/AFG.Core.Tests/InspectorTests.cs
using AFG.Shared.ViewModels;

namespace AFG.Core.Tests;

/// <summary>
/// 驗證 InspectorViewModel 屬性連動與資料綁定配置邏輯。
/// </summary>
public sealed class InspectorTests
{
    [Fact]
    public void LoadNode_ShouldPopulatePropertiesCorrectly()
    {
        // Arrange
        var node = new AstNode
        {
            Id = "btn1",
            Name = "SubmitBtn",
            Type = ControlType.Button,
            Content = "Click Me",
            Width = 150,
            Height = 40,
            Bindings = [new BindingDefinition { TargetProperty = "IsEnabled", ViewModelProperty = "CanSubmit" }],
            Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitCommand" }]
        };

        var inspector = new InspectorViewModel();

        // Act
        inspector.LoadNode(node);

        // Assert
        inspector.HasSelectedNode.Should().BeTrue();
        inspector.NodeName.Should().Be("SubmitBtn");
        inspector.Content.Should().Be("Click Me");
        inspector.Width.Should().Be(150);
        inspector.Height.Should().Be(40);
        inspector.Bindings.Should().HaveCount(1);
        inspector.Events.Should().HaveCount(1);
    }

    [Fact]
    public void PropertyChange_ShouldTriggerNodeUpdatedEvent()
    {
        // Arrange
        var node = new AstNode
        {
            Id = "txt1",
            Name = "UsernameInput",
            Type = ControlType.TextBox
        };

        var inspector = new InspectorViewModel();
        inspector.LoadNode(node);

        AstNode? updatedNode = null;
        inspector.NodeUpdated += n => updatedNode = n;

        // Act
        inspector.Text = "Hello World";

        // Assert
        updatedNode.Should().NotBeNull();
        updatedNode!.Text.Should().Be("Hello World");
        updatedNode.Name.Should().Be("UsernameInput");
    }

    [Fact]
    public void LoadNode_WhenSameNodeUpdatedWithNewDimensions_ShouldSynchronizeCoordinatesAndDimensions()
    {
        // Arrange
        var initialNode = new AstNode
        {
            Id = "btn1",
            Name = "SubmitBtn",
            Type = ControlType.Button,
            Width = 100,
            Height = 35,
            CanvasLeft = 50,
            CanvasTop = 60
        };

        var inspector = new InspectorViewModel();
        inspector.LoadNode(initialNode);

        var resizedNode = initialNode with
        {
            Width = 220,
            Height = 80,
            CanvasLeft = 80,
            CanvasTop = 90
        };

        // Act
        inspector.LoadNode(resizedNode);

        // Assert
        inspector.Width.Should().Be(220);
        inspector.Height.Should().Be(80);
        inspector.CanvasLeft.Should().Be(80);
        inspector.CanvasTop.Should().Be(90);
    }

    [Fact]
    public void LoadNode_WhenDispatcherTimerSelected_ShouldEnableTimerCapabilitiesAndDisableImageCapabilities()
    {
        // Arrange
        var timerNode = new AstNode
        {
            Id = "tmr1",
            Name = "ClockTimer",
            Type = ControlType.DispatcherTimer,
            Interval = 500
        };

        var inspector = new InspectorViewModel();

        // Act
        inspector.LoadNode(timerNode);

        // Assert
        inspector.IsTimerSupported.Should().BeTrue();
        inspector.IsImageSupported.Should().BeFalse();
        inspector.IsTextSupported.Should().BeFalse();
        inspector.IsVisualControl.Should().BeFalse();
        inspector.Interval.Should().Be(500);

        // Update interval
        AstNode? updated = null;
        inspector.NodeUpdated += n => updated = n;
        inspector.Interval = 250;

        updated.Should().NotBeNull();
        updated!.Interval.Should().Be(250);
    }

    [Fact]
    public void LoadNode_WhenPictureBoxSelected_ShouldEnableImageCapabilitiesAndDisableTimerCapabilities()
    {
        // Arrange
        var picNode = new AstNode
        {
            Id = "pic1",
            Name = "LogoImage",
            Type = ControlType.PictureBox,
            Source = "assets/logo.png",
            Stretch = Stretch.UniformToFill
        };

        var inspector = new InspectorViewModel();

        // Act
        inspector.LoadNode(picNode);

        // Assert
        inspector.IsImageSupported.Should().BeTrue();
        inspector.IsTimerSupported.Should().BeFalse();
        inspector.IsTextSupported.Should().BeFalse();
        inspector.IsVisualControl.Should().BeTrue();
        inspector.Source.Should().Be("assets/logo.png");
        inspector.Stretch.Should().Be(Stretch.UniformToFill);
    }

    [Fact]
    public void LoadNode_WhenButtonSelected_ShouldEnableAutoSizeCapabilityAndHandleAutoSizeChanges()
    {
        // Arrange
        var btnNode = new AstNode
        {
            Id = "btn1",
            Name = "SubmitBtn",
            Type = ControlType.Button,
            AutoSize = false
        };

        var inspector = new InspectorViewModel();
        inspector.LoadNode(btnNode);

        // Assert
        inspector.IsAutoSizeSupported.Should().BeTrue();
        inspector.AutoSize.Should().BeFalse();

        // Act
        AstNode? updated = null;
        inspector.NodeUpdated += n => updated = n;
        inspector.AutoSize = true;

        // Assert
        updated.Should().NotBeNull();
        updated!.AutoSize.Should().BeTrue();
    }
}
