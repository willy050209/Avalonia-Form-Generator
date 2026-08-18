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
    public void DeserializeDocument_ShouldThrowArgumentNullException_WhenInputIsNull()
    {
        var act = () => AfgSerializer.DeserializeDocument(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
