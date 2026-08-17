// filepath: tests/AFG.Generators.Tests/MvvmViewModelGeneratorTests.cs
namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 CommunityToolkit.Mvvm ViewModel 生成器。
/// </summary>
public sealed class MvvmViewModelGeneratorTests
{
    private readonly MvvmViewModelGenerator _generator = new();

    [Fact]
    public void Generate_ShouldProduceObservablePropertiesAndRelayCommands()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "UserFormViewModel",
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
                            new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "Username" }
                        ]
                    },
                    new AstNode
                    {
                        Id = "cb",
                        Type = ControlType.CheckBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "IsChecked", ViewModelProperty = "IsAdmin" }
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
        result.Should().NotBeNull();
        result.FileName.Should().Be("UserFormViewModel.cs");
        result.FileType.Should().Be(SourceFileType.ViewModel);
        result.Content.Should().Contain("public partial class UserFormViewModel : ObservableObject");
        result.Content.Should().Contain("[ObservableProperty]");
        result.Content.Should().Contain("private string _username = string.Empty;");
        result.Content.Should().Contain("private bool _isAdmin;");
        result.Content.Should().Contain("[RelayCommand]");
        result.Content.Should().Contain("private void Save()");

        // 語法樹診斷檢查
        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldDeduplicateRepeatedPropertiesAndCommands()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewModelClassName = "DuplicateTestViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.StackPanel,
                Children = [
                    new AstNode
                    {
                        Id = "1",
                        Type = ControlType.TextBox,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "SharedTitle" }],
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "RefreshCommand" }]
                    },
                    new AstNode
                    {
                        Id = "2",
                        Type = ControlType.TextBlock,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "SharedTitle" }],
                        Events = [new EventMappingDefinition { EventName = "Tapped", CommandProperty = "RefreshCommand" }]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        // 計算出現次數
        var propertyOccurrences = result.Content.Split("private string _sharedTitle").Length - 1;
        var commandOccurrences = result.Content.Split("private void Refresh()").Length - 1;

        propertyOccurrences.Should().Be(1);
        commandOccurrences.Should().Be(1);
    }
}
