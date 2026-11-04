// filepath: example/MediaPlayerShowcase/ShowcaseBuilder.cs
#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace MediaPlayerShowcase;

/// <summary>
/// 多媒體播放器展示範例 (MediaPlayerShowcase) 純 C# 宣告式 AST 建構器。
/// 
/// 本檔案示範如何以純物件導向 C# 程式碼建構 Avalonia Form Generator (AFG) 之核心中介語意樹 (UI Metadata AST)：
/// 1. 【FormDocument 配置】：定義視窗尺寸、深色主題背景、MVVM 混合模式與強型別編譯綁定 (Compiled Bindings)。
/// 2. 【多媒體控制項 (MediaPlayerControl)】：宣告視訊與音訊播放容器、設定音訊等化器、循環播放 (IsLooping)、自動播放與等比縮放模式。
/// 3. 【傳輸控制列 (Transport Bar)】：宣告播放、暫停、停止、音量調整、靜音切換、時間軸巡覽與全螢幕命令。
/// 4. 【雙向 MVVM 資料綁定】：使用 <see cref="BindingDefinition"/> 將 UI 控制項數值與 ViewModel 屬性精確對齊。
/// 5. 【專案序列化與匯出】：示範 <see cref="AfgSerializer"/> 儲存 JSON 以及 <see cref="ProjectExportService"/> 產出可編譯之多專案現代化方案。
/// </summary>
public static class ShowcaseBuilder
{
    /// <summary>
    /// 以純 C# 程式碼建構完整的多媒體播放器表單文件 (FormDocument) 語意樹。
    /// </summary>
    /// <returns>已完全配置控制項階層、MVVM 綁定與外觀樣式之 <see cref="FormDocument"/> 物件。</returns>
    public static FormDocument BuildDocument()
    {
        return new FormDocument
        {
            // --- 核心規格與專案中介詮釋資料 ---
            SchemaVersion = "1.0",
            ProjectName = "MediaPlayerShowcase",
            RootNamespace = "MediaPlayerShowcase",
            Title = "多媒體播放器控制項展示",
            ViewClassName = "MediaPlayerShowcaseView",
            ViewModelClassName = "MediaPlayerShowcaseViewModel",

            // --- 視窗幾何與現代深色風格設定 ---
            CanvasWidth = 640,
            CanvasHeight = 720,
            BackgroundColor = "#18181B", // Zinc-900 沉浸式暗黑背景
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowState = WindowState.Normal,
            CanResize = true,
            SystemDecorations = SystemDecorations.Full,

            // --- 架構模式：啟用相依性注入 (DI) 與強型別 C# Fluent Markup 編譯期綁定 ---
            EnableDependencyInjection = true,
            UseCompiledBindings = true,
            ArchitectureMode = ArchitectureMode.Hybrid,
            TargetLanguage = TargetLanguage.CSharp,

            // =========================================================================
            // 根畫布節點 (Root Canvas)
            // =========================================================================
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Width = 640,
                Height = 720,
                Children = [
                    // -----------------------------------------------------------------
                    // 1. 頂部標題卡片 (Header Card)
                    // -----------------------------------------------------------------
                    new AstNode
                    {
                        Id = "headerCard",
                        Name = "headerCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 20,
                        Width = 600,
                        Height = 70,
                        Background = "#27272A", // Zinc-800
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            // 主標題
                            new AstNode
                            {
                                Id = "txtHeaderTitle",
                                Name = "txtHeaderTitle",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 12,
                                Text = "多媒體播放器控制項展示",
                                FontSize = 20,
                                FontWeight = "Bold",
                                Foreground = "#818CF8" // Indigo-400
                            },
                            // 副標題與能力特色
                            new AstNode
                            {
                                Id = "txtHeaderSubtitle",
                                Name = "txtHeaderSubtitle",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 40,
                                Text = "Modern MediaPlayerConsole: Playback, Seeking, Volume & Audio Spectrum",
                                FontSize = 12,
                                Foreground = "#94A3B8" // Slate-400
                            }
                        ]
                    },

                    // -----------------------------------------------------------------
                    // 2. 視訊與音訊媒體渲染主區域 (Media Player Video / Canvas View Area)
                    // -----------------------------------------------------------------
                    new AstNode
                    {
                        Id = "playerAreaCard",
                        Name = "playerAreaCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 105,
                        Width = 600,
                        Height = 340,
                        Background = "#09090B", // Zinc-950 極致黑背景
                        BorderBrush = "#4F46E5", // Indigo-600 霓虹邊框
                        BorderThickness = new ThicknessModel(2, 2, 2, 2),
                        CornerRadius = new CornerRadiusModel(12, 12, 12, 12),
                        BoxShadow = new BoxShadowModel(0, 4, 16, 0, "#404F46E5", false),
                        Children = [
                            // 核心多媒體播放容器控制項 (支援自訂渲染、等化器 HUD 與硬體加速)
                            new AstNode
                            {
                                Id = "mediaPlayerControl",
                                Name = "mediaPlayerControl",
                                Type = ControlType.MediaPlayer,
                                CanvasLeft = 10,
                                CanvasTop = 10,
                                Width = 580,
                                Height = 320,
                                AutoPlay = true,
                                IsLooping = true,
                                Volume = 0.8,
                                Stretch = Stretch.Uniform,
                                Source = "avares://MediaPlayerShowcase/Assets/sample_stream.mp4"
                            }
                        ]
                    },

                    // -----------------------------------------------------------------
                    // 3. 播放傳輸控制列卡片 (Transport & Playback Controls Panel)
                    // -----------------------------------------------------------------
                    new AstNode
                    {
                        Id = "transportCard",
                        Name = "transportCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 460,
                        Width = 600,
                        Height = 165,
                        Background = "#27272A", // Zinc-800
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            // 曲目/檔案名稱顯示 (雙向/單向綁定 TrackTitle)
                            new AstNode
                            {
                                Id = "txtTrack",
                                Name = "txtTrack",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 15,
                                Text = "正在播放：Cyberpunk Synthwave - Neon Horizon (Track 01)",
                                FontSize = 13,
                                FontWeight = "Bold",
                                Foreground = "#F8FAFC",
                                Bindings = [
                                    new BindingDefinition("Text", "TrackTitle", BindingMode.Default)
                                ]
                            },

                            // 播放時間進度計數器 (目前時間 / 總時長，綁定 TimeDisplay)
                            new AstNode
                            {
                                Id = "lblTime",
                                Name = "lblTime",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 490,
                                CanvasTop = 15,
                                Text = "01:14 / 03:32",
                                FontSize = 12,
                                Foreground = "#94A3B8",
                                Bindings = [
                                    new BindingDefinition("Text", "TimeDisplay", BindingMode.Default)
                                ]
                            },

                            // 時間軸進度滑動拉桿 (雙向綁定 PositionValue: 0 ~ 100)
                            new AstNode
                            {
                                Id = "sldPosition",
                                Name = "sldPosition",
                                Type = ControlType.Slider,
                                CanvasLeft = 20,
                                CanvasTop = 40,
                                Width = 560,
                                Height = 28,
                                Minimum = 0,
                                Maximum = 100,
                                Value = 35,
                                Bindings = [
                                    new BindingDefinition("Value", "PositionValue", BindingMode.TwoWay)
                                ]
                            },

                            // 播放命令按鈕 (Click 映射 PlayCommand)
                            new AstNode
                            {
                                Id = "btnPlay",
                                Name = "btnPlay",
                                Type = ControlType.Button,
                                CanvasLeft = 20,
                                CanvasTop = 80,
                                Width = 90,
                                Height = 36,
                                Content = "▶ 播放",
                                Background = "#4F46E5",
                                Foreground = "#FFFFFF",
                                FontSize = 13,
                                CornerRadius = new CornerRadiusModel(6, 6, 6, 6),
                                Events = [
                                    new EventMappingDefinition
                                    {
                                        EventName = "Click",
                                        CommandProperty = "PlayCommand",
                                        IsAsync = true
                                    }
                                ]
                            },

                            // 暫停命令按鈕 (Click 映射 PauseCommand)
                            new AstNode
                            {
                                Id = "btnPause",
                                Name = "btnPause",
                                Type = ControlType.Button,
                                CanvasLeft = 120,
                                CanvasTop = 80,
                                Width = 90,
                                Height = 36,
                                Content = "⏸ 暫停",
                                Background = "#3F3F46",
                                Foreground = "#FFFFFF",
                                FontSize = 13,
                                CornerRadius = new CornerRadiusModel(6, 6, 6, 6),
                                Events = [
                                    new EventMappingDefinition
                                    {
                                        EventName = "Click",
                                        CommandProperty = "PauseCommand",
                                        IsAsync = true
                                    }
                                ]
                            },

                            // 停止命令按鈕 (Click 映射 StopCommand)
                            new AstNode
                            {
                                Id = "btnStop",
                                Name = "btnStop",
                                Type = ControlType.Button,
                                CanvasLeft = 220,
                                CanvasTop = 80,
                                Width = 90,
                                Height = 36,
                                Content = "⏹ 停止",
                                Background = "#DC2626",
                                Foreground = "#FFFFFF",
                                FontSize = 13,
                                CornerRadius = new CornerRadiusModel(6, 6, 6, 6),
                                Events = [
                                    new EventMappingDefinition
                                    {
                                        EventName = "Click",
                                        CommandProperty = "StopCommand",
                                        IsAsync = true
                                    }
                                ]
                            },

                            // 音量指示標籤 (動態更新百分比與靜音標記，綁定 VolumeLabel)
                            new AstNode
                            {
                                Id = "lblVol",
                                Name = "lblVol",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 330,
                                CanvasTop = 88,
                                Text = "音量 (80%)：",
                                Foreground = "#E2E8F0",
                                FontSize = 12,
                                Bindings = [
                                    new BindingDefinition("Text", "VolumeLabel", BindingMode.Default)
                                ]
                            },

                            // 音量滑動拉桿 (雙向綁定 VolumeValue: 0 ~ 100)
                            new AstNode
                            {
                                Id = "sldVol",
                                Name = "sldVol",
                                Type = ControlType.Slider,
                                CanvasLeft = 410,
                                CanvasTop = 84,
                                Width = 170,
                                Height = 26,
                                Minimum = 0,
                                Maximum = 100,
                                Value = 80,
                                Bindings = [
                                    new BindingDefinition("Value", "VolumeValue", BindingMode.TwoWay)
                                ]
                            },

                            // 循環播放複選方塊 (雙向綁定 IsLooping)
                            new AstNode
                            {
                                Id = "chkLoop",
                                Name = "chkLoop",
                                Type = ControlType.CheckBox,
                                CanvasLeft = 20,
                                CanvasTop = 126,
                                Content = "循環播放 (Looping)",
                                IsChecked = true,
                                Bindings = [
                                    new BindingDefinition("IsChecked", "IsLooping", BindingMode.TwoWay)
                                ]
                            }
                        ]
                    },

                    // -----------------------------------------------------------------
                    // 4. 底部播放狀態與格式診斷回饋列 (Status Diagnostic Feedback Bar)
                    // -----------------------------------------------------------------
                    new AstNode
                    {
                        Id = "statusCard",
                        Name = "statusCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 640,
                        Width = 600,
                        Height = 60,
                        Background = "#1E293B", // Slate-800
                        BorderBrush = "#3B82F6", // Blue-500
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                        Children = [
                            // 播放器狀態、音訊解碼格式與緩衝指標
                            new AstNode
                            {
                                Id = "txtStatus",
                                Name = "txtStatus",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 20,
                                Text = "播放器狀態：播放中 (Playing) | 格式：AAC / 48kHz Stereo | 輸出：立體聲聲道 | 緩衝：100%",
                                Foreground = "#34D399", // Emerald-400 霓虹綠
                                FontSize = 12,
                                Bindings = [
                                    new BindingDefinition("Text", "PlayStatusText", BindingMode.Default)
                                ]
                            }
                        ]
                    }
                ]
            }
        };
    }

    // =========================================================================
    // 序列化與跨平台方案匯出服務範例 (Serialization & Code Generation API)
    // =========================================================================

    /// <summary>
    /// 將 AST 表單文件透過 <see cref="AfgSerializer"/> 序列化為標準 .afg.json 格式字串。
    /// </summary>
    /// <param name="document">要序列化的表單文件模型。</param>
    /// <returns>格式化後的 JSON 字串。</returns>
    public static string SerializeToJson(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return AfgSerializer.SerializeDocument(document);
    }

    /// <summary>
    /// 將 AST 表單文件寫入至本地磁碟的 .afg.json 檔案。
    /// </summary>
    /// <param name="document">要儲存的表單文件模型。</param>
    /// <param name="filePath">目標檔案完整路徑。</param>
    public static void SaveToJsonFile(FormDocument document, string filePath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = SerializeToJson(document);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 從指定的 .afg.json 檔案讀取並反序列化為 AST 表單文件。
    /// </summary>
    /// <param name="filePath">來源 .afg.json 檔案路徑。</param>
    /// <returns>反序列化後的 <see cref="FormDocument"/> 物件。</returns>
    public static FormDocument LoadFromJsonFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"找不到指定的 AFG 設計專案檔：{filePath}", filePath);
        }

        var json = File.ReadAllText(filePath);
        return AfgSerializer.DeserializeDocument(json);
    }

    /// <summary>
    /// 示範如何透過 <see cref="ProjectExportService"/> 自動生成包含 .slnx, .Desktop, .Shared 之完整可編譯 Avalonia 方案。
    /// </summary>
    /// <param name="document">表單文件規格。</param>
    /// <param name="outputDirectory">輸出目錄。</param>
    public static async Task ExportCompleteProjectAsync(FormDocument document, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var exportService = new ProjectExportService();
        var options = new ProjectExportOptions(
            IncludeMobileProject: false,
            CustomProjectName: "MediaPlayerShowcase"
        );

        await exportService.ExportToFolderAsync(document, outputDirectory, options);
    }

    /// <summary>
    /// 獨立執行進入點：示範 AST 建構、JSON 輸出與專案匯出流程。
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("🚀 Avalonia Form Generator (AFG) - MediaPlayerShowcase AST Builder");
        Console.WriteLine("=================================================");

        // 1. 純 C# 宣告式建立 AST 模型
        Console.WriteLine("1. 建構 MediaPlayerShowcase FormDocument AST 模型樹...");
        var document = BuildDocument();
        Console.WriteLine($"   ✔ 根畫布尺寸：{document.CanvasWidth} x {document.CanvasHeight}");
        Console.WriteLine($"   ✔ 子控制項節點數：{document.RootNode.Children.Count} 個頂層容器");

        // 2. 序列化為 JSON
        var targetJsonPath = Path.Combine(AppContext.BaseDirectory, "MediaPlayerShowcase.generated.afg.json");
        Console.WriteLine($"2. 序列化儲存至：{targetJsonPath}");
        SaveToJsonFile(document, targetJsonPath);
        Console.WriteLine("   ✔ AfgSerializer 序列化成功！");

        // 3. 測試專案匯出引擎
        var exportFolder = Path.Combine(Path.GetTempPath(), "AFG_Export_MediaPlayerShowcase_" + Guid.NewGuid().ToString("N"));
        Console.WriteLine($"3. 呼叫 ProjectExportService 匯出跨平台專案至暫存目錄：{exportFolder}");
        await ExportCompleteProjectAsync(document, exportFolder);
        Console.WriteLine("   ✔ 專案匯出成功完成！");

        Console.WriteLine("\n🎉 所有 AST 建構與匯出流程已成功驗證！");
    }
}
