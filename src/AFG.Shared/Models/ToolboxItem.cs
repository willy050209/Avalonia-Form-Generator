// filepath: src/AFG.Shared/Models/ToolboxItem.cs
namespace AFG.Shared.Models;

/// <summary>
/// 表示工具箱中的單一控制項項目。
/// </summary>
public sealed record ToolboxItem(
    string DisplayName,
    string Category,
    ControlType Type,
    string IconGlyph,
    double DefaultWidth = 120,
    double DefaultHeight = 35,
    string? DefaultContent = null);
