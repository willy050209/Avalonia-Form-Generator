// filepath: tests/AFG.Generators.Tests/CSharpMarkupViewGeneratorTests.cs
namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 C# Declarative UI Markup 生成器產出邏輯與語法正確性（包含 Compiled / Lambda Bindings 支援）。
/// </summary>
public sealed class CSharpMarkupViewGeneratorTests
{
    private readonly CSharpMarkupViewGenerator _generator = new();

    [Fact]
    public void Generate_ShouldProduceValidUserControlClass_WithChainedProperties()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "LoginFormView",
            ViewModelClassName = "LoginFormViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Name = "MainGrid",
                Type = ControlType.Grid,
                RowDefinitions = [GridLengthModel.Auto, GridLengthModel.Star()],
                ColumnDefinitions = [GridLengthModel.Pixel(200), GridLengthModel.Star()],
                Children = [
                    new AstNode
                    {
                        Id = "btn",
                        Name = "LoginButton",
                        Type = ControlType.Button,
                        Content = "Login",
                        GridRow = 1,
                        GridColumn = 0,
                        Margin = new ThicknessModel(10, 5, 10, 5),
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "LoginCommand" }
                        ]
                    },
                    new AstNode
                    {
                        Id = "txt",
                        Name = "UsernameBox",
                        Type = ControlType.TextBox,
                        GridRow = 0,
                        GridColumn = 1,
                        Bindings = [
                            new BindingDefinition
                            {
                                TargetProperty = "Text",
                                ViewModelProperty = "Username",
                                Mode = BindingMode.TwoWay
                            }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("LoginFormView.cs");
        result.FileType.Should().Be(SourceFileType.View);
        result.Content.Should().Contain("namespace MyApp.Views;");
        result.Content.Should().Contain("public partial class LoginFormView : UserControl");
        result.Content.Should().Contain("new Grid()");
        result.Content.Should().Contain(".RowDefinitions(\"Auto\", \"*\")");
        result.Content.Should().Contain(".ColumnDefinitions(\"200\", \"*\")");
        result.Content.Should().Contain(".Content(\"Login\")");
        result.Content.Should().Contain(".Command((LoginFormViewModel vm) => vm.LoginCommand)");
        result.Content.Should().Contain(".Text((LoginFormViewModel vm) => vm.Username, BindingMode.TwoWay)");

        // 語法樹診斷檢查
        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldSupportReflectionBindings_WhenUseCompiledBindingsIsFalse()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "ReflectionBindingView",
            ViewModelClassName = "ReflectionBindingViewModel",
            UseCompiledBindings = false,
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.StackPanel,
                Children = [
                    new AstNode
                    {
                        Id = "tb",
                        Type = ControlType.TextBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "Title", Mode = BindingMode.TwoWay }
                        ]
                    },
                    new AstNode
                    {
                        Id = "btn",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveCommand" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Text(nameof(ReflectionBindingViewModel.Title), BindingMode.TwoWay)");
        result.Content.Should().Contain(".Command(nameof(ReflectionBindingViewModel.SaveCommand))");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldSupportCompiledBindings_WhenUseCompiledBindingsIsTrue()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "CompiledBindingView",
            ViewModelClassName = "CompiledBindingViewModel",
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.StackPanel,
                Children = [
                    new AstNode
                    {
                        Id = "tb",
                        Type = ControlType.TextBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "Title", Mode = BindingMode.TwoWay }
                        ]
                    },
                    new AstNode
                    {
                        Id = "btn",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveCommand" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Text((CompiledBindingViewModel vm) => vm.Title, BindingMode.TwoWay)");
        result.Content.Should().Contain(".Command((CompiledBindingViewModel vm) => vm.SaveCommand)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ForPictureBox_ShouldGenerateImageWithSourceAndStretch()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "PhotoView",
            ViewModelClassName = "PhotoViewModel",
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "pic1",
                        Name = "AvatarPictureBox",
                        Type = ControlType.PictureBox,
                        Width = 200,
                        Height = 150,
                        Source = "assets/avatar.png",
                        Stretch = Stretch.Uniform,
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "SelectPhotoCommand" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("new Image()");
        result.Content.Should().Contain(".Width(200)");
        result.Content.Should().Contain(".Height(150)");
        result.Content.Should().Contain(".Source(\"assets/avatar.png\")");
        result.Content.Should().Contain(".Stretch(Stretch.Uniform)");
        result.Content.Should().Contain(".Command((PhotoViewModel vm) => vm.SelectPhotoCommand)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void AvaloniaMarkupExtensionsSource_Code_ShouldHaveNoSyntaxErrors()
    {
        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(AvaloniaMarkupExtensionsSource.Code);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithAllControlsCompiledBindings_ShouldProduceValidLambdaExpressions()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "ComplexFormView",
            ViewModelClassName = "ComplexFormViewModel",
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.StackPanel,
                Children = [
                    new AstNode
                    {
                        Id = "tb",
                        Type = ControlType.TextBox,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "Username", Mode = BindingMode.TwoWay }]
                    },
                    new AstNode
                    {
                        Id = "chk",
                        Type = ControlType.CheckBox,
                        Bindings = [new BindingDefinition { TargetProperty = "IsChecked", ViewModelProperty = "IsActive", Mode = BindingMode.TwoWay }]
                    },
                    new AstNode
                    {
                        Id = "sld",
                        Type = ControlType.Slider,
                        Bindings = [new BindingDefinition { TargetProperty = "Value", ViewModelProperty = "VolumeLevel", Mode = BindingMode.TwoWay }]
                    },
                    new AstNode
                    {
                        Id = "cbo",
                        Type = ControlType.ComboBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "ItemsSource", ViewModelProperty = "AvailableOptions" },
                            new BindingDefinition { TargetProperty = "SelectedItem", ViewModelProperty = "SelectedOption", Mode = BindingMode.TwoWay }
                        ]
                    },
                    new AstNode
                    {
                        Id = "pic",
                        Type = ControlType.PictureBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Source", ViewModelProperty = "ProfilePhoto" },
                            new BindingDefinition { TargetProperty = "Stretch", ViewModelProperty = "PhotoStretch" }
                        ],
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "UpdatePhotoCommand" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Text((ComplexFormViewModel vm) => vm.Username, BindingMode.TwoWay)");
        result.Content.Should().Contain(".IsChecked((ComplexFormViewModel vm) => vm.IsActive, BindingMode.TwoWay)");
        result.Content.Should().Contain(".Value((ComplexFormViewModel vm) => vm.VolumeLevel, BindingMode.TwoWay)");
        result.Content.Should().Contain(".ItemsSource((ComplexFormViewModel vm) => vm.AvailableOptions, BindingMode.Default)");
        result.Content.Should().Contain(".SelectedItem((ComplexFormViewModel vm) => vm.SelectedOption, BindingMode.TwoWay)");
        result.Content.Should().Contain(".Source((ComplexFormViewModel vm) => vm.ProfilePhoto, BindingMode.Default)");
        result.Content.Should().Contain(".Stretch((ComplexFormViewModel vm) => vm.PhotoStretch, BindingMode.Default)");
        result.Content.Should().Contain(".Command((ComplexFormViewModel vm) => vm.UpdatePhotoCommand)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldThrowArgumentNullException_WhenDocumentIsNull()
    {
        var act = () => _generator.Generate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
