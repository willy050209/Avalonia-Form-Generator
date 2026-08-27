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

/// <summary>
/// 表示控制項陰影效果 (BoxShadow / DropShadow) 的不可變模型。
/// </summary>
public readonly record struct BoxShadowModel(
    double OffsetX = 0,
    double OffsetY = 4,
    double Blur = 8,
    double Spread = 0,
    string Color = "#40000000",
    bool IsInset = false)
{
    /// <summary>
    /// 取得標準柔和陰影。
    /// </summary>
    public static BoxShadowModel Default => new(0, 4, 8, 0, "#40000000", false);

    /// <summary>
    /// 格式化為 Avalonia BoxShadows 支援的字串表示法（例如 "0 4 8 0 #40000000" 或 "inset 0 2 4 0 #20000000"）。
    /// </summary>
    public override string ToString()
    {
        var insetStr = IsInset ? "inset " : "";
        return $"{insetStr}{OffsetX.ToString(System.Globalization.CultureInfo.InvariantCulture)} {OffsetY.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Blur.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Spread.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Color}";
    }

    private static readonly char[] Separators = [' ', ','];

    /// <summary>
    /// 解析字串為 BoxShadowModel。
    /// </summary>
    public static BoxShadowModel? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var isInset = false;
        var parts = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var numList = new System.Collections.Generic.List<double>();
        string color = "#40000000";

        foreach (var part in parts)
        {
            if (part.Equals("inset", StringComparison.OrdinalIgnoreCase))
            {
                isInset = true;
            }
            else if (part.StartsWith('#') || part.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                color = part;
            }
            else if (double.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                numList.Add(num);
            }
        }

        double offsetX = numList.Count > 0 ? numList[0] : 0;
        double offsetY = numList.Count > 1 ? numList[1] : 4;
        double blur = numList.Count > 2 ? numList[2] : 8;
        double spread = numList.Count > 3 ? numList[3] : 0;

        return new BoxShadowModel(offsetX, offsetY, blur, spread, color, isInset);
    }
}
