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
}
