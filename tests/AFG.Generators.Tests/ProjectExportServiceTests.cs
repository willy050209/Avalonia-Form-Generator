// filepath: tests/AFG.Generators.Tests/ProjectExportServiceTests.cs
using AFG.Generators.ProjectExport;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 ProjectExportService 模組與 Visual Studio 方案 (.slnx) 匯出功能。
/// </summary>
public sealed class ProjectExportServiceTests
{
    private readonly ProjectExportService _exportService = new();

    [Fact]
    public void GenerateFullProject_ShouldGenerateCompleteVisualStudioFiles_IncludingSlnx()
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
        
        // 驗證 Visual Studio .slnx 方案檔
        var slnx = files.FirstOrDefault(f => f.FileName == "OrderFormApp.slnx");
        slnx.Should().NotBeNull();
        slnx!.FileType.Should().Be(SourceFileType.SolutionFile);
        slnx.Content.Should().Contain("<Solution>");
        slnx.Content.Should().Contain("<Project Path=\"OrderFormApp.csproj\" />");

        // 驗證 .csproj、進入點與樣式檔
        files.Should().Contain(f => f.FileName == "OrderFormApp.csproj");
        files.Should().Contain(f => f.FileName == "Program.cs");
        files.Should().Contain(f => f.FileName == "App.axaml");
        files.Should().Contain(f => f.FileName == "App.axaml.cs");
        files.Should().Contain(f => f.FileName == "OrderFormView.cs");
        files.Should().Contain(f => f.FileName == "OrderFormViewModel.cs");
        files.Should().Contain(f => f.FileName == ".gitignore");
        files.Should().Contain(f => f.FileName == ".editorconfig");

        // 檢查 C# 語法正確性
        foreach (var csFile in files.Where(f => f.FileName.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var errors = RoslynCompilerService.CheckSyntaxDiagnostics(csFile.Content);
            errors.Should().BeEmpty($"檔案 {csFile.FileName} 不應存在語法錯誤");
        }
    }
}
