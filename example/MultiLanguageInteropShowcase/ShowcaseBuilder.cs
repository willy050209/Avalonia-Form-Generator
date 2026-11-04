// filepath: example/MultiLanguageInteropShowcase/ShowcaseBuilder.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AFG.Core.Models.Ast;
using AFG.Core.Models.Logic;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace MultiLanguageInteropShowcase;

/// <summary>
/// 🌟 AFG 官方多語言交互展示表單建構器 (Polyglot Showcase AST Builder)
/// <para>
/// 展示如何以純 C# 程式碼建構 AST 樹狀模型 (FormDocument)，包含：
/// 1. 現代深色系 (Zinc-900 / #18181B) 響應式卡片版面佈局
/// 2. F# 函數式金融計算服務 (FSharpFinancialService - .Logic.FSharp)
/// 3. VB.NET 商業規則與稅額驗證服務 (VisualBasicOrderService - .Logic.VB)
/// 4. C++ 原生硬體加速校驗碼模組 (CppNativeMathService - .Logic.Cpp / P-Invoke)
/// 5. MVVM 雙向綁定 (FSharpResultText, VbResultText, CppResultText, StatusText)
/// 6. 使用 AfgSerializer 序列化為 JSON，並以 ProjectExportService 匯出全多語言方案檔。
/// </para>
/// </summary>
public static class ShowcaseBuilder
{
    /// <summary>
    /// 建構多語言協同運算展示專案之完整抽象語法樹 (FormDocument AST)。
    /// </summary>
    /// <returns>具備完整 UI 與跨語言邏輯服務節點的 FormDocument 物件實例。</returns>
    public static FormDocument BuildDocument()
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
            EnableDependencyInjection = true,
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Width = 680,
                Height = 780,
                Children = [
                    // =========================================================
                    // 頂部導覽標頭卡片 (Header Card)
                    // =========================================================
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
                            new AstNode
                            {
                                Id = "txtHeaderTitle",
                                Name = "txtHeaderTitle",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 12,
                                Text = "多語言交互協同運算展示",
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
                                Text = "Polyglot Architecture: C# Host + F# Logic + VB.NET Logic + C++ Native DllImport",
                                FontSize = 12,
                                Foreground = "#94A3B8"
                            }
                        ]
                    },

                    // =========================================================
                    // 1. F# 函數式金融計算卡片 (F# Card)
                    // =========================================================
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
                            new AstNode
                            {
                                Id = "lblF1",
                                Name = "lblF1",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 15,
                                Text = "🔵 F# 函數式計算專案 (.Logic.FSharp)",
                                FontSize = 14,
                                FontWeight = "Bold",
                                Foreground = "#A5B4FC"
                            },
                            new AstNode
                            {
                                Id = "lblF2",
                                Name = "lblF2",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 45,
                                Text = "參數：初始本金 $100,000 | 複合年增長率 7.5% | 投資期間 10 年",
                                FontSize = 12,
                                Foreground = "#C7D2FE"
                            },
                            new AstNode
                            {
                                Id = "txtFResult",
                                Name = "txtFResult",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 80,
                                Text = "運算結果：F# 複利現值總額 NT$ 206,103.16 (以純函數式不可變模型計算)",
                                FontSize = 13,
                                FontWeight = "Bold",
                                Foreground = "#818CF8",
                                Bindings = [new BindingDefinition("Text", "FSharpResultText")]
                            }
                        ]
                    },

                    // =========================================================
                    // 2. VB.NET 商業規則與稅額驗證卡片 (VB Card)
                    // =========================================================
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
                            new AstNode
                            {
                                Id = "lblV1",
                                Name = "lblV1",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 15,
                                Text = "🔷 VB.NET 商業規則專案 (.Logic.VB)",
                                FontSize = 14,
                                FontWeight = "Bold",
                                Foreground = "#7DD3FC"
                            },
                            new AstNode
                            {
                                Id = "lblV2",
                                Name = "lblV2",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 45,
                                Text = "參數：訂單編號 ORD-2026-X99 | 未稅總計 NT$ 8,500.00 | 營業稅率 5.0%",
                                FontSize = 12,
                                Foreground = "#BAE6FD"
                            },
                            new AstNode
                            {
                                Id = "txtVResult",
                                Name = "txtVResult",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 80,
                                Text = "運算結果：VB.NET 格式化檢驗：[核可] 總金額含稅 NT$ 8,925.00 (稅額 NT$ 425.00)",
                                FontSize = 13,
                                FontWeight = "Bold",
                                Foreground = "#38BDF8",
                                Bindings = [new BindingDefinition("Text", "VbResultText")]
                            }
                        ]
                    },

                    // =========================================================
                    // 3. C++ 原生硬體加速校驗運算卡片 (C++ Card)
                    // =========================================================
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
                            new AstNode
                            {
                                Id = "lblC1",
                                Name = "lblC1",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 15,
                                Text = "🟠 C++ 原生運算模組 (.Logic.Cpp via DllImport)",
                                FontSize = 14,
                                FontWeight = "Bold",
                                Foreground = "#FDBA74"
                            },
                            new AstNode
                            {
                                Id = "lblC2",
                                Name = "lblC2",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 45,
                                Text = "參數：運算種子 0x7FFFFFFF | SIMD 迴圈次數 1,000,000 次位元運算",
                                FontSize = 12,
                                Foreground = "#FED7AA"
                            },
                            new AstNode
                            {
                                Id = "txtCResult",
                                Name = "txtCResult",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 80,
                                Text = "運算結果：C++ 原生校驗碼 0x9A4B8F3C (硬體指令極速耗時: 12 μs)",
                                FontSize = 13,
                                FontWeight = "Bold",
                                Foreground = "#FB923C",
                                Bindings = [new BindingDefinition("Text", "CppResultText")]
                            }
                        ]
                    },

                    // =========================================================
                    // 執行卡片：協同運算按鈕與即時連線狀態看板 (Action Card)
                    // =========================================================
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
                                Events = [
                                    new EventMappingDefinition
                                    {
                                        EventName = "Click",
                                        CommandProperty = "RunAllCommand",
                                        IsAsync = true
                                    }
                                ]
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

                    // =========================================================
                    // 非視覺化邏輯服務節點 1: F# 函數式金融計算模組
                    // =========================================================
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

                    // =========================================================
                    // 非視覺化邏輯服務節點 2: VB.NET 商業規則與格式化審核模組
                    // =========================================================
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

                    // =========================================================
                    // 非視覺化邏輯服務節點 3: C++ 原生高速位元校驗碼模組
                    // =========================================================
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

    /// <summary>
    /// 將 AST 表單定義序列化並匯出為包含全多語言子專案的完整解決方案 (.slnx)。
    /// </summary>
    /// <param name="outputDirectory">目標輸出目錄路徑。</param>
    public static async Task ExportShowcaseAsync(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        // 1. 建立 AST 表單文件
        var doc = BuildDocument();

        // 2. 序列化為 AFG 標準 JSON 設計檔案
        var json = AfgSerializer.SerializeDocument(doc);
        var jsonFilePath = Path.Combine(outputDirectory, $"{doc.ProjectName}.afg.json");
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(jsonFilePath, json);

        // 3. 調用 ProjectExportService 進行多語言方案生成
        var exportService = new ProjectExportService();
        var exportOptions = new ProjectExportOptions(
            IncludeMobileProject: false,
            CustomProjectName: doc.ProjectName
        );

        await exportService.ExportToFolderAsync(doc, outputDirectory, exportOptions);
        Console.WriteLine($"[AFG Builder] 多語言方案已成功匯出至：{outputDirectory}");
    }
}
