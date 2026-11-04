// filepath: example/VectorGraphicsShowcase/ShowcaseBuilder.cs
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Models.Common;
using AFG.Core.Models.Logic;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace VectorGraphicsShowcase;

/// <summary>
/// 向量幾何繪圖展示專案之程式化 AST 建構器 (Programmatic AST Showcase Builder)。
/// <para>
/// 示範如何以純 C# 強型別語法建構 Avalonia Form Generator (AFG) 的 UI 中介語意樹 (AST Document)，
/// 包含多層次立體卡片 (Card)、SVG 幾何路徑 (Path)、多邊形 (Polygon)、折線 (Polyline)、動態長條圖與 MVVM 雙向綁定。
/// 同時示範如何使用 <see cref="AfgSerializer"/> 進行 JSON 序列化與使用 <see cref="ProjectExportService"/> 匯出完整現代跨平台專案方案。
/// </para>
/// </summary>
public static class ShowcaseBuilder
{
    // =========================================================================
    // 1. 核心表單建構方法 (Main Document Builder)
    // =========================================================================

    /// <summary>
    /// 建立「擴充圖形函數與向量繪圖」之標準 FormDocument AST 語意模型。
    /// </summary>
    /// <returns>已具備完整階層樹、樣式與資料綁定之 FormDocument 物件。</returns>
    public static FormDocument BuildShowcaseDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "VectorGraphicsShowcase",
            RootNamespace = "VectorGraphicsShowcase",
            Title = "擴充圖形函數與向量繪圖範例",
            ViewClassName = "VectorGraphicsShowcaseView",
            ViewModelClassName = "VectorGraphicsShowcaseViewModel",
            CanvasWidth = 640,
            CanvasHeight = 720,
            BackgroundColor = "#18181B",
            TargetLanguage = TargetLanguage.CSharp,
            ArchitectureMode = ArchitectureMode.Hybrid,
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Width = 640,
                Height = 720,
                Children = [
                    // 1. 頂部標題卡片 (Header Card)
                    CreateHeaderCard(),

                    // 2. 三大即時系統性能向量指標卡片 (Metric Cards)
                    CreateMetricCard(
                        id: "metricCard1",
                        left: 20,
                        title: "⚡ CPU 使用率",
                        valueId: "valM1",
                        defaultValue: "42%",
                        bindingProperty: "CpuUsage",
                        barId: "barM1",
                        cardBg: "#312E81",
                        cardGlow: "#504F46E5",
                        titleColor: "#A5B4FC",
                        barBg: "#4338CA"
                    ),
                    CreateMetricCard(
                        id: "metricCard2",
                        left: 225,
                        title: "💾 記憶體負載",
                        valueId: "valM2",
                        defaultValue: "68%",
                        bindingProperty: "MemoryUsage",
                        barId: "barM2",
                        cardBg: "#064E3B",
                        cardGlow: "#50059669",
                        titleColor: "#6EE7B7",
                        barBg: "#047857"
                    ),
                    CreateMetricCard(
                        id: "metricCard3",
                        left: 430,
                        title: "🌐 網路傳輸",
                        valueId: "valM3",
                        defaultValue: "1.2 Gbps",
                        bindingProperty: "NetworkSpeed",
                        barId: "barM3",
                        cardBg: "#701A75",
                        cardGlow: "#50C026D3",
                        titleColor: "#F0ABFC",
                        barBg: "#A21CAF"
                    ),

                    // 3. 向量幾何分析圖表卡片 (Vector Bar Chart Card)
                    CreateVectorBarChartCard(),

                    // 4. 向量互動與動態主題控制卡片 (Interactive Control Card)
                    CreateControlCard()
                ]
            }
        };
    }

    // =========================================================================
    // 2. 幾何節點與卡片工廠輔助方法 (Helper Factory Methods)
    // =========================================================================

    /// <summary>
    /// 建立頂部標題卡片節點。
    /// </summary>
    private static AstNode CreateHeaderCard()
    {
        return new AstNode
        {
            Id = "headerCard",
            Name = "headerCard",
            Type = ControlType.Border,
            CanvasLeft = 20,
            CanvasTop = 20,
            Width = 600,
            Height = 70,
            Background = "#27272A",
            CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
            BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
            Children = [
                new AstNode
                {
                    Id = "txtHeaderTitle",
                    Name = "txtHeaderTitle",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 12,
                    Text = "擴充圖形函數與向量繪圖範例",
                    FontSize = 20,
                    FontWeight = "Bold",
                    Foreground = "#818CF8"
                },
                new AstNode
                {
                    Id = "txtHeaderSubtitle",
                    Name = "txtHeaderSubtitle",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 40,
                    Text = "向量拓撲：8 節點多邊形晶片 | 座標系：像素網格精確對齊 | 筆觸模式：Crisp 向量折線",
                    FontSize = 11,
                    Foreground = "#94A3B8",
                    Bindings = [new BindingDefinition("Text", "GeometryStatusText")]
                }
            ]
        };
    }

    /// <summary>
    /// 建立即時指標卡片 (Metric Card)，包含發光陰影 (Glow)、雙向數值綁定與向量進度條。
    /// </summary>
    private static AstNode CreateMetricCard(
        string id,
        double left,
        string title,
        string valueId,
        string defaultValue,
        string bindingProperty,
        string barId,
        string cardBg,
        string cardGlow,
        string titleColor,
        string barBg)
    {
        return new AstNode
        {
            Id = id,
            Name = id,
            Type = ControlType.Border,
            CanvasLeft = left,
            CanvasTop = 105,
            Width = 190,
            Height = 115,
            Background = cardBg,
            CornerRadius = new CornerRadiusModel(12, 12, 12, 12),
            BoxShadow = new BoxShadowModel(0, 4, 12, 0, cardGlow, false),
            Children = [
                new AstNode
                {
                    Id = $"lbl_{id}",
                    Name = $"lbl_{id}",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 16,
                    CanvasTop = 14,
                    Text = title,
                    FontSize = 13,
                    Foreground = titleColor
                },
                new AstNode
                {
                    Id = valueId,
                    Name = valueId,
                    Type = ControlType.TextBlock,
                    CanvasLeft = 16,
                    CanvasTop = 38,
                    Text = defaultValue,
                    FontSize = 28,
                    FontWeight = "Bold",
                    Foreground = "#FFFFFF",
                    Bindings = [new BindingDefinition("Text", bindingProperty)]
                },
                new AstNode
                {
                    Id = barId,
                    Name = barId,
                    Type = ControlType.Border,
                    CanvasLeft = 16,
                    CanvasTop = 85,
                    Width = 150,
                    Height = 6,
                    Background = barBg,
                    CornerRadius = new CornerRadiusModel(3, 3, 3, 3)
                }
            ]
        };
    }

    /// <summary>
    /// 建立向量長條圖分析卡片 (Vector Bar Chart Card)。
    /// 內部整合多組幾何柱體、水平基準軸線與各季度座標刻度。
    /// </summary>
    private static AstNode CreateVectorBarChartCard()
    {
        return new AstNode
        {
            Id = "chartCard",
            Name = "chartCard",
            Type = ControlType.Border,
            CanvasLeft = 20,
            CanvasTop = 240,
            Width = 600,
            Height = 330,
            Background = "#27272A",
            CornerRadius = new CornerRadiusModel(12, 12, 12, 12),
            BoxShadow = new BoxShadowModel(0, 4, 12, 0, "#40000000", false),
            Children = [
                // 圖表標題
                new AstNode
                {
                    Id = "lblChartTitle",
                    Name = "lblChartTitle",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 15,
                    Text = "即時向量柱狀分析圖 (Vector Bar Chart)",
                    FontSize = 14,
                    FontWeight = "Bold",
                    Foreground = "#F8FAFC",
                    Bindings = [new BindingDefinition("Text", "ChartTitle")]
                },

                // 6 組向量長條圖 (具有圓角上方邊緣與動態填色)
                CreateVectorBarNode("vBar1", 50, 140, 55, 130, "#4F46E5"),
                CreateVectorBarNode("vBar2", 135, 90, 55, 180, "#06B6D4"),
                CreateVectorBarNode("vBar3", 220, 50, 55, 220, "#10B981"),
                CreateVectorBarNode("vBar4", 305, 110, 55, 160, "#F59E0B"),
                CreateVectorBarNode("vBar5", 390, 40, 55, 230, "#EC4899"),
                CreateVectorBarNode("vBar6", 475, 70, 55, 200, "#8B5CF6"),

                // X 軸基準線 (水平向量分割線)
                new AstNode
                {
                    Id = "axisLine",
                    Name = "axisLine",
                    Type = ControlType.Border,
                    CanvasLeft = 35,
                    CanvasTop = 270,
                    Width = 530,
                    Height = 2,
                    Background = "#4B5563"
                },

                // X 軸季度座標標籤 (Q1 ~ Q6)
                new AstNode { Id = "q1", Name = "q1", Type = ControlType.TextBlock, CanvasLeft = 65, CanvasTop = 280, Text = "Q1", FontSize = 12, Foreground = "#9CA3AF" },
                new AstNode { Id = "q2", Name = "q2", Type = ControlType.TextBlock, CanvasLeft = 150, CanvasTop = 280, Text = "Q2", FontSize = 12, Foreground = "#9CA3AF" },
                new AstNode { Id = "q3", Name = "q3", Type = ControlType.TextBlock, CanvasLeft = 235, CanvasTop = 280, Text = "Q3", FontSize = 12, Foreground = "#9CA3AF" },
                new AstNode { Id = "q4", Name = "q4", Type = ControlType.TextBlock, CanvasLeft = 320, CanvasTop = 280, Text = "Q4", FontSize = 12, Foreground = "#9CA3AF" },
                new AstNode { Id = "q5", Name = "q5", Type = ControlType.TextBlock, CanvasLeft = 405, CanvasTop = 280, Text = "Q5", FontSize = 12, Foreground = "#9CA3AF" },
                new AstNode { Id = "q6", Name = "q6", Type = ControlType.TextBlock, CanvasLeft = 490, CanvasTop = 280, Text = "Q6", FontSize = 12, Foreground = "#9CA3AF" }
            ]
        };
    }

    /// <summary>
    /// 建立單一向量長條幾何節點 (Vector Bar)，上方具備 6px 圓角。
    /// </summary>
    private static AstNode CreateVectorBarNode(string id, double left, double top, double width, double height, string defaultColor)
    {
        return new AstNode
        {
            Id = id,
            Name = id,
            Type = ControlType.Border,
            CanvasLeft = left,
            CanvasTop = top,
            Width = width,
            Height = height,
            Background = defaultColor,
            CornerRadius = new CornerRadiusModel(6, 6, 0, 0)
        };
    }

    /// <summary>
    /// 建立互動控制卡片 (Control Card)，整合拉桿、隨機產生按鈕、主題切換按鈕與描述文字。
    /// </summary>
    private static AstNode CreateControlCard()
    {
        return new AstNode
        {
            Id = "ctrlCard",
            Name = "ctrlCard",
            Type = ControlType.Border,
            CanvasLeft = 20,
            CanvasTop = 590,
            Width = 600,
            Height = 110,
            Background = "#1E293B",
            BorderBrush = "#3B82F6",
            BorderThickness = new ThicknessModel(1, 1, 1, 1),
            CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
            Children = [
                // 1. 振幅比例文字標籤
                new AstNode
                {
                    Id = "lblScale",
                    Name = "lblScale",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 14,
                    Text = "向量圖表振幅比例 (Scale: 100%)：",
                    Foreground = "#60A5FA",
                    FontSize = 13,
                    Bindings = [new BindingDefinition("Text", "ScaleLabel")]
                },

                // 2. 數值拉桿 (Slider: 20% ~ 150%)
                new AstNode
                {
                    Id = "sldScale",
                    Name = "sldScale",
                    Type = ControlType.Slider,
                    CanvasLeft = 20,
                    CanvasTop = 40,
                    Width = 240,
                    Height = 30,
                    Minimum = 20,
                    Maximum = 150,
                    Value = 100,
                    Bindings = [new BindingDefinition("Value", "ChartScale", BindingMode.TwoWay)]
                },

                // 3. 隨機動態資料命令按鈕
                new AstNode
                {
                    Id = "btnRandomize",
                    Name = "btnRandomize",
                    Type = ControlType.Button,
                    CanvasLeft = 280,
                    CanvasTop = 38,
                    Width = 140,
                    Height = 36,
                    Content = "🎲 隨機動態資料",
                    Background = "#4F46E5",
                    Foreground = "#FFFFFF",
                    FontSize = 12,
                    CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                    Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "RandomizeDataCommand", IsAsync = true }]
                },

                // 4. 切換幾何主題命令按鈕
                new AstNode
                {
                    Id = "btnSwitchTheme",
                    Name = "btnSwitchTheme",
                    Type = ControlType.Button,
                    CanvasLeft = 435,
                    CanvasTop = 38,
                    Width = 145,
                    Height = 36,
                    Content = "🎨 切換幾何主題",
                    Background = "#059669",
                    Foreground = "#FFFFFF",
                    FontSize = 12,
                    CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                    Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SwitchThemeCommand", IsAsync = false }]
                },

                // 5. 主題動態描述文字
                new AstNode
                {
                    Id = "txtThemeDesc",
                    Name = "txtThemeDesc",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 82,
                    Text = "幾何主題：科技晶片多邊形 (Cyberpunk Neon) | 拓撲演算法：8 節點向量路徑",
                    Foreground = "#94A3B8",
                    FontSize = 11,
                    Bindings = [new BindingDefinition("Text", "ThemeDescription")]
                }
            ]
        };
    }

    // =========================================================================
    // 3. 擴充向量形狀節點示範 (SVG Path, Polygon, Polyline Demonstrations)
    // =========================================================================

    /// <summary>
    /// 示範如何建立標準 SVG 向量幾何路徑 (SVG Path Node)。
    /// 透過 <see cref="AstNode.CustomProperties"/> 攜帶 SVG Path 指令與外觀樣式。
    /// </summary>
    /// <param name="id">節點唯一識別碼。</param>
    /// <param name="left">畫布水平 X 座標。</param>
    /// <param name="top">畫布垂直 Y 座標。</param>
    /// <param name="svgPathData">標準 SVG Path 語法 (例: "M 0 50 Q 50 0, 100 50 T 200 50 Z")。</param>
    /// <param name="fillColor">填色色碼 (例: "#4F46E5")。</param>
    /// <param name="strokeColor">外框線條色碼 (例: "#818CF8")。</param>
    /// <param name="strokeThickness">線條粗細 (px)。</param>
    public static AstNode CreateSvgPathNode(
        string id,
        double left,
        double top,
        string svgPathData,
        string fillColor,
        string strokeColor,
        double strokeThickness = 2.0)
    {
        return new AstNode
        {
            Id = id,
            Name = id,
            Type = ControlType.Border,
            CanvasLeft = left,
            CanvasTop = top,
            Background = fillColor,
            BorderBrush = strokeColor,
            BorderThickness = new ThicknessModel(strokeThickness, strokeThickness, strokeThickness, strokeThickness),
            CustomProperties = ImmutableDictionary<string, string>.Empty
                .Add("NodeType", "Path")
                .Add("Data", svgPathData)
                .Add("Fill", fillColor)
                .Add("Stroke", strokeColor)
                .Add("StrokeThickness", strokeThickness.ToString("F1"))
        };
    }

    /// <summary>
    /// 示範如何建立封閉多邊形幾何節點 (Polygon Node)。
    /// </summary>
    /// <param name="id">節點唯一識別碼。</param>
    /// <param name="points">多邊形頂點序列 (例: "50,0 100,50 50,100 0,50")。</param>
    /// <param name="fillColor">填色色彩。</param>
    /// <param name="strokeColor">筆觸邊線色彩。</param>
    public static AstNode CreatePolygonNode(
        string id,
        string points,
        string fillColor,
        string strokeColor)
    {
        return new AstNode
        {
            Id = id,
            Name = id,
            Type = ControlType.Border,
            Background = fillColor,
            BorderBrush = strokeColor,
            CustomProperties = ImmutableDictionary<string, string>.Empty
                .Add("NodeType", "Polygon")
                .Add("Points", points)
                .Add("Fill", fillColor)
                .Add("Stroke", strokeColor)
        };
    }

    /// <summary>
    /// 示範如何建立開放折線幾何節點 (Polyline Node)。
    /// </summary>
    /// <param name="id">節點唯一識別碼。</param>
    /// <param name="points">折線頂點序列 (例: "0,50 50,20 100,80 150,30 200,60")。</param>
    /// <param name="strokeColor">筆觸邊線色彩。</param>
    /// <param name="strokeThickness">線條筆觸粗細。</param>
    public static AstNode CreatePolylineNode(
        string id,
        string points,
        string strokeColor,
        double strokeThickness = 2.5)
    {
        return new AstNode
        {
            Id = id,
            Name = id,
            Type = ControlType.Border,
            BorderBrush = strokeColor,
            BorderThickness = new ThicknessModel(strokeThickness, strokeThickness, strokeThickness, strokeThickness),
            CustomProperties = ImmutableDictionary<string, string>.Empty
                .Add("NodeType", "Polyline")
                .Add("Points", points)
                .Add("Stroke", strokeColor)
                .Add("StrokeThickness", strokeThickness.ToString("F1"))
        };
    }

    // =========================================================================
    // 4. 序列化與跨平台專案匯出示範 (Serialization & Project Export)
    // =========================================================================

    /// <summary>
    /// 將 FormDocument 序列化為格式化之 JSON 規格字串。
    /// </summary>
    public static string SerializeToJson(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return AfgSerializer.SerializeDocument(document);
    }

    /// <summary>
    /// 從 JSON 規格字串還原反序列化為強型別 FormDocument AST 語意樹。
    /// </summary>
    public static FormDocument DeserializeFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return AfgSerializer.DeserializeDocument(json);
    }

    /// <summary>
    /// 將表單定義儲存為 .afg.json 專案規格檔案。
    /// </summary>
    public static void SaveToFile(FormDocument document, string filePath)
    {
        var json = SerializeToJson(document);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 使用 AFG 專案匯出引擎 (<see cref="ProjectExportService"/>)，將 FormDocument 程式化匯出為
    /// 完整跨平台可編譯之 .NET 10 Visual Studio 方案（包含 Shared 共用專案與 Desktop 進入點）。
    /// </summary>
    /// <param name="document">UI AST 中介模型。</param>
    /// <param name="destinationDirectory">目標輸出資料夾路徑。</param>
    public static async Task ExportProjectAsync(FormDocument document, string destinationDirectory)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        var exportService = new ProjectExportService();
        var options = new ProjectExportOptions(
            IncludeMobileProject: false,
            CustomProjectName: document.ProjectName
        );

        await exportService.ExportToFolderAsync(document, destinationDirectory, options);
    }

    // =========================================================================
    // 5. 獨立執行進入點 (CLI Demonstration Main Entry)
    // =========================================================================

    /// <summary>
    /// 命令列輔助進入點：可直接編譯執行，產出本範例之 .afg.json 設計檔。
    /// </summary>
    public static void Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("🎨 AFG VectorGraphicsShowcase AST Builder");
        Console.WriteLine("==================================================");

        var doc = BuildShowcaseDocument();
        var json = SerializeToJson(doc);

        var outputPath = args.Length > 0
            ? args[0]
            : Path.Combine(AppContext.BaseDirectory, "VectorGraphicsShowcase.afg.json");

        SaveToFile(doc, outputPath);
        Console.WriteLine($"[OK] 成功產生並寫入專案 AST 模型至: {outputPath}");
        Console.WriteLine($"[OK] 畫布尺寸: {doc.CanvasWidth} x {doc.CanvasHeight}");
        Console.WriteLine($"[OK] 子節點總數: {doc.RootNode.Children.Count}");
    }
}
