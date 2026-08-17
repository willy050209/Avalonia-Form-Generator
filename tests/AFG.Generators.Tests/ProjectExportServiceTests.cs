// filepath: tests/AFG.Generators.Tests/ProjectExportServiceTests.cs
using AFG.Generators.ProjectExport;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 ProjectExportService 模組與專案匯出功能。
/// </summary>
public sealed class ProjectExportServiceTests
{
    private readonly ProjectExportService _exportService = new();

    [Fact]
    public void GenerateFullProject_ShouldGenerateCompleteProjectFiles()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "OrderFormView",
            ViewModelClassName = "OrderFormViewModel",
            Title = "訂單管理系統",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Grid,
                Children = [
                    new AstNode
                    {
                        Id = "btn",
                        Type = ControlType.Button,
                        Content = "Submit Order",
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitOrderCommand" }]
                    }
                ]
            }
        };

        // Act
        var files = _exportService.GenerateFullProject(doc);

        // Assert
        files.Should().NotBeNull();
        files.Should().Contain(f => f.FileName == "OrderFormApp.csproj");
        files.Should().Contain(f => f.FileName == "Program.cs");
        files.Should().Contain(f => f.FileName == "App.axaml");
        files.Should().Contain(f => f.FileName == "App.axaml.cs");
        files.Should().Contain(f => f.FileName == "OrderFormView.cs");
        files.Should().Contain(f => f.FileName == "OrderFormViewModel.cs");

        // 檢查 C# 語法正確性
        foreach (var csFile in files.Where(f => f.FileName.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var errors = RoslynCompilerService.CheckSyntaxDiagnostics(csFile.Content);
            errors.Should().BeEmpty($"檔案 {csFile.FileName} 不應存在語法錯誤");
        }
    }
}
