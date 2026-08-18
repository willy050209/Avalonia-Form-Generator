// filepath: src/AFG.Shared/Models/CanvasPreset.cs
namespace AFG.Shared.Models;

/// <summary>
/// 畫布解析度與長寬比預設模型。
/// </summary>
public sealed record CanvasPreset(
    string Name,
    string AspectRatio,
    double Width,
    double Height,
    bool IsMobile = false)
{
    public override string ToString() => $"{Name} ({Width}x{Height}, {AspectRatio})";

    public static IReadOnlyList<CanvasPreset> Presets { get; } =
    [
        // 桌面端預設
        new("Desktop 1080p", "16:9", 1920, 1080, IsMobile: false),
        new("Desktop 720p", "16:9", 1280, 720, IsMobile: false),
        new("Desktop Standard", "4:3", 800, 600, IsMobile: false),
        new("Desktop Small", "16:10", 960, 600, IsMobile: false),

        // 手機端預設（支援主流旗艦長寬比）
        new("Phone 9:19.5 (Modern Flagship)", "9:19.5", 390, 844, IsMobile: true),
        new("Phone 9:20 (Android Standard)", "9:20", 412, 915, IsMobile: true),
        new("Phone 9:16 (Classic Mobile)", "9:16", 360, 640, IsMobile: true),
        new("Phone FHD+ 9:20", "9:20", 1080, 2400, IsMobile: true),
        
        // 平板端預設
        new("Tablet 3:4 (iPad Standard)", "3:4", 768, 1024, IsMobile: true),
        new("Tablet 16:10", "16:10", 800, 1280, IsMobile: true)
    ];
}
