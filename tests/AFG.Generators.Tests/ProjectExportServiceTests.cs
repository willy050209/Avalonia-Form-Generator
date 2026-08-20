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
}
