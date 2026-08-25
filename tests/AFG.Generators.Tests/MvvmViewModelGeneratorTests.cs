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
        result.Content.Should().Contain("_pollTimer.Tick += (s, e) => TimerTickCommand.Execute(null);");
        result.Content.Should().Contain("_taskWorker.DoWork += (s, e) => PerformWorkCommand.Execute(null);");
        result.Content.Should().Contain("_taskWorker.RunWorkerCompleted += (s, e) => WorkCompletedCommand.Execute(null);");
        result.Content.Should().Contain("_bleScanner.DataReceived += (s, e) => BleDataCommand.Execute(null);");
        result.Content.Should().Contain("_serialDevice.DataReceived += (s, e) => SerialDataCommand.Execute(null);");

        // 驗證生成的 Commands
        result.Content.Should().Contain("private async Task TimerTickAsync()");
        result.Content.Should().Contain("private async Task PerformWorkAsync()");
        result.Content.Should().Contain("private void WorkCompleted()");
        result.Content.Should().Contain("private async Task BleDataAsync()");
        result.Content.Should().Contain("private async Task SerialDataAsync()");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldSupportDialogComponents_AndRegisterEventCallbacks()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "DialogApp.ViewModels",
            ViewModelClassName = "DialogFormViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
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
                        Name = "AlertBox",
                        Type = ControlType.MessageBox,
                        Events = [new EventMappingDefinition { EventName = "Confirmed", CommandProperty = "OnAlertConfirmedCommand", IsAsync = false }]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("private readonly OpenFileDialog _openFileDlg = new();");
        result.Content.Should().Contain("private readonly SaveFileDialog _saveFileDlg = new();");
        result.Content.Should().Contain("private readonly MessageBox _alertBox = new();");

        // 驗證建構子內的事件回呼掛載
        result.Content.Should().Contain("public DialogFormViewModel()");
        result.Content.Should().Contain("_openFileDlg.FileOk += (s, e) => FileOpenedCommand.Execute(null);");
        result.Content.Should().Contain("_saveFileDlg.FileOk += (s, e) => FileSavedCommand.Execute(null);");
        result.Content.Should().Contain("_alertBox.Confirmed += (s, e) => AlertConfirmedCommand.Execute(null);");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WithDebugConsole_ShouldInjectLogServiceAndExposeLogEntriesAndClearCommand()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "DiagnosticsApp.ViewModels",
            ViewModelClassName = "DiagnosticsViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "console1",
                        Name = "LiveConsole",
                        Type = ControlType.DebugConsole
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("using Microsoft.Extensions.Logging;");
        result.Content.Should().Contain("using DiagnosticsApp.ViewModels.Services;");
        result.Content.Should().Contain("private readonly InMemoryLogService? _logService;");
        result.Content.Should().Contain("private readonly ILogger<DiagnosticsViewModel>? _logger;");
        result.Content.Should().Contain("private ObservableCollection<LogEntry> _logEntries = [];");
        result.Content.Should().Contain("public DiagnosticsViewModel(InMemoryLogService? logService = null, ILogger<DiagnosticsViewModel>? logger = null) : this()");
        result.Content.Should().Contain("_logService = logService;");
        result.Content.Should().Contain("_logger = logger;");
        result.Content.Should().Contain("_logEntries = logService.Logs;");
        result.Content.Should().Contain("private void ClearLogs()");
        result.Content.Should().Contain("_logService?.Clear();");
        result.Content.Should().Contain("_logEntries.Clear();");

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
    public void Generate_ForPictureBoxWithInitBitmap_ShouldGenerateInitializedDefaultValue()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "CanvasEditorViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "pic",
                        Name = "CanvasBox",
                        Type = ControlType.PictureBox,
                        Width = 500,
                        Height = 400,
                        InitBitmap = true,
                        BitmapBackgroundColor = "#EEEEEE",
                        Bindings = [
                            new BindingDefinition { TargetProperty = "Source", ViewModelProperty = "CanvasBitmap" }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("private Avalonia.Media.IImage? _canvasBitmap = BitmapHelper.CreateInitializedBitmap(500, 400, Brush.Parse(\"#EEEEEE\"));");

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
        result.Content.Should().Contain("_pollTimer.Tick += (s, e) => PollTickCommand.Execute(null);");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenEventHasParameterType_ShouldProduceParameterizedRelayCommandMethod()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "ItemManagementViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.StackPanel,
                Children = [
                    new AstNode
                    {
                        Id = "btnDelete",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition
                            {
                                EventName = "Click",
                                CommandProperty = "DeleteItemCommand",
                                CommandParameterProperty = "id",
                                ParameterType = "int",
                                IsAsync = true
                            }
                        ]
                    },
                    new AstNode
                    {
                        Id = "btnSearch",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition
                            {
                                EventName = "Click",
                                CommandProperty = "FilterItemsCommand",
                                CommandParameterProperty = "keyword",
                                ParameterType = "string",
                                IsAsync = false
                            }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("[RelayCommand]");
        result.Content.Should().Contain("private async Task DeleteItemAsync(int? id = default)");
        result.Content.Should().Contain("private void FilterItems(string? keyword = default)");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ForClickAndPointerEvents_ShouldIncludeAvaloniaEventNamespacesAndEventArgsParameters()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "EventsFormViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "btnSubmit",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition
                            {
                                EventName = "Click",
                                CommandProperty = "SubmitCommand",
                                ParameterType = "RoutedEventArgs",
                                CommandParameterProperty = "e",
                                IsAsync = true
                            },
                            new EventMappingDefinition
                            {
                                EventName = "PointerPressed",
                                CommandProperty = "OnCanvasPressedCommand",
                                ParameterType = "PointerPressedEventArgs",
                                CommandParameterProperty = "e",
                                IsAsync = false
                            }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("using Avalonia.Interactivity;");
        result.Content.Should().Contain("using Avalonia.Input;");
        result.Content.Should().Contain("private async Task SubmitAsync(RoutedEventArgs? e = default)");
        result.Content.Should().Contain("private void CanvasPressed(PointerPressedEventArgs? e = default)");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhenEventHasMultipleParameters_ShouldEmitMethodWithAllParameters()
    {
        // Arrange
        var doc = new FormDocument
        {
            RootNamespace = "MyApp.ViewModels",
            ViewModelClassName = "MultiParamFormViewModel",
            RootNode = new AstNode
            {
                Id = "root",
                Type = ControlType.Canvas,
                Children = [
                    new AstNode
                    {
                        Id = "btnSave",
                        Type = ControlType.Button,
                        Events = [
                            new EventMappingDefinition
                            {
                                EventName = "Click",
                                CommandProperty = "SaveWithContextCommand",
                                Parameters = [
                                    new EventParameterDefinition("sender", "object?"),
                                    new EventParameterDefinition("e", "RoutedEventArgs"),
                                    new EventParameterDefinition("forceSave", "bool")
                                ],
                                IsAsync = true
                            }
                        ]
                    }
                ]
            }
        };

        // Act
        var result = _generator.Generate(doc);

        // Assert
        result.Content.Should().Contain("[RelayCommand]");
        result.Content.Should().Contain("private async Task SaveWithContextAsync((object? sender, RoutedEventArgs? e, bool? forceSave)? args = null)");

        var diagnostics = RoslynCompilerService.CheckSyntaxDiagnostics(result.Content);
        diagnostics.Should().BeEmpty();
    }
}
