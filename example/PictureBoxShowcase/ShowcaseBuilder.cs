// filepath: example/PictureBoxShowcase/ShowcaseBuilder.cs
using System;
using System.Collections.Immutable;
using System.IO;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace PictureBoxShowcase;

/// <summary>
/// 示範如何使用純 C# 程式碼建構 Avalonia Form Generator (AFG) 語意語法樹 (UI Metadata AST)。
/// 此程式展示如何以宣告式語意建立 FormDocument、配置 PictureBox / Image 圖片展示控制項、
/// 設定 Stretch 縮放模式、宣告 RadioButton / Button / TextBlock 控制項、配置 MVVM 資料與命令綁定，
/// 並透過 AfgSerializer 匯出 JSON 規格檔，以及調用 ProjectExportService 自動生成完整跨平台 Avalonia 方案。
/// </summary>
public static class ShowcaseBuilder
{
    /// <summary>
    /// 建構 PictureBoxShowcase 完整的表單語意樹模型 (FormDocument AST)。
    /// </summary>
    /// <returns>已具備完整階層、樣式與 MVVM 綁定的 FormDocument 實例。</returns>
    public static FormDocument BuildDocument()
    {
        // -------------------------------------------------------------------------------------------------
        // 1. 頂部標題卡片區塊 (Header Card & Titles)
        // -------------------------------------------------------------------------------------------------
        var txtHeaderTitle = new AstNode
        {
            Id = "txtHeaderTitle",
            Name = "txtHeaderTitle",
            Type = ControlType.TextBlock,
            CanvasLeft = 20,
            CanvasTop = 10,
            Text = "PictureBox 與圖片卷積展示範例",
            Foreground = "#818CF8",
            FontSize = 19,
            FontWeight = "Bold"
        };

        var txtHeaderSubtitle = new AstNode
        {
            Id = "txtHeaderSubtitle",
            Name = "txtHeaderSubtitle",
            Type = ControlType.TextBlock,
            CanvasLeft = 20,
            CanvasTop = 37,
            Text = "Stretch Modes, Dynamic Sources & Custom ImageConvolution Filters",
            Foreground = "#94A3B8",
            FontSize = 12
        };

        var headerCard = new AstNode
        {
            Id = "headerCard",
            Name = "headerCard",
            Type = ControlType.Border,
            Width = 600,
            Height = 65,
            CanvasLeft = 20,
            CanvasTop = 15,
            Background = "#27272A",
            CornerRadius = new CornerRadiusModel(10),
            BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
            Children = [txtHeaderTitle, txtHeaderSubtitle]
        };

        // -------------------------------------------------------------------------------------------------
        // 2. 圖片展示視窗卡片 (Image Viewer Card & PictureBox Control)
        // -------------------------------------------------------------------------------------------------
        // 此處建立 PictureBox 控制項，示範：
        // (1) Stretch 屬性設定為 Uniform（等比縮放適配）。
        // (2) MVVM 影像來源綁定：將 Image.Source 綁定至 ViewModel.ImageSource。
        // (3) MVVM 縮放模式綁定：將 Image.Stretch 綁定至 ViewModel.StretchMode。
        var imgDisplay = new AstNode
        {
            Id = "imgDisplay",
            Name = "imgDisplay",
            Type = ControlType.PictureBox,
            Width = 560,
            Height = 294,
            CanvasLeft = 20,
            CanvasTop = 18,
            Stretch = Stretch.Uniform,
            Bindings =
            [
                new BindingDefinition
                {
                    TargetProperty = "Source",
                    ViewModelProperty = "ImageSource",
                    Mode = BindingModeType.Default
                },
                new BindingDefinition
                {
                    TargetProperty = "Stretch",
                    ViewModelProperty = "StretchMode",
                    Mode = BindingModeType.Default
                }
            ]
        };

        var viewerCard = new AstNode
        {
            Id = "viewerCard",
            Name = "viewerCard",
            Type = ControlType.Border,
            Width = 600,
            Height = 330,
            CanvasLeft = 20,
            CanvasTop = 90,
            Background = "#27272A",
            CornerRadius = new CornerRadiusModel(10),
            BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
            Children = [imgDisplay]
        };

        // -------------------------------------------------------------------------------------------------
        // 3. 縮放模式、樣本切換與自訂卷積算子控制面板 (Control Card)
        // -------------------------------------------------------------------------------------------------
        var lblStretchTitle = new AstNode
        {
            Id = "lblStretchTitle",
            Name = "lblStretchTitle",
            Type = ControlType.TextBlock,
            CanvasLeft = 16,
            CanvasTop = 12,
            Text = "縮放呈現模式 (Stretch Mode)：",
            Foreground = "#38BDF8",
            FontSize = 13,
            FontWeight = "Bold"
        };

        // 四種 Stretch 模式單選按鈕 (RadioButton) 及其雙向 MVVM 綁定
        var rbUniform = new AstNode
        {
            Id = "rbUniform",
            Name = "rbUniform",
            Type = ControlType.RadioButton,
            CanvasLeft = 16,
            CanvasTop = 36,
            Content = "Uniform (等比)",
            IsChecked = true,
            Bindings =
            [
                new BindingDefinition
                {
                    TargetProperty = "IsChecked",
                    ViewModelProperty = "IsUniformStretch",
                    Mode = BindingModeType.TwoWay
                }
            ]
        };

        var rbUniformToFill = new AstNode
        {
            Id = "rbUniformToFill",
            Name = "rbUniformToFill",
            Type = ControlType.RadioButton,
            CanvasLeft = 160,
            CanvasTop = 36,
            Content = "UniformToFill (填滿)",
            IsChecked = false,
            Bindings =
            [
                new BindingDefinition
                {
                    TargetProperty = "IsChecked",
                    ViewModelProperty = "IsUniformToFillStretch",
                    Mode = BindingModeType.TwoWay
                }
            ]
        };

        var rbFill = new AstNode
        {
            Id = "rbFill",
            Name = "rbFill",
            Type = ControlType.RadioButton,
            CanvasLeft = 345,
            CanvasTop = 36,
            Content = "Fill (拉伸)",
            IsChecked = false,
            Bindings =
            [
                new BindingDefinition
                {
                    TargetProperty = "IsChecked",
                    ViewModelProperty = "IsFillStretch",
                    Mode = BindingModeType.TwoWay
                }
            ]
        };

        var rbNone = new AstNode
        {
            Id = "rbNone",
            Name = "rbNone",
            Type = ControlType.RadioButton,
            CanvasLeft = 475,
            CanvasTop = 36,
            Content = "None (原始)",
            IsChecked = false,
            Bindings =
            [
                new BindingDefinition
                {
                    TargetProperty = "IsChecked",
                    ViewModelProperty = "IsNoneStretch",
                    Mode = BindingModeType.TwoWay
                }
            ]
        };

        // 兩個樣本載入按鈕 (Button) 及其 Click 事件映射至非同步命令
        var btnSampleA = new AstNode
        {
            Id = "btnSampleA",
            Name = "btnSampleA",
            Type = ControlType.Button,
            Width = 180,
            Height = 32,
            CanvasLeft = 16,
            CanvasTop = 72,
            Content = "測試圖形 A (幾何網格)",
            Background = "#4F46E5",
            Foreground = "#FFFFFF",
            FontSize = 12,
            CornerRadius = new CornerRadiusModel(6),
            Events =
            [
                new EventMappingDefinition
                {
                    EventName = "Click",
                    CommandProperty = "LoadSampleACommand",
                    IsAsync = true
                }
            ]
        };

        var btnSampleB = new AstNode
        {
            Id = "btnSampleB",
            Name = "btnSampleB",
            Type = ControlType.Button,
            Width = 180,
            Height = 32,
            CanvasLeft = 206,
            CanvasTop = 72,
            Content = "測試圖形 B (漸層色塊)",
            Background = "#059669",
            Foreground = "#FFFFFF",
            FontSize = 12,
            CornerRadius = new CornerRadiusModel(6),
            Events =
            [
                new EventMappingDefinition
                {
                    EventName = "Click",
                    CommandProperty = "LoadSampleBCommand",
                    IsAsync = true
                }
            ]
        };

        // 自訂卷積算子即時展示區 (ImageConvolution)
        var lblConvTitle = new AstNode
        {
            Id = "lblConvTitle",
            Name = "lblConvTitle",
            Type = ControlType.TextBlock,
            CanvasLeft = 16,
            CanvasTop = 114,
            Text = "自訂卷積算子與特效 (ImageConvolution)：",
            Foreground = "#F472B6",
            FontSize = 13,
            FontWeight = "Bold"
        };

        var btnConvReset = new AstNode
        {
            Id = "btnConvReset",
            Name = "btnConvReset",
            Type = ControlType.Button,
            Width = 108,
            Height = 34,
            CanvasLeft = 16,
            CanvasTop = 140,
            Content = "原圖 (Reset)",
            Background = "#475569",
            Foreground = "#FFFFFF",
            FontSize = 11,
            CornerRadius = new CornerRadiusModel(6),
            Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ResetFilterCommand", IsAsync = false }]
        };

        var btnConvSharpen = new AstNode
        {
            Id = "btnConvSharpen",
            Name = "btnConvSharpen",
            Type = ControlType.Button,
            Width = 108,
            Height = 34,
            CanvasLeft = 131,
            CanvasTop = 140,
            Content = "銳化 3×3",
            Background = "#0284C7",
            Foreground = "#FFFFFF",
            FontSize = 11,
            CornerRadius = new CornerRadiusModel(6),
            Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ApplySharpenCommand", IsAsync = false }]
        };

        var btnConvEmboss = new AstNode
        {
            Id = "btnConvEmboss",
            Name = "btnConvEmboss",
            Type = ControlType.Button,
            Width = 108,
            Height = 34,
            CanvasLeft = 246,
            CanvasTop = 140,
            Content = "浮雕 3×3",
            Background = "#7C3AED",
            Foreground = "#FFFFFF",
            FontSize = 11,
            CornerRadius = new CornerRadiusModel(6),
            Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ApplyEmbossCommand", IsAsync = false }]
        };

        var btnConvSobel = new AstNode
        {
            Id = "btnConvSobel",
            Name = "btnConvSobel",
            Type = ControlType.Button,
            Width = 108,
            Height = 34,
            CanvasLeft = 361,
            CanvasTop = 140,
            Content = "Sobel 邊緣",
            Background = "#EA580C",
            Foreground = "#FFFFFF",
            FontSize = 11,
            CornerRadius = new CornerRadiusModel(6),
            Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ApplySobelCommand", IsAsync = false }]
        };

        var btnConvCustom = new AstNode
        {
            Id = "btnConvCustom",
            Name = "btnConvCustom",
            Type = ControlType.Button,
            Width = 108,
            Height = 34,
            CanvasLeft = 476,
            CanvasTop = 140,
            Content = "自訂 5×5",
            Background = "#DB2777",
            Foreground = "#FFFFFF",
            FontSize = 11,
            CornerRadius = new CornerRadiusModel(6),
            Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ApplyCustomKernelCommand", IsAsync = false }]
        };

        var controlCard = new AstNode
        {
            Id = "controlCard",
            Name = "controlCard",
            Type = ControlType.Border,
            Width = 600,
            Height = 195,
            CanvasLeft = 20,
            CanvasTop = 432,
            Background = "#27272A",
            CornerRadius = new CornerRadiusModel(10),
            BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
            Children =
            [
                lblStretchTitle,
                rbUniform,
                rbUniformToFill,
                rbFill,
                rbNone,
                btnSampleA,
                btnSampleB,
                lblConvTitle,
                btnConvReset,
                btnConvSharpen,
                btnConvEmboss,
                btnConvSobel,
                btnConvCustom
            ]
        };

        // -------------------------------------------------------------------------------------------------
        // 4. 底部圖片中繼資訊卡片 (Information Card & Status TextBlock)
        // -------------------------------------------------------------------------------------------------
        var txtImageInfo = new AstNode
        {
            Id = "txtImageInfo",
            Name = "txtImageInfo",
            Type = ControlType.TextBlock,
            CanvasLeft = 16,
            CanvasTop = 22,
            Text = "圖片：800×600 | 模式：Stretch.Uniform | 樣本 A (科技幾何網格) | 濾鏡：原圖",
            Foreground = "#E2E8F0",
            FontSize = 12,
            Bindings =
            [
                new BindingDefinition
                {
                    TargetProperty = "Text",
                    ViewModelProperty = "ImageInfoText",
                    Mode = BindingModeType.Default
                }
            ]
        };

        var infoCard = new AstNode
        {
            Id = "infoCard",
            Name = "infoCard",
            Type = ControlType.Border,
            Width = 600,
            Height = 65,
            CanvasLeft = 20,
            CanvasTop = 640,
            Background = "#1E293B",
            BorderBrush = "#3B82F6",
            BorderThickness = new ThicknessModel(1),
            CornerRadius = new CornerRadiusModel(8),
            Children = [txtImageInfo]
        };

        // -------------------------------------------------------------------------------------------------
        // 5. 畫布根節點 (Root Canvas)
        // -------------------------------------------------------------------------------------------------
        var rootCanvas = new AstNode
        {
            Id = "rootCanvas",
            Name = "RootCanvas",
            Type = ControlType.Canvas,
            Width = 640,
            Height = 720,
            Children = [headerCard, viewerCard, controlCard, infoCard]
        };

        // -------------------------------------------------------------------------------------------------
        // 6. 表單根文件配置 (FormDocument Definition)
        // -------------------------------------------------------------------------------------------------
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "PictureBoxShowcase",
            RootNamespace = "PictureBoxShowcase",
            ViewClassName = "PictureBoxShowcaseView",
            ViewModelClassName = "PictureBoxShowcaseViewModel",
            Title = "PictureBox 與圖片卷積展示範例",
            BackgroundColor = "#18181B",
            CanvasWidth = 640,
            CanvasHeight = 720,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowState = WindowState.Normal,
            CanResize = true,
            Topmost = false,
            ShowInTaskbar = true,
            SystemDecorations = SystemDecorations.Full,
            EnableDependencyInjection = true,
            UseCompiledBindings = true,
            ArchitectureMode = ArchitectureMode.Hybrid,
            TargetLanguage = TargetLanguage.CSharp,
            RootNode = rootCanvas
        };
    }

    /// <summary>
    /// 示範進入點：展示如何透過 C# 程式碼建構 AST、序列化為 JSON 規格檔，以及調用專案生成器匯出完整程式碼。
    /// </summary>
    public static void Main(string[] args)
    {
        Console.WriteLine("=== [AFG] PictureBoxShowcase AST 建構器展示 ===");

        // 1. 純 C# 宣告式建立 AST 模型
        var document = BuildDocument();
        Console.WriteLine($"[1/3] 成功建構 FormDocument AST：{document.ProjectName} (解析度: {document.CanvasWidth}x{document.CanvasHeight})");

        // 2. 序列化為 AFG 官方標準 JSON 規格檔
        string json = AfgSerializer.SerializeDocument(document);
        string jsonPath = Path.Combine(AppContext.BaseDirectory, "PictureBoxShowcase.afg.json");
        File.WriteAllText(jsonPath, json);
        Console.WriteLine($"[2/3] 成功序列化為 JSON 規格檔：{jsonPath} ({json.Length:N0} 位元組)");

        // 3. 使用 ProjectExportService 匯出跨平台方案程式碼
        var exportService = new ProjectExportService();
        var generatedFiles = exportService.GenerateFullProject(document, new ProjectExportOptions
        {
            IncludeMobileProject = false,
            IncludeLicense = false
        });

        Console.WriteLine($"[3/3] 成功調用 ProjectExportService 生成 {generatedFiles.Count} 個專案原始碼檔案：");
        foreach (var file in generatedFiles)
        {
            Console.WriteLine($"      -> {file.RelativePath} ({file.Content.Length:N0} 字元)");
        }

        Console.WriteLine("=== 建構展示完成 ===");
    }
}
