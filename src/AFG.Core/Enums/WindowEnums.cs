// filepath: src/AFG.Core/Enums/WindowEnums.cs
namespace AFG.Core.Enums;

/// <summary>
/// 視窗啟動時在螢幕或父視窗之初始定位位置。
/// </summary>
public enum WindowStartupLocation
{
    CenterScreen,
    CenterOwner,
    Manual
}

/// <summary>
/// 視窗初始與目前之顯示狀態。
/// </summary>
public enum WindowState
{
    Normal,
    Maximized,
    Minimized,
    FullScreen
}

/// <summary>
/// 視窗系統標題列與邊框裝飾樣式。
/// </summary>
public enum SystemDecorations
{
    Full,
    None,
    BorderOnly
}
