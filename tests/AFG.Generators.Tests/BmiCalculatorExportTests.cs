// filepath: tests/AFG.Generators.Tests/BmiCalculatorExportTests.cs
using System.IO;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 BMI 健康體位計算機範例專案之匯出功能與實體方案結構。
/// </summary>
public sealed class BmiCalculatorExportTests
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

    [Fact]
    public async Task ExportBmiCalculator_ToTempFolder_ShouldGenerateExpectedSolutionAndProjects()
    {
        // Arrange
        var rootDir = GetSolutionRoot();
        var jsonPath = Path.Combine(rootDir, "example", "BmiCalculator", "BmiCalculator.afg.json");
        File.Exists(jsonPath).Should().BeTrue();

        var json = await File.ReadAllTextAsync(jsonPath);
        var doc = AfgSerializer.DeserializeDocument(json);

        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_BmiCalculatorExport_" + Guid.NewGuid().ToString("N"));
        var exportService = new ProjectExportService();

        try
        {
            // Act
            await exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false, CustomProjectName: "BmiCalculator"));

            // Assert: 方案檔與專案檔結構
            var slnxPath = Path.Combine(tempFolder, "BmiCalculator.slnx");
            File.Exists(slnxPath).Should().BeTrue();

            var sharedCsprojPath = Path.Combine(tempFolder, "src", "BmiCalculator.Shared", "BmiCalculator.Shared.csproj");
            File.Exists(sharedCsprojPath).Should().BeTrue();

            var desktopCsprojPath = Path.Combine(tempFolder, "src", "BmiCalculator.Desktop", "BmiCalculator.Desktop.csproj");
            File.Exists(desktopCsprojPath).Should().BeTrue();

            var viewPath = Path.Combine(tempFolder, "src", "BmiCalculator.Shared", "Views", "BmiCalculatorView.cs");
            File.Exists(viewPath).Should().BeTrue();

            var viewModelPath = Path.Combine(tempFolder, "src", "BmiCalculator.Shared", "ViewModels", "BmiCalculatorViewModel.cs");
            File.Exists(viewModelPath).Should().BeTrue();

            var serviceInterfacePath = Path.Combine(tempFolder, "src", "BmiCalculator.Shared", "Services", "IBmiCalculatorService.cs");
            File.Exists(serviceInterfacePath).Should().BeTrue();

            var serviceImplPath = Path.Combine(tempFolder, "src", "BmiCalculator.Shared", "Services", "BmiCalculatorService.cs");
            File.Exists(serviceImplPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                try { Directory.Delete(tempFolder, true); } catch { }
            }
        }
    }

    [Fact]
    public void ExampleProject_Files_ShouldExistInWorkspace()
    {
        var rootDir = GetSolutionRoot();
        var exampleFolder = Path.Combine(rootDir, "example", "BmiCalculator");

        File.Exists(Path.Combine(exampleFolder, "BmiCalculator.slnx")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "src", "BmiCalculator.Desktop", "BmiCalculator.Desktop.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "src", "BmiCalculator.Shared", "BmiCalculator.Shared.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "src", "BmiCalculator.Shared", "Views", "BmiCalculatorView.cs")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "src", "BmiCalculator.Shared", "ViewModels", "BmiCalculatorViewModel.cs")).Should().BeTrue();
        File.Exists(Path.Combine(exampleFolder, "src", "BmiCalculator.Shared", "Services", "BmiCalculatorService.cs")).Should().BeTrue();
    }
}
