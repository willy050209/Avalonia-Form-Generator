// filepath: tests/AFG.Generators.Tests/ProjectExportServiceTests.cs
using AFG.Generators.ProjectExport;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 ProjectExportService 模組與 Visual Studio 方案 (.slnx) 及子資料夾結構匯出功能。
/// </summary>
public sealed class ProjectExportServiceTests
{
    private readonly ProjectExportService _exportService = new();

    [Fact]
    public void GenerateFullProject_ShouldGenerateCompleteVisualStudioFiles_WithSubfolderStructureAndMarkupExtensions()
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
        
        // 1. 方案根目錄檔案
        var slnx = files.FirstOrDefault(f => f.FileName == "OrderFormApp.slnx");
        slnx.Should().NotBeNull();
        slnx!.FileType.Should().Be(SourceFileType.SolutionFile);
        slnx.Content.Should().Contain("<Solution>");
        slnx.Content.Should().Contain("<Project Path=\"src/OrderFormApp/OrderFormApp.csproj\" />");

        files.Should().Contain(f => f.FileName == ".gitignore");
        files.Should().Contain(f => f.FileName == ".editorconfig");

        // 2. src/OrderFormApp/ 子資料夾專案檔案
        var projPath = Path.Combine("src", "OrderFormApp", "OrderFormApp.csproj");
        var programPath = Path.Combine("src", "OrderFormApp", "Program.cs");
        var appAxamlPath = Path.Combine("src", "OrderFormApp", "App.axaml");
        var appCsPath = Path.Combine("src", "OrderFormApp", "App.axaml.cs");
        var markupExtPath = Path.Combine("src", "OrderFormApp", "Markup", "AvaloniaMarkupExtensions.cs");
        var viewPath = Path.Combine("src", "OrderFormApp", "Views", "OrderFormView.cs");
        var vmPath = Path.Combine("src", "OrderFormApp", "ViewModels", "OrderFormViewModel.cs");

        files.Should().Contain(f => f.FileName == projPath);
        files.Should().Contain(f => f.FileName == programPath);
        files.Should().Contain(f => f.FileName == appAxamlPath);
        files.Should().Contain(f => f.FileName == appCsPath);
        files.Should().Contain(f => f.FileName == markupExtPath);
        files.Should().Contain(f => f.FileName == viewPath);
        files.Should().Contain(f => f.FileName == vmPath);

        // 3. 檢查所有 C# 檔案語法正確性
        foreach (var csFile in files.Where(f => f.FileName.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var errors = RoslynCompilerService.CheckSyntaxDiagnostics(csFile.Content);
            errors.Should().BeEmpty($"檔案 {csFile.FileName} 不應存在語法錯誤");
        }
    }

    [Fact]
    public async Task ExportToFolderAsync_ShouldCreateDirectoriesAndWriteFiles()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_TestExport_" + Guid.NewGuid().ToString("N"));
        var doc = FormDocument.CreateDefault();

        try
        {
            // Act
            await _exportService.ExportToFolderAsync(doc, tempFolder);

            // Assert
            Directory.Exists(tempFolder).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp.slnx")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, ".gitignore")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, ".editorconfig")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp", "MainFormApp.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp", "Markup", "AvaloniaMarkupExtensions.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp", "Views", "MainFormView.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp", "ViewModels", "MainFormViewModel.cs")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }
}
