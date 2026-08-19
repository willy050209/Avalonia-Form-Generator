// filepath: tests/AFG.Generators.Tests/MvvmViewModelGeneratorTests.cs
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Generators.Mvvm;
using AFG.Generators.Roslyn;

namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 CommunityToolkit.Mvvm ViewModel 生成器，包含自訂 C# 型別、動態 DI 服務注入、非同步/同步命令。
/// </summary>
public sealed class MvvmViewModelGeneratorTests
{
    private readonly MvvmViewModelGenerator _generator = new();

    [Fact]
    public void Generate_ShouldProduceCleanParameterlessConstructor_WhenInjectedServicesIsEmpty()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "UserFormViewModel",
            InjectedServices = [],
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
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "SaveCommand", IsAsync = true }
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
        result.Content.Should().NotContain("IGreetingService");
        result.Content.Should().Contain("[ObservableProperty]");
        result.Content.Should().Contain("private string _username = string.Empty;");
        result.Content.Should().Contain("private bool _isAdmin;");
        result.Content.Should().Contain("[RelayCommand]");
        result.Content.Should().Contain("private async Task SaveAsync()");
        result.Content.Should().Contain("await Task.CompletedTask;");

        // 語法樹診斷檢查
        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldSupportCustomDataTypes_AndDynamicDIServices()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "OrderViewModel",
            InjectedServices = [
                new ServiceDependencyDefinition { InterfaceName = "IOrderService" },
                new ServiceDependencyDefinition { InterfaceName = "IAuthService" }
            ],
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.StackPanel,
                Children = [
                    new AstNode
                    {
                        Id = "tbAmount",
                        Type = ControlType.TextBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "TotalAmount", CustomDataType = "decimal" }
                        ]
                    },
                    new AstNode
                    {
                        Id = "comboItems",
                        Type = ControlType.ComboBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "ItemsSource", ViewModelProperty = "ItemsList", CustomDataType = "ObservableCollection<string>" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("private readonly IOrderService? _orderService;");
        result.Content.Should().Contain("private readonly IAuthService? _authService;");
        result.Content.Should().Contain("public OrderViewModel(IOrderService orderService, IAuthService authService)");
        result.Content.Should().Contain("private decimal _totalAmount;");
        result.Content.Should().Contain("private ObservableCollection<string> _itemsList = [];");

        var syntaxDiagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        syntaxDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldSupportSynchronousRelayCommands_WhenIsAsyncIsFalse()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "SyncTestViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.StackPanel,
                Children = [
                    new AstNode
                    {
                        Id = "btnSync",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "ResetCommand", IsAsync = false }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("[RelayCommand]");
        result.Content.Should().Contain("private void Reset()");
        result.Content.Should().NotContain("private async Task ResetAsync()");
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
                        Events = [new EventMappingDefinition { EventName = "Click", CommandProperty = "RefreshCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "2",
                        Type = ControlType.TextBlock,
                        Bindings = [new BindingDefinition { TargetProperty = "Text", ViewModelProperty = "SharedTitle" }],
                        Events = [new EventMappingDefinition { EventName = "Tapped", CommandProperty = "RefreshCommand", IsAsync = true }]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        var propertyOccurrences = result.Content.Split("private string _sharedTitle").Length - 1;
        var commandOccurrences = result.Content.Split("private async Task RefreshAsync()").Length - 1;

        propertyOccurrences.Should().Be(1);
        commandOccurrences.Should().Be(1);
    }

    [Fact]
    public void Generate_ShouldSupportNonVisualHardwareComponents_AndRegisterEventCallbacks()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "HardwareApp.ViewModels",
            ViewModelClassName = "HardwareFormViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "tmr",
                        Name = "PollTimer",
                        Type = ControlType.DispatcherTimer,
                        Events = [new EventMappingDefinition { EventName = "Tick", CommandProperty = "OnTimerTickCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "bgw",
                        Name = "TaskWorker",
                        Type = ControlType.BackgroundWorker,
                        Events = [
                            new EventMappingDefinition { EventName = "DoWork", CommandProperty = "PerformWorkCommand", IsAsync = true },
                            new EventMappingDefinition { EventName = "RunWorkerCompleted", CommandProperty = "WorkCompletedCommand", IsAsync = false }
                        ]
                    },
                    new AstNode
                    {
                        Id = "ble",
                        Name = "BleScanner",
                        Type = ControlType.BluetoothClient,
                        Events = [new EventMappingDefinition { EventName = "DataReceived", CommandProperty = "OnBleDataCommand", IsAsync = true }]
                    },
                    new AstNode
                    {
                        Id = "com",
                        Name = "SerialDevice",
                        Type = ControlType.SerialPortService,
                        Events = [new EventMappingDefinition { EventName = "DataReceived", CommandProperty = "OnSerialDataCommand", IsAsync = true }]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("private readonly DispatcherTimer _pollTimer = new();");
        result.Content.Should().Contain("private readonly BackgroundWorker _taskWorker = new();");
        result.Content.Should().Contain("private readonly BluetoothClient _bleScanner = new();");
        result.Content.Should().Contain("private readonly SerialPortService _serialDevice = new();");

        // 驗證建構子內的事件回呼掛載
        result.Content.Should().Contain("public HardwareFormViewModel()");
        result.Content.Should().Contain("_pollTimer.Tick += (s, e) => OnTimerTickCommand.Execute(null);");
        result.Content.Should().Contain("_taskWorker.DoWork += (s, e) => PerformWorkCommand.Execute(null);");
        result.Content.Should().Contain("_taskWorker.RunWorkerCompleted += (s, e) => WorkCompletedCommand.Execute(null);");
        result.Content.Should().Contain("_bleScanner.DataReceived += (s, e) => OnBleDataCommand.Execute(null);");
        result.Content.Should().Contain("_serialDevice.DataReceived += (s, e) => OnSerialDataCommand.Execute(null);");

        // 驗證生成的 Commands
        result.Content.Should().Contain("private async Task OnTimerTickAsync()");
        result.Content.Should().Contain("private async Task PerformWorkAsync()");
        result.Content.Should().Contain("private void WorkCompleted()");
        result.Content.Should().Contain("private async Task OnBleDataAsync()");
        result.Content.Should().Contain("private async Task OnSerialDataAsync()");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ForPictureBox_ShouldInferImageSourceProperty()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "ImageViewerViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "pic",
                        Name = "PhotoDisplay",
                        Type = ControlType.PictureBox,
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Source", ViewModelProperty = "UserProfileImage" }
                        ],
                        Events = [
                            new EventMappingDefinition { EventName = "Click", CommandProperty = "ChangeImageCommand", IsAsync = true }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("private Avalonia.Media.IImage? _userProfileImage;");
        result.Content.Should().Contain("public partial class ImageViewerViewModel : ObservableObject");
        result.Content.Should().Contain("private async Task ChangeImageAsync()");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ForDispatcherTimer_ShouldIncludeConfiguredInterval()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "TimerViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "tmr",
                        Name = "PollTimer",
                        Type = ControlType.DispatcherTimer,
                        Interval = 250,
                        Events = [
                            new EventMappingDefinition { EventName = "Tick", CommandProperty = "OnPollTickCommand" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };");
        result.Content.Should().Contain("_pollTimer.Tick += (s, e) => OnPollTickCommand.Execute(null);");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }
}
