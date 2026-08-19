// filepath: src/AFG.Core/Enums/LayoutEnums.cs
namespace AFG.Core.Enums;

/// <summary>
/// 控制項水平對齊方式。
/// </summary>
public enum HorizontalAlignment
{
    Stretch,
    Left,
    Center,
    Right
}

/// <summary>
/// 控制項垂直對齊方式。
/// </summary>
public enum VerticalAlignment
{
    Stretch,
    Top,
    Center,
    Bottom
}

/// <summary>
/// 堆疊與排列方向。
/// </summary>
public enum Orientation
{
    Horizontal,
    Vertical
}

/// <summary>
/// DockPanel 停靠位置。
/// </summary>
public enum DockPosition
{
    Left,
    Top,
    Right,
    Bottom
}

/// <summary>
/// Grid 網格尺寸單位類型。
/// </summary>
public enum GridUnitType
{
    Auto,
    Pixel,
    Star
}

/// <summary>
/// 多節點批次對齊動作類型。
/// </summary>
public enum NodeAlignmentType
{
    AlignLeft,
    AlignHorizontalCenter,
    AlignRight,
    AlignTop,
    AlignVerticalCenter,
    AlignBottom
}

/// <summary>
/// 影像縮放拉伸模式（對應 Windows Forms PictureBox SizeMode）。
/// </summary>
public enum Stretch
{
    /// <summary>
    /// 原始大小不拉伸（對應 WinForms Normal / CenterImage）。
    /// </summary>
    None,

    /// <summary>
    /// 填滿拉伸變形（對應 WinForms StretchImage）。
    /// </summary>
    Fill,

    /// <summary>
    /// 等比例縮放以容納於範圍內（對應 WinForms Zoom）。
    /// </summary>
    Uniform,

    /// <summary>
    /// 等比例縮放並填滿剪裁。
    /// </summary>
    UniformToFill
}
