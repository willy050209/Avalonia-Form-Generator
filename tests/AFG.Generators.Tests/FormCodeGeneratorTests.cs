// filepath: tests/AFG.Generators.Tests/FormCodeGeneratorTests.cs
namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 FormCodeGenerator 整合產出 View 與 ViewModel 完整流程。
/// </summary>
public sealed class FormCodeGeneratorTests
{
    private readonly FormCodeGenerator _generator = new();

    [Fact]
    public void GenerateAll_ShouldProduceBothFormattedViewAndViewModelFiles()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "ContactFormView",
            ViewModelClassName = "ContactFormViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "tb_email",
                        Name = "EmailInput",
                        Type = ControlType.TextBox,
                        CanvasLeft = 50,
                        CanvasTop = 100,
                        Width = 300,
                        Watermark = "Enter your email",
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "Email" }
                        ]
                    },
                    new AstNode
                    {
                        Id = "btn_send",
                        Name = "SendButton",
                        Type = ControlType.Button,
                        CanvasLeft = 50,
                        CanvasTop = 160,
                        Content = "Send",
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "SendCommand", IsAsync = true }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.GenerateAll(doc);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Files.Should().HaveCount(2);

        var viewFile = result.Files.FirstOrDefault(f => f.FileType == SourceFileType.View);
        var vmFile = result.Files.FirstOrDefault(f => f.FileType == SourceFileType.ViewModel);

        viewFile.Should().NotBeNull();
        viewFile!.FileName.Should().Be("ContactFormView.cs");
        viewFile.Content.Should().Contain("ContactFormView : UserControl");
        viewFile.Content.Should().Contain(".CanvasLeft(50)");
        viewFile.Content.Should().Contain(".Watermark(\"Enter your email\")");

        vmFile.Should().NotBeNull();
        vmFile!.FileName.Should().Be("ContactFormViewModel.cs");
        vmFile.Content.Should().Contain("ContactFormViewModel : ObservableObject");
        vmFile.Content.Should().Contain("private string _email = string.Empty;");
        vmFile.Content.Should().Contain("private async Task SendAsync()");

        // 語法檢查
        RoslynCompilerService.CheckSyntaxDiagnostics(viewFile.Content).Should().BeEmpty();
        RoslynCompilerService.CheckSyntaxDiagnostics(vmFile.Content).Should().BeEmpty();
    }

    [Fact]
    public void GenerateAll_WithTextBlockBoundToTextBox_ShouldGenerateReactiveBinding()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "SyncFormView",
            ViewModelClassName = "SyncFormViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "tb_input",
                        Name = "InputBox",
                        Type = ControlType.TextBox,
                        Text = "Hello AFG",
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "InputMessage", Mode = BindingMode.TwoWay }
                        ]
                    },
                    new AstNode
                    {
                        Id = "tb_display",
                        Name = "DisplayLabel",
                        Type = ControlType.TextBlock,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "InputMessage", Mode = BindingMode.OneWay }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.GenerateAll(doc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var viewFile = result.Files.First(f => f.FileType == SourceFileType.View);
        var vmFile = result.Files.First(f => f.FileType == SourceFileType.ViewModel);

        // 驗證 View 包含兩者對同一個 ViewModel 屬性的綁定
        viewFile.Content.Should().Contain(".Text(nameof(SyncFormViewModel.InputMessage), BindingMode.TwoWay)");
        viewFile.Content.Should().Contain(".Text(nameof(SyncFormViewModel.InputMessage), BindingMode.OneWay)");

        // 驗證 ViewModel 包含該屬性並初始賦值
        vmFile.Content.Should().Contain("[ObservableProperty]");
        vmFile.Content.Should().Contain("private string _inputMessage = \"Hello AFG\";");

        RoslynCompilerService.CheckSyntaxDiagnostics(viewFile.Content).Should().BeEmpty();
        RoslynCompilerService.CheckSyntaxDiagnostics(vmFile.Content).Should().BeEmpty();
    }
}
