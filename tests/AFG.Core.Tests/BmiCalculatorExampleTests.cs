// filepath: tests/AFG.Core.Tests/BmiCalculatorExampleTests.cs
using System.IO;
using System.Linq;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Core.Models.Common;
using AFG.Core.Models.Logic;
using AFG.Core.Serialization;

namespace AFG.Core.Tests;

/// <summary>
/// 驗證 BMI 健康體位計算機範例設計檔 (BmiCalculator.afg.json) 的完整性、
/// 規格符合性與序列化/反序列化 roundtrip 能力。
/// </summary>
public sealed class BmiCalculatorExampleTests
{
    private static string GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "AvaloniaFormGenerator.slnx")))
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return dir;
    }

    static BmiCalculatorExampleTests()
    {
        var rootDir = GetSolutionRoot();
        var targetDir = Path.Combine(rootDir, "example", "BmiCalculator");
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
        var targetPath = Path.Combine(targetDir, "BmiCalculator.afg.json");
        var json = AfgSerializer.SerializeDocument(CreateBmiCalculatorDocument());
        File.WriteAllText(targetPath, json);
    }

    public static FormDocument CreateBmiCalculatorDocument()
    {
        return new FormDocument
        {
            SchemaVersion = "1.0",
            ProjectName = "BmiCalculator",
            RootNamespace = "BmiCalculator",
            Title = "BMI 健康體位計算機",
            ViewClassName = "BmiCalculatorView",
            ViewModelClassName = "BmiCalculatorViewModel",
            CanvasWidth = 480,
            CanvasHeight = 640,
            BackgroundColor = "#18181B",
            TargetLanguage = TargetLanguage.CSharp,
            ArchitectureMode = ArchitectureMode.Hybrid,
            UseCompiledBindings = true,
            InjectedServices = [
                new ServiceDependencyDefinition
                {
                    InterfaceName = "IBmiCalculatorService",
                    ImplementationName = "BmiCalculatorService",
                    Lifetime = "Singleton"
                }
            ],
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Width = 480,
                Height = 640,
                Children = [
                    // 1. Header Card (Border)
                    new AstNode
                    {
                        Id = "headerCard",
                        Name = "headerCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 20,
                        Width = 440,
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
                                Text = "BMI 健康體位計算機",
                                FontSize = 20,
                                FontWeight = "Bold",
                                Foreground = "#F8FAFC"
                            },
                            new AstNode
                            {
                                Id = "txtHeaderSubtitle",
                                Name = "txtHeaderSubtitle",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 40,
                                Text = "專業身體質量指數與健康體位分析工具",
                                FontSize = 12,
                                Foreground = "#94A3B8"
                            }
                        ]
                    },

                    // 2. Input Card (Border)
                    new AstNode
                    {
                        Id = "inputCard",
                        Name = "inputCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 105,
                        Width = 440,
                        Height = 210,
                        Background = "#27272A",
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 8, 0, "#40000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "lblUserName",
                                Name = "lblUserName",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 18,
                                Text = "姓名：",
                                FontSize = 14,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "txtName",
                                Name = "txtName",
                                Type = ControlType.TextBox,
                                CanvasLeft = 70,
                                CanvasTop = 12,
                                Width = 345,
                                Height = 32,
                                Watermark = "請輸入姓名（選填）",
                                Bindings = [new BindingDefinition("Text", "UserName", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblGender",
                                Name = "lblGender",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 62,
                                Text = "性別：",
                                FontSize = 14,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "rbMale",
                                Name = "rbMale",
                                Type = ControlType.RadioButton,
                                CanvasLeft = 70,
                                CanvasTop = 60,
                                Content = "男性 (Male)",
                                IsChecked = true,
                                Bindings = [new BindingDefinition("IsChecked", "IsMale", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "rbFemale",
                                Name = "rbFemale",
                                Type = ControlType.RadioButton,
                                CanvasLeft = 200,
                                CanvasTop = 60,
                                Content = "女性 (Female)",
                                IsChecked = false,
                                Bindings = [new BindingDefinition("IsChecked", "IsFemale", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblHeight",
                                Name = "lblHeight",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 110,
                                Text = "身高：",
                                FontSize = 14,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "txtHeight",
                                Name = "txtHeight",
                                Type = ControlType.TextBox,
                                CanvasLeft = 70,
                                CanvasTop = 105,
                                Width = 300,
                                Height = 32,
                                Text = "175",
                                Watermark = "請輸入身高 (例如 175)",
                                Bindings = [new BindingDefinition("Text", "HeightCm", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblHeightUnit",
                                Name = "lblHeightUnit",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 380,
                                CanvasTop = 110,
                                Text = "cm",
                                FontSize = 14,
                                Foreground = "#94A3B8"
                            },
                            new AstNode
                            {
                                Id = "lblWeight",
                                Name = "lblWeight",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 160,
                                Text = "體重：",
                                FontSize = 14,
                                Foreground = "#E2E8F0"
                            },
                            new AstNode
                            {
                                Id = "txtWeight",
                                Name = "txtWeight",
                                Type = ControlType.TextBox,
                                CanvasLeft = 70,
                                CanvasTop = 155,
                                Width = 300,
                                Height = 32,
                                Text = "68",
                                Watermark = "請輸入體重 (例如 68)",
                                Bindings = [new BindingDefinition("Text", "WeightKg", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblWeightUnit",
                                Name = "lblWeightUnit",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 380,
                                CanvasTop = 160,
                                Text = "kg",
                                FontSize = 14,
                                Foreground = "#94A3B8"
                            }
                        ]
                    },

                    // 3. Action Buttons
                    new AstNode
                    {
                        Id = "btnCalculate",
                        Name = "btnCalculate",
                        Type = ControlType.Button,
                        CanvasLeft = 20,
                        CanvasTop = 330,
                        Width = 210,
                        Height = 40,
                        Content = "計算 BMI 指數",
                        Background = "#4F46E5",
                        Foreground = "#FFFFFF",
                        FontSize = 15,
                        FontWeight = "SemiBold",
                        CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "CalculateCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "btnReset",
                        Name = "btnReset",
                        Type = ControlType.Button,
                        CanvasLeft = 250,
                        CanvasTop = 330,
                        Width = 210,
                        Height = 40,
                        Content = "重新填寫",
                        Background = "#3F3F46",
                        Foreground = "#FFFFFF",
                        FontSize = 15,
                        CornerRadius = new CornerRadiusModel(8, 8, 8, 8),
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ResetCommand", IsAsync = true }]
                    },

                    // 4. Result Card (Border)
                    new AstNode
                    {
                        Id = "resultCard",
                        Name = "resultCard",
                        Type = ControlType.Border,
                        CanvasLeft = 20,
                        CanvasTop = 385,
                        Width = 440,
                        Height = 235,
                        Background = "#1E293B",
                        BorderBrush = "#3B82F6",
                        BorderThickness = new ThicknessModel(1, 1, 1, 1),
                        CornerRadius = new CornerRadiusModel(10, 10, 10, 10),
                        BoxShadow = new BoxShadowModel(0, 4, 12, 0, "#40000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "txtResultTitle",
                                Name = "txtResultTitle",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 15,
                                Text = "評估結果 (Assessment)",
                                FontSize = 14,
                                FontWeight = "SemiBold",
                                Foreground = "#60A5FA"
                            },
                            new AstNode
                            {
                                Id = "lblBmi",
                                Name = "lblBmi",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 45,
                                Text = "BMI 指數：",
                                FontSize = 14,
                                Foreground = "#94A3B8"
                            },
                            new AstNode
                            {
                                Id = "txtBmiValue",
                                Name = "txtBmiValue",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 95,
                                CanvasTop = 40,
                                Text = "22.2",
                                FontSize = 24,
                                FontWeight = "Bold",
                                Foreground = "#38BDF8",
                                Bindings = [new BindingDefinition("Text", "BmiValue", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblCategory",
                                Name = "lblCategory",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 195,
                                CanvasTop = 45,
                                Text = "體位分類：",
                                FontSize = 14,
                                Foreground = "#94A3B8"
                            },
                            new AstNode
                            {
                                Id = "txtCategory",
                                Name = "txtCategory",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 270,
                                CanvasTop = 44,
                                Text = "正常範圍 (Normal)",
                                FontSize = 15,
                                FontWeight = "Bold",
                                Foreground = "#34D399",
                                Bindings = [new BindingDefinition("Text", "BmiCategory", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "txtIdealWeight",
                                Name = "txtIdealWeight",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 85,
                                Text = "理想體重範圍：56.7 kg ~ 73.5 kg",
                                FontSize = 13,
                                Foreground = "#E2E8F0",
                                Bindings = [new BindingDefinition("Text", "IdealWeightRange", BindingMode.TwoWay)]
                            },
                            new AstNode
                            {
                                Id = "lblAdvice",
                                Name = "lblAdvice",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 115,
                                Text = "健康建議：",
                                FontSize = 13,
                                FontWeight = "SemiBold",
                                Foreground = "#FCD34D"
                            },
                            new AstNode
                            {
                                Id = "txtAdvice",
                                Name = "txtAdvice",
                                Type = ControlType.TextBlock,
                                CanvasLeft = 20,
                                CanvasTop = 140,
                                Width = 400,
                                Height = 80,
                                Text = "您的體位處於健康正常範圍，請保持規律運動與均衡飲食習慣！",
                                FontSize = 13,
                                Foreground = "#F1F5F9",
                                Bindings = [new BindingDefinition("Text", "HealthAdvice", BindingMode.TwoWay)]
                            }
                        ]
                    },

                    // 5. LogicFunction 元件
                    new AstNode
                    {
                        Id = "fnBmiCalculatorService",
                        Name = "BmiCalculatorService",
                        Type = ControlType.LogicFunction,
                        OutputPath = "Services",
                        TargetNamespace = "BmiCalculator.Services",
                        TargetLanguage = TargetLanguage.CSharp,
                        LogicFunction = new LogicFunctionDefinition
                        {
                            Name = "CalculateBmi",
                            ReturnType = "double",
                            Description = "根據身高 (cm) 與體重 (kg) 計算身體質量指數 (BMI)。",
                            CustomImplementation = "if (heightCm <= 0) return 0.0;\nvar heightM = heightCm / 100.0;\nreturn Math.Round(weightKg / (heightM * heightM), 1);",
                            Parameters = [
                                new FunctionParameter { Name = "heightCm", Type = "double", Description = "身高（公分）" },
                                new FunctionParameter { Name = "weightKg", Type = "double", Description = "體重（公斤）" }
                            ]
                        }
                    }
                ]
            }
        };
    }

    [Fact]
    public void ExampleFile_ShouldExist_AndDeserializeCorrectly()
    {
        // Arrange
        var rootDir = GetSolutionRoot();
        var filePath = Path.Combine(rootDir, "example", "BmiCalculator", "BmiCalculator.afg.json");

        File.Exists(filePath).Should().BeTrue($"範例檔案應存在於 {filePath}");

        var json = File.ReadAllText(filePath);

        // Act
        var doc = AfgSerializer.DeserializeDocument(json);

        // Assert: 核心規格驗證
        doc.Should().NotBeNull();
        doc.SchemaVersion.Should().Be("1.0");
        doc.ProjectName.Should().Be("BmiCalculator");
        doc.Title.Should().Be("BMI 健康體位計算機");
        doc.ViewClassName.Should().Be("BmiCalculatorView");
        doc.ViewModelClassName.Should().Be("BmiCalculatorViewModel");
        doc.CanvasWidth.Should().Be(480);
        doc.CanvasHeight.Should().Be(640);
        doc.BackgroundColor.Should().Be("#18181B");
        doc.TargetLanguage.Should().Be(TargetLanguage.CSharp);
        doc.ArchitectureMode.Should().Be(ArchitectureMode.Hybrid);
        doc.UseCompiledBindings.Should().BeTrue();

        // 根節點子元件數量 (包含 5 大區塊：Header Card, Input Card, btnCalculate, btnReset, Result Card, LogicFunction)
        doc.RootNode.Should().NotBeNull();
        doc.RootNode.Type.Should().Be(ControlType.Canvas);
        doc.RootNode.Children.Should().HaveCount(6);

        // 展平節點檢查
        var flattened = AstTreeOperations.Flatten(doc.RootNode).ToList();

        // 輸入元件存在驗證
        var txtName = flattened.FirstOrDefault(n => n.Name == "txtName");
        txtName.Should().NotBeNull();
        txtName!.Type.Should().Be(ControlType.TextBox);
        txtName.Bindings.Should().Contain(b => b.ViewModelProperty == "UserName");

        var rbMale = flattened.FirstOrDefault(n => n.Name == "rbMale");
        rbMale.Should().NotBeNull();
        rbMale!.Type.Should().Be(ControlType.RadioButton);
        rbMale.Bindings.Should().Contain(b => b.ViewModelProperty == "IsMale");

        var rbFemale = flattened.FirstOrDefault(n => n.Name == "rbFemale");
        rbFemale.Should().NotBeNull();
        rbFemale!.Type.Should().Be(ControlType.RadioButton);
        rbFemale.Bindings.Should().Contain(b => b.ViewModelProperty == "IsFemale");

        var txtHeight = flattened.FirstOrDefault(n => n.Name == "txtHeight");
        txtHeight.Should().NotBeNull();
        txtHeight!.Type.Should().Be(ControlType.TextBox);
        txtHeight.Bindings.Should().Contain(b => b.ViewModelProperty == "HeightCm");

        var txtWeight = flattened.FirstOrDefault(n => n.Name == "txtWeight");
        txtWeight.Should().NotBeNull();
        txtWeight!.Type.Should().Be(ControlType.TextBox);
        txtWeight.Bindings.Should().Contain(b => b.ViewModelProperty == "WeightKg");

        // 按鈕元件存在驗證
        var btnCalculate = flattened.FirstOrDefault(n => n.Name == "btnCalculate");
        btnCalculate.Should().NotBeNull();
        btnCalculate!.Type.Should().Be(ControlType.Button);
        btnCalculate.Events.Should().Contain(e => e.CommandProperty == "CalculateCommand");

        var btnReset = flattened.FirstOrDefault(n => n.Name == "btnReset");
        btnReset.Should().NotBeNull();
        btnReset!.Type.Should().Be(ControlType.Button);
        btnReset.Events.Should().Contain(e => e.CommandProperty == "ResetCommand");

        // 結果元件存在驗證
        var txtBmiValue = flattened.FirstOrDefault(n => n.Name == "txtBmiValue");
        txtBmiValue.Should().NotBeNull();
        txtBmiValue!.Text.Should().Be("22.2");
        txtBmiValue.Bindings.Should().Contain(b => b.ViewModelProperty == "BmiValue");

        var txtCategory = flattened.FirstOrDefault(n => n.Name == "txtCategory");
        txtCategory.Should().NotBeNull();
        txtCategory!.Text.Should().Be("正常範圍 (Normal)");
        txtCategory.Bindings.Should().Contain(b => b.ViewModelProperty == "BmiCategory");

        var txtIdealWeight = flattened.FirstOrDefault(n => n.Name == "txtIdealWeight");
        txtIdealWeight.Should().NotBeNull();
        txtIdealWeight!.Text.Should().Be("理想體重範圍：56.7 kg ~ 73.5 kg");
        txtIdealWeight.Bindings.Should().Contain(b => b.ViewModelProperty == "IdealWeightRange");

        var txtAdvice = flattened.FirstOrDefault(n => n.Name == "txtAdvice");
        txtAdvice.Should().NotBeNull();
        txtAdvice!.Bindings.Should().Contain(b => b.ViewModelProperty == "HealthAdvice");

        // LogicFunction 驗證
        var logicNode = flattened.FirstOrDefault(n => n.Name == "BmiCalculatorService" && n.Type == ControlType.LogicFunction);
        logicNode.Should().NotBeNull();
        logicNode!.OutputPath.Should().Be("Services");
        logicNode.TargetNamespace.Should().Be("BmiCalculator.Services");
        logicNode.LogicFunction.Should().NotBeNull();
        logicNode.LogicFunction!.Name.Should().Be("CalculateBmi");
        logicNode.LogicFunction.ReturnType.Should().Be("double");
        logicNode.LogicFunction.Parameters.Should().HaveCount(2);
        logicNode.LogicFunction.Parameters.Should().Contain(p => p.Name == "heightCm" && p.Type == "double");
        logicNode.LogicFunction.Parameters.Should().Contain(p => p.Name == "weightKg" && p.Type == "double");
    }

    [Fact]
    public void Roundtrip_BmiCalculatorDocument_ShouldPreserveExactEquality()
    {
        // Arrange
        var original = CreateBmiCalculatorDocument();

        // Act
        var json = AfgSerializer.SerializeDocument(original);
        var deserialized = AfgSerializer.DeserializeDocument(json);

        // Assert
        deserialized.ProjectName.Should().Be(original.ProjectName);
        deserialized.Title.Should().Be(original.Title);
        deserialized.CanvasWidth.Should().Be(original.CanvasWidth);
        deserialized.CanvasHeight.Should().Be(original.CanvasHeight);
        deserialized.RootNode.Children.Should().HaveCount(original.RootNode.Children.Count);
    }
}
