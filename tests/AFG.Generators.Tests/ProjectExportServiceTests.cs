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
            InjectedServices = [new ServiceDependencyDefinition { InterfaceName = "IGreetingService", ImplementationName = "GreetingService" }],
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
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitOrderCommand", IsAsync = true }]
                    }
                ]
            }
        };

        // Act
        var files = _exportService.GenerateFullProject(doc, new ProjectExportOptions(IncludeMobileProject: true, IncludeLicense: true));

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

        files.Should().Contain(f => f.FileName == "LICENSE");
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
        var idialogCs = Path.Combine("src", "OrderFormApp.Shared", "Services", "IDialogService.cs");
        var dialogCs = Path.Combine("src", "OrderFormApp.Shared", "Services", "DialogService.cs");
        var msgWindowCs = Path.Combine("src", "OrderFormApp.Shared", "Services", "MessageBoxWindow.cs");
        var openFileCs = Path.Combine("src", "OrderFormApp.Shared", "Services", "OpenFileDialog.cs");
        var saveFileCs = Path.Combine("src", "OrderFormApp.Shared", "Services", "SaveFileDialog.cs");
        var msgBoxCs = Path.Combine("src", "OrderFormApp.Shared", "Services", "MessageBox.cs");
        var viewCs = Path.Combine("src", "OrderFormApp.Shared", "Views", "OrderFormView.cs");
        var vmCs = Path.Combine("src", "OrderFormApp.Shared", "ViewModels", "OrderFormViewModel.cs");

        files.Should().Contain(f => f.FileName == sharedProj);
        files.Should().Contain(f => f.FileName == appCs);
        files.Should().Contain(f => f.FileName == configCs);
        files.Should().Contain(f => f.FileName == globalUsingsCs);
        files.Should().Contain(f => f.FileName == markupExtCs);
        files.Should().Contain(f => f.FileName == igreetingCs);
        files.Should().Contain(f => f.FileName == greetingCs);
        files.Should().Contain(f => f.FileName == idialogCs);
        files.Should().Contain(f => f.FileName == dialogCs);
        files.Should().Contain(f => f.FileName == msgWindowCs);
        files.Should().Contain(f => f.FileName == openFileCs);
        files.Should().Contain(f => f.FileName == saveFileCs);
        files.Should().Contain(f => f.FileName == msgBoxCs);
        files.Should().Contain(f => f.FileName == viewCs);
        files.Should().Contain(f => f.FileName == vmCs);

        // 3. OrderFormApp.Desktop 專案檔案
        var desktopProj = Path.Combine("src", "OrderFormApp.Desktop", "OrderFormApp.Desktop.csproj");
        var desktopGlobalUsings = Path.Combine("src", "OrderFormApp.Desktop", "GlobalUsings.cs");
        var desktopProg = Path.Combine("src", "OrderFormApp.Desktop", "Program.cs");
        files.Should().Contain(f => f.FileName == desktopProj);
        files.Should().Contain(f => f.FileName == desktopGlobalUsings);
        files.Should().Contain(f => f.FileName == desktopProg);

        // 4. OrderFormApp.Android 專案檔案
        var androidProj = Path.Combine("src", "OrderFormApp.Android", "OrderFormApp.Android.csproj");
        var androidGlobalUsings = Path.Combine("src", "OrderFormApp.Android", "GlobalUsings.cs");
        var mainActivity = Path.Combine("src", "OrderFormApp.Android", "MainActivity.cs");
        var stylesXml = Path.Combine("src", "OrderFormApp.Android", "Resources", "values", "styles.xml");
        var iconXml = Path.Combine("src", "OrderFormApp.Android", "Resources", "drawable", "icon.xml");
        var manifest = Path.Combine("src", "OrderFormApp.Android", "AndroidManifest.xml");
        files.Should().Contain(f => f.FileName == androidProj);
        files.Should().Contain(f => f.FileName == androidGlobalUsings);
        files.Should().Contain(f => f.FileName == mainActivity);
        files.Should().Contain(f => f.FileName == stylesXml);
        var stylesFile = files.First(f => f.FileName == stylesXml);
        stylesFile.Content.Should().Contain("parent=\"Theme.AppCompat.DayNight.NoActionBar\"");
        files.Should().Contain(f => f.FileName == iconXml);
        files.Should().Contain(f => f.FileName == manifest);

        // 5. 檢查 App.cs 是否包含 DI 與預設視窗尺寸配置
        var appFile = files.First(f => f.FileName == appCs);
        appFile.Content.Should().Contain("ConfigureServices(IServiceCollection services)");
        appFile.Content.Should().NotContain("WindowState = WindowState.Maximized");
        appFile.Content.Should().Contain("Width = Config.DefaultWindowWidth");
        appFile.Content.Should().Contain("Height = Config.DefaultWindowHeight");
        appFile.Content.Should().Contain("services.AddSingleton<IGreetingService, GreetingService>()");
        appFile.Content.Should().Contain("services.AddTransient<OrderFormViewModel>()");
        appFile.Content.Should().Contain("services.AddTransient<OrderFormView>");
    }

    [Fact]
    public void GenerateMultiFormProject_ShouldGenerateMultipleViewsAndNavigationService()
    {
        // Arrange
        var doc1 = new FormDocument
        {
            ViewClassName = "HomeView",
            ViewModelClassName = "HomeViewModel",
            RootNode = new AstNode { Id = "r1", Type = ControlType.Canvas }
        };
        var doc2 = new FormDocument
        {
            ViewClassName = "SettingsView",
            ViewModelClassName = "SettingsViewModel",
            RootNode = new AstNode { Id = "r2", Type = ControlType.StackPanel }
        };
        var project = new FormProjectDefinition
        {
            ProjectName = "PortalApp",
            RootNamespace = "PortalApp",
            InitialFormName = "HomeView",
            Documents = [doc1, doc2]
        };

        // Act
        var files = _exportService.GenerateMultiFormProject(project, new ProjectExportOptions());

        // Assert
        files.Should().NotBeNull();
        files.Should().Contain(f => f.FileName == Path.Combine("src", "PortalApp.Shared", "Services", "INavigationService.cs"));
        files.Should().Contain(f => f.FileName == Path.Combine("src", "PortalApp.Shared", "Services", "NavigationService.cs"));
        files.Should().Contain(f => f.FileName == Path.Combine("src", "PortalApp.Shared", "Views", "HomeView.cs"));
        files.Should().Contain(f => f.FileName == Path.Combine("src", "PortalApp.Shared", "Views", "SettingsView.cs"));
        files.Should().Contain(f => f.FileName == Path.Combine("src", "PortalApp.Shared", "ViewModels", "HomeViewModel.cs"));
        files.Should().Contain(f => f.FileName == Path.Combine("src", "PortalApp.Shared", "ViewModels", "SettingsViewModel.cs"));

        var appFile = files.First(f => f.FileName == Path.Combine("src", "PortalApp.Shared", "App.cs"));
        appFile.Content.Should().Contain("services.AddSingleton<INavigationService, NavigationService>()");
        appFile.Content.Should().Contain("services.AddTransient<HomeView>");
        appFile.Content.Should().Contain("services.AddTransient<SettingsView>");
    }

    [Fact]
    public void GenerateFullProject_ByDefault_ShouldNotGenerateLicenseFile()
    {
        // Arrange
        var doc = FormDocument.CreateDefault();

        // Act
        var files = _exportService.GenerateFullProject(doc);

        // Assert
        files.Should().NotContain(f => f.FileName == "LICENSE");
        files.Should().Contain(f => f.FileName == ".gitignore");
        files.Should().Contain(f => f.FileName == ".editorconfig");
    }

    [Fact]
    public async Task ExportToFolderAsync_ShouldCreateDirectoriesAndWriteFiles()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_TestExport_" + Guid.NewGuid().ToString("N"));
        var doc = FormDocument.CreateDefault() with
        {
            InjectedServices = [new ServiceDependencyDefinition { InterfaceName = "IGreetingService", ImplementationName = "GreetingService" }]
        };

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
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Desktop", "GlobalUsings.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Desktop", "Program.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Android", "MainFormApp.Android.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Android", "GlobalUsings.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Android", "MainActivity.cs")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Android", "Resources", "values", "styles.xml")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Android", "Resources", "drawable", "icon.xml")).Should().BeTrue();
            File.Exists(Path.Combine(tempFolder, "src", "MainFormApp.Android", "AndroidManifest.xml")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportToFolderAsync_WithMediaPlayerRelativeAsset_ShouldCopyAssetAndGenerateAvaresUri()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_MediaExport_" + Guid.NewGuid().ToString("N"));
        var dummyMediaFile = Path.Combine(Path.GetTempPath(), "sample_intro_" + Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllTextAsync(dummyMediaFile, "dummy media content");

        var doc = FormDocument.CreateDefault() with
        {
            RootNamespace = "MainFormApp",
            RootNode = new AstNode
            {
                Id = "mediaPlayer1",
                Name = "mediaPlayer1",
                Type = ControlType.MediaPlayer,
                Source = dummyMediaFile,
                UseRelativePath = true,
                AutoPlay = true,
                IsLooping = true,
                Volume = 0.8
            }
        };

        try
        {
            // Act
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions());

            // Assert: Assets file should be copied to .Shared/Assets/
            var targetAssetFile = Path.Combine(tempFolder, "src", "MainFormApp.Shared", "Assets", Path.GetFileName(dummyMediaFile));
            File.Exists(targetAssetFile).Should().BeTrue();

            // Assert: Generated View code should contain avares:// URI
            var viewFile = Path.Combine(tempFolder, "src", "MainFormApp.Shared", "Views", "MainFormView.cs");
            File.Exists(viewFile).Should().BeTrue();
            var viewContent = await File.ReadAllTextAsync(viewFile);
            viewContent.Should().Contain($"avares://MainFormApp.Shared/Assets/{Path.GetFileName(dummyMediaFile)}");
            viewContent.Should().Contain(".AutoPlay(true)");
            viewContent.Should().Contain(".IsLooping(true)");
        }
        finally
        {
            if (File.Exists(dummyMediaFile))
            {
                try { File.Delete(dummyMediaFile); } catch { }
            }
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
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveCustomerCommand", IsAsync = true }]
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
                catch
                {
                    // 忽略清理時由鎖定引起之例外
                }
            }
        }
    }

    /// <summary>
    /// 端到端實體編譯測試：驗證包含 OpenFileDialog, SaveFileDialog 與 MessageBox 之專案可直接透過 dotnet CLI 成功編譯。
    /// </summary>
    [Fact]
    public async Task ExportedProject_WithDialogComponents_ShouldCompileDirectlyWithDotnetCli()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_DialogBuildVerification_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "EditorFormView",
            ViewModelClassName = "EditorFormViewModel",
            Title = "文字編輯器",
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "txtEditor",
                        Type = ControlType.TextBox,
                        Watermark = "請在此輸入文章內容...",
                        CanvasLeft = 20,
                        CanvasTop = 60,
                        Width = 400,
                        Height = 200,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "DocumentText", Mode = BindingMode.TwoWay }]
                    },
                    new AstNode
                    {
                        Id = "btnOpen",
                        Type = ControlType.Button,
                        Content = "開啟檔案",
                        CanvasLeft = 20,
                        CanvasTop = 15,
                        Width = 90,
                        Height = 32,
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "OpenClickCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "btnSave",
                        Type = ControlType.Button,
                        Content = "儲存檔案",
                        CanvasLeft = 120,
                        CanvasTop = 15,
                        Width = 90,
                        Height = 32,
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveClickCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "btnAbout",
                        Type = ControlType.Button,
                        Content = "關於",
                        CanvasLeft = 220,
                        CanvasTop = 15,
                        Width = 70,
                        Height = 32,
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "AboutClickCommand", IsAsync = false }]
                    },
                    new AstNode
                    {
                        Id = "ofd",
                        Name = "OpenFileDlg",
                        Type = ControlType.OpenFileDialog,
                        Events = [new EventMappingDefinition { EventName = "FileOk", CommandProperty = "OnFileOpenedCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "sfd",
                        Name = "SaveFileDlg",
                        Type = ControlType.SaveFileDialog,
                        Events = [new EventMappingDefinition { EventName = "FileOk", CommandProperty = "OnFileSavedCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "msg",
                        Name = "AboutMessageBox",
                        Type = ControlType.MessageBox,
                        Events = [new EventMappingDefinition { EventName = "Confirmed", CommandProperty = "OnAboutConfirmedCommand", IsAsync = false }]
                    }
                ]
            }
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false));

            var desktopCsprojPath = Path.Combine(tempFolder, "src", "EditorFormApp.Desktop", "EditorFormApp.Desktop.csproj");
            File.Exists(desktopCsprojPath).Should().BeTrue("Desktop 專案檔應存在於匯出目錄");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{desktopCsprojPath}\" -c Release --nologo",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process.Should().NotBeNull();

            var stdoutTask = process!.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            var completedInTime = process.WaitForExit(90000);
            completedInTime.Should().BeTrue("dotnet build 應在 90 秒內執行完成");

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            process.ExitCode.Should().Be(0, $"含對話方塊之匯出專案 dotnet build 應成功 (ExitCode 0)。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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
                }
            }
        }
    }

    /// <summary>
    /// 驗證包含 DebugConsole 元件的專案匯出後，能直接透過 dotnet CLI 成功編譯 (ExitCode 0)。
    /// </summary>
    [Fact]
    public async Task ExportedProject_WithDebugConsole_ShouldCompileDirectlyWithDotnetCli()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_DebugConsoleBuild_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "LogViewerFormView",
            ViewModelClassName = "LogViewerFormViewModel",
            Title = "Debug Console 測試表單",
            UseCompiledBindings = true,
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "btnAction",
                        Type = ControlType.Button,
                        Content = "執行任務",
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "RunActionCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "debugConsole",
                        Name = "LiveConsole",
                        Type = ControlType.DebugConsole,
                        Width = 500,
                        Height = 220,
                        CanvasLeft = 10,
                        CanvasTop = 60,
                        Text = "執行期即時日誌"
                    }
                ]
            }
        };

        var project = new FormProjectDefinition
        {
            ProjectName = "DebugConsoleApp",
            RootNamespace = "DebugConsoleApp",
            Title = "Debug Console App",
            Documents = [doc]
        };

        try
        {
            await _exportService.ExportMultiFormToFolderAsync(project, tempFolder, new ProjectExportOptions { IncludeMobileProject = false, IncludeLicense = false });

            var desktopCsproj = Path.Combine(tempFolder, "src", "DebugConsoleApp.Desktop", "DebugConsoleApp.Desktop.csproj");
            File.Exists(desktopCsproj).Should().BeTrue();

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{desktopCsproj}\" -c Release",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process.Should().NotBeNull();

            var stdoutTask = process!.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            var completedInTime = process.WaitForExit(90000);
            completedInTime.Should().BeTrue("dotnet build 應在 90 秒內執行完成");

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            process.ExitCode.Should().Be(0, $"含 DebugConsole 之匯出專案 dotnet build 應成功 (ExitCode 0)。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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
                }
            }
        }
    }

    /// <summary>
    /// 驗證複雜資料綁定 (Text, IsChecked, Value, IsEnabled, Opacity, Width, Height) 與同步/非同步命令混合情境下的實體編譯。
    /// </summary>
    [Fact]
    public async Task ExportedProject_WithComplexDataAndEventBindings_ShouldCompileAndMatchMvvmPattern()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_ComplexBindingsTest_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "ComplexFormView",
            ViewModelClassName = "ComplexFormViewModel",
            Title = "複合資料綁定測試",
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "txt1",
                        Type = ControlType.TextBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "TitleText", Mode = BindingMode.TwoWay },
                            new BindingDefinition { TargetProperty = "IsEnabled", ViewModelProperty = "CanEditText" }
                        ]
                    },
                    new AstNode
                    {
                        Id = "slider1",
                        Type = ControlType.Slider,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Value", ViewModelProperty = "SliderValue", Mode = BindingMode.TwoWay }
                        ]
                    },
                    new AstNode
                    {
                        Id = "btnSync",
                        Type = ControlType.Button,
                        Content = "同步重設",
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "ResetSyncCommand", IsAsync = false }
                        ]
                    },
                    new AstNode
                    {
                        Id = "btnAsync",
                        Type = ControlType.Button,
                        Content = "非同步提交",
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitDataCommand", IsAsync = true }
                        ]
                    }
                ]
            }
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false));
            var desktopCsprojPath = Path.Combine(tempFolder, "src", "ComplexFormApp.Desktop", "ComplexFormApp.Desktop.csproj");

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

            process.ExitCode.Should().Be(0, $"複合資料綁定與命令專案應成功編譯。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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
                        new AstNode { Id = "btn1", Type = ControlType.Button, Text = "計算", Content = "Button", Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "Button_1Command", IsAsync = true }] },
                        new AstNode { Id = "txt1", Type = ControlType.TextBox, Watermark = "請輸入數字", Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "TextBox_d2d5Property" }] },
                        new AstNode { Id = "tb1", Type = ControlType.TextBlock, Text = "答案" },
                        new AstNode { Id = "tb2", Type = ControlType.TextBlock, Bindings = [new BindingDefinition { TargetProperty = "IsEnabled", ViewModelProperty = "TextBlock_c1Property" }] }
                    ]
                }
            };
        }

        try
        {
            var def = FormProjectDefinition.FromSingleDocument(doc);
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false));
            var desktopCsprojPath = Path.Combine(tempFolder, "src", $"{def.ProjectName}.Desktop", $"{def.ProjectName}.Desktop.csproj");

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

    [Fact]
    public async Task ExportToFolderAsync_WithNonVisualComponents_ShouldBuildSuccessfully()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_NonVisual_Test_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "DeviceControlView",
            ViewModelClassName = "DeviceControlViewModel",
            Title = "裝置控制與不可視元件測試",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode { Id = "btn1", Type = ControlType.Button, Content = "開始同步", Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "StartSyncCommand", IsAsync = true }] },
                    new AstNode { Id = "tmr1", Type = ControlType.DispatcherTimer, Name = "PollingTimer" },
                    new AstNode { Id = "bgw1", Type = ControlType.BackgroundWorker, Name = "WorkerThread" },
                    new AstNode { Id = "ble1", Type = ControlType.BluetoothClient, Name = "BleSensor" },
                    new AstNode { Id = "com1", Type = ControlType.SerialPortService, Name = "SerialScanner" }
                ]
            }
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false));
            var desktopCsprojPath = Path.Combine(tempFolder, "src", "DeviceControlApp.Desktop", "DeviceControlApp.Desktop.csproj");

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

            process.ExitCode.Should().Be(0, $"含不可視元件之匯出專案應成功編譯。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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

    [Fact]
    public void GenerateFullProject_ShouldGenerateBluetoothAndSerialPortServices_WhenPresentInAst()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "CommsView",
            ViewModelClassName = "CommsViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode { Id = "ble", Name = "BleClient", Type = ControlType.BluetoothClient },
                    new AstNode { Id = "com", Name = "ComPort", Type = ControlType.SerialPortService }
                ]
            }
        };

        // Act
        var files = _exportService.GenerateFullProject(doc, new ProjectExportOptions());

        // Assert
        var bluetoothFile = files.FirstOrDefault(f => f.FileName.EndsWith(Path.Combine("Services", "BluetoothClient.cs"), StringComparison.Ordinal));
        var serialPortFile = files.FirstOrDefault(f => f.FileName.EndsWith(Path.Combine("Services", "SerialPortService.cs"), StringComparison.Ordinal));

        bluetoothFile.Should().NotBeNull();
        bluetoothFile!.Content.Should().Contain("public class BluetoothClient");
        bluetoothFile.Content.Should().Contain("public event EventHandler<byte[]>? DataReceived;");

        serialPortFile.Should().NotBeNull();
        serialPortFile!.Content.Should().Contain("public class SerialPortService");
        serialPortFile.Content.Should().Contain("public event EventHandler<string>? DataReceived;");
    }

    [Fact]
    public void GenerateFullProject_ShouldHonorCustomProjectName_WhenProvidedInOptionsOrDocument()
    {
        // Arrange
        var doc = new FormDocument
        {
            ProjectName = "InventoryManagerApp",
            ViewClassName = "InventoryView",
            ViewModelClassName = "InventoryViewModel",
            RootNode = new AstNode { Id = "root", Type = ControlType.Canvas }
        };

        // Act 1: 使用 Document 上的 ProjectName
        var filesFromDoc = _exportService.GenerateFullProject(doc, new ProjectExportOptions(IncludeMobileProject: true));

        // Assert 1
        filesFromDoc.Should().Contain(f => f.FileName == "InventoryManagerApp.slnx");
        filesFromDoc.Should().Contain(f => f.FileName == Path.Combine("src", "InventoryManagerApp.Shared", "InventoryManagerApp.Shared.csproj"));
        filesFromDoc.Should().Contain(f => f.FileName == Path.Combine("src", "InventoryManagerApp.Desktop", "InventoryManagerApp.Desktop.csproj"));
        filesFromDoc.Should().Contain(f => f.FileName == Path.Combine("src", "InventoryManagerApp.Android", "InventoryManagerApp.Android.csproj"));

        // Act 2: 使用 Options 覆寫 CustomProjectName
        var filesFromOption = _exportService.GenerateFullProject(doc, new ProjectExportOptions(CustomProjectName: "CustomPosSystem", IncludeMobileProject: false));

        // Assert 2
        filesFromOption.Should().Contain(f => f.FileName == "CustomPosSystem.slnx");
        filesFromOption.Should().Contain(f => f.FileName == Path.Combine("src", "CustomPosSystem.Shared", "CustomPosSystem.Shared.csproj"));
        filesFromOption.Should().Contain(f => f.FileName == Path.Combine("src", "CustomPosSystem.Desktop", "CustomPosSystem.Desktop.csproj"));
        filesFromOption.Should().NotContain(f => f.FileName.Contains(".Android"));
    }

    [Theory]
    [InlineData("../../MaliciousApp", "MaliciousApp")]
    [InlineData("..\\..\\HackProj", "HackProj")]
    [InlineData("Invalid<Name>:*?\"|", "InvalidName")]
    [InlineData("", "AvaloniaApp")]
    [InlineData("   ", "AvaloniaApp")]
    [InlineData(null, "AvaloniaApp")]
    public void SanitizeProjectName_ShouldRemovePathTraversalAndIllegalChars(string? input, string expected)
    {
        // Act
        var sanitized = ProjectExportService.SanitizeProjectName(input);

        // Assert
        sanitized.Should().Be(expected);
    }

    [Fact]
    public async Task ExportMultiFormToFolderAsync_WhenGivenValidDirectory_ShouldExportWithoutPathEscape()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "AFG_Test_Export_" + Guid.NewGuid().ToString("N"));
        try
        {
            var doc = new FormDocument
            {
                ProjectName = "SafeApp",
                ViewClassName = "MainView",
                ViewModelClassName = "MainViewModel",
                RootNode = new AstNode { Id = "root", Type = ControlType.Canvas }
            };

            // Act
            await _exportService.ExportToFolderAsync(doc, tempDir, new ProjectExportOptions(IncludeMobileProject: false));

            // Assert
            Directory.Exists(tempDir).Should().BeTrue();
            File.Exists(Path.Combine(tempDir, "SafeApp.slnx")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportToFolderAsync_WithPictureBoxAssetsAndInitBitmap_ShouldCopyAssetsAndBuildSuccessfully()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_PicTest_" + Guid.NewGuid().ToString("N"));
        var dummyImageDir = Path.Combine(Path.GetTempPath(), "AFG_PicSource_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dummyImageDir);
        var dummyImagePath = Path.Combine(dummyImageDir, "sample_logo.png");
        // Write minimal dummy binary/bytes
        await File.WriteAllBytesAsync(dummyImagePath, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var doc = new FormDocument
        {
            ProjectName = "PhotoManagerApp",
            RootNamespace = "PhotoManagerApp",
            ViewClassName = "PhotoManagerView",
            ViewModelClassName = "PhotoManagerViewModel",
            Title = "相片管理與 Bitmap 初始化測試",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "pic1",
                        Name = "LogoPicture",
                        Type = ControlType.PictureBox,
                        Width = 120,
                        Height = 60,
                        Source = dummyImagePath,
                        UseRelativePath = true,
                        Stretch = Stretch.Uniform
                    },
                    new AstNode
                    {
                        Id = "pic2",
                        Name = "DrawingCanvas",
                        Type = ControlType.PictureBox,
                        Width = 300,
                        Height = 200,
                        InitBitmap = true,
                        BitmapBackgroundColor = "#FAFAFA"
                    }
                ]
            }
        };

        try
        {
            // Act
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false));

            // Assert: Assets file should be copied to .Shared/Assets/
            var targetAssetPath = Path.Combine(tempFolder, "src", "PhotoManagerApp.Shared", "Assets", "sample_logo.png");
            File.Exists(targetAssetPath).Should().BeTrue();

            var desktopCsprojPath = Path.Combine(tempFolder, "src", "PhotoManagerApp.Desktop", "PhotoManagerApp.Desktop.csproj");

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

            process.ExitCode.Should().Be(0, $"包含 PictureBox 與 Bitmap 初始化的專案應成功編譯。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                try { Directory.Delete(tempFolder, recursive: true); } catch { }
            }
            if (Directory.Exists(dummyImageDir))
            {
                try { Directory.Delete(dummyImageDir, recursive: true); } catch { }
            }
        }
    }

    [Fact]
    public void GenerateFullProject_ShouldGenerateWindowControlPropertiesInAppAndConfig()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "PosMainView",
            ViewModelClassName = "PosMainViewModel",
            Title = "POS 終端收銀系統",
            BackgroundColor = "#1E293B",
            CanvasWidth = 1280,
            CanvasHeight = 800,
            MinWidth = 800,
            MinHeight = 600,
            MaxWidth = 1920,
            MaxHeight = 1080,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowState = WindowState.Normal,
            CanResize = false,
            Topmost = true,
            ShowInTaskbar = true,
            SystemDecorations = SystemDecorations.Full
        };

        // Act
        var files = _exportService.GenerateFullProject(doc, new ProjectExportOptions(IncludeMobileProject: false));

        // Assert
        var appFile = files.First(f => f.FileName.EndsWith("App.cs", StringComparison.Ordinal));
        appFile.Content.Should().Contain("MinWidth = 800");
        appFile.Content.Should().Contain("MinHeight = 600");
        appFile.Content.Should().Contain("MaxWidth = 1920");
        appFile.Content.Should().Contain("MaxHeight = 1080");
        appFile.Content.Should().Contain("Background = Brush.Parse(\"#1E293B\")");
        appFile.Content.Should().Contain("WindowStartupLocation = WindowStartupLocation.CenterScreen");
        appFile.Content.Should().Contain("WindowState = WindowState.Normal");
        appFile.Content.Should().Contain("CanResize = Config.CanResize");
        appFile.Content.Should().Contain("Topmost = Config.Topmost");
        appFile.Content.Should().Contain("ShowInTaskbar = Config.ShowInTaskbar");
        appFile.Content.Should().Contain("SystemDecorations = SystemDecorations.Full");

        var configFile = files.First(f => f.FileName.EndsWith("Config.cs", StringComparison.Ordinal));
        configFile.Content.Should().Contain("public const string AppTitle = \"POS 終端收銀系統\";");
        configFile.Content.Should().Contain("public const double DefaultWindowWidth = 1280;");
        configFile.Content.Should().Contain("public const double DefaultWindowHeight = 800;");
        configFile.Content.Should().Contain("public const bool CanResize = false;");
        configFile.Content.Should().Contain("public const bool Topmost = true;");
        configFile.Content.Should().Contain("public const bool ShowInTaskbar = true;");

        var viewFile = files.First(f => f.FileName.EndsWith("PosMainView.cs", StringComparison.Ordinal));
        viewFile.Content.Should().Contain("Background = Brush.Parse(\"#1E293B\");");
    }

    [Fact]
    public async Task ExportToFolderAsync_WithMediaPlayerAndFormEvents_ShouldBuildSuccessfully()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), $"AFG_MediaPlayer_Test_{Guid.NewGuid():N}");
        var doc = new FormDocument
        {
            ProjectName = "MediaPlayerApp",
            RootNamespace = "MediaPlayerApp",
            ViewClassName = "MediaPlayerView",
            ViewModelClassName = "MediaPlayerViewModel",
            Title = "多媒體播放器測試應用程式",
            Events = [
                new EventMappingDefinition { EventName = "Loaded", CommandProperty = "FormLoadedCommand", IsAsync = true },
                new EventMappingDefinition { EventName = "PointerPressed", CommandProperty = "FormClickedCommand", IsAsync = false, ParameterType = "PointerPressedEventArgs" }
            ],
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "player1",
                        Type = ControlType.MediaPlayer,
                        Name = "MainPlayer",
                        Source = "https://example.com/video.mp4",
                        AutoPlay = true,
                        IsLooping = true,
                        Volume = 0.85,
                        Width = 640,
                        Height = 360,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Source", ViewModelProperty = "VideoSource" },
                            new BindingDefinition { TargetProperty = "Position", ViewModelProperty = "PlaybackPosition" },
                            new BindingDefinition { TargetProperty = "Volume", ViewModelProperty = "VolumeLevel" },
                            new BindingDefinition { TargetProperty = "CurrentFrame", ViewModelProperty = "CurrentSnapshot" }
                        ],
                        Events = [
                            new EventMappingDefinition { EventName = "MediaOpened", CommandProperty = "OnMediaOpenedCommand", IsAsync = true },
                            new EventMappingDefinition { EventName = "FrameCaptured", CommandProperty = "OnFrameCapturedCommand", ParameterType = "Bitmap" }
                        ]
                    },
                    new AstNode
                    {
                        Id = "btnPlay",
                        Type = ControlType.Button,
                        Content = "播放",
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "PlayMediaCommand" }]
                    }
                ]
            }
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false));
            var desktopCsprojPath = Path.Combine(tempFolder, "src", "MediaPlayerApp.Desktop", "MediaPlayerApp.Desktop.csproj");

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

            process.ExitCode.Should().Be(0, $"含 MediaPlayer 與表單事件之匯出專案應成功編譯。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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
    /// 驗證 Code-Behind / Event-Driven 模式下匯出專案之實體編譯與無 ViewModel 正常運作。
    /// </summary>
    [Fact]
    public async Task ExportedProject_WithCodeBehindArchitectureMode_ShouldCompileSuccessfully()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_CodeBehindExportTest_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "CodeBehindMainView",
            ViewModelClassName = "CodeBehindMainViewModel",
            Title = "Code Behind Export Test",
            ArchitectureMode = ArchitectureMode.CodeBehind,
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "txtInput",
                        Name = "txtInput",
                        Type = ControlType.TextBox,
                        Text = "Hello WinForms Style"
                    },
                    new AstNode
                    {
                        Id = "btnSubmit",
                        Name = "btnSubmit",
                        Type = ControlType.Button,
                        Text = "Click Me",
                        Events = [new EventMappingDefinition { EventName = "Click" }]
                    }
                ]
            },
            Events = [new EventMappingDefinition { EventName = "Loaded" }]
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false, CustomProjectName: "CodeBehindExportTest"));
            var desktopCsprojPath = Path.Combine(tempFolder, "src", "CodeBehindExportTest.Desktop", "CodeBehindExportTest.Desktop.csproj");

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

            process.ExitCode.Should().Be(0, $"Code-Behind 模式匯出專案應成功編譯。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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
    /// 驗證 Code-Behind 模式下包含不可視元件、對話方塊與硬體通訊時匯出專案之實體編譯通過。
    /// </summary>
    [Fact]
    public async Task ExportedProject_WithCodeBehindMode_AndNonVisualComponents_ShouldCompileSuccessfully()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_CodeBehindNonVisual_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "DeviceControlView",
            ViewModelClassName = "DeviceControlViewModel",
            Title = "Device Control App",
            ArchitectureMode = ArchitectureMode.CodeBehind,
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "timer1",
                        Name = "pollTimer",
                        Type = ControlType.DispatcherTimer,
                        Interval = 500,
                        Events = [new EventMappingDefinition { EventName = "Tick" }]
                    },
                    new AstNode
                    {
                        Id = "worker1",
                        Name = "backgroundWorker",
                        Type = ControlType.BackgroundWorker,
                        Events = [new EventMappingDefinition { EventName = "DoWork" }]
                    },
                    new AstNode
                    {
                        Id = "ble1",
                        Name = "bleClient",
                        Type = ControlType.BluetoothClient,
                        Events = [new EventMappingDefinition { EventName = "DataReceived" }]
                    },
                    new AstNode
                    {
                        Id = "sp1",
                        Name = "serialPort",
                        Type = ControlType.SerialPortService,
                        Events = [new EventMappingDefinition { EventName = "DataReceived" }]
                    },
                    new AstNode
                    {
                        Id = "openDlg1",
                        Name = "openFileDialog",
                        Type = ControlType.OpenFileDialog,
                        Events = [new EventMappingDefinition { EventName = "FileOk" }]
                    },
                    new AstNode
                    {
                        Id = "saveDlg1",
                        Name = "saveFileDialog",
                        Type = ControlType.SaveFileDialog,
                        Events = [new EventMappingDefinition { EventName = "FileOk" }]
                    },
                    new AstNode
                    {
                        Id = "msgBox1",
                        Name = "messageBox",
                        Type = ControlType.MessageBox,
                        Events = [new EventMappingDefinition { EventName = "Confirmed" }]
                    },
                    new AstNode
                    {
                        Id = "btnStart",
                        Name = "btnStart",
                        Type = ControlType.Button,
                        Text = "開始監控",
                        Events = [new EventMappingDefinition { EventName = "Click" }]
                    }
                ]
            },
            Events = [new EventMappingDefinition { EventName = "Loaded" }]
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false, CustomProjectName: "DeviceControlApp"));
            var desktopCsprojPath = Path.Combine(tempFolder, "src", "DeviceControlApp.Desktop", "DeviceControlApp.Desktop.csproj");

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

            process.ExitCode.Should().Be(0, $"Code-Behind 模式含不可視元件之匯出專案應成功編譯。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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

    [Fact]
    public async Task ExportToFolderAsync_WithBorderAndBoxShadow_ShouldBuildSuccessfully()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_BorderShadow_Test_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            Title = "邊框與陰影卡片測試",
            CanvasWidth = 800,
            CanvasHeight = 600,
            RootNamespace = "BorderShadowApp.Views",
            ViewClassName = "BorderShadowView",
            ViewModelClassName = "BorderShadowViewModel",
            ArchitectureMode = ArchitectureMode.PureMvvm,
            RootNode = new AstNode
            {
                Name = "RootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "cardBorder",
                        Name = "cardBorder",
                        Type = ControlType.Border,
                        Width = 300,
                        Height = 200,
                        CanvasLeft = 50,
                        CanvasTop = 50,
                        Background = "#FFFFFF",
                        BorderBrush = "#3B82F6",
                        BorderThickness = new ThicknessModel(2, 2, 2, 2),
                        CornerRadius = new CornerRadiusModel(12, 12, 12, 12),
                        BoxShadow = new BoxShadowModel(0, 8, 20, 0, "#26000000", false),
                        Children = [
                            new AstNode
                            {
                                Id = "btnAction",
                                Name = "btnAction",
                                Type = ControlType.Button,
                                Text = "送出卡片",
                                BorderBrush = "#2563EB",
                                BorderThickness = new ThicknessModel(1, 1, 1, 1),
                                CornerRadius = new CornerRadiusModel(6, 6, 6, 6),
                                BoxShadow = new BoxShadowModel(0, 2, 4, 0, "#1A000000", false)
                            }
                        ]
                    }
                ]
            }
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false, CustomProjectName: "BorderShadowApp"));
            var desktopCsprojPath = Path.Combine(tempFolder, "src", "BorderShadowApp.Desktop", "BorderShadowApp.Desktop.csproj");

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

            process.ExitCode.Should().Be(0, $"含邊框與陰影之匯出專案應成功編譯。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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
    /// 端到端實體編譯測試：驗證匯出之 .Android 專案可透過 dotnet build (-t:SignAndroidPackage) 成功編譯出 APK 檔。
    /// 若執行環境未安裝 Android SDK 則自動跳過此測試。
    /// </summary>
    [Fact]
    public async Task ExportedProject_Android_ShouldCompileAndGenerateApk_WhenAndroidSdkAvailable()
    {
        if (!IsAndroidBuildEnvironmentAvailable())
        {
            // 未完整安裝 Android SDK 或 .NET Android Workload 之環境自動略過測試
            return;
        }

        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_AndroidBuildTest_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "MobileOrderView",
            ViewModelClassName = "MobileOrderViewModel",
            Title = "行動訂單系統",
            RootNode = new AstNode
            {
                Id = "rootCanvas",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "txtWelcome",
                        Type = ControlType.TextBlock,
                        Text = "歡迎使用行動端",
                        CanvasLeft = 20,
                        CanvasTop = 30,
                        FontSize = 16
                    },
                    new AstNode
                    {
                        Id = "btnAction",
                        Type = ControlType.Button,
                        Content = "送出",
                        CanvasLeft = 20,
                        CanvasTop = 80,
                        Width = 100,
                        Height = 40,
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitCommand", IsAsync = false }]
                    }
                ]
            }
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: true, CustomProjectName: "MobileOrderApp"));

            var androidCsprojPath = Path.Combine(tempFolder, "src", "MobileOrderApp.Android", "MobileOrderApp.Android.csproj");
            File.Exists(androidCsprojPath).Should().BeTrue("Android 專案檔應存在於匯出目錄");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{androidCsprojPath}\" -c Release -t:SignAndroidPackage -nodeReuse:false -p:UseSharedCompilation=false",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process.Should().NotBeNull();

            var stdoutTask = process!.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            process.ExitCode.Should().Be(0, $"Android 專案 build 應成功 (ExitCode 0)。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");

            // 驗證是否成功產生 .apk 檔案
            var androidDir = Path.Combine(tempFolder, "src", "MobileOrderApp.Android");
            var apkFiles = Directory.GetFiles(androidDir, "*.apk", SearchOption.AllDirectories);
            apkFiles.Should().NotBeEmpty("編譯後應於 bin / obj 目錄下產出 APK 檔案");
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
                    // 忽略清理鎖定例外
                }
            }
        }
    }

    [Fact]
    public void GenerateFullProject_WhenTargetLanguageIsFSharp_ShouldGenerateFSharpProjectFiles()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "FSharpOrderView",
            ViewModelClassName = "FSharpOrderViewModel",
            TargetLanguage = TargetLanguage.FSharp,
            RootNode = new AstNode
            {
                Type = ControlType.Canvas,
                Children = [new AstNode { Type = ControlType.Button, Content = "Submit" }]
            }
        };

        // Act
        var files = _exportService.GenerateFullProject(doc, new ProjectExportOptions(CustomProjectName: "FSharpApp"));

        // Assert
        files.Should().Contain(f => f.FileName == "FSharpApp.slnx");
        files.Should().Contain(f => f.FileName.EndsWith("FSharpApp.Shared.fsproj"));
        files.Should().Contain(f => f.FileName.EndsWith("FSharpApp.Desktop.fsproj"));
        files.Should().Contain(f => f.FileName.EndsWith("App.fs"));
        files.Should().Contain(f => f.FileName.EndsWith("Config.fs"));
        files.Should().Contain(f => f.FileName.EndsWith("FSharpOrderView.fs"));
        files.Should().Contain(f => f.FileName.EndsWith("FSharpOrderViewModel.fs"));
        files.Should().Contain(f => f.FileName.EndsWith("Program.fs"));
    }

    [Fact]
    public void GenerateFullProject_WhenTargetLanguageIsVisualBasic_ShouldGenerateVisualBasicProjectFiles()
    {
        // Arrange
        var doc = new FormDocument
        {
            ViewClassName = "VBOrderView",
            ViewModelClassName = "VBOrderViewModel",
            TargetLanguage = TargetLanguage.VisualBasic,
            RootNode = new AstNode
            {
                Type = ControlType.Canvas,
                Children = [new AstNode { Type = ControlType.Button, Content = "Submit" }]
            }
        };

        // Act
        var files = _exportService.GenerateFullProject(doc, new ProjectExportOptions(CustomProjectName: "VBApp"));

        // Assert
        files.Should().Contain(f => f.FileName == "VBApp.slnx");
        files.Should().Contain(f => f.FileName.EndsWith("VBApp.Shared.vbproj"));
        files.Should().Contain(f => f.FileName.EndsWith("VBApp.Desktop.vbproj"));
        files.Should().Contain(f => f.FileName.EndsWith("App.vb"));
        files.Should().Contain(f => f.FileName.EndsWith("Config.vb"));
        files.Should().Contain(f => f.FileName.EndsWith("VBOrderView.vb"));
        files.Should().Contain(f => f.FileName.EndsWith("VBOrderViewModel.vb"));
        files.Should().Contain(f => f.FileName.EndsWith("Program.vb"));
    }

    [Fact]
    public async Task ExportedProject_FSharp_ShouldCompileDirectlyWithDotnetCli()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_FSBuildTest_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "FSCustomerView",
            ViewModelClassName = "FSCustomerViewModel",
            TargetLanguage = TargetLanguage.FSharp,
            Title = "F# Customer System",
            RootNode = new AstNode
            {
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Name = "btnSubmit",
                        Type = ControlType.Button,
                        Content = "Submit",
                        CanvasLeft = 20,
                        CanvasTop = 30,
                        Width = 100,
                        Height = 40,
                        Bindings = [new BindingDefinition { TargetProperty = "Content", ViewModelProperty = "ButtonText" }],
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitCommand" }]
                    },
                    new AstNode
                    {
                        Name = "picAvatar",
                        Type = ControlType.PictureBox,
                        CanvasLeft = 140,
                        CanvasTop = 30,
                        Width = 80,
                        Height = 80,
                        InitBitmap = true,
                        BitmapBackgroundColor = "#38BDF8"
                    },
                    new AstNode
                    {
                        Name = "mediaPlayer1",
                        Type = ControlType.MediaPlayer,
                        CanvasLeft = 20,
                        CanvasTop = 120,
                        Width = 300,
                        Height = 200,
                        Source = "https://example.com/demo.mp4",
                        AutoPlay = true,
                        IsLooping = true
                    }
                ]
            }
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false, CustomProjectName: "FSCustomerApp"));

            var desktopFsprojPath = Path.Combine(tempFolder, "src", "FSCustomerApp.Desktop", "FSCustomerApp.Desktop.fsproj");
            File.Exists(desktopFsprojPath).Should().BeTrue("F# Desktop 專案檔應存在於匯出目錄");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{desktopFsprojPath}\" -c Release -nodeReuse:false -p:UseSharedCompilation=false",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process.Should().NotBeNull();

            var stdoutTask = process!.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            process.ExitCode.Should().Be(0, $"F# 專案 build 應成功 (ExitCode 0)。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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

    [Fact]
    public async Task ExportedProject_VisualBasic_ShouldCompileDirectlyWithDotnetCli()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AFG_VBBuildTest_" + Guid.NewGuid().ToString("N"));
        var doc = new FormDocument
        {
            ViewClassName = "VBCustomerView",
            ViewModelClassName = "VBCustomerViewModel",
            TargetLanguage = TargetLanguage.VisualBasic,
            Title = "VB Customer System",
            RootNode = new AstNode
            {
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Name = "btnSubmit",
                        Type = ControlType.Button,
                        Content = "Submit",
                        CanvasLeft = 20,
                        CanvasTop = 30,
                        Width = 100,
                        Height = 40,
                        Bindings = [new BindingDefinition { TargetProperty = "Content", ViewModelProperty = "ButtonText" }],
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "SubmitCommand" }]
                    },
                    new AstNode
                    {
                        Name = "picAvatar",
                        Type = ControlType.PictureBox,
                        CanvasLeft = 140,
                        CanvasTop = 30,
                        Width = 80,
                        Height = 80,
                        InitBitmap = true,
                        BitmapBackgroundColor = "#38BDF8"
                    },
                    new AstNode
                    {
                        Name = "mediaPlayer1",
                        Type = ControlType.MediaPlayer,
                        CanvasLeft = 20,
                        CanvasTop = 120,
                        Width = 300,
                        Height = 200,
                        Source = "https://example.com/demo.mp4",
                        AutoPlay = true,
                        IsLooping = true
                    }
                ]
            }
        };

        try
        {
            await _exportService.ExportToFolderAsync(doc, tempFolder, new ProjectExportOptions(IncludeMobileProject: false, CustomProjectName: "VBCustomerApp"));

            var desktopVbprojPath = Path.Combine(tempFolder, "src", "VBCustomerApp.Desktop", "VBCustomerApp.Desktop.vbproj");
            File.Exists(desktopVbprojPath).Should().BeTrue("VB Desktop 專案檔應存在於匯出目錄");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{desktopVbprojPath}\" -c Release -nodeReuse:false -p:UseSharedCompilation=false",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process.Should().NotBeNull();

            var stdoutTask = process!.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            process.ExitCode.Should().Be(0, $"VB 專案 build 應成功 (ExitCode 0)。\n標準輸出:\n{stdout}\n錯誤輸出:\n{stderr}");
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
    /// 檢查當前執行環境是否已完整安裝 Android SDK 與 .NET Android Workload。
    /// </summary>
    private static bool IsAndroidBuildEnvironmentAvailable()
    {
        if (!IsAndroidSdkInstalled())
        {
            return false;
        }

        return IsDotNetAndroidWorkloadInstalled();
    }

    /// <summary>
    /// 檢查 .NET SDK 是否已安裝 android workload。
    /// </summary>
    private static bool IsDotNetAndroidWorkloadInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "workload list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            return stdout.Contains("android", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 檢查當前執行環境是否已安裝 Android SDK。
    /// </summary>
    private static bool IsAndroidSdkInstalled()
    {
        var candidates = new List<string?>
        {
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
        };

        if (OperatingSystem.IsWindows())
        {
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Android", "sdk"));
        }
        else if (OperatingSystem.IsLinux())
        {
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Android", "Sdk"));
            candidates.Add("/usr/lib/android-sdk");
            candidates.Add("/opt/android-sdk");
        }

        foreach (var dir in candidates)
        {
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                var platformsDir = Path.Combine(dir, "platforms");
                var buildToolsDir = Path.Combine(dir, "build-tools");
                if (Directory.Exists(platformsDir) && Directory.EnumerateFileSystemEntries(platformsDir).Any() &&
                    Directory.Exists(buildToolsDir) && Directory.EnumerateFileSystemEntries(buildToolsDir).Any())
                {
                    return true;
                }
            }
        }

        return false;
    }
}

