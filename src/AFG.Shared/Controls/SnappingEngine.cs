// filepath: src/AFG.Shared/Controls/SnappingEngine.cs
namespace AFG.Shared.Controls;

/// <summary>
/// 輔助對齊線方向。
/// </summary>
public enum GuideLineOrientation
{
    Horizontal,
    Vertical
}

/// <summary>
/// 輔助對齊線模型。
/// </summary>
public readonly record struct GuideLine(GuideLineOrientation Orientation, double Position, double Start, double End);

/// <summary>
/// 吸附計算結果。
/// </summary>
public readonly record struct SnapResult(
    double Left,
    double Top,
    ImmutableList<GuideLine> GuideLines);

/// <summary>
/// 提供設計畫布網格吸附與控制項邊界/中心對齊計算的純函數引擎。
/// </summary>
public static class SnappingEngine
{
    /// <summary>
    /// 計算網格座標吸附。
    /// </summary>
    public static double SnapToGrid(double value, double gridSize = 8.0)
    {
        if (gridSize <= 0)
        {
            return value;
        }

        return Math.Round(value / gridSize, MidpointRounding.AwayFromZero) * gridSize;
    }

    /// <summary>
    /// 計算與其他控制項邊界、中心線或畫布網格的吸附位置與輔助線。
    /// </summary>
    public static SnapResult CalculateSnap(
        double rawLeft,
        double rawTop,
        double width,
        double height,
        IReadOnlyList<AstNode> targetNodes,
        double snapThreshold = 6.0,
        double gridSize = 8.0,
        bool snapToGrid = true)
    {
        var finalLeft = rawLeft;
        var finalTop = rawTop;
        var guideLines = new List<GuideLine>();

        // 1. 網格吸附優先（若無控制項邊界吸附時的基礎）
        if (snapToGrid)
        {
            if (Math.Abs(rawLeft - SnapToGrid(rawLeft, gridSize)) < snapThreshold)
            {
                finalLeft = SnapToGrid(rawLeft, gridSize);
            }

            if (Math.Abs(rawTop - SnapToGrid(rawTop, gridSize)) < snapThreshold)
            {
                finalTop = SnapToGrid(rawTop, gridSize);
            }
        }

        // 2. 與其他節點對齊吸附（左、中、右、頂、中、底）
        var rawRight = rawLeft + width;
        var rawCenterX = rawLeft + (width / 2.0);
        var rawBottom = rawTop + height;
        var rawCenterY = rawTop + (height / 2.0);

        foreach (var other in targetNodes)
        {
            var otherLeft = other.CanvasLeft ?? 0;
            var otherTop = other.CanvasTop ?? 0;
            var otherWidth = other.Width ?? 100;
            var otherHeight = other.Height ?? 30;
            var otherRight = otherLeft + otherWidth;
            var otherCenterX = otherLeft + (otherWidth / 2.0);
            var otherBottom = otherTop + otherHeight;
            var otherCenterY = otherTop + (otherHeight / 2.0);

            // 水平吸附 (X 軸)
            if (Math.Abs(rawLeft - otherLeft) < snapThreshold)
            {
                finalLeft = otherLeft;
                guideLines.Add(new GuideLine(GuideLineOrientation.Vertical, otherLeft, Math.Min(rawTop, otherTop), Math.Max(rawBottom, otherBottom)));
            }
            else if (Math.Abs(rawCenterX - otherCenterX) < snapThreshold)
            {
                finalLeft = otherCenterX - (width / 2.0);
                guideLines.Add(new GuideLine(GuideLineOrientation.Vertical, otherCenterX, Math.Min(rawTop, otherTop), Math.Max(rawBottom, otherBottom)));
            }
            else if (Math.Abs(rawRight - otherRight) < snapThreshold)
            {
                finalLeft = otherRight - width;
                guideLines.Add(new GuideLine(GuideLineOrientation.Vertical, otherRight, Math.Min(rawTop, otherTop), Math.Max(rawBottom, otherBottom)));
            }

            // 垂直吸附 (Y 軸)
            if (Math.Abs(rawTop - otherTop) < snapThreshold)
            {
                finalTop = otherTop;
                guideLines.Add(new GuideLine(GuideLineOrientation.Horizontal, otherTop, Math.Min(rawLeft, otherLeft), Math.Max(rawRight, otherRight)));
            }
            else if (Math.Abs(rawCenterY - otherCenterY) < snapThreshold)
            {
                finalTop = otherCenterY - (height / 2.0);
                guideLines.Add(new GuideLine(GuideLineOrientation.Horizontal, otherCenterY, Math.Min(rawLeft, otherLeft), Math.Max(rawRight, otherRight)));
            }
            else if (Math.Abs(rawBottom - otherBottom) < snapThreshold)
            {
                finalTop = otherBottom - height;
                guideLines.Add(new GuideLine(GuideLineOrientation.Horizontal, otherBottom, Math.Min(rawLeft, otherLeft), Math.Max(rawRight, otherRight)));
            }
        }

        return new SnapResult(finalLeft, finalTop, guideLines.ToImmutableList());
    }
}
