// filepath: tests/AFG.Core.Tests/AstValidatorTests.cs
namespace AFG.Core.Tests;

/// <summary>
/// 驗證 AstValidator 語意、結構與命名規則檢查。
/// </summary>
public sealed class AstValidatorTests
{
    [Fact]
    public void ValidateDocument_ShouldPass_ForValidDefaultDocument()
    {
        // Arrange
        var doc = FormDocument.CreateDefault("LoginFormView");

        // Act
        var result = AstValidator.ValidateDocument(doc);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123InvalidName")]
    [InlineData("Invalid-Name")]
    [InlineData("Invalid Name")]
    public void ValidateDocument_ShouldFail_WhenViewClassNameIsInvalid(string invalidName)
    {
        // Arrange
        var doc = new FormDocument { ViewClassName = invalidName };

        // Act
        var result = AstValidator.ValidateDocument(doc);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AFG001" || e.ErrorCode == "AFG002");
    }

    [Fact]
    public void ValidateTree_ShouldDetectDuplicateNodeIds()
    {
        // Arrange
        var duplicateId = "btn_submit";
        var btn1 = new AstNode { Id = duplicateId, Name = "Btn1", Type = ControlType.Button };
        var btn2 = new AstNode { Id = duplicateId, Name = "Btn2", Type = ControlType.Button };
        var root = new AstNode { Id = "root", Type = ControlType.StackPanel, Children = [btn1, btn2] };

        // Act
        var result = AstValidator.ValidateTree(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AFG101");
    }

    [Fact]
    public void ValidateTree_ShouldDetectNonContainerWithChildren()
    {
        // Arrange
        var child = new AstNode { Id = "text", Type = ControlType.TextBlock };
        var buttonWithChildren = new AstNode
        {
            Id = "btn",
            Type = ControlType.Button,
            Children = [child]
        };

        // Act
        var result = AstValidator.ValidateTree(buttonWithChildren);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AFG103");
    }

    [Fact]
    public void ValidateTree_ShouldDetectInvalidBindingPropertyNames()
    {
        // Arrange
        var node = new AstNode
        {
            Id = "txt",
            Type = ControlType.TextBox,
            Bindings = [
                new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "123_InvalidProp" }
            ]
        };

        // Act
        var result = AstValidator.ValidateTree(node);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AFG203");
    }

    [Fact]
    public void ValidateTree_ShouldDetectNegativeDimensions()
    {
        // Arrange
        var node = new AstNode
        {
            Id = "box",
            Type = ControlType.TextBox,
            Width = -100,
            Height = -50
        };

        // Act
        var result = AstValidator.ValidateTree(node);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AFG104");
        result.Errors.Should().Contain(e => e.ErrorCode == "AFG105");
    }

    [Fact]
    public void ValidateDocument_WithFormEvents_ShouldValidateProperly()
    {
        // Arrange
        var invalidDoc = new FormDocument
        {
            ViewClassName = "MainView",
            ViewModelClassName = "MainViewModel",
            Events = [
                new EventMappingDefinition { EventName = "", CommandProperty = "LoadedCommand" },
                new EventMappingDefinition { EventName = "Loaded", CommandProperty = "123InvalidCmd" }
            ]
        };

        // Act
        var result = AstValidator.ValidateDocument(invalidDoc);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "AFG301");
        result.Errors.Should().Contain(e => e.ErrorCode == "AFG303");
    }
}
