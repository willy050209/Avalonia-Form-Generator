// filepath: tests/AFG.Generators.Tests/ProjectExportServiceTests.cs
using System.Diagnostics;
using AFG.Core.Serialization;
using AFG.Generators.ProjectExport;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 ProjectExportService 模組與 Visual Studio 方案 (.slnx) 及純 C# 分層結構匯出功能，並確保匯出專案即可直接透過 dotnet build 編譯。
/// </summary>
public sealed class ProjectExportServiceTests
{
    private readonly ProjectExportService _exportService = new();

    [Fact]
    public void GenerateFullProject_ShouldGenerateCompleteVisualStudioFiles_WithPureCSharpStructure()
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
        slnx.Content.Should().Contain("<Project Path=\"OrderFormApp/OrderFormApp.csproj\" />");

        files.Should().Contain(f => f.FileName == ".gitignore");
        files.Should().Contain(f => f.FileName == ".editorconfig");

        // 2. OrderFormApp/ 專案目錄結構檔案
        var projPath = Path.Combine("OrderFormApp", "OrderFormApp.csproj");
        var appCsPath = Path.Combine("OrderFormApp", "App.cs");
        var configPath = Path.Combine("OrderFormApp", "Config.cs");
        var globalUsingsPath = Path.Combine("OrderFormApp", "GlobalUsings.cs");
        var programPath = Path.Combine("OrderFormApp", "Program.cs");
        var markupExtPath = Path.Combine("OrderFormApp", "Markup", "AvaloniaMarkupExtensions.cs");
        var servicePath = Path.Combine("OrderFormApp", "Services", "GreetingService.cs");
        var viewPath = Path.Combine("OrderFormApp", "Views", "OrderFormView.cs");
        var vmPath = Path.Combine("OrderFormApp", "ViewModels", "OrderFormViewModel.cs");

        files.Should().Contain(f => f.FileName == projPath);
        files.Should().Contain(f => f.FileName == appCsPath);
        files.Should().Contain(f => f.FileName == configPath);
        files.Should().Contain(f => f.FileName == globalUsingsPath);
        files.Should().Contain(f => f.FileName == programPath);
        files.Should().Contain(f => f.FileName == markupExtPath);
        files.Should().Contain(f => f.FileName == servicePath);
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
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "MainFormApp.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "App.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "Config.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "GlobalUsings.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "Program.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "Markup", "AvaloniaMarkupExtensions.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "Services", "GreetingService.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "Views", "MainFormView.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp", "ViewModels", "MainFormViewModel.cs")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }

    /// <summary>
    /// 端到端實體編譯測試：將包含複雜控制項與資料綁定的 AST 表單完整匯出至實體目錄，並透過 dotnet CLI 驗證可 0 錯誤成功編譯。
    /// </summary>
    [Fact]
    public async Task ExportedProject_ShouldCompileDirectlyWithDotnetCli()
    {
        // Arrange: 建立包含 Grid, TextBlock, TextBox, CheckBox, Button, 雙向綁定與命令映射之完整 AST
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_BuildVerification_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "CustomerFormView",
            ViewModelClassName = "CustomerFormViewModel",
            Title = "客戶管理系統",
            RootNode = new AstNode
            {
                Id = "rootGrid",
                Type = ControlType.Grid,
                RowDefinitions = [GridLengthModel.Auto, GridLengthModel.Star(), GridLengthModel.Auto],
                ColumnDefinitions = [GridLengthModel.Auto, GridLengthModel.Star()],
                Children = [
                    new AstNode
                    {
                        Id = "headerText",
                        Type = ControlType.TextBlock,
                        Text = "客戶資料維護",
                        FontSize = 18,
                        GridRow = 0,
                        GridColumnSpan = 2,
                        Margin = new ThicknessModel(10, 10, 10, 10)
                    },
                    new AstNode
                    {
                        Id = "inputName",
                        Type = ControlType.TextBox,
                        Watermark = "請輸入姓名",
                        GridRow = 1,
                        GridColumn = 1,
                        Width = 240,
                        Height = 35,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "CustomerName", Mode = BindingMode.TwoWay }]
                    },
                    new AstNode
                    {
                        Id = "chkActive",
                        Type = ControlType.CheckBox,
                        Content = "帳號啟用狀態",
                        GridRow = 1,
                        GridColumn = 0,
                        Bindings = [new BindingDefinition { TargetProperty = "IsChecked", ViewModelProperty = "IsActive", Mode = BindingMode.TwoWay }]
                    },
                    new AstNode
                    {
                        Id = "btnSave",
                        Type = ControlType.Button,
                        Content = "儲存客戶資料",
                        GridRow = 2,
                        GridColumnSpan = 2,
                        Width = 140,
                        Height = 35,
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveCustomerCommand" }]
                    }
                ]
            }
        };

        try
        {
            // Act: 匯出完整方案
            await _exportService.ExportToFolderAsync(doc, tempFolder);

            var csprojPath = Path.Combine(tempFolder, "CustomerFormApp", "CustomerFormApp.csproj");
            File.Exists(csprojPath).Should().BeTrue("專案檔應存在於匯出目錄");

            // 執行 dotnet build 驗證可編譯性
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" -c Release",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process.Should().NotBeNull();

            var stdout = await process!.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Assert: 驗證 ExitCode 為 0 (編譯成功)
            process.ExitCode.Should().Be(0, $"匯出專案之 dotnet build 應成功 (ExitCode 0)。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                try
                {
                    Directory.Delete(tempFolder, recursive: true);
                }
                catch
                {
                    // 忽略清理時由鎖定引起之例外
                }
            }
        }
    }

    /// <summary>
    /// 驗證使用者真實 MainFormView.afg.json (包含 Button, TextBox, TextBlock 及 IsEnabled 綁定) 匯出後可 0 錯誤編譯。
    /// </summary>
    [Fact]
    public async Task ExportedProject_FromMainFormViewWithIsEnabledBinding_ShouldCompileSuccessfully()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_UserJsonTest_" + Guid.NewGuid().ToString("N"));
        var userJsonPath = @"D:\Downloads\AFG\MainFormView.afg.json";

        FormDocument doc;
        if (File.Exists(userJsonPath))
        {
            var json = await File.ReadAllTextAsync(userJsonPath);
            doc = AfgSerializer.DeserializeDocument(json);
        }
        else
        {
            // 回退模擬結構
            doc = new FormDocument
            {
                ViewClassName = "MainFormView",
                ViewModelClassName = "MainFormViewModel",
                Title = "Avalonia Form",
                RootNode = new AstNode
                {
                    Id = "root",
                    Type = ControlType.Canvas,
                    Children = [
                        new AstNode { Id = "btn1", Type = ControlType.Button, Text = "計算", Content = "Button", Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "Button_1Command" }] },
                        new AstNode { Id = "txt1", Type = ControlType.TextBox, Watermark = "請輸入數字", Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "TextBox_d2d5Property" }] },
                        new AstNode { Id = "tb1", Type = ControlType.TextBlock, Text = "答案" },
                        new AstNode { Id = "tb2", Type = ControlType.TextBlock, Bindings = [new BindingDefinition { TargetProperty = "IsEnabled", ViewModelProperty = "TextBlock_c1Property" }] }
                    ]
                }
            };
        }

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder);
            var csprojPath = Path.Combine(tempFolder, "MainFormApp", "MainFormApp.csproj");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" -c Release",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            var stdout = await process!.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            process.ExitCode.Should().Be(0, $"MainFormView.afg.json 匯出專案應成功編譯。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                try
                {
                    Directory.Delete(tempFolder, recursive: true);
                }
                catch { }
            }
        }
    }
}
