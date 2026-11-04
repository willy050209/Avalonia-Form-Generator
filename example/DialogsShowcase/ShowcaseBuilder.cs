// filepath: example/DialogsShowcase/ShowcaseBuilder.cs
using System.Collections.Immutable;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Models.Common;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace DialogsShowcase;

/// <summary>
/// 對話方塊與訊息視窗展示範例 (DialogsShowcase) 的純 C# AST 建構器。
/// <para>
/// 本類別示範如何使用 AFG 核心中介模型（<see cref="FormDocument"/> 與 <see cref="AstNode"/>），
/// 完全透過 C# 物件初值設定式建置現代化、深色主題的 UI 語意樹，
/// 並透過 <see cref="AfgSerializer"/> 進行序列化，或調用 <see cref="ProjectExportService"/> 產出跨平台專案原始碼。
/// </para>
/// </summary>
public static class ShowcaseBuilder
{
    /// <summary>
    /// 建構完整的 <see cref="FormDocument"/> 表單文件物件模型。
    /// 包含視窗設定、RootCanvas 根畫布、四大現代卡片佈局，以及非可視對話框服務元件。
    /// </summary>
    /// <returns>代表此表單規格的完整 <see cref="FormDocument"/> 實例。</returns>
    public static FormDocument BuildDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "DialogsShowcase",
            RootNamespace = "DialogsShowcase",
            ViewClassName = "DialogsShowcaseView",
            ViewModelClassName = "DialogsShowcaseViewModel",
            Title = "對話方塊與訊息視窗展示",
            BackgroundColor = "#18181B",
            CanvasWidth = 600,
            CanvasHeight = 680,
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
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Width = 600,
                Height = 680,
                Children =
                [
                    // 1. 頂部主副標題資訊卡片 (Header Card)
                    BuildHeaderCard(),

                    // 2. 訊息與確認對話方塊展示卡片 (MessageBox: Alert & Confirm)
                    BuildMessageBoxCard(),

                    // 3. 檔案選擇與另存新檔對話方塊卡片 (File Dialogs: Open & Save)
                    BuildFileDialogCard(),

                    // 4. 即時對話方塊調用歷程紀錄卡片 (Dialog Invocation History Log)
                    BuildLogCard(),

                    // 5. 不可視元件：開啟檔案對話方塊服務 (OpenFileDialog Component)
                    new AstNode
                    {
                        Id = "openDlg",
                        Name = "OpenFileDialog",
                        Type = ControlType.OpenFileDialog
                    },

                    // 6. 不可視元件：儲存檔案對話方塊服務 (SaveFileDialog Component)
                    new AstNode
                    {
                        Id = "saveDlg",
                        Name = "SaveFileDialog",
                        Type = ControlType.SaveFileDialog
                    },

                    // 7. 不可視元件：訊息快顯視窗服務 (MessageBox Component)
                    new AstNode
                    {
                        Id = "msgBox",
                        Name = "MessageBox",
                        Type = ControlType.MessageBox
                    }
                ]
            }
        };
    }

    /// <summary>
    /// 建立「頂部主副標題卡片」節點。
    /// 包含深色背景、圓角、立體陰影與漸層感文字標題。
    /// </summary>
    private static AstNode BuildHeaderCard()
    {
        return new AstNode
        {
            Id = "headerCard",
            Name = "headerCard",
            Type = ControlType.Border,
            CanvasLeft = 20,
            CanvasTop = 20,
            Width = 560,
            Height = 70,
            Background = "#27272A",
            CornerRadius = CornerRadiusModel.Uniform(10),
            BoxShadow = new BoxShadowModel
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 8,
                Spread = 0,
                Color = "#40000000"
            },
            Children =
            [
                // 主標題：20pt Bold 亮藍紫色字體
                new AstNode
                {
                    Id = "txtHeaderTitle",
                    Name = "txtHeaderTitle",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 12,
                    Text = "對話方塊與訊息視窗展示",
                    Foreground = "#818CF8",
                    FontSize = 20,
                    FontWeight = "Bold"
                },
                // 副標題：12pt 柔和灰藍字體
                new AstNode
                {
                    Id = "txtHeaderSubtitle",
                    Name = "txtHeaderSubtitle",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 40,
                    Text = "MessageBox, OpenFileDialog, SaveFileDialog & IDialogService",
                    Foreground = "#94A3B8",
                    FontSize = 12
                }
            ]
        };
    }

    /// <summary>
    /// 建立「訊息提示與確認對話方塊」展示卡片。
    /// 包含 Alert 與 Confirm 雙按鈕，以及接收回傳結果的狀態標籤。
    /// </summary>
    private static AstNode BuildMessageBoxCard()
    {
        return new AstNode
        {
            Id = "msgBoxCard",
            Name = "msgBoxCard",
            Type = ControlType.Border,
            CanvasLeft = 20,
            CanvasTop = 105,
            Width = 560,
            Height = 155,
            Background = "#27272A",
            CornerRadius = CornerRadiusModel.Uniform(10),
            BoxShadow = new BoxShadowModel
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 8,
                Spread = 0,
                Color = "#40000000"
            },
            Children =
            [
                // 區塊小標題
                new AstNode
                {
                    Id = "lblSec1",
                    Name = "lblSec1",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 15,
                    Text = "訊息對話視窗 (MessageBox Demonstrations)",
                    Foreground = "#38BDF8",
                    FontSize = 14,
                    FontWeight = "Bold"
                },
                // 提示按鈕：綁定 ShowAlertCommand 非同步命令
                new AstNode
                {
                    Id = "btnAlert",
                    Name = "btnAlert",
                    Type = ControlType.Button,
                    CanvasLeft = 20,
                    CanvasTop = 50,
                    Width = 240,
                    Height = 38,
                    Content = "💬 顯示提示對話框 (Alert)",
                    Background = "#0284C7",
                    Foreground = "#FFFFFF",
                    FontSize = 13,
                    CornerRadius = CornerRadiusModel.Uniform(8),
                    Events =
                    [
                        new EventMappingDefinition
                        {
                            EventName = "Click",
                            CommandProperty = "ShowAlertCommand",
                            IsAsync = true
                        }
                    ]
                },
                // 確認按鈕：綁定 ShowConfirmCommand 非同步命令
                new AstNode
                {
                    Id = "btnConfirm",
                    Name = "btnConfirm",
                    Type = ControlType.Button,
                    CanvasLeft = 280,
                    CanvasTop = 50,
                    Width = 240,
                    Height = 38,
                    Content = "❓ 顯示確認對話框 (Confirm)",
                    Background = "#E11D48",
                    Foreground = "#FFFFFF",
                    FontSize = 13,
                    CornerRadius = CornerRadiusModel.Uniform(8),
                    Events =
                    [
                        new EventMappingDefinition
                        {
                            EventName = "Click",
                            CommandProperty = "ShowConfirmCommand",
                            IsAsync = true
                        }
                    ]
                },
                // 靜態說明文字
                new AstNode
                {
                    Id = "lblConfirmStatus",
                    Name = "lblConfirmStatus",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 110,
                    Text = "最後操作確認狀態：",
                    Foreground = "#94A3B8",
                    FontSize = 13
                },
                // 動態綁定結果文字：綁定 ViewModel 的 ConfirmResultText
                new AstNode
                {
                    Id = "txtConfirmResult",
                    Name = "txtConfirmResult",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 160,
                    CanvasTop = 110,
                    Text = "使用者點擊了：【確認】(True)",
                    Foreground = "#34D399",
                    FontSize = 13,
                    FontWeight = "Bold",
                    Bindings =
                    [
                        new BindingDefinition
                        {
                            TargetProperty = "Text",
                            ViewModelProperty = "ConfirmResultText",
                            Mode = BindingMode.Default
                        }
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// 建立「檔案選擇與另存新檔對話方塊」卡片。
    /// 包含 OpenFile / SaveFile 按鈕、輸入框雙向資料綁定與清空紀錄按鈕。
    /// </summary>
    private static AstNode BuildFileDialogCard()
    {
        return new AstNode
        {
            Id = "fileDialogCard",
            Name = "fileDialogCard",
            Type = ControlType.Border,
            CanvasLeft = 20,
            CanvasTop = 275,
            Width = 560,
            Height = 220,
            Background = "#27272A",
            CornerRadius = CornerRadiusModel.Uniform(10),
            BoxShadow = new BoxShadowModel
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 8,
                Spread = 0,
                Color = "#40000000"
            },
            Children =
            [
                // 區塊小標題
                new AstNode
                {
                    Id = "lblSec2",
                    Name = "lblSec2",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 15,
                    Text = "檔案選擇與另存新檔 (Open/Save File Dialogs)",
                    Foreground = "#34D399",
                    FontSize = 14,
                    FontWeight = "Bold"
                },
                // 開啟檔案按鈕：綁定 OpenFileCommand
                new AstNode
                {
                    Id = "btnOpenFile",
                    Name = "btnOpenFile",
                    Type = ControlType.Button,
                    CanvasLeft = 20,
                    CanvasTop = 50,
                    Width = 170,
                    Height = 36,
                    Content = "📂 開啟檔案...",
                    Background = "#4F46E5",
                    Foreground = "#FFFFFF",
                    FontSize = 13,
                    CornerRadius = CornerRadiusModel.Uniform(8),
                    Events =
                    [
                        new EventMappingDefinition
                        {
                            EventName = "Click",
                            CommandProperty = "OpenFileCommand",
                            IsAsync = true
                        }
                    ]
                },
                // 開啟路徑文字方塊：雙向綁定 SelectedOpenFilePath
                new AstNode
                {
                    Id = "txtOpenPath",
                    Name = "txtOpenPath",
                    Type = ControlType.TextBox,
                    CanvasLeft = 200,
                    CanvasTop = 50,
                    Width = 340,
                    Height = 36,
                    Watermark = "選取之檔案路徑...",
                    Text = "D:\\Documents\\Projects\\DataReport_2026.json",
                    Bindings =
                    [
                        new BindingDefinition
                        {
                            TargetProperty = "Text",
                            ViewModelProperty = "SelectedOpenFilePath",
                            Mode = BindingMode.TwoWay
                        }
                    ]
                },
                // 另存新檔按鈕：綁定 SaveFileCommand
                new AstNode
                {
                    Id = "btnSaveFile",
                    Name = "btnSaveFile",
                    Type = ControlType.Button,
                    CanvasLeft = 20,
                    CanvasTop = 105,
                    Width = 170,
                    Height = 36,
                    Content = "💾 另存新檔...",
                    Background = "#059669",
                    Foreground = "#FFFFFF",
                    FontSize = 13,
                    CornerRadius = CornerRadiusModel.Uniform(8),
                    Events =
                    [
                        new EventMappingDefinition
                        {
                            EventName = "Click",
                            CommandProperty = "SaveFileCommand",
                            IsAsync = true
                        }
                    ]
                },
                // 另存路徑文字方塊：雙向綁定 SelectedSaveFilePath
                new AstNode
                {
                    Id = "txtSavePath",
                    Name = "txtSavePath",
                    Type = ControlType.TextBox,
                    CanvasLeft = 200,
                    CanvasTop = 105,
                    Width = 340,
                    Height = 36,
                    Watermark = "另存檔案路徑...",
                    Text = "D:\\Exports\\AnnualReport_Export.json",
                    Bindings =
                    [
                        new BindingDefinition
                        {
                            TargetProperty = "Text",
                            ViewModelProperty = "SelectedSaveFilePath",
                            Mode = BindingMode.TwoWay
                        }
                    ]
                },
                // 清空日誌按鈕：綁定 ClearLogsCommand
                new AstNode
                {
                    Id = "btnClearLogs",
                    Name = "btnClearLogs",
                    Type = ControlType.Button,
                    CanvasLeft = 20,
                    CanvasTop = 165,
                    Width = 170,
                    Height = 34,
                    Content = "清除操作紀錄",
                    Background = "#3F3F46",
                    Foreground = "#FFFFFF",
                    FontSize = 12,
                    CornerRadius = CornerRadiusModel.Uniform(6),
                    Events =
                    [
                        new EventMappingDefinition
                        {
                            EventName = "Click",
                            CommandProperty = "ClearLogsCommand",
                            IsAsync = true
                        }
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// 建立「對話方塊調用歷程紀錄卡片」。
    /// 具備發光藍色邊框、多行文字展示面板，並綁定 ViewModel 的 DialogLogText 屬性。
    /// </summary>
    private static AstNode BuildLogCard()
    {
        return new AstNode
        {
            Id = "logCard",
            Name = "logCard",
            Type = ControlType.Border,
            CanvasLeft = 20,
            CanvasTop = 510,
            Width = 560,
            Height = 145,
            Background = "#1E293B",
            BorderBrush = "#3B82F6",
            BorderThickness = ThicknessModel.Uniform(1),
            CornerRadius = CornerRadiusModel.Uniform(10),
            BoxShadow = new BoxShadowModel
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 12,
                Spread = 0,
                Color = "#40000000"
            },
            Children =
            [
                // 日誌區標題
                new AstNode
                {
                    Id = "lblLogTitle",
                    Name = "lblLogTitle",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 12,
                    Text = "對話方塊調用日誌 (Dialog Invocation History)",
                    Foreground = "#60A5FA",
                    FontSize = 13,
                    FontWeight = "Bold"
                },
                // 多行歷程文字區：單向綁定 DialogLogText
                new AstNode
                {
                    Id = "txtLogHistory",
                    Name = "txtLogHistory",
                    Type = ControlType.TextBlock,
                    CanvasLeft = 20,
                    CanvasTop = 40,
                    Width = 520,
                    Height = 90,
                    Text = "[10:00:15] OpenFileDialog: 選取檔案 D:\\Documents\\Projects\\DataReport_2026.json\n[10:00:22] MessageBox: 彈出確認視窗，回傳 True\n[10:00:30] SaveFileDialog: 儲存至 D:\\Exports\\AnnualReport_Export.json",
                    Foreground = "#CBD5E1",
                    FontSize = 12,
                    Bindings =
                    [
                        new BindingDefinition
                        {
                            TargetProperty = "Text",
                            ViewModelProperty = "DialogLogText",
                            Mode = BindingMode.Default
                        }
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// 將 AST 抽象語法樹序列化為格式化 AFG 規格 JSON 字串。
    /// </summary>
    /// <param name="document">要進行序列化的表單文件。</param>
    /// <returns>格式化 JSON 字串。</returns>
    public static string SerializeToJson(FormDocument document)
    {
        return AfgSerializer.SerializeDocument(document);
    }

    /// <summary>
    /// 展示如何使用 <see cref="ProjectExportService"/> 將 AST 模型一鍵匯出為實體可執行的 .NET 方案與專案。
    /// </summary>
    /// <param name="document">表單設計模型。</param>
    /// <param name="outputDirectory">目標輸出目錄路徑。</param>
    public static async Task ExportProjectAsync(FormDocument document, string outputDirectory)
    {
        var exportService = new ProjectExportService();
        var options = new ProjectExportOptions(
            IncludeMobileProject: false,
            CustomProjectName: document.ProjectName ?? "DialogsShowcase"
        );

        await exportService.ExportToFolderAsync(document, outputDirectory, options);
    }
}
