// filepath: src/AFG.Core/Models/Common/LayoutModels.cs
namespace AFG.Core.Models.Common;

/// <summary>
/// 表示外距 (Margin) 或內距 (Padding) 的不可變模型。
/// </summary>
public readonly record struct ThicknessModel(double Left, double Top, double Right, double Bottom)
{
    /// <summary>
    /// 取得四邊皆為 0 的空邊距。
    /// </summary>
    public static ThicknessModel Zero => new(0, 0, 0, 0);

    /// <summary>
    /// 建立四邊相等的邊距。
    /// </summary>
    /// <param name="uniformLength">統一邊距值。</param>
    public static ThicknessModel Uniform(double uniformLength) => new(uniformLength, uniformLength, uniformLength, uniformLength);

    /// <summary>
    /// 建立水平與垂直對稱的邊距。
    /// </summary>
    /// <param name="horizontal">水平邊距。</param>
    /// <param name="vertical">垂直邊距。</param>
    public static ThicknessModel Symmetric(double horizontal, double vertical) => new(horizontal, vertical, horizontal, vertical);

    /// <summary>
    /// 格式化為 Avalonia XAML / C# 支援的字串表示法。
    /// </summary>
    public override string ToString() => $"{Left},{Top},{Right},{Bottom}";
}

/// <summary>
/// 表示圓角半徑 (CornerRadius) 的不可變模型。
/// </summary>
public readonly record struct CornerRadiusModel(double TopLeft, double TopRight, double BottomRight, double BottomLeft)
{
    /// <summary>
    /// 取得四角皆為 0 的圓角。
    /// </summary>
    public static CornerRadiusModel Zero => new(0, 0, 0, 0);

    /// <summary>
    /// 建立四角相等的圓角。
    /// </summary>
    /// <param name="uniformRadius">統一圓角半徑。</param>
    public static CornerRadiusModel Uniform(double uniformRadius) => new(uniformRadius, uniformRadius, uniformRadius, uniformRadius);

    public override string ToString() => $"{TopLeft},{TopRight},{BottomRight},{BottomLeft}";
}

/// <summary>
/// 表示 Grid 列/欄尺寸長度定義。
/// </summary>
public readonly record struct GridLengthModel(double Value, GridUnitType UnitType)
{
    /// <summary>
    /// 自動尺寸 (Auto)。
    /// </summary>
    public static GridLengthModel Auto => new(0, GridUnitType.Auto);

    /// <summary>
    /// 比例尺寸 (1*)。
    /// </summary>
    public static GridLengthModel Star(double factor = 1.0) => new(factor, GridUnitType.Star);

    /// <summary>
    /// 絕對像素尺寸。
    /// </summary>
    public static GridLengthModel Pixel(double pixels) => new(pixels, GridUnitType.Pixel);

    public override string ToString() => UnitType switch
    {
        GridUnitType.Auto => "Auto",
        GridUnitType.Star => Value == 1.0 ? "*" : $"{Value}*",
        GridUnitType.Pixel => $"{Value}",
        _ => "Auto"
    };
}
