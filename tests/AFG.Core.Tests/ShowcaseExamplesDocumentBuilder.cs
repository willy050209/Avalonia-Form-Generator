// filepath: tests/AFG.Core.Tests/ShowcaseExamplesDocumentBuilder.cs
using System.Collections.Immutable;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Models.Common;
using AFG.Core.Models.Logic;

namespace AFG.Core.Tests;

/// <summary>
/// 建立 7 大擴充實機範例之標準 FormDocument AST 模型建構器。
/// </summary>
public static class ShowcaseExamplesDocumentBuilder
{
    // =========================================================================
    // 1. BasicControlsShowcase (基本常見控制項)
    // =========================================================================
    public static FormDocument CreateBasicControlsDocument()
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
                    // Header Card
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

                    // 1. Text & Input Card
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
                            new AstNode { Id = "lblSec1", Name = "lblSec1", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 10, Text = "文字輸入 (Text & Input Controls)", FontSize = 13, FontWeight = "Bold", Foreground = "#38BDF8" },
                            new AstNode { Id = "lblName", Name = "lblName", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 42, Text = "使用者名稱：", FontSize = 13, Foreground = "#E2E8F0" },
                            new AstNode { Id = "txtName", Name = "txtName", Type = ControlType.TextBox, CanvasLeft = 110, CanvasTop = 36, Width = 170, Height = 32, Text = "陳大明", Watermark = "請輸入姓名", Bindings = [new BindingDefinition("Text", "UserName", BindingMode.TwoWay)] },
                            new AstNode { Id = "lblEmail", Name = "lblEmail", Type = ControlType.TextBlock, CanvasLeft = 300, CanvasTop = 42, Text = "電子郵件：", FontSize = 13, Foreground = "#E2E8F0" },
                            new AstNode { Id = "txtEmail", Name = "txtEmail", Type = ControlType.TextBox, CanvasLeft = 380, CanvasTop = 36, Width = 200, Height = 32, Text = "daming.chen@example.com", Watermark = "請輸入 Email", Bindings = [new BindingDefinition("Text", "Email", BindingMode.TwoWay)] },
                            new AstNode { Id = "lblBio", Name = "lblBio", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 88, Text = "個人簡介：", FontSize = 13, Foreground = "#E2E8F0" },
                            new AstNode { Id = "txtBio", Name = "txtBio", Type = ControlType.TextBox, CanvasLeft = 110, CanvasTop = 82, Width = 470, Height = 32, Text = "熱愛跨平台開發與 Avalonia 現代化介面架構設計。", Watermark = "請輸入簡介", Bindings = [new BindingDefinition("Text", "Bio", BindingMode.TwoWay)] }
                        ]
                    },

                    // 2. Choice & Options Card
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
                            new AstNode { Id = "lblSec2", Name = "lblSec2", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 10, Text = "選項與清單 (Choice & List Controls)", FontSize = 13, FontWeight = "Bold", Foreground = "#34D399" },
                            new AstNode { Id = "chkDarkMode", Name = "chkDarkMode", Type = ControlType.CheckBox, CanvasLeft = 20, CanvasTop = 40, Content = "啟用深色高對比主題", IsChecked = true, Bindings = [new BindingDefinition("IsChecked", "IsDarkMode", BindingMode.TwoWay)] },
                            new AstNode { Id = "chkNotifications", Name = "chkNotifications", Type = ControlType.CheckBox, CanvasLeft = 240, CanvasTop = 40, Content = "接收即時推播通知", IsChecked = true, Bindings = [new BindingDefinition("IsChecked", "ReceiveNotifications", BindingMode.TwoWay)] },
                            new AstNode { Id = "lblAccount", Name = "lblAccount", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 75, Text = "帳戶類型：", FontSize = 13, Foreground = "#E2E8F0" },
                            new AstNode { Id = "rbPersonal", Name = "rbPersonal", Type = ControlType.RadioButton, CanvasLeft = 110, CanvasTop = 73, Content = "個人帳戶", IsChecked = true, Bindings = [new BindingDefinition("IsChecked", "IsPersonalAccount", BindingMode.TwoWay)] },
                            new AstNode { Id = "rbEnterprise", Name = "rbEnterprise", Type = ControlType.RadioButton, CanvasLeft = 240, CanvasTop = 73, Content = "企業組織帳戶", IsChecked = false, Bindings = [new BindingDefinition("IsChecked", "IsEnterpriseAccount", BindingMode.TwoWay)] },
                            new AstNode { Id = "lblRole", Name = "lblRole", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 104, Text = "身分角色：", FontSize = 13, Foreground = "#E2E8F0" },
                            new AstNode { Id = "cmbRole", Name = "cmbRole", Type = ControlType.ComboBox, CanvasLeft = 110, CanvasTop = 98, Width = 200, Height = 28, Content = "資深系統架構師" }
                        ]
                    },

                    // 3. Range & Progress Card
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
                            new AstNode { Id = "lblSec3", Name = "lblSec3", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 10, Text = "數值、進度與日期時間 (Range, Progress & DateTime)", FontSize = 13, FontWeight = "Bold", Foreground = "#FBBF24" },
                            new AstNode { Id = "lblVolume", Name = "lblVolume", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 40, Text = "系統主音量 (85%)：", FontSize = 13, Foreground = "#E2E8F0", Bindings = [new BindingDefinition("Text", "VolumeLabel")] },
                            new AstNode { Id = "sldVolume", Name = "sldVolume", Type = ControlType.Slider, CanvasLeft = 170, CanvasTop = 36, Width = 400, Height = 26, Minimum = 0, Maximum = 100, Value = 85, Bindings = [new BindingDefinition("Value", "VolumeValue", BindingMode.TwoWay)] },
                            new AstNode { Id = "lblProgress", Name = "lblProgress", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 75, Text = "雲端同步進度 (75%)：", FontSize = 13, Foreground = "#E2E8F0", Bindings = [new BindingDefinition("Text", "ProgressLabel")] },
                            new AstNode { Id = "pbProgress", Name = "pbProgress", Type = ControlType.ProgressBar, CanvasLeft = 170, CanvasTop = 75, Width = 400, Height = 16, Minimum = 0, Maximum = 100, Value = 75, Bindings = [new BindingDefinition("Value", "ProgressValue", BindingMode.TwoWay)] },
                            new AstNode { Id = "lblDate", Name = "lblDate", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 108, Text = "排程預約：", FontSize = 13, Foreground = "#E2E8F0" },
                            new AstNode { Id = "dpDate", Name = "dpDate", Type = ControlType.DatePicker, CanvasLeft = 110, CanvasTop = 103, Width = 180, Height = 30 },
                            new AstNode { Id = "tpTime", Name = "tpTime", Type = ControlType.TimePicker, CanvasLeft = 310, CanvasTop = 103, Width = 150, Height = 30 }
                        ]
                    },

                    // 4. Action Buttons
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

                    // 5. Status Summary Card
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
                            new AstNode { Id = "lblStatusTitle", Name = "lblStatusTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 10, Text = "即時設定摘要 (Live Summary)", FontSize = 13, FontWeight = "Bold", Foreground = "#60A5FA" },
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

    // =========================================================================
    // 2. DialogsShowcase (對話方塊與訊息視窗)
    // =========================================================================
    public static FormDocument CreateDialogsDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "DialogsShowcase",
            RootNamespace = "DialogsShowcase",
            Title = "對話方塊與訊息視窗展示",
            ViewClassName = "DialogsShowcaseView",
            ViewModelClassName = "DialogsShowcaseViewModel",
            CanvasWidth = 600,
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
                Width = 600,
                Height = 680,
                Children = [
                    // Header Card
                    new AstNode
                    {
                        Id = "headerCard",
                        Name = "headerCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 20,
                        Width = 560,
                        Height = 70,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "txtHeaderTitle", Name = "txtHeaderTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 12, Text = "對話方塊與訊息視窗展示", FontSize = 20, FontWeight = "Bold", Foreground = "#818CF8" },
                            new AstNode { Id = "txtHeaderSubtitle", Name = "txtHeaderSubtitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 40, Text = "MessageBox, OpenFileDialog, SaveFileDialog & IDialogService", FontSize = 12, Foreground = "#94A3B8" }
                        ]
                    },

                    // 1. MessageBox Demonstrations
                    new AstNode
                    {
                        Id = "msgBoxCard",
                        Name = "msgBoxCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 105,
                        Width = 560,
                        Height = 155,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "lblSec1", Name = "lblSec1", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 15, Text = "訊息對話視窗 (MessageBox Demonstrations)", FontSize = 14, FontWeight = "Bold", Foreground = "#38BDF8" },
                            new AstNode { Id = "btnAlert", Name = "btnAlert", Type = ControlType.Button, CanvasLeft = 20, CanvasTop = 50, Width = 240, Height = 38, Content = "💬 顯示提示對話框 (Alert)", Background = "#0284C7", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(8, 8, 8, 8), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ShowAlertCommand", IsAsync = true }] },
                            new AstNode { Id = "btnConfirm", Name = "btnConfirm", Type = ControlType.Button, CanvasLeft = 280, CanvasTop = 50, Width = 240, Height = 38, Content = "❓ 顯示確認對話框 (Confirm)", Background = "#E11D48", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(8, 8, 8, 8), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ShowConfirmCommand", IsAsync = true }] },
                            new AstNode { Id = "lblConfirmStatus", Name = "lblConfirmStatus", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 110, Text = "最後操作確認狀態：", FontSize = 13, Foreground = "#94A3B8" },
                            new AstNode { Id = "txtConfirmResult", Name = "txtConfirmResult", Type = ControlType.TextBlock, CanvasLeft = 160, CanvasTop = 110, Text = "使用者點擊了：【確認】(True)", FontSize = 13, FontWeight = "Bold", Foreground = "#34D399", Bindings = [new BindingDefinition("Text", "ConfirmResultText")] }
                        ]
                    },

                    // 2. Open/Save File Dialogs
                    new AstNode
                    {
                        Id = "fileDialogCard",
                        Name = "fileDialogCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 275,
                        Width = 560,
                        Height = 220,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "lblSec2", Name = "lblSec2", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 15, Text = "檔案選擇與另存新檔 (Open/Save File Dialogs)", FontSize = 14, FontWeight = "Bold", Foreground = "#34D399" },
                            new AstNode { Id = "btnOpenFile", Name = "btnOpenFile", Type = ControlType.Button, CanvasLeft = 20, CanvasTop = 50, Width = 170, Height = 36, Content = "📂 開啟檔案...", Background = "#4F46E5", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(8, 8, 8, 8), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "OpenFileCommand", IsAsync = true }] },
                            new AstNode { Id = "txtOpenPath", Name = "txtOpenPath", Type = ControlType.TextBox, CanvasLeft = 200, CanvasTop = 50, Width = 340, Height = 36, Text = "D:\\Documents\\Projects\\DataReport_2026.json", Watermark = "選取之檔案路徑...", Bindings = [new BindingDefinition("Text", "SelectedOpenFilePath", BindingMode.TwoWay)] },
                            new AstNode { Id = "btnSaveFile", Name = "btnSaveFile", Type = ControlType.Button, CanvasLeft = 20, CanvasTop = 105, Width = 170, Height = 36, Content = "💾 另存新檔...", Background = "#059669", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(8, 8, 8, 8), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveFileCommand", IsAsync = true }] },
                            new AstNode { Id = "txtSavePath", Name = "txtSavePath", Type = ControlType.TextBox, CanvasLeft = 200, CanvasTop = 105, Width = 340, Height = 36, Text = "D:\\Exports\\AnnualReport_Export.json", Watermark = "另存檔案路徑...", Bindings = [new BindingDefinition("Text", "SelectedSaveFilePath", BindingMode.TwoWay)] },
                            new AstNode { Id = "btnClearLogs", Name = "btnClearLogs", Type = ControlType.Button, CanvasLeft = 20, CanvasTop = 165, Width = 170, Height = 34, Content = "清除操作紀錄", Background = "#3F3F46", Foreground = "#FFFFFF", FontSize = 12, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ClearLogsCommand", IsAsync = true }] }
                        ]
                    },

                    // 3. Log History Card
                    new AstNode
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
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 12, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "lblLogTitle", Name = "lblLogTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 12, Text = "對話方塊調用日誌 (Dialog Invocation History)", FontSize = 13, FontWeight = "Bold", Foreground = "#60A5FA" },
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
                                FontSize = 12,
                                Foreground = "#CBD5E1",
                                Bindings = [new BindingDefinition("Text", "DialogLogText")]
                            }
                        ]
                    },

                    // Non-visual dialog components
                    new AstNode { Id = "openDlg", Name = "OpenFileDialog", Type = ControlType.OpenFileDialog },
                    new AstNode { Id = "saveDlg", Name = "SaveFileDialog", Type = ControlType.SaveFileDialog },
                    new AstNode { Id = "msgBox", Name = "MessageBox", Type = ControlType.MessageBox }
                ]
            }
        };
    }

    // =========================================================================
    // 3. PictureBoxShowcase (圖片展示與縮放模式)
    // =========================================================================
    public static FormDocument CreatePictureBoxDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "PictureBoxShowcase",
            RootNamespace = "PictureBoxShowcase",
            Title = "PictureBox 與圖片卷積展示範例",
            ViewClassName = "PictureBoxShowcaseView",
            ViewModelClassName = "PictureBoxShowcaseViewModel",
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
                    new AstNode
                    {
                        Id = "headerCard",
                        Name = "headerCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 15,
                        Width = 600,
                        Height = 65,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "txtHeaderTitle", Name = "txtHeaderTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 10, Text = "PictureBox 與圖片卷積展示範例", FontSize = 19, FontWeight = "Bold", Foreground = "#818CF8" },
                            new AstNode { Id = "txtHeaderSubtitle", Name = "txtHeaderSubtitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 37, Text = "Stretch Modes, Dynamic Sources & Custom ImageConvolution Filters", FontSize = 12, Foreground = "#94A3B8" }
                        ]
                    },

                    // Viewer Card
                    new AstNode
                    {
                        Id = "viewerCard",
                        Name = "viewerCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 90,
                        Width = 600,
                        Height = 330,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "imgDisplay",
                                Name = "imgDisplay",
                                Type = ControlType.PictureBox,
                                CanvasLeft = 20,
                                CanvasTop = 18,
                                Width = 560,
                                Height = 294,
                                Stretch = Stretch.Uniform,
                                Bindings = [new BindingDefinition("Source", "ImageSource"), new BindingDefinition("Stretch", "StretchMode")]
                            }
                        ]
                    },

                    // Control Card
                    new AstNode
                    {
                        Id = "controlCard",
                        Name = "controlCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 432,
                        Width = 600,
                        Height = 195,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "lblStretchTitle", Name = "lblStretchTitle", Type = ControlType.TextBlock, CanvasLeft = 16, CanvasTop = 12, Text = "縮放呈現模式 (Stretch Mode)：", FontSize = 13, FontWeight = "Bold", Foreground = "#38BDF8" },
                            new AstNode { Id = "rbUniform", Name = "rbUniform", Type = ControlType.RadioButton, CanvasLeft = 16, CanvasTop = 36, Content = "Uniform (等比)", IsChecked = true, Bindings = [new BindingDefinition("IsChecked", "IsUniformStretch", BindingMode.TwoWay)] },
                            new AstNode { Id = "rbUniformToFill", Name = "rbUniformToFill", Type = ControlType.RadioButton, CanvasLeft = 160, CanvasTop = 36, Content = "UniformToFill (填滿)", IsChecked = false, Bindings = [new BindingDefinition("IsChecked", "IsUniformToFillStretch", BindingMode.TwoWay)] },
                            new AstNode { Id = "rbFill", Name = "rbFill", Type = ControlType.RadioButton, CanvasLeft = 345, CanvasTop = 36, Content = "Fill (拉伸)", IsChecked = false, Bindings = [new BindingDefinition("IsChecked", "IsFillStretch", BindingMode.TwoWay)] },
                            new AstNode { Id = "rbNone", Name = "rbNone", Type = ControlType.RadioButton, CanvasLeft = 475, CanvasTop = 36, Content = "None (原始)", IsChecked = false, Bindings = [new BindingDefinition("IsChecked", "IsNoneStretch", BindingMode.TwoWay)] },
                            new AstNode { Id = "btnSampleA", Name = "btnSampleA", Type = ControlType.Button, CanvasLeft = 16, CanvasTop = 72, Width = 180, Height = 32, Content = "測試圖形 A (幾何網格)", Background = "#4F46E5", Foreground = "#FFFFFF", FontSize = 12, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "LoadSampleACommand", IsAsync = true }] },
                            new AstNode { Id = "btnSampleB", Name = "btnSampleB", Type = ControlType.Button, CanvasLeft = 206, CanvasTop = 72, Width = 180, Height = 32, Content = "測試圖形 B (漸層色塊)", Background = "#059669", Foreground = "#FFFFFF", FontSize = 12, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "LoadSampleBCommand", IsAsync = true }] },
                            new AstNode { Id = "lblConvTitle", Name = "lblConvTitle", Type = ControlType.TextBlock, CanvasLeft = 16, CanvasTop = 114, Text = "自訂卷積算子與特效 (ImageConvolution)：", FontSize = 13, FontWeight = "Bold", Foreground = "#F472B6" },
                            new AstNode { Id = "btnConvReset", Name = "btnConvReset", Type = ControlType.Button, CanvasLeft = 16, CanvasTop = 140, Width = 108, Height = 34, Content = "原圖 (Reset)", Background = "#475569", Foreground = "#FFFFFF", FontSize = 11, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ResetFilterCommand", IsAsync = false }] },
                            new AstNode { Id = "btnConvSharpen", Name = "btnConvSharpen", Type = ControlType.Button, CanvasLeft = 131, CanvasTop = 140, Width = 108, Height = 34, Content = "銳化 3×3", Background = "#0284C7", Foreground = "#FFFFFF", FontSize = 11, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ApplySharpenCommand", IsAsync = false }] },
                            new AstNode { Id = "btnConvEmboss", Name = "btnConvEmboss", Type = ControlType.Button, CanvasLeft = 246, CanvasTop = 140, Width = 108, Height = 34, Content = "浮雕 3×3", Background = "#7C3AED", Foreground = "#FFFFFF", FontSize = 11, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ApplyEmbossCommand", IsAsync = false }] },
                            new AstNode { Id = "btnConvSobel", Name = "btnConvSobel", Type = ControlType.Button, CanvasLeft = 361, CanvasTop = 140, Width = 108, Height = 34, Content = "Sobel 邊緣", Background = "#EA580C", Foreground = "#FFFFFF", FontSize = 11, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ApplySobelCommand", IsAsync = false }] },
                            new AstNode { Id = "btnConvCustom", Name = "btnConvCustom", Type = ControlType.Button, CanvasLeft = 476, CanvasTop = 140, Width = 108, Height = 34, Content = "自訂 5×5", Background = "#DB2777", Foreground = "#FFFFFF", FontSize = 11, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ApplyCustomKernelCommand", IsAsync = false }] }
                        ]
                    },

                    // Info Card
                    new AstNode
                    {
                        Id = "infoCard",
                        Name = "infoCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 640,
                        Width = 600,
                        Height = 65,
                        Background = "#1E293B",
                        BorderBrush = "#3B82F6",
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                        Children = [
                            new AstNode
                            {
                                Id = "txtImageInfo",
                                Name = "txtImageInfo",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 16,
                                CanvasTop = 22,
                                Text = "圖片：800×600 | 模式：Stretch.Uniform | 樣本 A (科技幾何網格) | 濾鏡：原圖",
                                Foreground = "#E2E8F0",
                                FontSize = 12,
                                Bindings = [new BindingDefinition("Text", "ImageInfoText")]
                            }
                        ]
                    }
                ]
            }
        };
    }

    // =========================================================================
    // 4. VectorGraphicsShowcase (擴充圖形函數與向量繪圖)
    // =========================================================================
    public static FormDocument CreateVectorGraphicsDocument()
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
                    new AstNode
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
                            new AstNode { Id = "txtHeaderTitle", Name = "txtHeaderTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 12, Text = "擴充圖形函數與向量繪圖範例", FontSize = 20, FontWeight = "Bold", Foreground = "#818CF8" },
                            new AstNode { Id = "txtHeaderSubtitle", Name = "txtHeaderSubtitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 40, Text = "Vector Badges, Layered Borders, Gauges & Dynamic Charts", FontSize = 12, Foreground = "#94A3B8" }
                        ]
                    },

                    // Metric Cards (3 items)
                    new AstNode
                    {
                        Id = "metricCard1",
                        Name = "metricCard1",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 105,
                        Width = 190,
                        Height = 115,
                        Background = "#312E81",
                        CornerRadius = new CornerRadiusModel(12, 12, 12, 12),
                        BoxShadow = new BoxShadowModel(0, 4, 12, 0, "#504F46E5", false),
                        Children = [
                            new AstNode { Id = "lblM1", Name = "lblM1", Type = ControlType.TextBlock, CanvasLeft = 16, CanvasTop = 14, Text = "⚡ CPU 使用率", FontSize = 13, Foreground = "#A5B4FC" },
                            new AstNode { Id = "valM1", Name = "valM1", Type = ControlType.TextBlock, CanvasLeft = 16, CanvasTop = 38, Text = "42%", FontSize = 28, FontWeight = "Bold", Foreground = "#FFFFFF" },
                            new AstNode { Id = "barM1", Name = "barM1", Type = ControlType.Border, CanvasLeft = 16, CanvasTop = 85, Width = 150, Height = 6, Background = "#4338CA", CornerRadius = new CornerRadiusModel(3, 3, 3, 3) }
                        ]
                    },
                    new AstNode
                    {
                        Id = "metricCard2",
                        Name = "metricCard2",
                        Type = ControlType.Border,
                        CanvasLeft = 225,
                        CanvasTop = 105,
                        Width = 190,
                        Height = 115,
                        Background = "#064E3B",
                        CornerRadius = new CornerRadiusModel(12, 12, 12, 12),
                        BoxShadow = new BoxShadowModel(0, 4, 12, 0, "#50059669", false),
                        Children = [
                            new AstNode { Id = "lblM2", Name = "lblM2", Type = ControlType.TextBlock, CanvasLeft = 16, CanvasTop = 14, Text = "💾 記憶體負載", FontSize = 13, Foreground = "#6EE7B7" },
                            new AstNode { Id = "valM2", Name = "valM2", Type = ControlType.TextBlock, CanvasLeft = 16, CanvasTop = 38, Text = "68%", FontSize = 28, FontWeight = "Bold", Foreground = "#FFFFFF" },
                            new AstNode { Id = "barM2", Name = "barM2", Type = ControlType.Border, CanvasLeft = 16, CanvasTop = 85, Width = 150, Height = 6, Background = "#047857", CornerRadius = new CornerRadiusModel(3, 3, 3, 3) }
                        ]
                    },
                    new AstNode
                    {
                        Id = "metricCard3",
                        Name = "metricCard3",
                        Type = ControlType.Border,
                        CanvasLeft = 430,
                        CanvasTop = 105,
                        Width = 190,
                        Height = 115,
                        Background = "#701A75",
                        CornerRadius = new CornerRadiusModel(12, 12, 12, 12),
                        BoxShadow = new BoxShadowModel(0, 4, 12, 0, "#50C026D3", false),
                        Children = [
                            new AstNode { Id = "lblM3", Name = "lblM3", Type = ControlType.TextBlock, CanvasLeft = 16, CanvasTop = 14, Text = "🌐 網路傳輸", FontSize = 13, Foreground = "#F0ABFC" },
                            new AstNode { Id = "valM3", Name = "valM3", Type = ControlType.TextBlock, CanvasLeft = 16, CanvasTop = 38, Text = "1.2 Gbps", FontSize = 26, FontWeight = "Bold", Foreground = "#FFFFFF" },
                            new AstNode { Id = "barM3", Name = "barM3", Type = ControlType.Border, CanvasLeft = 16, CanvasTop = 85, Width = 150, Height = 6, Background = "#A21CAF", CornerRadius = new CornerRadiusModel(3, 3, 3, 3) }
                        ]
                    },

                    // Chart Card
                    new AstNode
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
                            new AstNode { Id = "lblChartTitle", Name = "lblChartTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 15, Text = "即時向量柱狀分析圖 (Vector Bar Chart)", FontSize = 14, FontWeight = "Bold", Foreground = "#F8FAFC" },
                            // 6 vector bars
                            new AstNode { Id = "vBar1", Name = "vBar1", Type = ControlType.Border, CanvasLeft = 50, CanvasTop = 140, Width = 55, Height = 130, Background = "#4F46E5", CornerRadius = new CornerRadiusModel(6, 6, 0, 0) },
                            new AstNode { Id = "vBar2", Name = "vBar2", Type = ControlType.Border, CanvasLeft = 135, CanvasTop = 90, Width = 55, Height = 180, Background = "#06B6D4", CornerRadius = new CornerRadiusModel(6, 6, 0, 0) },
                            new AstNode { Id = "vBar3", Name = "vBar3", Type = ControlType.Border, CanvasLeft = 220, CanvasTop = 50, Width = 55, Height = 220, Background = "#10B981", CornerRadius = new CornerRadiusModel(6, 6, 0, 0) },
                            new AstNode { Id = "vBar4", Name = "vBar4", Type = ControlType.Border, CanvasLeft = 305, CanvasTop = 110, Width = 55, Height = 160, Background = "#F59E0B", CornerRadius = new CornerRadiusModel(6, 6, 0, 0) },
                            new AstNode { Id = "vBar5", Name = "vBar5", Type = ControlType.Border, CanvasLeft = 390, CanvasTop = 40, Width = 55, Height = 230, Background = "#EC4899", CornerRadius = new CornerRadiusModel(6, 6, 0, 0) },
                            new AstNode { Id = "vBar6", Name = "vBar6", Type = ControlType.Border, CanvasLeft = 475, CanvasTop = 70, Width = 55, Height = 200, Background = "#8B5CF6", CornerRadius = new CornerRadiusModel(6, 6, 0, 0) },
                            // Base Axis
                            new AstNode { Id = "axisLine", Name = "axisLine", Type = ControlType.Border, CanvasLeft = 35, CanvasTop = 270, Width = 530, Height = 2, Background = "#4B5563" },
                            // Axis Labels
                            new AstNode { Id = "q1", Name = "q1", Type = ControlType.TextBlock, CanvasLeft = 65, CanvasTop = 280, Text = "Q1", FontSize = 12, Foreground = "#9CA3AF" },
                            new AstNode { Id = "q2", Name = "q2", Type = ControlType.TextBlock, CanvasLeft = 150, CanvasTop = 280, Text = "Q2", FontSize = 12, Foreground = "#9CA3AF" },
                            new AstNode { Id = "q3", Name = "q3", Type = ControlType.TextBlock, CanvasLeft = 235, CanvasTop = 280, Text = "Q3", FontSize = 12, Foreground = "#9CA3AF" },
                            new AstNode { Id = "q4", Name = "q4", Type = ControlType.TextBlock, CanvasLeft = 320, CanvasTop = 280, Text = "Q4", FontSize = 12, Foreground = "#9CA3AF" },
                            new AstNode { Id = "q5", Name = "q5", Type = ControlType.TextBlock, CanvasLeft = 405, CanvasTop = 280, Text = "Q5", FontSize = 12, Foreground = "#9CA3AF" },
                            new AstNode { Id = "q6", Name = "q6", Type = ControlType.TextBlock, CanvasLeft = 490, CanvasTop = 280, Text = "Q6", FontSize = 12, Foreground = "#9CA3AF" }
                        ]
                    },

                    // Control Card
                    new AstNode
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
                            new AstNode { Id = "lblScale", Name = "lblScale", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 14, Text = "向量圖表振幅比例 (Scale: 100%)：", Foreground = "#60A5FA", FontSize = 13, Bindings = [new BindingDefinition("Text", "ScaleLabel")] },
                            new AstNode { Id = "sldScale", Name = "sldScale", Type = ControlType.Slider, CanvasLeft = 20, CanvasTop = 40, Width = 240, Height = 30, Minimum = 20, Maximum = 150, Value = 100, Bindings = [new BindingDefinition("Value", "ChartScale", BindingMode.TwoWay)] },
                            new AstNode { Id = "btnRandomize", Name = "btnRandomize", Type = ControlType.Button, CanvasLeft = 280, CanvasTop = 38, Width = 140, Height = 36, Content = "🎲 隨機動態資料", Background = "#4F46E5", Foreground = "#FFFFFF", FontSize = 12, CornerRadius = new CornerRadiusModel(8, 8, 8, 8), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "RandomizeDataCommand", IsAsync = true }] },
                            new AstNode { Id = "btnSwitchTheme", Name = "btnSwitchTheme", Type = ControlType.Button, CanvasLeft = 435, CanvasTop = 38, Width = 145, Height = 36, Content = "🎨 切換幾何主題", Background = "#059669", Foreground = "#FFFFFF", FontSize = 12, CornerRadius = new CornerRadiusModel(8, 8, 8, 8), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SwitchThemeCommand", IsAsync = false }] },
                            new AstNode { Id = "txtThemeDesc", Name = "txtThemeDesc", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 82, Text = "幾何主題：科技晶片多邊形 (Cyberpunk Neon) | 拓撲演算法：8 節點向量路徑", Foreground = "#94A3B8", FontSize = 11, Bindings = [new BindingDefinition("Text", "ThemeDescription")] }
                        ]
                    }
                ]
            }
        };
    }

    // =========================================================================
    // 5. HighPerformanceDrawingShowcase (表單高效繪圖與 C++ Skia 橋接)
    // =========================================================================
    public static FormDocument CreateHighPerformanceDrawingDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "HighPerformanceDrawingShowcase",
            RootNamespace = "HighPerformanceDrawingShowcase",
            Title = "表單高效繪圖與 C++ Skia 橋接範例",
            ViewClassName = "HighPerformanceDrawingShowcaseView",
            ViewModelClassName = "HighPerformanceDrawingShowcaseViewModel",
            CanvasWidth = 680,
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
                Width = 680,
                Height = 680,
                Children = [
                    new AstNode
                    {
                        Id = "headerCard",
                        Name = "headerCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 15,
                        Width = 640,
                        Height = 60,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "txtHeaderTitle", Name = "txtHeaderTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 8, Text = "表單高效繪圖與 C++ Skia 橋接範例", FontSize = 18, FontWeight = "Bold", Foreground = "#818CF8" },
                            new AstNode { Id = "txtHeaderSubtitle", Name = "txtHeaderSubtitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 34, Text = "60 FPS Real-time Waveform Oscilloscope via Native C++ & WriteableBitmap", FontSize = 12, Foreground = "#94A3B8" }
                        ]
                    },

                    // Oscilloscope Screen Card
                    new AstNode
                    {
                        Id = "screenCard",
                        Name = "screenCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 85,
                        Width = 640,
                        Height = 310,
                        Background = "#09090B",
                        BorderBrush = "#22C55E",
                        BorderThickness = new ThicknessModel(2, 2, 2, 2),
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 0, 20, 0, "#3022C55E", false),
                        Children = [
                            new AstNode
                            {
                                Id = "imgOscilloscope",
                                Name = "imgOscilloscope",
                                Type = ControlType.Image,
                                CanvasLeft = 10,
                                CanvasTop = 10,
                                Width = 620,
                                Height = 290,
                                Stretch = Stretch.Fill,
                                Bindings = [new BindingDefinition("Source", "WaveformImageSource")]
                            }
                        ]
                    },

                    // Controls Card
                    new AstNode
                    {
                        Id = "ctrlCard",
                        Name = "ctrlCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 405,
                        Width = 640,
                        Height = 155,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "lblFreq", Name = "lblFreq", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 15, Text = "訊號頻率 (Frequency: 5.0 Hz)：", Foreground = "#86EFAC", FontSize = 13, Bindings = [new BindingDefinition("Text", "FrequencyLabel")] },
                            new AstNode { Id = "sldFreq", Name = "sldFreq", Type = ControlType.Slider, CanvasLeft = 220, CanvasTop = 12, Width = 400, Height = 28, Minimum = 1, Maximum = 20, Value = 5, Bindings = [new BindingDefinition("Value", "FrequencyValue", BindingMode.TwoWay)] },
                            new AstNode { Id = "lblAmp", Name = "lblAmp", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 55, Text = "訊號振幅 (Amplitude: 80 px)：", Foreground = "#86EFAC", FontSize = 13, Bindings = [new BindingDefinition("Text", "AmplitudeLabel")] },
                            new AstNode { Id = "sldAmp", Name = "sldAmp", Type = ControlType.Slider, CanvasLeft = 220, CanvasTop = 52, Width = 400, Height = 28, Minimum = 10, Maximum = 140, Value = 80, Bindings = [new BindingDefinition("Value", "AmplitudeValue", BindingMode.TwoWay)] },
                            new AstNode { Id = "btnStart", Name = "btnStart", Type = ControlType.Button, CanvasLeft = 20, CanvasTop = 105, Width = 150, Height = 38, Content = "▶ 啟動 60 FPS 刷新", Background = "#16A34A", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(8, 8, 8, 8), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "StartRefreshCommand", IsAsync = true }] },
                            new AstNode { Id = "btnStop", Name = "btnStop", Type = ControlType.Button, CanvasLeft = 185, CanvasTop = 105, Width = 150, Height = 38, Content = "⏹ 停止即時刷新", Background = "#DC2626", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(8, 8, 8, 8), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "StopRefreshCommand", IsAsync = true }] },
                            new AstNode { Id = "rbSine", Name = "rbSine", Type = ControlType.RadioButton, CanvasLeft = 360, CanvasTop = 110, Content = "正弦波", IsChecked = true, Bindings = [new BindingDefinition("IsChecked", "IsSineWave", BindingMode.TwoWay)] },
                            new AstNode { Id = "rbSquare", Name = "rbSquare", Type = ControlType.RadioButton, CanvasLeft = 445, CanvasTop = 110, Content = "方波", IsChecked = false, Bindings = [new BindingDefinition("IsChecked", "IsSquareWave", BindingMode.TwoWay)] },
                            new AstNode { Id = "rbNoise", Name = "rbNoise", Type = ControlType.RadioButton, CanvasLeft = 520, CanvasTop = 110, Content = "諧波雜訊", IsChecked = false, Bindings = [new BindingDefinition("IsChecked", "IsNoiseWave", BindingMode.TwoWay)] }
                        ]
                    },

                    // Telemetry Card
                    new AstNode
                    {
                        Id = "telemetryCard",
                        Name = "telemetryCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 570,
                        Width = 640,
                        Height = 95,
                        Background = "#1E293B",
                        BorderBrush = "#22C55E",
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                        Children = [
                            new AstNode
                            {
                                Id = "txtTelemetry",
                                Name = "txtTelemetry",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 22,
                                Width = 600,
                                Height = 50,
                                Text = "即時效能監控：60.0 FPS | 渲染延遲：< 180 μs | 採樣緩衝區：620 × 290 (179,800 像素) | 橋接方式：C++ DllImport SIMD 向量化計算",
                                Foreground = "#4ADE80",
                                FontSize = 13,
                                FontWeight = "SemiBold",
                                Bindings = [new BindingDefinition("Text", "TelemetryText")]
                            }
                        ]
                    },

                    // LogicFunction (C++ Native)
                    new AstNode
                    {
                        Id = "fnFastSignalRenderer",
                        Name = "FastSignalRendererService",
                        Type = ControlType.LogicFunction,
                        OutputPath = "Services",
                        TargetNamespace = "HighPerformanceDrawingShowcase.Services",
                        TargetLanguage = TargetLanguage.Cpp,
                        LogicFunction = new LogicFunctionDefinition
                        {
                            Name = "ComputeWaveformBuffer",
                            ReturnType = "void",
                            Description = "使用 C++ 原生向量運算填充示波器像素緩衝區。",
                            CustomImplementation = "// C++ high-performance buffer fill loop",
                            Parameters = [
                                new FunctionParameter { Name = "bufferPtr", Type = "long", Description = "像素緩衝區記憶體指標" },
                                new FunctionParameter { Name = "width", Type = "int", Description = "畫布寬度" },
                                new FunctionParameter { Name = "height", Type = "int", Description = "畫布高度" },
                                new FunctionParameter { Name = "frequency", Type = "double", Description = "頻率" },
                                new FunctionParameter { Name = "amplitude", Type = "double", Description = "振幅" },
                                new FunctionParameter { Name = "phase", Type = "double", Description = "相位" }
                            ]
                        }
                    }
                ]
            }
        };
    }

    // =========================================================================
    // 6. MediaPlayerShowcase (多媒體播放器控制項)
    // =========================================================================
    public static FormDocument CreateMediaPlayerDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "MediaPlayerShowcase",
            RootNamespace = "MediaPlayerShowcase",
            Title = "多媒體播放器控制項展示",
            ViewClassName = "MediaPlayerShowcaseView",
            ViewModelClassName = "MediaPlayerShowcaseViewModel",
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
                    new AstNode
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
                            new AstNode { Id = "txtHeaderTitle", Name = "txtHeaderTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 12, Text = "多媒體播放器控制項展示", FontSize = 20, FontWeight = "Bold", Foreground = "#818CF8" },
                            new AstNode { Id = "txtHeaderSubtitle", Name = "txtHeaderSubtitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 40, Text = "Modern MediaPlayerConsole: Playback, Seeking, Volume & Audio Spectrum", FontSize = 12, Foreground = "#94A3B8" }
                        ]
                    },

                    // Media Player Area
                    new AstNode
                    {
                        Id = "playerAreaCard",
                        Name = "playerAreaCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 105,
                        Width = 600,
                        Height = 340,
                        Background = "#09090B",
                        BorderBrush = "#4F46E5",
                        BorderThickness = new ThicknessModel(2, 2, 2, 2),
                        CornerRadius = new CornerRadiusModel(12, 12, 12, 12),
                        BoxShadow = new BoxShadowModel(0, 4, 16, 0, "#404F46E5", false),
                        Children = [
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
                                Volume = 0.8
                            }
                        ]
                    },

                    // Transport & Controls Card
                    new AstNode
                    {
                        Id = "transportCard",
                        Name = "transportCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 460,
                        Width = 600,
                        Height = 165,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "txtTrack", Name = "txtTrack", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 15, Text = "正在播放：Cyberpunk Synthwave - Neon Horizon (Track 01)", FontSize = 13, FontWeight = "Bold", Foreground = "#F8FAFC", Bindings = [new BindingDefinition("Text", "TrackTitle", BindingMode.Default)] },
                            new AstNode { Id = "lblTime", Name = "lblTime", Type = ControlType.TextBlock, CanvasLeft = 490, CanvasTop = 15, Text = "01:14 / 03:32", FontSize = 12, Foreground = "#94A3B8", Bindings = [new BindingDefinition("Text", "TimeDisplay", BindingMode.Default)] },
                            new AstNode { Id = "sldPosition", Name = "sldPosition", Type = ControlType.Slider, CanvasLeft = 20, CanvasTop = 40, Width = 560, Height = 28, Minimum = 0, Maximum = 100, Value = 35, Bindings = [new BindingDefinition("Value", "PositionValue", BindingMode.TwoWay)] },
                            new AstNode { Id = "btnPlay", Name = "btnPlay", Type = ControlType.Button, CanvasLeft = 20, CanvasTop = 80, Width = 90, Height = 36, Content = "▶ 播放", Background = "#4F46E5", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "PlayCommand", IsAsync = true }] },
                            new AstNode { Id = "btnPause", Name = "btnPause", Type = ControlType.Button, CanvasLeft = 120, CanvasTop = 80, Width = 90, Height = 36, Content = "⏸ 暫停", Background = "#3F3F46", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "PauseCommand", IsAsync = true }] },
                            new AstNode { Id = "btnStop", Name = "btnStop", Type = ControlType.Button, CanvasLeft = 220, CanvasTop = 80, Width = 90, Height = 36, Content = "⏹ 停止", Background = "#DC2626", Foreground = "#FFFFFF", FontSize = 13, CornerRadius = new CornerRadiusModel(6, 6, 6, 6), Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "StopCommand", IsAsync = true }] },
                            new AstNode { Id = "lblVol", Name = "lblVol", Type = ControlType.TextBlock, CanvasLeft = 330, CanvasTop = 88, Text = "音量 (80%)：", Foreground = "#E2E8F0", FontSize = 12, Bindings = [new BindingDefinition("Text", "VolumeLabel", BindingMode.Default)] },
                            new AstNode { Id = "sldVol", Name = "sldVol", Type = ControlType.Slider, CanvasLeft = 410, CanvasTop = 84, Width = 170, Height = 26, Minimum = 0, Maximum = 100, Value = 80, Bindings = [new BindingDefinition("Value", "VolumeValue", BindingMode.TwoWay)] },
                            new AstNode { Id = "chkLoop", Name = "chkLoop", Type = ControlType.CheckBox, CanvasLeft = 20, CanvasTop = 126, Content = "循環播放 (Looping)", IsChecked = true, Bindings = [new BindingDefinition("IsChecked", "IsLooping", BindingMode.TwoWay)] }
                        ]
                    },

                    // Status Card
                    new AstNode
                    {
                        Id = "statusCard",
                        Name = "statusCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 640,
                        Width = 600,
                        Height = 60,
                        Background = "#1E293B",
                        BorderBrush = "#3B82F6",
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                        Children = [
                            new AstNode { Id = "txtStatus", Name = "txtStatus", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 20, Text = "播放器狀態：播放中 (Playing) | 格式：AAC / 48kHz Stereo | 輸出：立體聲聲道 | 緩衝：100%", Foreground = "#34D399", FontSize = 12, Bindings = [new BindingDefinition("Text", "PlayStatusText", BindingMode.Default)] }
                        ]
                    }
                ]
            }
        };
    }

    // =========================================================================
    // 7. MultiLanguageInteropShowcase (各語言交互)
    // =========================================================================
    public static FormDocument CreateMultiLanguageInteropDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "MultiLanguageInteropShowcase",
            RootNamespace = "MultiLanguageInteropShowcase",
            Title = "多語言交互協同運算展示",
            ViewClassName = "MultiLanguageInteropShowcaseView",
            ViewModelClassName = "MultiLanguageInteropShowcaseViewModel",
            CanvasWidth = 680,
            CanvasHeight = 780,
            BackgroundColor = "#18181B",
            TargetLanguage = TargetLanguage.CSharp,
            ArchitectureMode = ArchitectureMode.Hybrid,
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Width = 680,
                Height = 780,
                Children = [
                    new AstNode
                    {
                        Id = "headerCard",
                        Name = "headerCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 20,
                        Width = 640,
                        Height = 70,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode { Id = "txtHeaderTitle", Name = "txtHeaderTitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 12, Text = "多語言交互協同運算展示", FontSize = 20, FontWeight = "Bold", Foreground = "#818CF8" },
                            new AstNode { Id = "txtHeaderSubtitle", Name = "txtHeaderSubtitle", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 40, Text = "Polyglot Architecture: C# Host + F# Logic + VB.NET Logic + C++ Native DllImport", FontSize = 12, Foreground = "#94A3B8" }
                        ]
                    },

                    // 1. F# Card
                    new AstNode
                    {
                        Id = "fsharpCard",
                        Name = "fsharpCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 105,
                        Width = 640,
                        Height = 135,
                        Background = "#1E1B4B",
                        BorderBrush = "#6366F1",
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        Children = [
                            new AstNode { Id = "lblF1", Name = "lblF1", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 15, Text = "🔵 F# 函數式計算專案 (.Logic.FSharp)", FontSize = 14, FontWeight = "Bold", Foreground = "#A5B4FC" },
                            new AstNode { Id = "lblF2", Name = "lblF2", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 45, Text = "參數：初始本金 $100,000 | 複合年增長率 7.5% | 投資期間 10 年", FontSize = 12, Foreground = "#C7D2FE" },
                            new AstNode { Id = "txtFResult", Name = "txtFResult", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 80, Text = "運算結果：F# 複利現值總額 NT$ 206,103.16 (以純函數式不可變模型計算)", FontSize = 13, FontWeight = "Bold", Foreground = "#818CF8", Bindings = [new BindingDefinition("Text", "FSharpResultText")] }
                        ]
                    },

                    // 2. VB Card
                    new AstNode
                    {
                        Id = "vbCard",
                        Name = "vbCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 255,
                        Width = 640,
                        Height = 135,
                        Background = "#1E293B",
                        BorderBrush = "#0EA5E9",
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        Children = [
                            new AstNode { Id = "lblV1", Name = "lblV1", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 15, Text = "🔷 VB.NET 商業規則專案 (.Logic.VB)", FontSize = 14, FontWeight = "Bold", Foreground = "#7DD3FC" },
                            new AstNode { Id = "lblV2", Name = "lblV2", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 45, Text = "參數：訂單編號 ORD-2026-X99 | 未稅總計 NT$ 8,500.00 | 營業稅率 5.0%", FontSize = 12, Foreground = "#BAE6FD" },
                            new AstNode { Id = "txtVResult", Name = "txtVResult", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 80, Text = "運算結果：VB.NET 格式化檢驗：[核可] 總金額含稅 NT$ 8,925.00 (稅額 NT$ 425.00)", FontSize = 13, FontWeight = "Bold", Foreground = "#38BDF8", Bindings = [new BindingDefinition("Text", "VbResultText")] }
                        ]
                    },

                    // 3. C++ Card
                    new AstNode
                    {
                        Id = "cppCard",
                        Name = "cppCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 405,
                        Width = 640,
                        Height = 135,
                        Background = "#1C1917",
                        BorderBrush = "#F97316",
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        Children = [
                            new AstNode { Id = "lblC1", Name = "lblC1", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 15, Text = "🟠 C++ 原生運算模組 (.Logic.Cpp via DllImport)", FontSize = 14, FontWeight = "Bold", Foreground = "#FDBA74" },
                            new AstNode { Id = "lblC2", Name = "lblC2", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 45, Text = "參數：運算種子 0x7FFFFFFF | SIMD 迴圈次數 1,000,000 次位元運算", FontSize = 12, Foreground = "#FED7AA" },
                            new AstNode { Id = "txtCResult", Name = "txtCResult", Type = ControlType.TextBlock, CanvasLeft = 20, CanvasTop = 80, Text = "運算結果：C++ 原生校驗碼 0x9A4B8F3C (硬體指令極速耗時: 12 μs)", FontSize = 13, FontWeight = "Bold", Foreground = "#FB923C", Bindings = [new BindingDefinition("Text", "CppResultText")] }
                        ]
                    },

                    // Action Card
                    new AstNode
                    {
                        Id = "actionCard",
                        Name = "actionCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 555,
                        Width = 640,
                        Height = 195,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "btnRunAll",
                                Name = "btnRunAll",
                                Type = ControlType.Button,
                                CanvasLeft = 20,
                                CanvasTop = 20,
                                Width = 320,
                                Height = 42,
                                Content = "🚀 執行多語言協同運算 (Run All)",
                                Background = "#4F46E5",
                                Foreground = "#FFFFFF",
                                FontSize = 14,
                                FontWeight = "Bold",
                                CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                                Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "RunAllCommand", IsAsync = true }]
                            },
                            new AstNode
                            {
                                Id = "txtStatus",
                                Name = "txtStatus",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 80,
                                Width = 600,
                                Height = 90,
                                Text = "多語言分散式運算架構已連線就緒。\nC# 視圖模型透過相依性注入容器調用 F#、VB.NET 與 C++ 原生函式庫完成平行計算！",
                                FontSize = 13,
                                Foreground = "#34D399",
                                Bindings = [new BindingDefinition("Text", "StatusText")]
                            }
                        ]
                    },

                    // LogicFunction 1: F#
                    new AstNode
                    {
                        Id = "fnFSharpFinancial",
                        Name = "FSharpFinancialService",
                        Type = ControlType.LogicFunction,
                        OutputPath = "Services",
                        TargetNamespace = "MultiLanguageInteropShowcase.Services",
                        TargetLanguage = TargetLanguage.FSharp,
                        LogicFunction = new LogicFunctionDefinition
                        {
                            Name = "CalculateCompoundGrowth",
                            ReturnType = "double",
                            Description = "使用 F# 函數式計算複利增長現值。",
                            CustomImplementation = "initialCapital * Math.Pow(1.0 + riskRate, float years)",
                            Parameters = [
                                new FunctionParameter { Name = "initialCapital", Type = "double" },
                                new FunctionParameter { Name = "riskRate", Type = "double" },
                                new FunctionParameter { Name = "years", Type = "int" }
                            ]
                        }
                    },

                    // LogicFunction 2: VB
                    new AstNode
                    {
                        Id = "fnVisualBasicOrder",
                        Name = "VisualBasicOrderService",
                        Type = ControlType.LogicFunction,
                        OutputPath = "Services",
                        TargetNamespace = "MultiLanguageInteropShowcase.Services",
                        TargetLanguage = TargetLanguage.VisualBasic,
                        LogicFunction = new LogicFunctionDefinition
                        {
                            Name = "ValidateAndFormatOrder",
                            ReturnType = "string",
                            Description = "使用 VB.NET 驗證訂單並格式化稅額字串。",
                            CustomImplementation = "Dim tax = amount * taxRate\nDim total = amount + tax\nReturn String.Format(\"[核可] 總金額含稅 NT$ {0:N2} (稅額 NT$ {1:N2})\", total, tax)",
                            Parameters = [
                                new FunctionParameter { Name = "orderId", Type = "string" },
                                new FunctionParameter { Name = "amount", Type = "double" },
                                new FunctionParameter { Name = "taxRate", Type = "double" }
                            ]
                        }
                    },

                    // LogicFunction 3: C++
                    new AstNode
                    {
                        Id = "fnCppNativeMath",
                        Name = "CppNativeMathService",
                        Type = ControlType.LogicFunction,
                        OutputPath = "Services",
                        TargetNamespace = "MultiLanguageInteropShowcase.Services",
                        TargetLanguage = TargetLanguage.Cpp,
                        LogicFunction = new LogicFunctionDefinition
                        {
                            Name = "ComputeFastChecksum",
                            ReturnType = "long",
                            Description = "使用 C++ 原生高速運算校驗碼。",
                            CustomImplementation = "long result = dataSeed;\nfor (int i = 0; i < iterations; i++) result = (result ^ (i * 31)) + 0x9A4B;\nreturn result;",
                            Parameters = [
                                new FunctionParameter { Name = "dataSeed", Type = "long" },
                                new FunctionParameter { Name = "iterations", Type = "int" }
                            ]
                        }
                    }
                ]
            }
        };
    }
}
