// filepath: tests/AFG.Core.Tests/AstTreeOperationsTests.cs
namespace AFG.Core.Tests;

/// <summary>
/// 驗證 AstTreeOperations 所有純函數之邏輯正確性、不可變性與防禦機制。
/// </summary>
public sealed class AstTreeOperationsTests
{
    [Fact]
    public void FindNodeById_ShouldReturnCorrectNode_WhenNodeExists()
    {
        // Arrange
        var targetNode = new AstNode { Id = "button_target", Name = "TargetButton", Type = ControlType.Button };
        var root = new AstNode
        {
            Id = "root_grid",
            Type = ControlType.Grid,
            Children = [
                new AstNode { Id = "stack_1", Type = ControlType.StackPanel, Children = [targetNode] }
            ]
        };

        // Act
        var result = AstTreeOperations.FindNodeById(root, "button_target");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("button_target");
        result.Name.Should().Be("TargetButton");
    }

    [Fact]
    public void FindNodeById_ShouldReturnNull_WhenNodeDoesNotExist()
    {
        // Arrange
        var root = new AstNode { Id = "root_canvas", Type = ControlType.Canvas };

        // Act
        var result = AstTreeOperations.FindNodeById(root, "non_existing_id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindNodeById_ShouldThrowArgumentNullException_WhenArgumentsAreNull()
    {
        var root = new AstNode { Id = "root", Type = ControlType.Canvas };

        var act1 = () => AstTreeOperations.FindNodeById(null!, "id");
        var act2 = () => AstTreeOperations.FindNodeById(root, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FindParentNode_ShouldReturnDirectParent()
    {
        // Arrange
        var child = new AstNode { Id = "child_1", Type = ControlType.Button };
        var parent = new AstNode { Id = "parent_stack", Type = ControlType.StackPanel, Children = [child] };
        var root = new AstNode { Id = "root_grid", Type = ControlType.Grid, Children = [parent] };

        // Act
        var foundParent = AstTreeOperations.FindParentNode(root, "child_1");

        // Assert
        foundParent.Should().NotBeNull();
        foundParent!.Id.Should().Be("parent_stack");
    }

    [Fact]
    public void Flatten_ShouldReturnAllDescendantNodes()
    {
        // Arrange
        var n3 = new AstNode { Id = "3", Type = ControlType.TextBox };
        var n2 = new AstNode { Id = "2", Type = ControlType.StackPanel, Children = [n3] };
        var root = new AstNode { Id = "1", Type = ControlType.Grid, Children = [n2] };

        // Act
        var allNodes = AstTreeOperations.Flatten(root);

        // Assert
        allNodes.Should().HaveCount(3);
        allNodes.Select(n => n.Id).Should().ContainInOrder("1", "2", "3");
    }

    [Fact]
    public void GetAncestors_ShouldReturnOrderedPathFromRoot()
    {
        // Arrange
        var leaf = new AstNode { Id = "leaf", Type = ControlType.Button };
        var middle = new AstNode { Id = "middle", Type = ControlType.StackPanel, Children = [leaf] };
        var root = new AstNode { Id = "root", Type = ControlType.Grid, Children = [middle] };

        // Act
        var ancestors = AstTreeOperations.GetAncestors(root, "leaf");

        // Assert
        ancestors.Should().HaveCount(2);
        ancestors.Select(a => a.Id).Should().ContainInOrder("root", "middle");
    }

    [Fact]
    public void AddChild_ShouldImmutablyAddNewNodeToTargetContainer()
    {
        // Arrange
        var root = new AstNode { Id = "root", Type = ControlType.Canvas };
        var newButton = new AstNode { Id = "btn", Name = "NewBtn", Type = ControlType.Button };

        // Act
        var newRoot = AstTreeOperations.AddChild(root, "root", newButton);

        // Assert
        root.Children.Should().BeEmpty("原始 root 不應被修改");
        newRoot.Children.Should().HaveCount(1);
        newRoot.Children[0].Id.Should().Be("btn");
    }

    [Fact]
    public void RemoveChild_ShouldRemoveNodeAndPreserveOtherChildren()
    {
        // Arrange
        var btn1 = new AstNode { Id = "btn1", Type = ControlType.Button };
        var btn2 = new AstNode { Id = "btn2", Type = ControlType.Button };
        var root = new AstNode { Id = "root", Type = ControlType.StackPanel, Children = [btn1, btn2] };

        // Act
        var newRoot = AstTreeOperations.RemoveChild(root, "btn1");

        // Assert
        root.Children.Should().HaveCount(2);
        newRoot.Children.Should().HaveCount(1);
        newRoot.Children[0].Id.Should().Be("btn2");
    }

    [Fact]
    public void MoveChild_ShouldRelocateNodeToAnotherContainer()
    {
        // Arrange
        var btn = new AstNode { Id = "btn1", Type = ControlType.Button };
        var panelA = new AstNode { Id = "panelA", Type = ControlType.StackPanel, Children = [btn] };
        var panelB = new AstNode { Id = "panelB", Type = ControlType.StackPanel };
        var root = new AstNode { Id = "root", Type = ControlType.Grid, Children = [panelA, panelB] };

        // Act
        var newRoot = AstTreeOperations.MoveChild(root, "btn1", "panelB");

        // Assert
        var updatedPanelA = AstTreeOperations.FindNodeById(newRoot, "panelA");
        var updatedPanelB = AstTreeOperations.FindNodeById(newRoot, "panelB");

        updatedPanelA!.Children.Should().BeEmpty();
        updatedPanelB!.Children.Should().HaveCount(1);
        updatedPanelB.Children[0].Id.Should().Be("btn1");
    }

    [Fact]
    public void MoveChild_ShouldThrowInvalidOperationException_WhenAttemptingCyclicMove()
    {
        // Arrange
        var childContainer = new AstNode { Id = "child_container", Type = ControlType.StackPanel };
        var parentContainer = new AstNode { Id = "parent_container", Type = ControlType.StackPanel, Children = [childContainer] };
        var root = new AstNode { Id = "root", Type = ControlType.Grid, Children = [parentContainer] };

        // Act & Assert: 不能把父容器移入自己的子容器中
        var act = () => AstTreeOperations.MoveChild(root, "parent_container", "child_container");
        act.Should().Throw<InvalidOperationException>();
    }
}
