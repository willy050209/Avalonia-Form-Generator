// ==============================================================================================
// 專案名稱：Avalonia Form Generator (AFG)
// 範例名稱：基本常見控制項綜合展示 AST 建構器 (Basic Controls Showcase AST Builder)
// 檔案路徑：example/BasicControlsShowcase/ShowcaseBuilder.cs
// 說明：本檔案示範如何完全以純 C# 程式碼建構 AFG 中介語意抽象語法樹 (UI Metadata AST)、
//       配置所有常用核心 UI 控制項與雙向 MVVM 資料綁定，並透過 AfgSerializer 進行序列化
//       以及調用 ProjectExportService 匯出跨平台可獨立編譯執行的 Avalonia 應用程式方案。
// ==============================================================================================

using System;
using System.IO;
using System.Threading.Tasks;
using AFG.Core.Models;
using AFG.Core.Models.Ast;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace BasicControlsShowcase;

/// <summary>
/// 提供以純 C# 程式碼動態生成 <see cref="FormDocument"/> 之範例建構器。
/// 支援完整的序列化 (.afg.json) 與跨平台完整專案代碼產生流程。
/// </summary>
public static class ShowcaseBuilder
{
    /// <summary>
    /// 建構「基本常見控制項綜合展示 (BasicControlsShowcase)」之完整 UI 中介語意樹 (AST)。
    /// 涵蓋 Canvas、Border、TextBlock、TextBox、CheckBox、RadioButton、ComboBox、
    /// Slider、ProgressBar、DatePicker、TimePicker、Button 等 12 種控制項之配置與 MVVM 雙向綁定。
    /// </summary>
    /// <returns>符合 AFG 規範之宣告式 <see cref="FormDocument"/> 物件模型。</returns>
    public static FormDocument BuildDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "BasicControlsShowcase",
            RootNamespace = "BasicControlsShowcase",
            Title = "基本常見控制項綜合展示",
            ViewClassName = "BasicControlsShowcaseView",
            ViewModelClassName = "BasicControlsShowcaseViewModel",
            CanvasWidth = 640,
            CanvasHeight = 680,
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
                Height = 680,
                Children = [
                    // =========================================================================
                    // 頂部導覽標題卡片 (Header Card)
                    // =========================================================================
                    new AstNode
                    {
                        Id = "headerCard",
                        Name = "headerCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 15,
                        Width = 600,
                        Height = 60,
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
                                CanvasTop = 8,
                                Text = "基本常見控制項綜合展示",
                                FontSize = 18,
                                FontWeight = "Bold",
                                Foreground = "#818CF8"
                            },
                            new AstNode
                            {
                                Id = "txtHeaderSubtitle",
                                Name = "txtHeaderSubtitle",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 34,
                                Text = "常用 UI 控制項與雙向 MVVM 資料綁定示範",
                                FontSize = 12,
                                Foreground = "#94A3B8"
                            }
                        ]
                    },

                    // =========================================================================
                    // 區塊 1：文字輸入面板 (Text & Input Controls)
                    // 展示 TextBox 之單行、多行文字輸入、浮水印 (Watermark) 與雙向資料綁定
                    // =========================================================================
                    new AstNode
                    {
                        Id = "inputCard",
                        Name = "inputCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 85,
                        Width = 600,
                        Height = 135,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "lblSec1",
                                Name = "lblSec1",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 10,
                                Text = "文字輸入 (Text & Input Controls)",
                                FontSize = 13,
                                FontWeight = "Bold",
                                Foreground = "#38BDF8"
                            },
                            new AstNode
                            {
                                Id = "lblName",
                                Name = "lblName",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 42,
                                Text = "使用者名稱：",
                                FontSize = 13,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "txtName",
                                Name = "txtName",
                                Type = ControlType.TextBox,
                                CanvasLeft = 110,
                                CanvasTop = 36,
                                Width = 170,
                                Height = 32,
                                Text = "陳大明",
                                Watermark = "請輸入姓名",
                                Bindings = [new BindingDefinition("Text", "UserName", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblEmail",
                                Name = "lblEmail",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 300,
                                CanvasTop = 42,
                                Text = "電子郵件：",
                                FontSize = 13,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "txtEmail",
                                Name = "txtEmail",
                                Type = ControlType.TextBox,
                                CanvasLeft = 380,
                                CanvasTop = 36,
                                Width = 200,
                                Height = 32,
                                Text = "daming.chen@example.com",
                                Watermark = "請輸入 Email",
                                Bindings = [new BindingDefinition("Text", "Email", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblBio",
                                Name = "lblBio",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 88,
                                Text = "個人簡介：",
                                FontSize = 13,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "txtBio",
                                Name = "txtBio",
                                Type = ControlType.TextBox,
                                CanvasLeft = 110,
                                CanvasTop = 82,
                                Width = 470,
                                Height = 32,
                                Text = "熱愛跨平台開發與 Avalonia 現代化介面架構設計。",
                                Watermark = "請輸入簡介",
                                Bindings = [new BindingDefinition("Text", "Bio", BindingMode.TwoWay)]
                            }
                        ]
                    },

                    // =========================================================================
                    // 區塊 2：選項與清單面板 (Choice & List Controls)
                    // 展示 CheckBox (複選)、RadioButton (互斥單選) 與 ComboBox (下拉選單)
                    // =========================================================================
                    new AstNode
                    {
                        Id = "choiceCard",
                        Name = "choiceCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 230,
                        Width = 600,
                        Height = 135,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "lblSec2",
                                Name = "lblSec2",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 10,
                                Text = "選項與清單 (Choice & List Controls)",
                                FontSize = 13,
                                FontWeight = "Bold",
                                Foreground = "#34D399"
                            },
                            new AstNode
                            {
                                Id = "chkDarkMode",
                                Name = "chkDarkMode",
                                Type = ControlType.CheckBox,
                                CanvasLeft = 20,
                                CanvasTop = 40,
                                Content = "啟用深色高對比主題",
                                IsChecked = true,
                                Bindings = [new BindingDefinition("IsChecked", "IsDarkMode", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "chkNotifications",
                                Name = "chkNotifications",
                                Type = ControlType.CheckBox,
                                CanvasLeft = 240,
                                CanvasTop = 40,
                                Content = "接收即時推播通知",
                                IsChecked = true,
                                Bindings = [new BindingDefinition("IsChecked", "ReceiveNotifications", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblAccount",
                                Name = "lblAccount",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 75,
                                Text = "帳戶類型：",
                                FontSize = 13,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "rbPersonal",
                                Name = "rbPersonal",
                                Type = ControlType.RadioButton,
                                CanvasLeft = 110,
                                CanvasTop = 73,
                                Content = "個人帳戶",
                                IsChecked = true,
                                Bindings = [new BindingDefinition("IsChecked", "IsPersonalAccount", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "rbEnterprise",
                                Name = "rbEnterprise",
                                Type = ControlType.RadioButton,
                                CanvasLeft = 240,
                                CanvasTop = 73,
                                Content = "企業組織帳戶",
                                IsChecked = false,
                                Bindings = [new BindingDefinition("IsChecked", "IsEnterpriseAccount", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblRole",
                                Name = "lblRole",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 104,
                                Text = "身分角色：",
                                FontSize = 13,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "cmbRole",
                                Name = "cmbRole",
                                Type = ControlType.ComboBox,
                                CanvasLeft = 110,
                                CanvasTop = 98,
                                Width = 200,
                                Height = 28,
                                Content = "資深系統架構師"
                            }
                        ]
                    },

                    // =========================================================================
                    // 區塊 3：數值、進度與日期時間面板 (Range, Progress & DateTime)
                    // 展示 Slider (音量拉桿)、ProgressBar (進度條)、DatePicker 與 TimePicker
                    // =========================================================================
                    new AstNode
                    {
                        Id = "rangeCard",
                        Name = "rangeCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 375,
                        Width = 600,
                        Height = 145,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "lblSec3",
                                Name = "lblSec3",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 10,
                                Text = "數值、進度與日期時間 (Range, Progress & DateTime)",
                                FontSize = 13,
                                FontWeight = "Bold",
                                Foreground = "#FBBF24"
                            },
                            new AstNode
                            {
                                Id = "lblVolume",
                                Name = "lblVolume",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 40,
                                Text = "系統主音量 (85%)：",
                                FontSize = 13,
                                Foreground = "#E2E8F0",
                                Bindings = [new BindingDefinition("Text", "VolumeLabel")]
                            },
                            new AstNode
                            {
                                Id = "sldVolume",
                                Name = "sldVolume",
                                Type = ControlType.Slider,
                                CanvasLeft = 170,
                                CanvasTop = 36,
                                Width = 400,
                                Height = 26,
                                Minimum = 0,
                                Maximum = 100,
                                Value = 85,
                                Bindings = [new BindingDefinition("Value", "VolumeValue", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblProgress",
                                Name = "lblProgress",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 75,
                                Text = "雲端同步進度 (75%)：",
                                FontSize = 13,
                                Foreground = "#E2E8F0",
                                Bindings = [new BindingDefinition("Text", "ProgressLabel")]
                            },
                            new AstNode
                            {
                                Id = "pbProgress",
                                Name = "pbProgress",
                                Type = ControlType.ProgressBar,
                                CanvasLeft = 170,
                                CanvasTop = 75,
                                Width = 400,
                                Height = 16,
                                Minimum = 0,
                                Maximum = 100,
                                Value = 75,
                                Bindings = [new BindingDefinition("Value", "ProgressValue", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblDate",
                                Name = "lblDate",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 108,
                                Text = "排程預約：",
                                FontSize = 13,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "dpDate",
                                Name = "dpDate",
                                Type = ControlType.DatePicker,
                                CanvasLeft = 110,
                                CanvasTop = 103,
                                Width = 180,
                                Height = 30
                            },
                            new AstNode
                            {
                                Id = "tpTime",
                                Name = "tpTime",
                                Type = ControlType.TimePicker,
                                CanvasLeft = 310,
                                CanvasTop = 103,
                                Width = 150,
                                Height = 30
                            }
                        ]
                    },

                    // =========================================================================
                    // 區塊 4：命令操作按鈕 (Action Buttons)
                    // 展示 Button 樣式、圓角、背景色以及 Click 事件映射至非同步 RelayCommand
                    // =========================================================================
                    new AstNode
                    {
                        Id = "btnSave",
                        Name = "btnSave",
                        Type = ControlType.Button,
                        CanvasLeft = 20,
                        CanvasTop = 530,
                        Width = 190,
                        Height = 36,
                        Content = "儲存所有設定",
                        Background = "#4F46E5",
                        Foreground = "#FFFFFF",
                        FontSize = 13,
                        FontWeight = "SemiBold",
                        CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveSettingsCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "btnReset",
                        Name = "btnReset",
                        Type = ControlType.Button,
                        CanvasLeft = 225,
                        CanvasTop = 530,
                        Width = 190,
                        Height = 36,
                        Content = "恢復預設值",
                        Background = "#3F3F46",
                        Foreground = "#FFFFFF",
                        FontSize = 13,
                        CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ResetSettingsCommand", IsAsync = true }]
                    },

                    // =========================================================================
                    // 區塊 5：即時摘要卡片 (Live Status Summary Card)
                    // 展示 TextBlock 之即時雙向資料綁定、彩色邊框與立體陰影視覺效果
                    // =========================================================================
                    new AstNode
                    {
                        Id = "statusCard",
                        Name = "statusCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 575,
                        Width = 600,
                        Height = 95,
                        Background = "#1E293B",
                        BorderBrush = "#3B82F6",
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 12, 0, "#40000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "lblStatusTitle",
                                Name = "lblStatusTitle",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 10,
                                Text = "即時設定摘要 (Live Summary)",
                                FontSize = 13,
                                FontWeight = "Bold",
                                Foreground = "#60A5FA"
                            },
                            new AstNode
                            {
                                Id = "txtSummary",
                                Name = "txtSummary",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 32,
                                Width = 560,
                                Height = 55,
                                Text = "使用者：陳大明 (daming.chen@example.com) | 主題：深色模式 | 通知：已開啟 | 帳戶：個人帳戶 | 音量：85% | 進度：75%",
                                FontSize = 12,
                                Foreground = "#E2E8F0",
                                Bindings = [new BindingDefinition("Text", "StatusSummary", BindingMode.TwoWay)]
                            }
                        ]
                    }
                ]
            }
        };
    }

    /// <summary>
    /// 將 AST 表單模型序列化並儲存為符合 AFG 格式之 JSON 檔案 (*.afg.json)。
    /// </summary>
    /// <param name="document">要儲存之表單文檔物件模型。</param>
    /// <param name="filePath">輸出之目標檔案路徑。</param>
    public static void SaveToJson(FormDocument document, string filePath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = AfgSerializer.SerializeDocument(document);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 從指定的 JSON 檔案載入並反序列化為 AFG 表單文檔物件模型 (<see cref="FormDocument"/>)。
    /// </summary>
    /// <param name="filePath">來源 *.afg.json 檔案路徑。</param>
    /// <returns>反序列化完成之 <see cref="FormDocument"/> 物件實例。</returns>
    public static FormDocument LoadFromJson(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"找不到指定的 AFG 表單設計檔：{filePath}", filePath);
        }

        var json = File.ReadAllText(filePath);
        return AfgSerializer.DeserializeDocument(json);
    }

    /// <summary>
    /// 調用 <see cref="ProjectExportService"/> 將指定的表單文檔匯出為完整的 Visual Studio 現代化跨平台方案。
    /// 生成包含 .slnx、.Shared (Fluent C# Markup 視圖、ViewModel、DI 設定、服務) 與 .Desktop 進入點專案。
    /// </summary>
    /// <param name="document">要匯出之表單模型。</param>
    /// <param name="outputDirectory">目標匯出根目錄路徑。</param>
    /// <param name="includeMobile">是否一併產出 Android/iOS 行動端專案。</param>
    public static async Task ExportProjectAsync(FormDocument document, string outputDirectory, bool includeMobile = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var exportService = new ProjectExportService();
        var options = new ProjectExportOptions(
            IncludeMobileProject: includeMobile,
            IncludeLicense: false,
            CustomProjectName: document.ProjectName
        );

        await exportService.ExportToFolderAsync(document, outputDirectory, options);
    }
}
