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
    public void GenerateFullProject_ShouldGenerateCompleteVisualStudioFiles_WithMultiProjectAndDISupport()
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
        var files = _exportService.GenerateFullProject(doc, new ProjectExportOptions(IncludeMobileProject: true));

        // Assert
        files.Should().NotBeNull();
        
        // 1. 方案根目錄檔案
        var slnx = files.FirstOrDefault(f => f.FileName == "OrderFormApp.slnx");
        slnx.Should().NotBeNull();
        slnx!.FileType.Should().Be(SourceFileType.SolutionFile);
        slnx.Content.Should().Contain("<Solution>");
        slnx.Content.Should().Contain("<Project Path=\"src/OrderFormApp.Shared/OrderFormApp.Shared.csproj\" />");
        slnx.Content.Should().Contain("<Project Path=\"src/OrderFormApp.Desktop/OrderFormApp.Desktop.csproj\" />");
        slnx.Content.Should().Contain("<Project Path=\"src/OrderFormApp.Android/OrderFormApp.Android.csproj\" />");

        files.Should().Contain(f => f.FileName == ".gitignore");
        files.Should().Contain(f => f.FileName == ".editorconfig");

        // 2. OrderFormApp.Shared 專案目錄結構檔案
        var sharedProj = Path.Combine("src", "OrderFormApp.Shared", "OrderFormApp.Shared.csproj");
        var appCs = Path.Combine("src", "OrderFormApp.Shared", "App.cs");
        var configCs = Path.Combine("src", "OrderFormApp.Shared", "Config.cs");
        var globalUsingsCs = Path.Combine("src", "OrderFormApp.Shared", "GlobalUsings.cs");
        var markupExtCs = Path.Combine("src", "OrderFormApp.Shared", "Markup", "AvaloniaMarkupExtensions.cs");
        var igreetingCs = Path.Combine("src", "OrderFormApp.Shared", "Services", "IGreetingService.cs");
        var greetingCs = Path.Combine("src", "OrderFormApp.Shared", "Services", "GreetingService.cs");
        var viewCs = Path.Combine("src", "OrderFormApp.Shared", "Views", "OrderFormView.cs");
        var vmCs = Path.Combine("src", "OrderFormApp.Shared", "ViewModels", "OrderFormViewModel.cs");

        files.Should().Contain(f => f.FileName == sharedProj);
        files.Should().Contain(f => f.FileName == appCs);
        files.Should().Contain(f => f.FileName == configCs);
        files.Should().Contain(f => f.FileName == globalUsingsCs);
        files.Should().Contain(f => f.FileName == markupExtCs);
        files.Should().Contain(f => f.FileName == igreetingCs);
        files.Should().Contain(f => f.FileName == greetingCs);
        files.Should().Contain(f => f.FileName == viewCs);
        files.Should().Contain(f => f.FileName == vmCs);

        // 3. OrderFormApp.Desktop 專案檔案
        var desktopProj = Path.Combine("src", "OrderFormApp.Desktop", "OrderFormApp.Desktop.csproj");
        var desktopProg = Path.Combine("src", "OrderFormApp.Desktop", "Program.cs");
        files.Should().Contain(f => f.FileName == desktopProj);
        files.Should().Contain(f => f.FileName == desktopProg);

        // 4. OrderFormApp.Android 專案檔案
        var androidProj = Path.Combine("src", "OrderFormApp.Android", "OrderFormApp.Android.csproj");
        var mainActivity = Path.Combine("src", "OrderFormApp.Android", "MainActivity.cs");
        var splashActivity = Path.Combine("src", "OrderFormApp.Android", "SplashActivity.cs");
        var manifest = Path.Combine("src", "OrderFormApp.Android", "AndroidManifest.xml");
        files.Should().Contain(f => f.FileName == androidProj);
        files.Should().Contain(f => f.FileName == mainActivity);
        files.Should().Contain(f => f.FileName == splashActivity);
        files.Should().Contain(f => f.FileName == manifest);

        // 5. 檢查 App.cs 是否包含 DI 與最大化視窗
        var appFile = files.First(f => f.FileName == appCs);
        appFile.Content.Should().Contain("ConfigureServices(IServiceCollection services)");
        appFile.Content.Should().Contain("WindowState = WindowState.Maximized");
        appFile.Content.Should().Contain("services.AddSingleton<IGreetingService, GreetingService>()");
        appFile.Content.Should().Contain("services.AddTransient<OrderFormViewModel>()");
        appFile.Content.Should().Contain("services.AddTransient<OrderFormView>");
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
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: true));

            // Assert
            Directory.Exists(tempFolder).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "MainFormApp.slnx")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, ".gitignore")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, ".editorconfig")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Shared", "MainFormApp.Shared.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Shared", "App.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Shared", "Config.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Shared", "GlobalUsings.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Shared", "Markup", "AvaloniaMarkupExtensions.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Shared", "Services", "GreetingService.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Shared", "Views", "MainFormView.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Shared", "ViewModels", "MainFormViewModel.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Desktop", "MainFormApp.Desktop.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Desktop", "Program.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Android", "MainFormApp.Android.csproj")).Should().BeTrue();
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
    /// 端到端實體編譯測試：驗證匯出之 .Shared 與 .Desktop 專案可直接透過 dotnet CLI 0 錯誤成功編譯。
    /// </summary>
    [Fact]
    public async Task ExportedProject_ShouldCompileDirectlyWithDotnetCli()
    {
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
            // Act: 匯出完整方案 (不含 Android 以便快速進行 CI 桌面端編譯)
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false));

            var desktopCsprojPath = Path.Combine(tempFolder, "src", "CustomerFormApp.Desktop", "CustomerFormApp.Desktop.csproj");
            File.Exists(desktopCsprojPath).Should().BeTrue("Desktop 專案檔應存在於匯出目錄");

            // 執行 dotnet build 驗證可編譯性
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{desktopCsprojPath}\" -c Release",
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
                catch { }
            }
        }
    }

    /// <summary>
    /// 驗證使用者真實 MainFormView.afg.json 匯出後可 0 錯誤編譯。
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
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false));
            var desktopCsprojPath = Path.Combine(tempFolder, "src", "MainFormApp.Desktop", "MainFormApp.Desktop.csproj");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{desktopCsprojPath}\" -c Release",
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
