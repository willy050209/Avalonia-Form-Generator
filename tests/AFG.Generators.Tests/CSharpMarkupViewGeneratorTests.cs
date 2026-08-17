// filepath: tests/AFG.Generators.Tests/CSharpMarkupViewGeneratorTests.cs
namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 C# Declarative UI Markup 生成器產出邏輯與語法正確性。
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
        result.Content.Should().Contain(".Command(nameof(LoginFormViewModel.LoginCommand))");
        result.Content.Should().Contain(".Text(nameof(LoginFormViewModel.Username), BindingMode.TwoWay)");

        // 語法樹診斷檢查
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
