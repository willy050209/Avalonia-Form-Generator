// filepath: tests/AFG.Core.Tests/AfgSerializerTests.cs
using System.Text.Json;

namespace AFG.Core.Tests;

/// <summary>
/// 驗證 AfgSerializer JSON 序列化與反序列化 roundtrip 完整性，包含非同步與同步事件命令定義及損毀檔案防禦。
/// </summary>
public sealed class AfgSerializerTests
{
    [Fact]
    public void Roundtrip_FormDocument_ShouldPreserveAllAttributesAndHierarchy()
    {
        // Arrange
        var originalDoc = new FormDocument
        {
            ViewClassName = "UserRegistrationView",
            ViewModelClassName = "UserRegistrationViewModel",
            Title = "User Registration",
            CanvasWidth = 1024,
            CanvasHeight = 768,
            RootNode = new AstNode
            {
                Id = "root_grid",
                Name = "MainGrid",
                Type = ControlType.Grid,
                RowDefinitions = [GridLengthModel.Auto, GridLengthModel.Star(1)],
                ColumnDefinitions = [GridLengthModel.Pixel(200), GridLengthModel.Star(1)],
                Children = [
                    new AstNode
                    {
                        Id = "btn_submit",
                        Name = "SubmitBtn",
                        Type = ControlType.Button,
                        Content = "Submit",
                        GridRow = 1,
                        GridColumn = 1,
                        Margin = new ThicknessModel(10, 10, 10, 10),
                        Bindings = [
                            new BindingDefinition
                            {
                                TargetProperty = "IsEnabled",
                                ViewModelProperty = "CanSubmit",
                                Mode = BindingMode.OneWay
                            }
                        ],
                        Events = [
                            new EventMappingDefinition
                            {
                                EventName = "Click",
                                CommandProperty = "SubmitCommand",
                                IsAsync = true
                            }
                        ]
                    }
                ]
            }
        };

        // Act
        var json = AfgSerializer.SerializeDocument(originalDoc);
        var deserializedDoc = AfgSerializer.DeserializeDocument(json);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        deserializedDoc.Should().NotBeNull();
        deserializedDoc.ViewClassName.Should().Be(originalDoc.ViewClassName);
        deserializedDoc.ViewModelClassName.Should().Be(originalDoc.ViewModelClassName);
        deserializedDoc.CanvasWidth.Should().Be(originalDoc.CanvasWidth);
        deserializedDoc.RootNode.Children.Should().HaveCount(1);

        var child = deserializedDoc.RootNode.Children[0];
        child.Id.Should().Be("btn_submit");
        child.Content.Should().Be("Submit");
        child.Bindings.Should().HaveCount(1);
        child.Bindings[0].ViewModelProperty.Should().Be("CanSubmit");
        child.Events.Should().HaveCount(1);
        child.Events[0].CommandProperty.Should().Be("SubmitCommand");
        child.Events[0].IsAsync.Should().BeTrue();
    }

    [Fact]
    public void Roundtrip_EventMappingDefinition_ShouldPreserveSyncAndAsyncFlags()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "btnSync",
                        Type = ControlType.Button,
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "ClearCommand", IsAsync = false }]
                    },
                    new AstNode
                    {
                        Id = "btnAsync",
                        Type = ControlType.Button,
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "FetchDataCommand", IsAsync = true }]
                    }
                ]
            }
        };

        // Act
        var json = AfgSerializer.SerializeDocument(doc);
        var result = AfgSerializer.DeserializeDocument(json);

        // Assert
        result.RootNode.Children[0].Events[0].IsAsync.Should().BeFalse();
        result.RootNode.Children[1].Events[0].IsAsync.Should().BeTrue();
    }

    [Fact]
    public void Roundtrip_EventMappingDefinition_ShouldPreserveCommandParameterProperties()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "btnDelete",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition
                            {
                                EventName = "Click",
                                CommandProperty = "DeleteItemCommand",
                                CommandParameterProperty = "SelectedItem.Id",
                                ParameterType = "int",
                                IsConstantParameter = false,
                                IsAsync = true
                            },
                            new EventMappingDefinition
                            {
                                EventName = "Click",
                                CommandProperty = "ConfirmActionCommand",
                                CommandParameterProperty = "HardReset",
                                ParameterType = "string",
                                IsConstantParameter = true,
                                IsAsync = false
                            }
                        ]
                    }
                ]
            }
        };

        // Act
        var json = AfgSerializer.SerializeDocument(doc);
        var result = AfgSerializer.DeserializeDocument(json);

        // Assert
        var events = result.RootNode.Children[0].Events;
        events.Should().HaveCount(2);

        events[0].CommandProperty.Should().Be("DeleteItemCommand");
        events[0].CommandParameterProperty.Should().Be("SelectedItem.Id");
        events[0].ParameterType.Should().Be("int");
        events[0].IsConstantParameter.Should().BeFalse();
        events[0].IsAsync.Should().BeTrue();

        events[1].CommandProperty.Should().Be("ConfirmActionCommand");
        events[1].CommandParameterProperty.Should().Be("HardReset");
        events[1].ParameterType.Should().Be("string");
        events[1].IsConstantParameter.Should().BeTrue();
        events[1].IsAsync.Should().BeFalse();
    }

    [Fact]
    public void Roundtrip_EventMappingDefinition_ShouldPreserveMultipleParameters()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "btnMulti",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition
                            {
                                EventName = "Click",
                                CommandProperty = "ProcessItemCommand",
                                Parameters = [
                                    new EventParameterDefinition("sender", "object?"),
                                    new EventParameterDefinition("e", "RoutedEventArgs"),
                                    new EventParameterDefinition("force", "bool", "true", true)
                                ],
                                IsAsync = true
                            }
                        ]
                    }
                ]
            }
        };

        // Act
        var json = AfgSerializer.SerializeDocument(doc);
        var result = AfgSerializer.DeserializeDocument(json);

        // Assert
        var evt = result.RootNode.Children[0].Events[0];
        evt.Parameters.Should().NotBeNull();
        evt.Parameters.Should().HaveCount(3);
        evt.Parameters![0].Name.Should().Be("sender");
        evt.Parameters[0].Type.Should().Be("object?");
        evt.Parameters[1].Name.Should().Be("e");
        evt.Parameters[1].Type.Should().Be("RoutedEventArgs");
        evt.Parameters[2].Name.Should().Be("force");
        evt.Parameters[2].Type.Should().Be("bool");
        evt.Parameters[2].ValueOrPath.Should().Be("true");
        evt.Parameters[2].IsConstant.Should().BeTrue();
    }

    [Fact]
    public void DeserializeDocument_ShouldThrowJsonException_WhenGivenCorruptJson()
    {
        // Arrange
        var corruptJson = "{ \"ViewClassName\": \"Broken\", \"RootNode\": { \"Type\": ";

        // Act & Assert
        var act = () => AfgSerializer.DeserializeDocument(corruptJson);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void SerializeDocument_ShouldThrowArgumentNullException_WhenInputIsNull()
    {
        var act = () => AfgSerializer.SerializeDocument(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Roundtrip_PictureBoxNode_ShouldPreserveSourceAndStretch()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "pic",
                        Name = "ProductPhoto",
                        Type = ControlType.PictureBox,
                        Width = 320,
                        Height = 240,
                        Source = "images/product.png",
                        Stretch = Stretch.Uniform,
                        UseRelativePath = true,
                        InitBitmap = true,
                        BitmapBackgroundColor = "#FAFAFA"
                    }
                ]
            }
        };

        // Act
        var json = AfgSerializer.SerializeDocument(doc);
        var result = AfgSerializer.DeserializeDocument(json);

        // Assert
        var child = result.RootNode.Children[0];
        child.Type.Should().Be(ControlType.PictureBox);
        child.Name.Should().Be("ProductPhoto");
        child.Width.Should().Be(320);
        child.Height.Should().Be(240);
        child.Source.Should().Be("images/product.png");
        child.Stretch.Should().Be(Stretch.Uniform);
        child.UseRelativePath.Should().BeTrue();
        child.InitBitmap.Should().BeTrue();
        child.BitmapBackgroundColor.Should().Be("#FAFAFA");
    }

    [Fact]
    public void Roundtrip_FormDocument_ShouldPreserveFormAndWindowProperties()
    {
        // Arrange
        var doc = new FormDocument
        {
            Title = "POS 終端收銀系統",
            BackgroundColor = "#2D3748",
            CanvasWidth = 1280,
            CanvasHeight = 800,
            MinWidth = 800,
            MinHeight = 600,
            MaxWidth = 1920,
            MaxHeight = 1080,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowState = WindowState.Normal,
            CanResize = false,
            Topmost = true,
            ShowInTaskbar = true,
            Icon = "Assets/pos_icon.ico",
            SystemDecorations = SystemDecorations.Full
        };

        // Act
        var json = AfgSerializer.SerializeDocument(doc);
        var result = AfgSerializer.DeserializeDocument(json);

        // Assert
        result.Title.Should().Be("POS 終端收銀系統");
        result.BackgroundColor.Should().Be("#2D3748");
        result.CanvasWidth.Should().Be(1280);
        result.CanvasHeight.Should().Be(800);
        result.MinWidth.Should().Be(800);
        result.MinHeight.Should().Be(600);
        result.MaxWidth.Should().Be(1920);
        result.MaxHeight.Should().Be(1080);
        result.WindowStartupLocation.Should().Be(WindowStartupLocation.CenterScreen);
        result.WindowState.Should().Be(WindowState.Normal);
        result.CanResize.Should().BeFalse();
        result.Topmost.Should().BeTrue();
        result.ShowInTaskbar.Should().BeTrue();
        result.Icon.Should().Be("Assets/pos_icon.ico");
        result.SystemDecorations.Should().Be(SystemDecorations.Full);
    }

    [Fact]
    public void Roundtrip_FormDocument_ShouldPreserveGenerateCodeBehindFields()
    {
        // Arrange
        var doc = new FormDocument
        {
            Title = "CodeBehindTest",
            ArchitectureMode = ArchitectureMode.PureMvvm
        };

        // Act
        var json = AfgSerializer.SerializeDocument(doc);
        var result = AfgSerializer.DeserializeDocument(json);

        // Assert
        result.GenerateCodeBehindFields.Should().BeFalse();
        result.ArchitectureMode.Should().Be(ArchitectureMode.PureMvvm);
    }

    [Theory]
    [InlineData(ArchitectureMode.CodeBehind)]
    [InlineData(ArchitectureMode.PureMvvm)]
    [InlineData(ArchitectureMode.Hybrid)]
    public void Roundtrip_FormDocument_ShouldPreserveArchitectureMode(ArchitectureMode mode)
    {
        // Arrange
        var doc = new FormDocument
        {
            Title = "ArchModeTest",
            ArchitectureMode = mode
        };

        // Act
        var json = AfgSerializer.SerializeDocument(doc);
        var result = AfgSerializer.DeserializeDocument(json);

        // Assert
        result.ArchitectureMode.Should().Be(mode);
    }

    [Fact]
    public void DeserializeDocument_ShouldThrowArgumentNullException_WhenInputIsNull()
    {
        var act = () => AfgSerializer.DeserializeDocument(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
