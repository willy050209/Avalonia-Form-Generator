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
        result.Content.Should().Contain("// MainGrid");
        result.Content.Should().Contain("// LoginButton");
        result.Content.Should().Contain("// UsernameBox");
        result.Content.Should().Contain("new Grid()");
        result.Content.Should().Contain(".RowDefinitions(\"Auto\", \"*\")");
        result.Content.Should().Contain(".ColumnDefinitions(\"200\", \"*\")");
        result.Content.Should().Contain(".Content(\"Login\")");
        result.Content.Should().Contain(".OnClick((LoginFormViewModel vm) => vm.LoginCommand)");
        result.Content.Should().Contain(".Text((LoginFormViewModel vm) => vm.Username, BindingMode.TwoWay)");

        // 語法樹診斷檢查
        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldIncludeNodeNameCommentsAboveConstructors()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "TestApp.Views",
            ViewClassName = "SampleFormView",
            ViewModelClassName = "SampleFormViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children =
                [
                    new AstNode { Id = "btn1", Name = "SubmitButton", Type = ControlType.Button, Content = "Submit" },
                    new AstNode { Id = "txt1", Name = "EmailTextBox", Type = ControlType.TextBox }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("// RootCanvas");
        result.Content.Should().Contain("// SubmitButton");
        result.Content.Should().Contain("// EmailTextBox");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenButtonHasText_ShouldGenerateTextCall()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "ButtonView",
            ViewModelClassName = "ButtonViewModel",
            RootNode = new AstNode
            {
                Id = "btn",
                Type = ControlType.Button,
                Text = "Submit",
                Width = 100,
                Height = 30
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Text(\"Submit\")");
        result.Content.Should().NotContain(".Content(\"Submit\")");

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
        result.Content.Should().Contain(".OnClick(nameof(ReflectionBindingViewModel.SaveCommand))");

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
        result.Content.Should().Contain(".OnClick((CompiledBindingViewModel vm) => vm.SaveCommand)");

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
        result.Content.Should().Contain(".Source(BitmapHelper.LoadBitmap(\"avares://MyApp.Views.Shared/Assets/avatar.png\"))");
        result.Content.Should().Contain(".Stretch(Stretch.Uniform)");
        result.Content.Should().Contain(".OnClick((PhotoViewModel vm) => vm.SelectPhotoCommand)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ForPictureBoxWithInitBitmap_ShouldGenerateCreateInitializedBitmap()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "CanvasView",
            ViewModelClassName = "CanvasViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "canvasPic",
                        Name = "DrawingCanvas",
                        Type = ControlType.PictureBox,
                        Width = 400,
                        Height = 300,
                        InitBitmap = true,
                        BitmapBackgroundColor = "#F5F5F5"
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("new Image()");
        result.Content.Should().Contain(".Source(BitmapHelper.CreateInitializedBitmap(400, 300, Brush.Parse(\"#F5F5F5\")))");

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
        result.Content.Should().Contain(".OnClick((ComplexFormViewModel vm) => vm.UpdatePhotoCommand)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenNodeHasAutoSizeTrue_ShouldOmitFixedWidthAndHeight()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "AutoSizeFormView",
            ViewModelClassName = "AutoSizeFormViewModel",
            RootNode = new AstNode
            {
                Id = "btn",
                Type = ControlType.Button,
                Text = "Very Long Dynamic Content Button",
                Width = 200,
                Height = 40,
                AutoSize = true
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Text(\"Very Long Dynamic Content Button\")");
        result.Content.Should().NotContain(".Width(200)");
        result.Content.Should().NotContain(".Height(40)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenNodeContainsSpecialCharacters_ShouldEscapeProperlyAndCompileCleanly()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "SpecialCharView",
            ViewModelClassName = "SpecialCharViewModel",
            RootNode = new AstNode
            {
                Id = "btn",
                Type = ControlType.Button,
                Text = "Welcome \"Admin\" \\ User\r\nNext\tLine",
                Watermark = "Path: C:\\Users\\Name\\",
                Background = "#1E293B"
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Text(\"Welcome \\\"Admin\\\" \\\\ User\\r\\nNext\\tLine\")");
        result.Content.Should().Contain(".Watermark(\"Path: C:\\\\Users\\\\Name\\\\\")");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenNodeIsInStackPanelOrGrid_ShouldOmitCanvasCoordinates()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "StackPanelView",
            ViewModelClassName = "StackPanelViewModel",
            RootNode = new AstNode
            {
                Id = "stack",
                Type = ControlType.StackPanel,
                Children = [
                    new AstNode
                    {
                        Id = "btn",
                        Type = ControlType.Button,
                        Text = "Button in StackPanel",
                        CanvasLeft = 100,
                        CanvasTop = 200
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("new StackPanel()");
        result.Content.Should().Contain("new Button()");
        result.Content.Should().NotContain(".CanvasLeft(100)");
        result.Content.Should().NotContain(".CanvasTop(200)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenBindingToReservedKeyword_ShouldEscapeKeywordWithAtSign()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "KeywordView",
            ViewModelClassName = "KeywordViewModel",
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "txt",
                Type = ControlType.TextBox,
                Bindings = [
                    new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "event" }
                ],
                Events = [
                    new EventMappingDefinition { EventName = "Click", CommandProperty = "class" }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Text((KeywordViewModel vm) => vm.Event");
        result.Content.Should().Contain(".OnClick((KeywordViewModel vm) => vm.ClassCommand");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenEventHasDynamicCommandParameter_ShouldGenerateExpressionCommandWithParameter()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "ParamCommandView",
            ViewModelClassName = "ParamCommandViewModel",
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "btn",
                Type = ControlType.Button,
                Events = [
                    new EventMappingDefinition
                    {
                        EventName = "Click",
                        CommandProperty = "DeleteItemCommand",
                        CommandParameterProperty = "SelectedId",
                        ParameterType = "int",
                        IsConstantParameter = false
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Command((ParamCommandViewModel vm) => vm.DeleteItemCommand, (ParamCommandViewModel vm) => vm.SelectedId)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenEventHasConstantCommandParameter_ShouldGenerateCommandWithConstantValue()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "ConstParamCommandView",
            ViewModelClassName = "ConstParamCommandViewModel",
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "btn",
                Type = ControlType.Button,
                Events = [
                    new EventMappingDefinition
                    {
                        EventName = "Click",
                        CommandProperty = "TriggerActionCommand",
                        CommandParameterProperty = "PermanentDelete",
                        IsConstantParameter = true
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain(".Command((ConstParamCommandViewModel vm) => vm.TriggerActionCommand, \"PermanentDelete\")");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithDebugConsole_ShouldProduceBorderAndListBoxStructure()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "DebugView",
            ViewModelClassName = "DebugViewModel",
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "dbg",
                        Name = "ConsolePanel",
                        Type = ControlType.DebugConsole,
                        Width = 450,
                        Height = 200,
                        CanvasLeft = 20,
                        CanvasTop = 100,
                        Text = "Live System Logs"
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("// ConsolePanel");
        result.Content.Should().Contain("new Border()");
        result.Content.Should().Contain(".Background(Brush.Parse(\"#09090B\"))");
        result.Content.Should().Contain(".Width(450)");
        result.Content.Should().Contain(".Height(200)");
        result.Content.Should().Contain(".CanvasLeft(20)");
        result.Content.Should().Contain(".CanvasTop(100)");
        result.Content.Should().Contain(".Text(\"Live System Logs\")");
        result.Content.Should().Contain(".Command((DebugViewModel vm) => vm.ClearLogsCommand)");
        result.Content.Should().Contain(".ItemsSource((DebugViewModel vm) => vm.LogEntries)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithNestedChildren_ShouldFormatIndentationAndCommentsCorrectly()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.Views",
            ViewClassName = "NestedView",
            ViewModelClassName = "NestedViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children =
                [
                    new AstNode
                    {
                        Id = "btn1",
                        Name = "SubmitBtn",
                        Type = ControlType.Button,
                        Width = 100,
                        Height = 30
                    },
                    new AstNode
                    {
                        Id = "txt1",
                        Name = "InputBox",
                        Type = ControlType.TextBox,
                        Width = 200
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        var normalized = result.Content.Replace("\r\n", "\n");
        normalized.Should().Contain("        // RootCanvas\n        Content = new Canvas()");
        normalized.Should().Contain("            .Children(\n                // SubmitBtn\n                _submitBtn = new Button()\n                    .Name(\"submitBtn\")\n                    .Width(100)\n                    .Height(30),");
        normalized.Should().Contain("                // InputBox\n                _inputBox = new TextBox()\n                    .Name(\"inputBox\")\n                    .Width(200)\n            );");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenDocumentHasFormEvents_ShouldWireUpFormEventsInView()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "FormEventsApp.Views",
            ViewClassName = "EventsFormView",
            ViewModelClassName = "EventsFormViewModel",
            Events = [
                new EventMappingDefinition { EventName = "Loaded", CommandProperty = "FormLoadedCommand" },
                new EventMappingDefinition { EventName = "PointerPressed", CommandProperty = "FormClickedCommand" },
                new EventMappingDefinition { EventName = "SizeChanged", CommandProperty = "FormResizedCommand" }
            ],
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("// 表單事件 (Form Events)");
        result.Content.Should().Contain("Loaded += (sender, e) => (DataContext as EventsFormViewModel)?.FormLoadedCommand.Execute(e);");
        result.Content.Should().Contain("PointerPressed += (sender, e) => (DataContext as EventsFormViewModel)?.FormClickedCommand.Execute(e);");
        result.Content.Should().Contain("SizeChanged += (sender, e) => (DataContext as EventsFormViewModel)?.FormResizedCommand.Execute(e);");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithMediaPlayerControl_ShouldGenerateMediaPlayerFluentCode()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MediaApp.Views",
            ViewClassName = "VideoPlayerView",
            ViewModelClassName = "VideoPlayerViewModel",
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Name = "VideoPlayer",
                        Type = ControlType.MediaPlayer,
                        Width = 640,
                        Height = 360,
                        Source = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                        AutoPlay = true,
                        IsLooping = true,
                        Volume = 0.8,
                        Stretch = Core.Enums.Stretch.UniformToFill,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Source", ViewModelProperty = "VideoSource" },
                            new BindingDefinition { TargetProperty = "Position", ViewModelProperty = "PlaybackPosition", Mode = BindingMode.TwoWay }
                        ],
                        Events = [
                            new EventMappingDefinition { EventName = "MediaOpened", CommandProperty = "OnMediaOpenedCommand" },
                            new EventMappingDefinition { EventName = "FrameCaptured", CommandProperty = "OnSnapshotCommand" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("new MediaPlayerControl()");
        result.Content.Should().Contain(".Width(640)");
        result.Content.Should().Contain(".Height(360)");
        result.Content.Should().Contain(".AutoPlay(true)");
        result.Content.Should().Contain(".IsLooping(true)");
        result.Content.Should().Contain(".Volume(0.8)");
        result.Content.Should().Contain(".Stretch(Stretch.UniformToFill)");
        result.Content.Should().Contain(".Source((VideoPlayerViewModel vm) => vm.VideoSource, BindingMode.Default)");
        result.Content.Should().Contain(".Position((VideoPlayerViewModel vm) => vm.PlaybackPosition, BindingMode.TwoWay)");
        result.Content.Should().Contain(".OnMediaOpened((VideoPlayerViewModel vm) => vm.MediaOpenedCommand)");
        result.Content.Should().Contain(".OnFrameCaptured((VideoPlayerViewModel vm) => vm.SnapshotCommand)");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldThrowArgumentNullException_WhenDocumentIsNull()
    {
        var act = () => _generator.Generate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Generate_WithCodeBehindFriendlyMode_ShouldEmitTypedFields_AndNameScopeAssignments()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "TestApp.Views",
            ViewClassName = "MainView",
            ViewModelClassName = "MainViewModel",
            ArchitectureMode = ArchitectureMode.Hybrid,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "u1",
                        Name = "txtUsername",
                        Type = ControlType.TextBox,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "Username" }]
                    },
                    new AstNode
                    {
                        Id = "b1",
                        Name = "btnSubmit",
                        Type = ControlType.Button,
                        Text = "送出",
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitCommand" }]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert: 類別中宣告強型別私有欄位
        result.Content.Should().Contain("private TextBox _txtUsername;");
        result.Content.Should().Contain("private Button _btnSubmit;");

        // Assert: 在 InitializeComponent 內賦值並註冊 NameScope
        result.Content.Should().Contain("_txtUsername = new TextBox()");
        result.Content.Should().Contain(".Name(\"txtUsername\")");
        result.Content.Should().Contain("_btnSubmit = new Button()");
        result.Content.Should().Contain(".Name(\"btnSubmit\")");

        // Assert: 保留 MVVM 資料綁定與事件命令
        result.Content.Should().Contain(".Text((MainViewModel vm) => vm.Username");
        result.Content.Should().Contain(".OnClick((MainViewModel vm) => vm.SubmitCommand)");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithDuplicateNodeNames_ShouldResolveConflictWithIdSuffix_AndEmitWarningComment()
    {
        // Arrange: 兩個同名為 "txtInput" 的節點
        var doc = new FormDocument
        {
            RootNamespace = "TestApp.Views",
            ViewClassName = "DuplicateView",
            ViewModelClassName = "DuplicateViewModel",
            ArchitectureMode = ArchitectureMode.Hybrid,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "nodeA",
                        Name = "txtInput",
                        Type = ControlType.TextBox
                    },
                    new AstNode
                    {
                        Id = "nodeB",
                        Name = "txtInput",
                        Type = ControlType.TextBox
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert: 第一個使用 _txtInput，第二個自動加上 ID 後綴 _txtInput_nodeB 並輸出警告提示
        result.Content.Should().Contain("private TextBox _txtInput;");
        result.Content.Should().Contain("private TextBox _txtInput_nodeB;");
        result.Content.Should().Contain("// 提示: 控制項名稱 'txtInput' 發生重複，已自動附加 ID 後綴 'nodeB' 以消除衝突保護編譯安全");

        result.Content.Should().Contain("_txtInput = new TextBox()");
        result.Content.Should().Contain(".Name(\"txtInput\")");
        result.Content.Should().Contain("_txtInput_nodeB = new TextBox()");
        result.Content.Should().Contain(".Name(\"txtInput_nodeB\")");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithPureMvvmMode_ShouldNotEmitFields_AndKeepInlineDeclaration()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "TestApp.Views",
            ViewClassName = "PureMvvmView",
            ViewModelClassName = "PureMvvmViewModel",
            ArchitectureMode = ArchitectureMode.PureMvvm, // 關閉 Code-Behind 欄位
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "b1",
                        Name = "btnSubmit",
                        Type = ControlType.Button
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert: 不應包含欄位宣告，維持 Inline
        result.Content.Should().NotContain("private Button _btnSubmit;");
        result.Content.Should().NotContain("_btnSubmit = new Button()");
        result.Content.Should().Contain("new Button()");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_StaticContainersWithoutInteraction_ShouldBeFilteredOutFromFields()
    {
        // Arrange: 靜態無自訂名稱的 StackPanel 與裝飾用的 TextBlock
        var doc = new FormDocument
        {
            RootNamespace = "TestApp.Views",
            ViewClassName = "StaticLayoutView",
            ViewModelClassName = "StaticLayoutViewModel",
            ArchitectureMode = ArchitectureMode.Hybrid,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Name = "StackPanel", // 預設名稱，無事件無綁定
                        Type = ControlType.StackPanel,
                        Children = [
                            new AstNode
                            {
                                Name = "TextBlock", // 預設裝飾文字，無事件無綁定
                                Type = ControlType.TextBlock,
                                Text = "靜態標籤"
                            },
                            new AstNode
                            {
                                Id = "btnSave",
                                Name = "btnSave", // 具名按鈕
                                Type = ControlType.Button,
                                Text = "儲存"
                            }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert: 靜態容器與裝飾文字不產生欄位，僅具名按鈕產生欄位
        result.Content.Should().NotContain("private StackPanel _stackPanel;");
        result.Content.Should().NotContain("private TextBlock _textBlock;");
        result.Content.Should().Contain("private Button _btnSave;");
        result.Content.Should().Contain("_btnSave = new Button()");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithCodeBehindArchitectureMode_ShouldEmitFields_DirectEventHandlers_AndMethodStubs_WithoutViewModelBindings()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "TestApp.Views",
            ViewClassName = "CodeBehindView",
            ViewModelClassName = "CodeBehindViewModel",
            ArchitectureMode = ArchitectureMode.CodeBehind,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "b1",
                        Name = "btnSubmit",
                        Type = ControlType.Button,
                        Text = "送出",
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitCommand" }]
                    },
                    new AstNode
                    {
                        Id = "t1",
                        Name = "txtUsername",
                        Type = ControlType.TextBox,
                        Text = "預設使用者"
                    }
                ]
            },
            Events = [new EventMappingDefinition { EventName = "Loaded", CommandProperty = "LoadedCommand" }]
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert: 欄位宣告與 NameScope
        result.Content.Should().Contain("private Button _btnSubmit;");
        result.Content.Should().Contain("private TextBox _txtUsername;");
        result.Content.Should().Contain("_btnSubmit = new Button()");
        result.Content.Should().Contain(".Name(\"btnSubmit\")");
        result.Content.Should().Contain("_txtUsername = new TextBox()");
        result.Content.Should().Contain(".Name(\"txtUsername\")");

        // Assert: 直接事件處理器綁定
        result.Content.Should().Contain(".OnClick(BtnSubmit_Click)");
        result.Content.Should().Contain("Loaded += CodeBehindView_Loaded;");

        // Assert: 事件處理常式 Method Stubs
        result.Content.Should().Contain("private void BtnSubmit_Click(object? sender, RoutedEventArgs e)");
        result.Content.Should().Contain("private void CodeBehindView_Loaded(object? sender, EventArgs e)");

        // Assert: 不包含 DataContext 或 ViewModel 依賴
        result.Content.Should().NotContain("DataContext");
        result.Content.Should().NotContain("CodeBehindViewModel");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithHybridArchitectureMode_ShouldEmitFields_AndMvvmBindings()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "TestApp.Views",
            ViewClassName = "HybridView",
            ViewModelClassName = "HybridViewModel",
            ArchitectureMode = ArchitectureMode.Hybrid,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "b1",
                        Name = "btnSubmit",
                        Type = ControlType.Button,
                        Text = "送出",
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitCommand" }]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert: 包含欄位宣告與 MVVM RelayCommand 綁定
        result.Content.Should().Contain("private Button _btnSubmit;");
        result.Content.Should().Contain("_btnSubmit = new Button()");
        result.Content.Should().Contain(".Name(\"btnSubmit\")");
        result.Content.Should().Contain(".OnClick((HybridViewModel vm) => vm.SubmitCommand)");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }
}
