// filepath: tests/AFG.Core.Tests/SnappingEngineTests.cs
using AFG.Shared.Controls;

namespace AFG.Core.Tests;

/// <summary>
/// 驗證 SnappingEngine 幾何吸附、網格定位與輔助線計算純函數。
/// </summary>
public sealed class SnappingEngineTests
{
    [Theory]
    [InlineData(0, 8, 0)]
    [InlineData(7, 8, 8)]
    [InlineData(11, 8, 8)]
    [InlineData(13, 8, 16)]
    [InlineData(25, 10, 30)]
    public void SnapToGrid_ShouldRoundToNearestGridMultiple(double input, double gridSize, double expected)
    {
        var result = SnappingEngine.SnapToGrid(input, gridSize);
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateSnap_ShouldSnapToSiblingLeft_WhenWithinThreshold()
    {
        // Arrange
        var sibling = new AstNode
        {
            Id = "sib",
            CanvasLeft = 100,
            CanvasTop = 50,
            Width = 100,
            Height = 30
        };

        // Act (rawLeft = 102, 距 sibling.CanvasLeft 100 在 threshold 6 內)
        var result = SnappingEngine.CalculateSnap(
            rawLeft: 102,
            rawTop: 50,
            width: 80,
            height: 30,
            targetNodes: [sibling],
            snapThreshold: 6.0,
            gridSize: 8.0,
            snapToGrid: false);

        // Assert
        result.Left.Should().Be(100);
        result.GuideLines.Should().Contain(g => g.Orientation == GuideLineOrientation.Vertical && g.Position == 100);
    }

    [Fact]
    public void CalculateSnap_ShouldSnapToSiblingTop_WhenWithinThreshold()
    {
        // Arrange
        var sibling = new AstNode
        {
            Id = "sib",
            CanvasLeft = 100,
            CanvasTop = 200,
            Width = 100,
            Height = 30
        };

        // Act (rawTop = 198, 距 sibling.CanvasTop 200 在 threshold 6 內)
        var result = SnappingEngine.CalculateSnap(
            rawLeft: 50,
            rawTop: 198,
            width: 80,
            height: 30,
            targetNodes: [sibling],
            snapThreshold: 6.0,
            gridSize: 8.0,
            snapToGrid: false);

        // Assert
        result.Top.Should().Be(200);
        result.GuideLines.Should().Contain(g => g.Orientation == GuideLineOrientation.Horizontal && g.Position == 200);
    }
}
