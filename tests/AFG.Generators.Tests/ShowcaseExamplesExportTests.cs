// filepath: tests/AFG.Generators.Tests/ShowcaseExamplesExportTests.cs
using System.IO;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 7 大擴充實機範例之專案匯出功能與檔案結構生成。
/// </summary>
public sealed class ShowcaseExamplesExportTests
{
    private static string GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "AvaloniaFormGenerator.slnx")))
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return dir;
    }

    [Theory]
    [InlineData("BasicControlsShowcase", "BasicControlsShowcase.afg.json")]
    [InlineData("DialogsShowcase", "DialogsShowcase.afg.json")]
    [InlineData("PictureBoxShowcase", "PictureBoxShowcase.afg.json")]
    [InlineData("VectorGraphicsShowcase", "VectorGraphicsShowcase.afg.json")]
    [InlineData("HighPerformanceDrawingShowcase", "HighPerformanceDrawingShowcase.afg.json")]
    [InlineData("MediaPlayerShowcase", "MediaPlayerShowcase.afg.json")]
    [InlineData("MultiLanguageInteropShowcase", "MultiLanguageInteropShowcase.afg.json")]
    public async Task ExportShowcaseExample_ToTempDirectory_ShouldGenerateExpectedFiles(string folder, string fileName)
    {
        var rootDir = GetSolutionRoot();
        var jsonPath = Path.Combine(rootDir, "example", folder, fileName);
        File.Exists(jsonPath).Should().BeTrue();

        var json = await File.ReadAllTextAsync(jsonPath);
        var doc = AfgSerializer.DeserializeDocument(json);

        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_ShowcaseExport_" + Guid.NewGuid().ToString("N"));
        var exportService = new ProjectExportService();

        try
        {
            await exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false, CustomProjectName: folder));

            // Assert: 方案檔與專案檔結構
            var slnxPath = Path.Combine(tempFolder, $"{folder}.slnx");
            File.Exists(slnxPath).Should().BeTrue();

            var sharedCsproj = Path.Combine(tempFolder, "src", $"{folder}.Shared", $"{folder}.Shared.csproj");
            File.Exists(sharedCsproj).Should().BeTrue();

            var desktopCsproj = Path.Combine(tempFolder, "src", $"{folder}.Desktop", $"{folder}.Desktop.csproj");
            File.Exists(desktopCsproj).Should().BeTrue();

            var viewPath = Path.Combine(tempFolder, "src", $"{folder}.Shared", "Views", $"{doc.ViewClassName}.cs");
            File.Exists(viewPath).Should().BeTrue();

            var viewModelPath = Path.Combine(tempFolder, "src", $"{folder}.Shared", "ViewModels", $"{doc.ViewModelClassName}.cs");
            File.Exists(viewModelPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                try { Directory.Delete(tempFolder, true); } catch { }
            }
        }
    }

    [Theory]
    [InlineData("BasicControlsShowcase")]
    [InlineData("DialogsShowcase")]
    [InlineData("PictureBoxShowcase")]
    [InlineData("VectorGraphicsShowcase")]
    [InlineData("HighPerformanceDrawingShowcase")]
    [InlineData("MediaPlayerShowcase")]
    [InlineData("MultiLanguageInteropShowcase")]
    public void ExampleProject_Files_ShouldExistInWorkspace(string folder)
    {
        var rootDir = GetSolutionRoot();
        var exampleFolder = Path.Combine(rootDir, "example", folder);

        File.Exists(Path.Combine(exampleFolder, $"{folder}.slnx")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "src", $"{folder}.Desktop", $"{folder}.Desktop.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "src", $"{folder}.Shared", $"{folder}.Shared.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "screenshots", "editor_design_view.png")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "screenshots", "runtime_result.png")).Should().BeTrue();
    }
}
