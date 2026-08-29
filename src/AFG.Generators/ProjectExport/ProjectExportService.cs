// filepath: src/AFG.Generators/ProjectExport/ProjectExportService.cs
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;
using AFG.Generators.Constants;
using AFG.Generators.CSharpMarkup;
using AFG.Generators.FSharp;
using AFG.Generators.Mvvm;
using AFG.Generators.VisualBasic;

namespace AFG.Generators.ProjectExport;

/// <summary>
/// 專案匯出選項設定。
/// </summary>
public sealed record ProjectExportOptions(
    bool IncludeMobileProject = true,
    bool IncludeLicense = false,
    string? CustomProjectName = null);

/// <summary>
/// 產出具備相依性注入 (DI)、多表單導航 (NavigationService) 與跨平台多專案架構 (.Shared, .Desktop, 可選 .Android) 之 Avalonia 現代化方案匯出服務。
/// </summary>
public sealed class ProjectExportService(FormCodeGenerator? codeGenerator = null)
{
    private readonly FormCodeGenerator _codeGenerator = codeGenerator ?? new FormCodeGenerator();
    private readonly FSharpViewGenerator _fsharpViewGenerator = new();
    private readonly FSharpViewModelGenerator _fsharpViewModelGenerator = new();
    private readonly VisualBasicViewGenerator _vbViewGenerator = new();
    private readonly VisualBasicViewModelGenerator _vbViewModelGenerator = new();

    /// <summary>
    /// 生成包含 Visual Studio 現代化方案檔 (.slnx)、.Shared 共用核心、.Desktop 桌面宿主及可選 .Android 行動端專案的單一表單檔案集合。
    /// </summary>
    public IReadOnlyList<GeneratedSourceFile> GenerateFullProject(FormDocument document, ProjectExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return GenerateMultiFormProject(FormProjectDefinition.FromSingleDocument(document), options);
    }

    /// <summary>
    /// 生成包含多表單 (Multi-Form) 與導航服務 (NavigationService) 的完整跨平台方案檔案集合。
    /// </summary>
    public IReadOnlyList<GeneratedSourceFile> GenerateMultiFormProject(FormProjectDefinition project, ProjectExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        options ??= new ProjectExportOptions();

        if (project.Documents.Count == 0)
        {
            throw new ArgumentException("專案必須至少包含一個表單文檔。", nameof(project));
        }

        if (project.TargetLanguage == TargetLanguage.FSharp)
        {
            return GenerateFSharpProject(project, options);
        }

        if (project.TargetLanguage == TargetLanguage.VisualBasic)
        {
            return GenerateVisualBasicProject(project, options);
        }

        var files = new List<GeneratedSourceFile>();
        var rawProjectName = string.IsNullOrWhiteSpace(options.CustomProjectName) ? project.ProjectName : options.CustomProjectName;
        var baseProjectName = SanitizeProjectName(rawProjectName);

        var sharedProjectName = $"{baseProjectName}.Shared";
        var desktopProjectName = $"{baseProjectName}.Desktop";
        var androidProjectName = $"{baseProjectName}.Android";

        var sharedDir = Path.Combine("src", sharedProjectName);
        var desktopDir = Path.Combine("src", desktopProjectName);
        var androidDir = Path.Combine("src", androidProjectName);

        // ==========================================
        // 1. 方案根目錄：Visual Studio 現代化方案檔 (.slnx)
        // ==========================================
        var slnxBuilder = new StringBuilder();
        slnxBuilder.AppendLine("<Solution>");
        slnxBuilder.AppendLine($"  <Project Path=\"src/{sharedProjectName}/{sharedProjectName}.csproj\" />");
        slnxBuilder.AppendLine($"  <Project Path=\"src/{desktopProjectName}/{desktopProjectName}.csproj\" />");
        if (options.IncludeMobileProject)
        {
            slnxBuilder.AppendLine($"  <Project Path=\"src/{androidProjectName}/{androidProjectName}.csproj\" />");
        }
        slnxBuilder.AppendLine("</Solution>");

        files.Add(new GeneratedSourceFile($"{baseProjectName}.slnx", slnxBuilder.ToString().TrimEnd(), SourceFileType.SolutionFile));

        // 方案根目錄：LICENSE (可選), .gitignore 與 .editorconfig
        if (options.IncludeLicense)
        {
            var mitLicenseContent = $"""
            MIT License

            Copyright (c) {DateTime.Now.Year}

            Permission is hereby granted, free of charge, to any person obtaining a copy
            of this software and associated documentation files (the "Software"), to deal
            in the Software without restriction, including without limitation the rights
            to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
            copies of the Software, and to permit persons to whom the Software is
            furnished to do so, subject to the following conditions:

            The above copyright notice and this permission notice shall be included in all
            copies or substantial portions of the Software.

            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
            IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
            FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
            AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
            LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
            OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
            SOFTWARE.
            """;
            files.Add(new GeneratedSourceFile("LICENSE", mitLicenseContent, SourceFileType.ProjectFile));
        }

        var gitignoreContent = """
        ## Visual Studio & .NET
        .vs/
        [Bb]in/
        [Oo]bj/
        *.user
        *.suo
        *.userosscache
        *.sln.docstates
        .idea/
        *.apk
        *.aab
        """;
        files.Add(new GeneratedSourceFile(".gitignore", gitignoreContent, SourceFileType.ProjectFile));

        var editorconfigContent = """
        root = true

        [*]
        indent_style = space
        indent_size = 4
        end_of_line = lf
        charset = utf-8
        trim_trailing_whitespace = true
        insert_final_newline = true

        [*.cs]
        csharp_prefer_braces = true:suggestion
        csharp_prefer_simple_using_statement = true:suggestion
        csharp_style_namespace_declarations = file_scoped:suggestion
        """;
        files.Add(new GeneratedSourceFile(".editorconfig", editorconfigContent, SourceFileType.ProjectFile));

        // ==========================================
        // 2. 共用核心專案：src/{ProjectName}.Shared/
        // ==========================================
        var sharedCsprojContent = $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <LangVersion>latest</LangVersion>
            <RootNamespace>{project.RootNamespace}</RootNamespace>
            <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
            <NoWarn>$(NoWarn);NU1903</NoWarn>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Avalonia" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Themes.Fluent" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="CommunityToolkit.Mvvm" Version="{PackageVersions.CommunityToolkitMvvm}" />
            <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="{PackageVersions.MicrosoftExtensionsDependencyInjection}" />
            <PackageReference Include="Microsoft.Extensions.Logging" Version="{PackageVersions.MicrosoftExtensionsLogging}" />
          </ItemGroup>

          <ItemGroup>
            <AvaloniaResource Include="Assets\**" />
            <None Update="Assets\**" CopyToOutputDirectory="PreserveNewest" />
          </ItemGroup>
        </Project>
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, $"{sharedProjectName}.csproj"), sharedCsprojContent, SourceFileType.ProjectFile));

        // 彙整所有文檔注入的服務 (包含 AST 中硬體通訊不可視元件)
        var hardwareServiceNodes = project.Documents
            .SelectMany(d => AstTreeOperations.Flatten(d.RootNode))
            .Where(n => n.Type is ControlType.BluetoothClient or ControlType.SerialPortService)
            .ToList();

        var synthesizedServices = new List<ServiceDependencyDefinition>();
        if (hardwareServiceNodes.Any(n => n.Type == ControlType.BluetoothClient))
        {
            synthesizedServices.Add(new ServiceDependencyDefinition { InterfaceName = "IBluetoothClient", ImplementationName = "BluetoothClient" });
        }
        if (hardwareServiceNodes.Any(n => n.Type == ControlType.SerialPortService))
        {
            synthesizedServices.Add(new ServiceDependencyDefinition { InterfaceName = "ISerialPortService", ImplementationName = "SerialPortService" });
        }

        var allServices = project.Documents
            .SelectMany(d => d.InjectedServices)
            .Concat(synthesizedServices)
            .DistinctBy(s => s.InterfaceName)
            .ToList();

        var serviceRegistrations = allServices.Count > 0
            ? string.Join("\n", allServices.Select(s => $"        services.AddSingleton<{s.InterfaceName}, {(!string.IsNullOrEmpty(s.ImplementationName) ? s.ImplementationName : (s.InterfaceName.StartsWith('I') ? s.InterfaceName[1..] : $"{s.InterfaceName}Impl"))}>();")) + "\n"
            : string.Empty;

        // 彙整所有 View 與 ViewModel 註冊
        var viewRegistrations = new StringBuilder();
        foreach (var doc in project.Documents)
        {
            if (doc.ArchitectureMode == ArchitectureMode.CodeBehind)
            {
                viewRegistrations.AppendLine($"        // 註冊 {doc.ViewClassName} (Code-Behind 模式)");
                viewRegistrations.AppendLine($"        services.AddTransient<{doc.ViewClassName}>();");
            }
            else
            {
                viewRegistrations.AppendLine($"        // 註冊 {doc.ViewClassName} 與對應 ViewModel");
                viewRegistrations.AppendLine($"        services.AddTransient<{doc.ViewModelClassName}>();");
                viewRegistrations.AppendLine($"        services.AddTransient<{doc.ViewClassName}>(sp =>");
                viewRegistrations.AppendLine("        {");
                viewRegistrations.AppendLine($"            var view = new {doc.ViewClassName}();");
                viewRegistrations.AppendLine($"            view.DataContext = sp.GetRequiredService<{doc.ViewModelClassName}>();");
                viewRegistrations.AppendLine("            return view;");
                viewRegistrations.AppendLine("        });");
            }
        }

        var initialDoc = project.Documents.FirstOrDefault(d => d.ViewClassName == project.InitialFormName) ?? project.Documents[0];

        var minWidthProp = initialDoc.MinWidth.HasValue ? $"MinWidth = {initialDoc.MinWidth.Value.ToString(CultureInfo.InvariantCulture)},\n                        " : "";
        var minHeightProp = initialDoc.MinHeight.HasValue ? $"MinHeight = {initialDoc.MinHeight.Value.ToString(CultureInfo.InvariantCulture)},\n                        " : "";
        var maxWidthProp = initialDoc.MaxWidth.HasValue ? $"MaxWidth = {initialDoc.MaxWidth.Value.ToString(CultureInfo.InvariantCulture)},\n                        " : "";
        var maxHeightProp = initialDoc.MaxHeight.HasValue ? $"MaxHeight = {initialDoc.MaxHeight.Value.ToString(CultureInfo.InvariantCulture)},\n                        " : "";
        var bgProp = !string.IsNullOrWhiteSpace(initialDoc.BackgroundColor) ? $"Background = Brush.Parse(\"{initialDoc.BackgroundColor}\"),\n                        " : "";
        var iconProp = !string.IsNullOrWhiteSpace(initialDoc.Icon) ? $"Icon = new WindowIcon(BitmapHelper.LoadBitmap(\"{initialDoc.Icon}\")!),\n                        " : "";

        // App.cs（內建 DI 相依性注入、導航容器與視窗最大化配置）
        var appCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}};

        /// <summary>
        /// 跨平台 Avalonia 應用程式主入口與全域相依性注入容器設定。
        /// </summary>
        public partial class App : Application
        {
            public static IServiceProvider Services { get; private set; } = null!;
            private static Window? s_mainWindow;
            private static ISingleViewApplicationLifetime? s_singleViewLifetime;

            public override void Initialize()
            {
                Styles.Add(new FluentTheme());
                RequestedThemeVariant = ThemeVariant.Dark;
            }

            public override void OnFrameworkInitializationCompleted()
            {
                // 配置相依性注入容器
                var services = new ServiceCollection();
                ConfigureServices(services);
                Services = services.BuildServiceProvider();

                // 桌面端生命週期 
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var initialView = Services.GetRequiredService<{{initialDoc.ViewClassName}}>();
                    s_mainWindow = new Window
                    {
                        Title = Config.AppTitle,
                        Width = Config.DefaultWindowWidth,
                        Height = Config.DefaultWindowHeight,
                        {{minWidthProp}}{{minHeightProp}}{{maxWidthProp}}{{maxHeightProp}}{{bgProp}}WindowStartupLocation = WindowStartupLocation.{{initialDoc.WindowStartupLocation}},
                        WindowState = WindowState.{{initialDoc.WindowState}},
                        CanResize = Config.CanResize,
                        Topmost = Config.Topmost,
                        ShowInTaskbar = Config.ShowInTaskbar,
                        SystemDecorations = SystemDecorations.{{initialDoc.SystemDecorations}},
                        {{iconProp}}Content = initialView
                    };
                    desktop.MainWindow = s_mainWindow;
                }
                // 行動端生命週期 (Android / iOS 單視圖呈現)
                else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                {
                    s_singleViewLifetime = singleView;
                    singleView.MainView = Services.GetRequiredService<{{initialDoc.ViewClassName}}>();
                }

                base.OnFrameworkInitializationCompleted();
            }

            public static void SetActiveView(Control view)
            {
                if (s_mainWindow is not null)
                {
                    s_mainWindow.Content = view;
                }
                else if (s_singleViewLifetime is not null)
                {
                    s_singleViewLifetime.MainView = view;
                }
            }

            public static TopLevel? GetTopLevel() => (TopLevel?)s_mainWindow ?? (TopLevel?)s_singleViewLifetime?.MainView;
            public static Window? GetMainWindow() => s_mainWindow;

            private static void ConfigureServices(IServiceCollection services)
            {
                var logService = new InMemoryLogService();
                logService.RedirectStandardOutput();
                services.AddSingleton(logService);

                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                    builder.AddProvider(new InMemoryLoggerProvider(logService));
                });

        {{serviceRegistrations}}
                // 註冊導航服務 (NavigationService) 與對話方塊服務 (DialogService)
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();

        {{viewRegistrations.ToString().TrimEnd()}}
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "App.cs"), appCs, SourceFileType.ProjectFile));

        // Config.cs
        var configCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}};

        /// <summary>
        /// 全域靜態組態配置（視窗大小、標題、版本與目標平台等）。
        /// </summary>
        public static class Config
        {
            public const string AppTitle = "{{project.Title}}";
            public const string Version = "1.0.0";
            public const double DefaultWindowWidth = {{initialDoc.CanvasWidth.ToString(CultureInfo.InvariantCulture)}};
            public const double DefaultWindowHeight = {{initialDoc.CanvasHeight.ToString(CultureInfo.InvariantCulture)}};
            public const bool CanResize = {{(initialDoc.CanResize ? "true" : "false")}};
            public const bool Topmost = {{(initialDoc.Topmost ? "true" : "false")}};
            public const bool ShowInTaskbar = {{(initialDoc.ShowInTaskbar ? "true" : "false")}};
            public const bool IsMobileSupported = {{(options.IncludeMobileProject ? "true" : "false")}};
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Config.cs"), configCs, SourceFileType.ProjectFile));

        // GlobalUsings.cs
        var globalUsingsCs = $$"""
        // <auto-generated />
        global using System;
        global using System.Collections.Generic;
        global using System.Collections.ObjectModel;
        global using System.ComponentModel;
        global using System.Diagnostics;
        global using System.Globalization;
        global using System.IO;
        global using System.Linq;
        global using System.Linq.Expressions;
        global using System.Net.Http;
        global using System.Numerics;
        global using System.Reflection;
        global using System.Runtime.CompilerServices;
        global using System.Runtime.InteropServices;
        global using System.Runtime.Intrinsics;
        global using System.Text;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using System.Windows.Input;
        global using Avalonia;
        global using Avalonia.Animation;
        global using Avalonia.Controls;
        global using Avalonia.Controls.ApplicationLifetimes;
        global using Avalonia.Controls.Primitives;
        global using Avalonia.Controls.Shapes;
        global using Avalonia.Data;
        global using Avalonia.Input;
        global using Avalonia.Interactivity;
        global using Avalonia.Layout;
        global using Avalonia.Media;
        global using Avalonia.Media.Imaging;
        global using Avalonia.Platform;
        global using Avalonia.Platform.Storage;
        global using Avalonia.Styling;
        global using Avalonia.Themes.Fluent;
        global using Avalonia.Threading;
        global using CommunityToolkit.Mvvm.ComponentModel;
        global using CommunityToolkit.Mvvm.Input;
        global using Microsoft.Extensions.DependencyInjection;
        global using Microsoft.Extensions.Logging;
        global using {{project.RootNamespace}};
        global using {{project.RootNamespace}}.Services;
        global using OpenFileDialog = {{project.RootNamespace}}.Services.OpenFileDialog;
        global using SaveFileDialog = {{project.RootNamespace}}.Services.SaveFileDialog;
        global using MessageBox = {{project.RootNamespace}}.Services.MessageBox;
        global using LogEntry = {{project.RootNamespace}}.Services.LogEntry;
        global using InMemoryLogService = {{project.RootNamespace}}.Services.InMemoryLogService;
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "GlobalUsings.cs"), globalUsingsCs, SourceFileType.ProjectFile));

        // C# Declarative UI 擴充方法：Markup/AvaloniaMarkupExtensions.cs
        files.Add(new GeneratedSourceFile(
            Path.Combine(sharedDir, "Markup", "AvaloniaMarkupExtensions.cs"),
            AvaloniaMarkupExtensionsSource.Code,
            SourceFileType.ProjectFile));

        // 導航介面與實作：Services/INavigationService.cs & Services/NavigationService.cs
        var inavCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 提供多表單與頁面間切換導航的統一介面。
        /// </summary>
        public interface INavigationService
        {
            void NavigateTo<TView>() where TView : Control;
            void NavigateTo(Type viewType);
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "INavigationService.cs"), inavCs, SourceFileType.ProjectFile));

        var navCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 透過 DI 容器與 App 宿主進行視圖切換的導航服務實作。
        /// </summary>
        public sealed class NavigationService(IServiceProvider serviceProvider) : INavigationService
        {
            public void NavigateTo<TView>() where TView : Control => NavigateTo(typeof(TView));

            public void NavigateTo(Type viewType)
            {
                ArgumentNullException.ThrowIfNull(viewType);
                var view = (Control)serviceProvider.GetRequiredService(viewType);
                App.SetActiveView(view);
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "NavigationService.cs"), navCs, SourceFileType.ProjectFile));

        // 對話方塊介面與實作：Services/IDialogService.cs & Services/DialogService.cs & Services/MessageBoxWindow.cs
        var idialogCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 提供檔案選取、儲存與訊息對話框的統一跨平台服務介面。
        /// </summary>
        public interface IDialogService
        {
            string? ShowOpenFileDialog(string title = "開啟檔案", string? filter = null);
            Task<string?> ShowOpenFileDialogAsync(string title = "開啟檔案", string? filter = null);
            string? ShowSaveFileDialog(string title = "儲存檔案", string defaultFileName = "Untitled", string? defaultExtension = null, string? filter = null);
            Task<string?> ShowSaveFileDialogAsync(string title = "儲存檔案", string defaultFileName = "Untitled", string? defaultExtension = null, string? filter = null);
            bool ShowMessageBox(string message, string title = "提示");
            Task<bool> ShowMessageBoxAsync(string message, string title = "提示");
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "IDialogService.cs"), idialogCs, SourceFileType.ProjectFile));

        var dialogCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 透過 Avalonia 現代化 StorageProvider 與自訂 MessageBox 對話框實現的對話方塊服務。
        /// </summary>
        public sealed class DialogService : IDialogService
        {
            public string? ShowOpenFileDialog(string title = "開啟檔案", string? filter = null) =>
                RunSynchronously(() => ShowOpenFileDialogAsync(title, filter));

            public async Task<string?> ShowOpenFileDialogAsync(string title = "開啟檔案", string? filter = null)
            {
                var topLevel = App.GetTopLevel();
                if (topLevel?.StorageProvider is null) return null;

                var options = new FilePickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false
                };

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    options.FileTypeFilter =
                    [
                        new FilePickerFileType(filter)
                        {
                            Patterns = [$"{filter}", "*.json", "*.*"]
                        }
                    ];
                }

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
                return files.Count > 0 ? files[0].Path.LocalPath : null;
            }

            public string? ShowSaveFileDialog(string title = "儲存檔案", string defaultFileName = "Untitled", string? defaultExtension = null, string? filter = null) =>
                RunSynchronously(() => ShowSaveFileDialogAsync(title, defaultFileName, defaultExtension, filter));

            public async Task<string?> ShowSaveFileDialogAsync(string title = "儲存檔案", string defaultFileName = "Untitled", string? defaultExtension = null, string? filter = null)
            {
                var topLevel = App.GetTopLevel();
                if (topLevel?.StorageProvider is null) return null;

                var options = new FilePickerSaveOptions
                {
                    Title = title,
                    SuggestedFileName = defaultFileName,
                    DefaultExtension = defaultExtension
                };

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    options.FileTypeChoices =
                    [
                        new FilePickerFileType(filter)
                        {
                            Patterns = [$"*.{filter}", "*.*"]
                        }
                    ];
                }

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
                return file?.Path.LocalPath;
            }

            public bool ShowMessageBox(string message, string title = "提示") =>
                RunSynchronously(() => ShowMessageBoxAsync(message, title));

            public async Task<bool> ShowMessageBoxAsync(string message, string title = "提示")
            {
                var window = App.GetMainWindow();
                if (window is null) return false;

                var dialog = new MessageBoxWindow(title, message);
                return await dialog.ShowDialog<bool>(window);
            }

            private static T RunSynchronously<T>(Func<Task<T>> taskFunc)
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    var frame = new DispatcherFrame();
                    T result = default!;
                    Exception? error = null;

                    _ = taskFunc().ContinueWith(t =>
                    {
                        if (t.IsFaulted) error = t.Exception?.GetBaseException() ?? t.Exception;
                        else result = t.Result;
                        frame.Continue = false;
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    Dispatcher.UIThread.PushFrame(frame);
                    if (error is not null) throw error;
                    return result;
                }
                else
                {
                    return Dispatcher.UIThread.InvokeAsync(taskFunc).GetAwaiter().GetResult();
                }
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "DialogService.cs"), dialogCs, SourceFileType.ProjectFile));

        var msgWindowCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 跨平台現代化訊息對話視窗。
        /// </summary>
        public sealed class MessageBoxWindow : Window
        {
            public MessageBoxWindow(string title, string message)
            {
                Title = title;
                Width = 380;
                Height = 160;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                CanResize = false;
                Background = Brush.Parse("#18181B");

                var grid = new Grid
                {
                    Margin = new Thickness(20),
                    RowDefinitions = new RowDefinitions("*, Auto")
                };

                var textBlock = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(textBlock, 0);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10
                };
                Grid.SetRow(buttonPanel, 1);

                var okButton = new Button
                {
                    Content = "確定",
                    Padding = new Thickness(16, 6),
                    Background = Brush.Parse("#27272A"),
                    Foreground = Brushes.White
                };
                okButton.Click += (s, e) => Close(true);

                var cancelButton = new Button
                {
                    Content = "取消",
                    Padding = new Thickness(16, 6),
                    Background = Brush.Parse("#27272A"),
                    Foreground = Brushes.White
                };
                cancelButton.Click += (s, e) => Close(false);

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);

                grid.Children.Add(textBlock);
                grid.Children.Add(buttonPanel);

                Content = grid;
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "MessageBoxWindow.cs"), msgWindowCs, SourceFileType.ProjectFile));

        // 對話方塊元件類別：OpenFileDialog.cs, SaveFileDialog.cs, MessageBox.cs
        var openFileDialogCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 開啟檔案對話方塊元件。
        /// </summary>
        public class OpenFileDialog
        {
            public string Title { get; set; } = "開啟檔案";
            public string? Filter { get; set; }
            public bool AllowMultiple { get; set; }

            public event EventHandler<string?>? FileOk;

            public string? Show()
            {
                var dialogService = App.Services?.GetService<IDialogService>() ?? new DialogService();
                var result = dialogService.ShowOpenFileDialog(Title, Filter);
                if (!string.IsNullOrEmpty(result))
                {
                    FileOk?.Invoke(this, result);
                }
                return result;
            }

            public static string? Show(string title = "開啟檔案", string? filter = null)
            {
                var dlg = new OpenFileDialog { Title = title, Filter = filter };
                return dlg.Show();
            }

            public async Task<string?> ShowAsync()
            {
                var dialogService = App.Services?.GetService<IDialogService>() ?? new DialogService();
                var result = await dialogService.ShowOpenFileDialogAsync(Title, Filter);
                if (!string.IsNullOrEmpty(result))
                {
                    FileOk?.Invoke(this, result);
                }
                return result;
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "OpenFileDialog.cs"), openFileDialogCs, SourceFileType.ProjectFile));

        var saveFileDialogCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 儲存檔案對話方塊元件。
        /// </summary>
        public class SaveFileDialog
        {
            public string Title { get; set; } = "儲存檔案";
            public string? DefaultExtension { get; set; }
            public string? Filter { get; set; }

            public event EventHandler<string?>? FileOk;

            public string? Show(string? defaultFileName = null)
            {
                var dialogService = App.Services?.GetService<IDialogService>() ?? new DialogService();
                var result = dialogService.ShowSaveFileDialog(Title, defaultFileName ?? "Untitled", DefaultExtension, Filter);
                if (!string.IsNullOrEmpty(result))
                {
                    FileOk?.Invoke(this, result);
                }
                return result;
            }

            public static string? Show(string defaultFileName = "Untitled", string title = "儲存檔案", string? filter = null)
            {
                var dlg = new SaveFileDialog { Title = title, Filter = filter };
                return dlg.Show(defaultFileName);
            }

            public async Task<string?> ShowAsync(string? defaultFileName = null)
            {
                var dialogService = App.Services?.GetService<IDialogService>() ?? new DialogService();
                var result = await dialogService.ShowSaveFileDialogAsync(Title, defaultFileName ?? "Untitled", DefaultExtension, Filter);
                if (!string.IsNullOrEmpty(result))
                {
                    FileOk?.Invoke(this, result);
                }
                return result;
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "SaveFileDialog.cs"), saveFileDialogCs, SourceFileType.ProjectFile));

        var messageBoxCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 訊息方塊 (MessageBox) 元件。
        /// </summary>
        public class MessageBox
        {
            public string Title { get; set; } = "提示";
            public string Message { get; set; } = string.Empty;

            public event EventHandler<bool?>? Confirmed;

            public bool? Show()
            {
                var dialogService = App.Services?.GetService<IDialogService>() ?? new DialogService();
                var result = dialogService.ShowMessageBox(Message, Title);
                Confirmed?.Invoke(this, result);
                return result;
            }

            public static bool? Show(string message, string title = "提示")
            {
                var msgBox = new MessageBox { Message = message, Title = title };
                return msgBox.Show();
            }

            public async Task<bool?> ShowAsync(string? message = null, string? title = null)
            {
                var msg = message ?? Message;
                var ttl = title ?? Title;
                var dialogService = App.Services?.GetService<IDialogService>() ?? new DialogService();
                var result = await dialogService.ShowMessageBoxAsync(msg, ttl);
                Confirmed?.Invoke(this, result);
                return result;
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "MessageBox.cs"), messageBoxCs, SourceFileType.ProjectFile));

        // 日誌模型與服務：LogEntry.cs, ConsoleRedirectWriter.cs, InMemoryLogService.cs, InMemoryLoggerProvider.cs
        var logEntryCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 結構化除錯日誌條目。
        /// </summary>
        public record LogEntry(
            DateTime Timestamp,
            LogLevel Level,
            string Category,
            string Message,
            Exception? Exception = null)
        {
            public override string ToString()
            {
                var lvl = Level switch
                {
                    LogLevel.Trace => "[TRC]",
                    LogLevel.Debug => "[DBG]",
                    LogLevel.Information => "[INF]",
                    LogLevel.Warning => "[WRN]",
                    LogLevel.Error => "[ERR]",
                    LogLevel.Critical => "[CRT]",
                    _ => "[LOG]"
                };
                var ex = Exception is not null ? $" | Exception: {Exception.Message}" : string.Empty;
                return $"[{Timestamp:HH:mm:ss}] {lvl} [{Category}] {Message}{ex}";
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "LogEntry.cs"), logEntryCs, SourceFileType.ProjectFile));

        var consoleRedirectWriterCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 自訂 TextWriter，攔截標準輸出並重定向至 InMemoryLogService。
        /// </summary>
        public sealed class ConsoleRedirectWriter : TextWriter
        {
            private readonly InMemoryLogService _logService;
            private readonly string _category;
            private readonly LogLevel _logLevel;
            private readonly TextWriter? _originalOut;
            private readonly StringBuilder _buffer = new();

            public ConsoleRedirectWriter(InMemoryLogService logService, string category = "Console", LogLevel logLevel = LogLevel.Information, TextWriter? originalOut = null)
            {
                _logService = logService ?? throw new ArgumentNullException(nameof(logService));
                _category = category;
                _logLevel = logLevel;
                _originalOut = originalOut;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                _originalOut?.Write(value);
                if (value == '\n')
                {
                    FlushBuffer();
                }
                else if (value != '\r')
                {
                    _buffer.Append(value);
                }
            }

            public override void Write(string? value)
            {
                _originalOut?.Write(value);
                if (string.IsNullOrEmpty(value)) return;

                if (value.Contains('\n'))
                {
                    _buffer.Append(value);
                    FlushBuffer();
                }
                else
                {
                    _buffer.Append(value);
                }
            }

            public override void WriteLine(string? value)
            {
                _originalOut?.WriteLine(value);
                if (!string.IsNullOrEmpty(value))
                {
                    _buffer.Append(value);
                }
                FlushBuffer();
            }

            public override void Flush()
            {
                _originalOut?.Flush();
                FlushBuffer();
            }

            private void FlushBuffer()
            {
                if (_buffer.Length == 0) return;
                var message = _buffer.ToString();
                _buffer.Clear();
                _logService.AddLog(_logLevel, _category, message, null);
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "ConsoleRedirectWriter.cs"), consoleRedirectWriterCs, SourceFileType.ProjectFile));

        var inMemoryLogServiceCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 跨平台執行緒安全記憶體日誌服務。
        /// </summary>
        public sealed class InMemoryLogService
        {
            public ObservableCollection<LogEntry> Logs { get; } = [];

            public void AddLog(LogLevel level, string category, string message, Exception? exception = null)
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    if (Logs.Count > 1000) Logs.RemoveAt(0);
                    Logs.Add(new LogEntry(DateTime.Now, level, category, message, exception));
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (Logs.Count > 1000) Logs.RemoveAt(0);
                        Logs.Add(new LogEntry(DateTime.Now, level, category, message, exception));
                    });
                }
            }

            public void Clear()
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    Logs.Clear();
                }
                else
                {
                    Dispatcher.UIThread.Post(Logs.Clear);
                }
            }

            /// <summary>
            /// 重定向標準輸出 (Console.Out / Console.Error) 至記憶體日誌服務。
            /// </summary>
            public void RedirectStandardOutput()
            {
                var outWriter = new ConsoleRedirectWriter(this, "Console.Out", LogLevel.Information, Console.Out);
                var errWriter = new ConsoleRedirectWriter(this, "Console.Error", LogLevel.Error, Console.Error);
                Console.SetOut(outWriter);
                Console.SetError(errWriter);
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "InMemoryLogService.cs"), inMemoryLogServiceCs, SourceFileType.ProjectFile));

        var inMemoryLoggerProviderCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Services;

        /// <summary>
        /// 提供記憶體 Logger 之 ILoggerProvider 實作。
        /// </summary>
        public sealed class InMemoryLoggerProvider : ILoggerProvider
        {
            private readonly InMemoryLogService _logService;

            public InMemoryLoggerProvider(InMemoryLogService logService)
            {
                _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            }

            public ILogger CreateLogger(string categoryName) => new InMemoryLogger(categoryName, _logService);

            public void Dispose() { }
        }

        /// <summary>
        /// 攔截日誌輸出並轉導至 InMemoryLogService。
        /// </summary>
        public sealed class InMemoryLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly InMemoryLogService _logService;

            public InMemoryLogger(string categoryName, InMemoryLogService logService)
            {
                _categoryName = categoryName;
                _logService = logService;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                _logService.AddLog(logLevel, _categoryName, message, exception);
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "InMemoryLoggerProvider.cs"), inMemoryLoggerProviderCs, SourceFileType.ProjectFile));

        var allNodesInProject = project.Documents.SelectMany(d => AstTreeOperations.Flatten(d.RootNode)).ToList();
        var hasBluetooth = allNodesInProject.Any(n => n.Type == ControlType.BluetoothClient) || allServices.Any(s => s.InterfaceName == "IBluetoothClient");
        var hasSerialPort = allNodesInProject.Any(n => n.Type == ControlType.SerialPortService) || allServices.Any(s => s.InterfaceName == "ISerialPortService");

        // 自訂 DI 服務層介面與實作：Services/*.cs (硬體通訊服務由下方豐富介面提供)
        foreach (var svc in allServices.Where(s => s.InterfaceName != "IBluetoothClient" && s.InterfaceName != "ISerialPortService"))
        {
            var implName = !string.IsNullOrEmpty(svc.ImplementationName)
                ? svc.ImplementationName
                : (svc.InterfaceName.StartsWith('I') ? svc.InterfaceName[1..] : $"{svc.InterfaceName}Impl");

            var ifaceCs = $$"""
            // <auto-generated />
            #nullable enable
            namespace {{project.RootNamespace}}.Services;

            public interface {{svc.InterfaceName}}
            {
                string Execute(string input);
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", $"{svc.InterfaceName}.cs"), ifaceCs, SourceFileType.ProjectFile));

            var implCs = $$"""
            // <auto-generated />
            #nullable enable
            namespace {{project.RootNamespace}}.Services;

            public sealed class {{implName}} : {{svc.InterfaceName}}
            {
                public string Execute(string input) => $"Processed: {input}";
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", $"{implName}.cs"), implCs, SourceFileType.ProjectFile));
        }

        // 不可視硬體通訊服務層輸出 (BluetoothClient / SerialPortService)
        if (hasBluetooth)
        {
            var ibluetoothCs = $$"""
            // <auto-generated />
            #nullable enable

            namespace {{project.RootNamespace}}.Services;

            /// <summary>
            /// 跨平台低功耗藍牙 (BLE) 通訊客戶端服務介面。
            /// </summary>
            public interface IBluetoothClient
            {
                event EventHandler<string>? DeviceDiscovered;
                event EventHandler? Connected;
                event EventHandler? Disconnected;
                event EventHandler<byte[]>? DataReceived;
                void Connect(string deviceAddress);
                void Disconnect();
                void Send(byte[] data);
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "IBluetoothClient.cs"), ibluetoothCs, SourceFileType.ProjectFile));

            var bluetoothCs = $$"""
            // <auto-generated />
            #nullable enable

            namespace {{project.RootNamespace}}.Services;

            /// <summary>
            /// 跨平台低功耗藍牙 (BLE) 通訊客戶端服務實作。
            /// </summary>
            public class BluetoothClient : IBluetoothClient
            {
                public event EventHandler<string>? DeviceDiscovered;
                public event EventHandler? Connected;
                public event EventHandler? Disconnected;
                public event EventHandler<byte[]>? DataReceived;

                public void Connect(string deviceAddress) => Connected?.Invoke(this, EventArgs.Empty);
                public void Disconnect() => Disconnected?.Invoke(this, EventArgs.Empty);
                public void Send(byte[] data) { }
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "BluetoothClient.cs"), bluetoothCs, SourceFileType.ProjectFile));
        }

        if (hasSerialPort)
        {
            var iserialPortCs = $$"""
            // <auto-generated />
            #nullable enable

            namespace {{project.RootNamespace}}.Services;

            /// <summary>
            /// 序列埠 (RS-232 / UART) 硬體通訊客戶端服務介面。
            /// </summary>
            public interface ISerialPortService
            {
                event EventHandler<string>? DataReceived;
                event EventHandler<string>? ErrorReceived;
                event EventHandler? PinChanged;
                void Open(string portName, int baudRate = 9600);
                void Close();
                void Write(string text);
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "ISerialPortService.cs"), iserialPortCs, SourceFileType.ProjectFile));

            var serialPortCs = $$"""
            // <auto-generated />
            #nullable enable

            namespace {{project.RootNamespace}}.Services;

            /// <summary>
            /// 序列埠 (RS-232 / UART) 硬體通訊客戶端服務實作。
            /// </summary>
            public class SerialPortService : ISerialPortService
            {
                public event EventHandler<string>? DataReceived;
                public event EventHandler<string>? ErrorReceived;
                public event EventHandler? PinChanged;

                public void Open(string portName, int baudRate = 9600) { }
                public void Close() { }
                public void Write(string text) { }
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "SerialPortService.cs"), serialPortCs, SourceFileType.ProjectFile));
        }

        // 依序生成所有表單之 Views 與 ViewModels
        foreach (var doc in project.Documents)
        {
            var effectiveDoc = doc with { RootNamespace = project.RootNamespace };
            var genResult = _codeGenerator.GenerateAll(effectiveDoc);
            var viewFile = genResult.Files.FirstOrDefault(f => f.FileType == SourceFileType.View);
            if (viewFile is not null)
            {
                files.Add(new GeneratedSourceFile(
                    Path.Combine(sharedDir, "Views", viewFile.FileName),
                    viewFile.Content,
                    SourceFileType.View));
            }

            var vmFile = genResult.Files.FirstOrDefault(f => f.FileType == SourceFileType.ViewModel);
            if (vmFile is not null && effectiveDoc.ArchitectureMode != ArchitectureMode.CodeBehind)
            {
                files.Add(new GeneratedSourceFile(
                    Path.Combine(sharedDir, "ViewModels", vmFile.FileName),
                    vmFile.Content,
                    SourceFileType.ViewModel));
            }
        }

        // ==========================================
        // 3. 桌面端宿主專案：src/{ProjectName}.Desktop/
        // ==========================================
        var desktopCsprojContent = $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>WinExe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <LangVersion>latest</LangVersion>
            <RootNamespace>{project.RootNamespace}.Desktop</RootNamespace>
            <NoWarn>$(NoWarn);NU1903</NoWarn>
          </PropertyGroup>

          <ItemGroup>
            <ProjectReference Include="..\{sharedProjectName}\{sharedProjectName}.csproj" />
            <PackageReference Include="Avalonia.Desktop" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
          </ItemGroup>
        </Project>
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(desktopDir, $"{desktopProjectName}.csproj"), desktopCsprojContent, SourceFileType.ProjectFile));

        var desktopGlobalUsingsCs = $$"""
        // <auto-generated />
        global using System;
        global using Avalonia;
        global using {{project.RootNamespace}};
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(desktopDir, "GlobalUsings.cs"), desktopGlobalUsingsCs, SourceFileType.ProjectFile));

        var desktopProgramCs = $$"""
        // <auto-generated />
        #nullable enable

        namespace {{project.RootNamespace}}.Desktop;

        internal static class Program
        {
            [STAThread]
            public static void Main(string[] args) =>
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

            public static AppBuilder BuildAvaloniaApp() =>
                AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .LogToTrace();
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(desktopDir, "Program.cs"), desktopProgramCs, SourceFileType.ProjectFile));

        // ==========================================
        // 4. 行動端宿主專案：src/{ProjectName}.Android/ (可選)
        // ==========================================
        if (options.IncludeMobileProject)
        {
            var androidCsprojContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0-android</TargetFramework>
                <SupportedOSPlatformVersion>21</SupportedOSPlatformVersion>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <LangVersion>latest</LangVersion>
                <RootNamespace>{project.RootNamespace}.Android</RootNamespace>
                <ApplicationId>com.afg.{baseProjectName.ToLowerInvariant()}</ApplicationId>
                <ApplicationVersion>1</ApplicationVersion>
                <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
                <AndroidPackageFormat>apk</AndroidPackageFormat>
                <AndroidEnablePreBuildValidation>true</AndroidEnablePreBuildValidation>
                <NoWarn>$(NoWarn);NU1903;XA4211</NoWarn>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="..\{sharedProjectName}\{sharedProjectName}.csproj" />
                <PackageReference Include="Avalonia.Android" Version="{PackageVersions.Avalonia}" />
                <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
              </ItemGroup>
            </Project>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, $"{androidProjectName}.csproj"), androidCsprojContent, SourceFileType.ProjectFile));

            var androidGlobalUsingsCs = $$"""
            // <auto-generated />
            global using System;
            global using Android.App;
            global using Android.Content.PM;
            global using Android.OS;
            global using Avalonia;
            global using Avalonia.Android;
            global using {{project.RootNamespace}};
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "GlobalUsings.cs"), androidGlobalUsingsCs, SourceFileType.ProjectFile));

            var mainActivityCs = $$"""
            // <auto-generated />
            #nullable enable

            namespace {{project.RootNamespace}}.Android;

            [Activity(
                Label = "{{project.Title}}",
                Theme = "@style/MyTheme.NoActionBar",
                Icon = "@drawable/icon",
                MainLauncher = true,
                ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
            public class MainActivity : AvaloniaMainActivity<App>
            {
                protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
                {
                    return base.CustomizeAppBuilder(builder)
                        .WithInterFont();
                }
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "MainActivity.cs"), mainActivityCs, SourceFileType.ProjectFile));

            var stylesXml = """
            <?xml version="1.0" encoding="utf-8" ?>
            <resources>
              <style name="MyTheme.NoActionBar" parent="Theme.AppCompat.DayNight.NoActionBar">
                <item name="android:windowNoTitle">true</item>
                <item name="android:windowActionBar">false</item>
                <item name="android:windowFullscreen">false</item>
              </style>
            </resources>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "Resources", "values", "styles.xml"), stylesXml, SourceFileType.ProjectFile));

            var iconXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="48dp"
                android:height="48dp"
                android:viewportWidth="48"
                android:viewportHeight="48">
              <path
                  android:fillColor="#3B82F6"
                  android:pathData="M24,4C12.95,4 4,12.95 4,24s8.95,20 20,20 20,-8.95 20,-20S35.05,4 24,4z"/>
            </vector>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "Resources", "drawable", "icon.xml"), iconXml, SourceFileType.ProjectFile));

            var androidManifestXml = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <manifest xmlns:android="http://schemas.android.com/apk/res/android"
                      package="com.afg.{{baseProjectName.ToLowerInvariant()}}"
                      android:versionCode="1"
                      android:versionName="1.0">
              <uses-sdk android:minSdkVersion="21" android:targetSdkVersion="34" />
              <application android:label="{{project.Title}}" android:icon="@drawable/icon" android:theme="@style/MyTheme.NoActionBar" />
            </manifest>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "AndroidManifest.xml"), androidManifestXml, SourceFileType.ProjectFile));
        }

        return files.ToImmutableList();
    }

    /// <summary>
    /// 將完整專案檔案寫出至本機資料夾，並自動建立分層子資料夾階層。
    /// </summary>
    public async Task ExportToFolderAsync(FormDocument document, string destinationDirectory, ProjectExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        await ExportMultiFormToFolderAsync(FormProjectDefinition.FromSingleDocument(document), destinationDirectory, options, cancellationToken);
    }

    /// <summary>
    /// 將多表單專案檔案寫出至本機資料夾，並自動建立分層子資料夾階層（具備目錄穿越安全防護）。
    /// </summary>
    public async Task ExportMultiFormToFolderAsync(FormProjectDefinition project, string destinationDirectory, ProjectExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        var fullDestinationDir = Path.GetFullPath(destinationDirectory);
        if (!Directory.Exists(fullDestinationDir))
        {
            Directory.CreateDirectory(fullDestinationDir);
        }

        var files = GenerateMultiFormProject(project, options);
        foreach (var file in files)
        {
            var fullFilePath = Path.GetFullPath(Path.Combine(fullDestinationDir, file.FileName));
            if (!fullFilePath.StartsWith(fullDestinationDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"檢測到潛在的路徑穿越 (Path Traversal) 非法檔案路徑：{file.FileName}");
            }

            var fileDir = Path.GetDirectoryName(fullFilePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            await File.WriteAllTextAsync(fullFilePath, file.Content, Encoding.UTF8, cancellationToken);
        }

        // 自動複製 PictureBox / MediaPlayer 相對路徑實體資源至 .Shared/Assets/ 資料夾
        var rawProjectName = string.IsNullOrWhiteSpace(options?.CustomProjectName) ? project.ProjectName : options.CustomProjectName;
        var baseProjectName = SanitizeProjectName(rawProjectName);
        var sharedAssetsDir = Path.Combine(fullDestinationDir, "src", $"{baseProjectName}.Shared", "Assets");

        var mediaAndPictureNodes = project.Documents
            .SelectMany(d => AstTreeOperations.Flatten(d.RootNode))
            .Where(n => n.Type is ControlType.PictureBox or ControlType.Image or ControlType.MediaPlayer)
            .ToList();

        foreach (var mediaNode in mediaAndPictureNodes)
        {
            if (mediaNode.UseRelativePath && !string.IsNullOrWhiteSpace(mediaNode.Source))
            {
                var sourcePath = mediaNode.Source.Trim();
                var resolvedSourcePath = File.Exists(sourcePath)
                    ? sourcePath
                    : (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), sourcePath))
                        ? Path.Combine(Directory.GetCurrentDirectory(), sourcePath)
                        : (File.Exists(Path.Combine(AppContext.BaseDirectory, sourcePath))
                            ? Path.Combine(AppContext.BaseDirectory, sourcePath)
                            : null));

                if (resolvedSourcePath != null)
                {
                    if (!Directory.Exists(sharedAssetsDir))
                    {
                        Directory.CreateDirectory(sharedAssetsDir);
                    }
                    var targetAssetFile = Path.Combine(sharedAssetsDir, Path.GetFileName(resolvedSourcePath.Replace('\\', '/')));
                    File.Copy(resolvedSourcePath, targetAssetFile, overwrite: true);
                }
            }
        }

        // 自動複製 FormDocument.Icon 實體圖示至 .Shared/Assets/ 資料夾
        foreach (var doc in project.Documents)
        {
            if (!string.IsNullOrWhiteSpace(doc.Icon))
            {
                var iconPath = doc.Icon.Trim();
                var resolvedIconPath = File.Exists(iconPath)
                    ? iconPath
                    : (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), iconPath))
                        ? Path.Combine(Directory.GetCurrentDirectory(), iconPath)
                        : (File.Exists(Path.Combine(AppContext.BaseDirectory, iconPath))
                            ? Path.Combine(AppContext.BaseDirectory, iconPath)
                            : null));

                if (resolvedIconPath != null)
                {
                    if (!Directory.Exists(sharedAssetsDir))
                    {
                        Directory.CreateDirectory(sharedAssetsDir);
                    }
                    var targetAssetFile = Path.Combine(sharedAssetsDir, Path.GetFileName(resolvedIconPath.Replace('\\', '/')));
                    File.Copy(resolvedIconPath, targetAssetFile, overwrite: true);
                }
            }
        }
    }

    /// <summary>
    /// 生成包含 F# 專案檔 (.fsproj)、.Shared 核心與 .Desktop 桌面宿主之完整 F# 方案檔案集合。
    /// </summary>
    public IReadOnlyList<GeneratedSourceFile> GenerateFSharpProject(FormProjectDefinition project, ProjectExportOptions options)
    {
        var files = new List<GeneratedSourceFile>();
        var rawProjectName = string.IsNullOrWhiteSpace(options.CustomProjectName) ? project.ProjectName : options.CustomProjectName;
        var baseProjectName = SanitizeProjectName(rawProjectName);

        var sharedProjectName = $"{baseProjectName}.Shared";
        var desktopProjectName = $"{baseProjectName}.Desktop";
        var androidProjectName = $"{baseProjectName}.Android";

        var sharedDir = Path.Combine("src", sharedProjectName);
        var desktopDir = Path.Combine("src", desktopProjectName);
        var androidDir = Path.Combine("src", androidProjectName);

        // 1. 方案檔 (.slnx)
        var slnxBuilder = new StringBuilder();
        slnxBuilder.AppendLine("<Solution>");
        slnxBuilder.AppendLine($"  <Project Path=\"src/{sharedProjectName}/{sharedProjectName}.fsproj\" />");
        slnxBuilder.AppendLine($"  <Project Path=\"src/{desktopProjectName}/{desktopProjectName}.fsproj\" />");
        if (options.IncludeMobileProject)
        {
            slnxBuilder.AppendLine($"  <Project Path=\"src/{androidProjectName}/{androidProjectName}.csproj\" />");
        }
        slnxBuilder.AppendLine("</Solution>");
        files.Add(new GeneratedSourceFile($"{baseProjectName}.slnx", slnxBuilder.ToString().TrimEnd(), SourceFileType.SolutionFile));

        // .gitignore
        var gitignoreContent = """
        ## Visual Studio & .NET
        .vs/
        [Bb]in/
        [Oo]bj/
        *.user
        *.suo
        """;
        files.Add(new GeneratedSourceFile(".gitignore", gitignoreContent, SourceFileType.ProjectFile));

        var initialDoc = project.Documents.FirstOrDefault(d => d.ViewClassName == project.InitialFormName) ?? project.Documents[0];

        // 2. .Shared 專案 (.fsproj)
        var fsprojCompileItems = new StringBuilder();
        fsprojCompileItems.AppendLine("    <Compile Include=\"Config.fs\" />");
        fsprojCompileItems.AppendLine("    <Compile Include=\"Helpers/BitmapHelper.fs\" />");
        fsprojCompileItems.AppendLine("    <Compile Include=\"Controls/MediaPlayerControl.fs\" />");
        fsprojCompileItems.AppendLine("    <Compile Include=\"Services/INavigationService.fs\" />");

        foreach (var doc in project.Documents)
        {
            if (doc.ArchitectureMode != ArchitectureMode.CodeBehind)
            {
                fsprojCompileItems.AppendLine($"    <Compile Include=\"ViewModels/{doc.ViewModelClassName}.fs\" />");
            }
            fsprojCompileItems.AppendLine($"    <Compile Include=\"Views/{doc.ViewClassName}.fs\" />");
        }
        fsprojCompileItems.AppendLine("    <Compile Include=\"App.fs\" />");

        var sharedFsprojContent = $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <NoWarn>$(NoWarn);FS3261;FS3262;FS3263;FS1183;FS0020;3261;3262;3263;1183;0020</NoWarn>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Avalonia" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Themes.Fluent" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="CommunityToolkit.Mvvm" Version="{PackageVersions.CommunityToolkitMvvm}" />
            <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="{PackageVersions.MicrosoftExtensionsDependencyInjection}" />
            <PackageReference Include="Microsoft.Extensions.Logging" Version="{PackageVersions.MicrosoftExtensionsLogging}" />
          </ItemGroup>

          <ItemGroup>
            <AvaloniaResource Include="Assets\**" />
            <None Update="Assets\**" CopyToOutputDirectory="PreserveNewest" />
          </ItemGroup>

          <ItemGroup>
        {fsprojCompileItems.ToString().TrimEnd()}
          </ItemGroup>
        </Project>
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, $"{sharedProjectName}.fsproj"), sharedFsprojContent, SourceFileType.ProjectFile));

        // Config.fs
        var configFs = $"""
        // <auto-generated />
        #nowarn "3261" "3262" "3263" "1183" "0020"
        namespace {project.RootNamespace}

        module Config =
            let [<Literal>] AppTitle = "{project.Title}"
            let [<Literal>] Version = "1.0.0"
            let [<Literal>] DefaultWindowWidth = {initialDoc.CanvasWidth.ToString(CultureInfo.InvariantCulture)}
            let [<Literal>] DefaultWindowHeight = {initialDoc.CanvasHeight.ToString(CultureInfo.InvariantCulture)}
            let [<Literal>] CanResize = {(initialDoc.CanResize ? "true" : "false")}
            let [<Literal>] Topmost = {(initialDoc.Topmost ? "true" : "false")}
            let [<Literal>] ShowInTaskbar = {(initialDoc.ShowInTaskbar ? "true" : "false")}
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Config.fs"), configFs, SourceFileType.ProjectFile));

        // Helpers/BitmapHelper.fs
        var bitmapHelperFs = $$"""
        // <auto-generated />
        #nowarn "3261" "3262" "3263" "1183" "0020"
        namespace {{project.RootNamespace}}.Helpers

        open System
        open System.IO
        open System.Net.Http
        open Avalonia
        open Avalonia.Media
        open Avalonia.Media.Imaging
        open Avalonia.Platform

        module BitmapHelper =
            let CreateInitializedBitmap(width: float, height: float, backgroundColor: Color) : WriteableBitmap =
                let w = int (max 1.0 width)
                let h = int (max 1.0 height)
                let dpi = Vector(96.0, 96.0)
                let wb = new WriteableBitmap(PixelSize(w, h), dpi, Nullable PixelFormat.Bgra8888, Nullable AlphaFormat.Premul)
                use fb = wb.Lock()
                let totalBytes = fb.RowBytes * h
                let buffer = Array.create totalBytes 0uy
                let cB = backgroundColor.B
                let cG = backgroundColor.G
                let cR = backgroundColor.R
                let cA = backgroundColor.A
                for y = 0 to h - 1 do
                    let rowOffset = y * fb.RowBytes
                    for x = 0 to w - 1 do
                        let offset = rowOffset + (x * 4)
                        buffer.[offset + 0] <- cB
                        buffer.[offset + 1] <- cG
                        buffer.[offset + 2] <- cR
                        buffer.[offset + 3] <- cA
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, fb.Address, totalBytes)
                wb

            let LoadBitmap(pathOrUri: string) : Bitmap =
                if String.IsNullOrWhiteSpace(pathOrUri) then null
                else
                    let trimmed = pathOrUri.Trim()
                    if File.Exists(trimmed) then
                        try new Bitmap(trimmed) with _ -> null
                    elif trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
                        try
                            use client = new HttpClient(Timeout = TimeSpan.FromSeconds(10.0))
                            let bytes = client.GetByteArrayAsync(trimmed).GetAwaiter().GetResult()
                            use ms = new MemoryStream(bytes)
                            new Bitmap(ms)
                        with _ -> null
                    elif trimmed.StartsWith("avares://", StringComparison.OrdinalIgnoreCase) then
                        try
                            let uri = Uri(trimmed)
                            if AssetLoader.Exists(uri) then
                                use stream = AssetLoader.Open(uri)
                                new Bitmap(stream)
                            else
                                let resPath = uri.AbsolutePath.TrimStart('/')
                                let baseDir = AppContext.BaseDirectory
                                let cand1 = Path.Combine(baseDir, resPath)
                                let cand2 = Path.Combine(baseDir, "Assets", Path.GetFileName(resPath))
                                if File.Exists(cand1) then new Bitmap(cand1)
                                elif File.Exists(cand2) then new Bitmap(cand2)
                                else null
                        with _ -> null
                    else
                        let baseDir = AppContext.BaseDirectory
                        let cand1 = Path.Combine(baseDir, trimmed)
                        let cand2 = Path.Combine(baseDir, "Assets", Path.GetFileName(trimmed))
                        if File.Exists(cand1) then new Bitmap(cand1)
                        elif File.Exists(cand2) then new Bitmap(cand2)
                        else null
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Helpers", "BitmapHelper.fs"), bitmapHelperFs, SourceFileType.ProjectFile));

        // Controls/MediaPlayerControl.fs
        var mediaPlayerFs = $$"""
        // <auto-generated />
        #nowarn "3261" "3262" "3263" "1183" "0020"
        namespace {{project.RootNamespace}}.Controls

        open System
        open System.IO
        open Avalonia
        open Avalonia.Controls
        open Avalonia.Layout
        open Avalonia.Media
        open Avalonia.Media.Imaging
        open Avalonia.Threading
        open {{project.RootNamespace}}.Helpers

        type MediaState =
            | Stopped = 0
            | Playing = 1
            | Paused = 2
            | Buffering = 3
            | Error = 4

        [<AllowNullLiteral>]
        type MediaPlayerControl() as this =
            inherit UserControl()

            static let s_sourceProperty = AvaloniaProperty.Register<MediaPlayerControl, string>("Source")
            static let s_autoPlayProperty = AvaloniaProperty.Register<MediaPlayerControl, bool>("AutoPlay", false)
            static let s_isLoopingProperty = AvaloniaProperty.Register<MediaPlayerControl, bool>("IsLooping", false)
            static let s_volumeProperty = AvaloniaProperty.Register<MediaPlayerControl, float>("Volume", 1.0)
            static let s_positionProperty = AvaloniaProperty.Register<MediaPlayerControl, TimeSpan>("Position", TimeSpan.Zero)
            static let s_durationProperty = AvaloniaProperty.Register<MediaPlayerControl, TimeSpan>("Duration", TimeSpan.FromSeconds(10.0))
            static let s_stateProperty = AvaloniaProperty.Register<MediaPlayerControl, MediaState>("State", MediaState.Stopped)
            static let s_currentFrameProperty = AvaloniaProperty.Register<MediaPlayerControl, IImage>("CurrentFrame")

            let _playbackTimer = new DispatcherTimer(Interval = TimeSpan.FromMilliseconds(50.0))
            let _frameImage = new Image(HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch)
            let _titleTextBlock = new TextBlock(Text = "▶ Media Player", Foreground = (Color.Parse("#F4F4F5") |> SolidColorBrush), FontSize = 13.0, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center)
            let _btnPlayPause = new Button(Content = "▶", FontSize = 11.0, Padding = Thickness(6.0, 2.0))
            let _btnStop = new Button(Content = "⏹", FontSize = 11.0, Padding = Thickness(6.0, 2.0))
            let _timeTextBlock = new TextBlock(Text = "00:00 / 00:10", FontSize = 10.0, Foreground = (Color.Parse("#A1A1AA") |> SolidColorBrush), VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(6.0, 0.0, 6.0, 0.0))
            let _seekSlider = new Slider(Minimum = 0.0, Maximum = 100.0, Value = 0.0, VerticalAlignment = VerticalAlignment.Center)

            do
                this.Background <- Color.Parse("#09090B") |> SolidColorBrush
                this.ClipToBounds <- true

                _btnPlayPause.Click.Add(fun _ -> if this.State = MediaState.Playing then this.Pause() else this.Play())
                _btnStop.Click.Add(fun _ -> this.Stop())

                let controlsGrid = new Grid()
                controlsGrid.ColumnDefinitions.Add(ColumnDefinition(GridLength.Auto))
                controlsGrid.ColumnDefinitions.Add(ColumnDefinition(GridLength.Auto))
                controlsGrid.ColumnDefinitions.Add(ColumnDefinition(GridLength.Star))
                controlsGrid.ColumnDefinitions.Add(ColumnDefinition(GridLength.Auto))

                Grid.SetColumn(_btnPlayPause, 0)
                Grid.SetColumn(_btnStop, 1)
                Grid.SetColumn(_seekSlider, 2)
                Grid.SetColumn(_timeTextBlock, 3)

                controlsGrid.Children.Add(_btnPlayPause)
                controlsGrid.Children.Add(_btnStop)
                controlsGrid.Children.Add(_seekSlider)
                controlsGrid.Children.Add(_timeTextBlock)

                let controlsBar = new Border(
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Background = (Color.Parse("#D918181B") |> SolidColorBrush),
                    BorderBrush = (Color.Parse("#27272A") |> SolidColorBrush),
                    BorderThickness = Thickness(0.0, 1.0, 0.0, 0.0),
                    Padding = Thickness(8.0, 4.0),
                    Child = controlsGrid
                )

                let hudPanel = new StackPanel(HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center)
                hudPanel.Children.Add(_titleTextBlock)

                let mainGrid = new Grid()
                mainGrid.Children.Add(_frameImage)
                mainGrid.Children.Add(hudPanel)
                mainGrid.Children.Add(controlsBar)
                this.Content <- mainGrid

                _playbackTimer.Tick.Add(fun _ ->
                    if this.State = MediaState.Playing then
                        let newPos = this.Position + TimeSpan.FromMilliseconds(50.0)
                        if this.Duration > TimeSpan.Zero && newPos >= this.Duration then
                            if this.IsLooping then
                                this.Position <- TimeSpan.Zero
                            else
                                this.Position <- this.Duration
                                this.Stop()
                        else
                            this.Position <- newPos
                        this.UpdatePlaybackUi()
                )

            static member SourceProperty = s_sourceProperty
            static member AutoPlayProperty = s_autoPlayProperty
            static member IsLoopingProperty = s_isLoopingProperty
            static member VolumeProperty = s_volumeProperty
            static member PositionProperty = s_positionProperty
            static member DurationProperty = s_durationProperty
            static member StateProperty = s_stateProperty
            static member CurrentFrameProperty = s_currentFrameProperty

            member this.Source
                with get() = this.GetValue(s_sourceProperty)
                and set(v: string) =
                    this.SetValue(s_sourceProperty, v) |> ignore
                    if not (String.IsNullOrWhiteSpace(v)) then
                        this.Load(v)

            member this.AutoPlay
                with get() = this.GetValue(s_autoPlayProperty)
                and set(v: bool) = this.SetValue(s_autoPlayProperty, v) |> ignore

            member this.IsLooping
                with get() = this.GetValue(s_isLoopingProperty)
                and set(v: bool) = this.SetValue(s_isLoopingProperty, v) |> ignore

            member this.Volume
                with get() = this.GetValue(s_volumeProperty)
                and set(v: float) = this.SetValue(s_volumeProperty, Math.Clamp(v, 0.0, 1.0)) |> ignore

            member this.Position
                with get() = this.GetValue(s_positionProperty)
                and set(v: TimeSpan) = this.SetValue(s_positionProperty, v) |> ignore

            member this.Duration
                with get() = this.GetValue(s_durationProperty)
                and set(v: TimeSpan) = this.SetValue(s_durationProperty, v) |> ignore

            member this.State
                with get() = this.GetValue(s_stateProperty)
                and set(v: MediaState) = this.SetValue(s_stateProperty, v) |> ignore

            member this.CurrentFrame
                with get() = this.GetValue(s_currentFrameProperty)
                and set(v: IImage) =
                    this.SetValue(s_currentFrameProperty, v) |> ignore
                    _frameImage.Source <- v

            member private this.FormatTime(ts: TimeSpan) =
                sprintf "%02d:%02d" (int ts.TotalMinutes) ts.Seconds

            member private this.UpdatePlaybackUi() =
                _timeTextBlock.Text <- sprintf "%s / %s" (this.FormatTime(this.Position)) (this.FormatTime(this.Duration))
                if this.Duration > TimeSpan.Zero then
                    _seekSlider.Value <- (this.Position.TotalSeconds / this.Duration.TotalSeconds) * 100.0
                match this.State with
                | MediaState.Playing -> _btnPlayPause.Content <- "⏸"
                | _ -> _btnPlayPause.Content <- "▶"

            member this.Play() =
                if this.Duration <= TimeSpan.Zero then
                    this.Duration <- TimeSpan.FromSeconds(30.0)
                this.State <- MediaState.Playing
                _playbackTimer.Start()
                this.UpdatePlaybackUi()

            member this.Pause() =
                if this.State = MediaState.Playing then
                    this.State <- MediaState.Paused
                    _playbackTimer.Stop()
                    this.UpdatePlaybackUi()

            member this.Stop() =
                this.State <- MediaState.Stopped
                _playbackTimer.Stop()
                this.Position <- TimeSpan.Zero
                this.UpdatePlaybackUi()

            member this.Load(source: string) =
                if String.IsNullOrWhiteSpace(source) then
                    this.Stop()
                    this.CurrentFrame <- null
                    _titleTextBlock.Text <- "▶ Media Player"
                else
                    let bmp = BitmapHelper.LoadBitmap(source)
                    if not (isNull bmp) then
                        this.CurrentFrame <- bmp
                    let name = Path.GetFileName(source)
                    _titleTextBlock.Text <- if String.IsNullOrWhiteSpace(name) then source else name
                    if this.AutoPlay then this.Play()
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Controls", "MediaPlayerControl.fs"), mediaPlayerFs, SourceFileType.ProjectFile));

        // INavigationService.fs
        var inavFs = $"""
        // <auto-generated />
        #nowarn "3261" "3262" "3263" "1183" "0020"
        namespace {project.RootNamespace}.Services

        open System
        open Avalonia.Controls

        type INavigationService =
            abstract member NavigateTo: Type -> unit
            abstract member NavigateTo<'TView when 'TView :> Control> : unit -> unit
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "INavigationService.fs"), inavFs, SourceFileType.ProjectFile));

        // ViewModels and Views
        var fsharpRegistrations = new StringBuilder();
        foreach (var doc in project.Documents)
        {
            var viewRes = _fsharpViewGenerator.Generate(doc);
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Views", viewRes.FileName), viewRes.Content, SourceFileType.View));

            if (doc.ArchitectureMode != ArchitectureMode.CodeBehind)
            {
                var vmRes = _fsharpViewModelGenerator.Generate(doc);
                files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "ViewModels", vmRes.FileName), vmRes.Content, SourceFileType.ViewModel));

                fsharpRegistrations.AppendLine($"        services.AddTransient<{doc.ViewModelClassName}>() |> ignore");
                fsharpRegistrations.AppendLine($"        services.AddTransient<{doc.ViewClassName}>(fun sp ->");
                fsharpRegistrations.AppendLine($"            let view = {doc.ViewClassName}()");
                fsharpRegistrations.AppendLine($"            view.DataContext <- sp.GetRequiredService<{doc.ViewModelClassName}>()");
                fsharpRegistrations.AppendLine("            view) |> ignore");
            }
            else
            {
                fsharpRegistrations.AppendLine($"        services.AddTransient<{doc.ViewClassName}>() |> ignore");
            }
        }

        // App.fs (包含 NavigationService 實作以滿足 F# 編譯相依性順序)
        var appFs = $"""
        // <auto-generated />
        #nowarn "3261" "3262" "3263" "1183" "0020"
        namespace {project.RootNamespace}

        open System
        open Avalonia
        open Avalonia.Controls
        open Avalonia.Controls.ApplicationLifetimes
        open Avalonia.Themes.Fluent
        open Microsoft.Extensions.DependencyInjection
        open {project.RootNamespace}.Services

        type App() =
            inherit Application()

            static let mutable _services: IServiceProvider = null
            static let mutable _mainWindow: Window = null
            static let mutable _singleViewLifetime: ISingleViewApplicationLifetime = null

            static member Services with get() = _services and private set(v) = _services <- v
            static member MainWindow with get() = _mainWindow and private set(v) = _mainWindow <- v

            static member SetActiveView(view: Control) =
                if not (isNull _mainWindow) then
                    _mainWindow.Content <- view
                elif not (isNull _singleViewLifetime) then
                    _singleViewLifetime.MainView <- view

            override this.Initialize() =
                this.Styles.Add(FluentTheme())

            override this.OnFrameworkInitializationCompleted() =
                let services = ServiceCollection()
                App.ConfigureServices(services)
                _services <- services.BuildServiceProvider()

                match this.ApplicationLifetime with
                | :? IClassicDesktopStyleApplicationLifetime as desktop ->
                    let initialView = _services.GetRequiredService<{initialDoc.ViewClassName}>()
                    let window = Window()
                    window.Title <- Config.AppTitle
                    window.Width <- Config.DefaultWindowWidth
                    window.Height <- Config.DefaultWindowHeight
                    window.Content <- initialView
                    _mainWindow <- window
                    desktop.MainWindow <- window
                | :? ISingleViewApplicationLifetime as singleView ->
                    _singleViewLifetime <- singleView
                    singleView.MainView <- _services.GetRequiredService<{initialDoc.ViewClassName}>()
                | _ -> ()

                base.OnFrameworkInitializationCompleted()

            static member private ConfigureServices(services: IServiceCollection) =
                services.AddSingleton<INavigationService, NavigationService>() |> ignore
        {fsharpRegistrations.ToString().TrimEnd()}

        and NavigationService(serviceProvider: IServiceProvider) =
            interface INavigationService with
                member this.NavigateTo(viewType: Type) =
                    ArgumentNullException.ThrowIfNull(viewType)
                    let view = serviceProvider.GetRequiredService(viewType) :?> Control
                    App.SetActiveView(view)

                member this.NavigateTo<'TView when 'TView :> Control>() =
                    (this :> INavigationService).NavigateTo(typeof<'TView>)
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "App.fs"), appFs, SourceFileType.ProjectFile));

        // 3. .Desktop 專案 (.fsproj)
        var desktopFsprojContent = $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>WinExe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <NoWarn>$(NoWarn);FS3261;FS3262;FS3263;FS1183;FS0020;3261;3262;3263;1183;0020</NoWarn>
          </PropertyGroup>

          <ItemGroup>
            <ProjectReference Include="..\..\src\{sharedProjectName}\{sharedProjectName}.fsproj" />
          </ItemGroup>

          <ItemGroup>
            <PackageReference Include="Avalonia.Desktop" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Diagnostics" Version="{PackageVersions.Avalonia}" />
          </ItemGroup>

          <ItemGroup>
            <Compile Include="Program.fs" />
          </ItemGroup>
        </Project>
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(desktopDir, $"{desktopProjectName}.fsproj"), desktopFsprojContent, SourceFileType.ProjectFile));

        // Program.fs
        var programFs = $"""
        // <auto-generated />
        #nowarn "3261" "3262" "3263" "1183" "0020"
        namespace {project.RootNamespace}.Desktop

        open System
        open Avalonia
        open Avalonia.Fonts.Inter
        open {project.RootNamespace}

        module Program =
            [<CompiledName "BuildAvaloniaApp">]
            let buildAvaloniaApp () =
                AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .LogToTrace()

            [<EntryPoint; STAThread>]
            let main args =
                buildAvaloniaApp().StartWithClassicDesktopLifetime(args)
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(desktopDir, "Program.fs"), programFs, SourceFileType.ProjectFile));

        // 4. 行動端宿主專案：src/{ProjectName}.Android/ (可選)
        if (options.IncludeMobileProject)
        {
            var androidCsprojContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0-android</TargetFramework>
                <SupportedOSPlatformVersion>21</SupportedOSPlatformVersion>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <LangVersion>latest</LangVersion>
                <RootNamespace>{project.RootNamespace}.Android</RootNamespace>
                <ApplicationId>com.afg.{baseProjectName.ToLowerInvariant()}</ApplicationId>
                <ApplicationVersion>1</ApplicationVersion>
                <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
                <AndroidPackageFormat>apk</AndroidPackageFormat>
                <AndroidEnablePreBuildValidation>true</AndroidEnablePreBuildValidation>
                <NoWarn>$(NoWarn);NU1903;XA4211</NoWarn>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="..\{sharedProjectName}\{sharedProjectName}.fsproj" />
                <PackageReference Include="Avalonia.Android" Version="{PackageVersions.Avalonia}" />
                <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
              </ItemGroup>
            </Project>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, $"{androidProjectName}.csproj"), androidCsprojContent, SourceFileType.ProjectFile));

            var androidGlobalUsingsCs = $$"""
            // <auto-generated />
            global using System;
            global using Android.App;
            global using Android.Content.PM;
            global using Android.OS;
            global using Avalonia;
            global using Avalonia.Android;
            global using {{project.RootNamespace}};
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "GlobalUsings.cs"), androidGlobalUsingsCs, SourceFileType.ProjectFile));

            var mainActivityCs = $$"""
            // <auto-generated />
            #nullable enable

            namespace {{project.RootNamespace}}.Android;

            [Activity(
                Label = "{{project.Title}}",
                Theme = "@style/MyTheme.NoActionBar",
                Icon = "@drawable/icon",
                MainLauncher = true,
                ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
            public class MainActivity : AvaloniaMainActivity<App>
            {
                protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
                {
                    return base.CustomizeAppBuilder(builder)
                        .WithInterFont();
                }
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "MainActivity.cs"), mainActivityCs, SourceFileType.ProjectFile));

            var stylesXml = """
            <?xml version="1.0" encoding="utf-8" ?>
            <resources>
              <style name="MyTheme.NoActionBar" parent="Theme.AppCompat.DayNight.NoActionBar">
                <item name="android:windowNoTitle">true</item>
                <item name="android:windowActionBar">false</item>
                <item name="android:windowFullscreen">false</item>
              </style>
            </resources>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "Resources", "values", "styles.xml"), stylesXml, SourceFileType.ProjectFile));

            var iconXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="48dp"
                android:height="48dp"
                android:viewportWidth="48"
                android:viewportHeight="48">
              <path
                  android:fillColor="#3B82F6"
                  android:pathData="M24,4C12.95,4 4,12.95 4,24s8.95,20 20,20 20,-8.95 20,-20S35.05,4 24,4z"/>
            </vector>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "Resources", "drawable", "icon.xml"), iconXml, SourceFileType.ProjectFile));

            var androidManifestXml = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <manifest xmlns:android="http://schemas.android.com/apk/res/android"
                      package="com.afg.{{baseProjectName.ToLowerInvariant()}}"
                      android:versionCode="1"
                      android:versionName="1.0">
              <uses-sdk android:minSdkVersion="21" android:targetSdkVersion="34" />
              <application android:label="{{project.Title}}" android:icon="@drawable/icon" android:theme="@style/MyTheme.NoActionBar" />
            </manifest>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "AndroidManifest.xml"), androidManifestXml, SourceFileType.ProjectFile));
        }

        return files;
    }

    /// <summary>
    /// 生成包含 Visual Basic 專案檔 (.vbproj)、.Shared 核心與 .Desktop 桌面宿主之完整 VB.NET 方案檔案集合。
    /// </summary>
    public IReadOnlyList<GeneratedSourceFile> GenerateVisualBasicProject(FormProjectDefinition project, ProjectExportOptions options)
    {
        var files = new List<GeneratedSourceFile>();
        var rawProjectName = string.IsNullOrWhiteSpace(options.CustomProjectName) ? project.ProjectName : options.CustomProjectName;
        var baseProjectName = SanitizeProjectName(rawProjectName);

        var sharedProjectName = $"{baseProjectName}.Shared";
        var desktopProjectName = $"{baseProjectName}.Desktop";
        var androidProjectName = $"{baseProjectName}.Android";

        var sharedDir = Path.Combine("src", sharedProjectName);
        var desktopDir = Path.Combine("src", desktopProjectName);
        var androidDir = Path.Combine("src", androidProjectName);

        // 1. 方案檔 (.slnx)
        var slnxBuilder = new StringBuilder();
        slnxBuilder.AppendLine("<Solution>");
        slnxBuilder.AppendLine($"  <Project Path=\"src/{sharedProjectName}/{sharedProjectName}.vbproj\" />");
        slnxBuilder.AppendLine($"  <Project Path=\"src/{desktopProjectName}/{desktopProjectName}.vbproj\" />");
        if (options.IncludeMobileProject)
        {
            slnxBuilder.AppendLine($"  <Project Path=\"src/{androidProjectName}/{androidProjectName}.csproj\" />");
        }
        slnxBuilder.AppendLine("</Solution>");
        files.Add(new GeneratedSourceFile($"{baseProjectName}.slnx", slnxBuilder.ToString().TrimEnd(), SourceFileType.SolutionFile));

        // .gitignore
        var gitignoreContent = """
        ## Visual Studio & .NET
        .vs/
        [Bb]in/
        [Oo]bj/
        *.user
        *.suo
        """;
        files.Add(new GeneratedSourceFile(".gitignore", gitignoreContent, SourceFileType.ProjectFile));

        var initialDoc = project.Documents.FirstOrDefault(d => d.ViewClassName == project.InitialFormName) ?? project.Documents[0];

        // 2. .Shared 專案 (.vbproj)
        var sharedVbprojContent = $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <OptionExplicit>On</OptionExplicit>
            <OptionStrict>On</OptionStrict>
            <RootNamespace></RootNamespace>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Avalonia" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Themes.Fluent" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="CommunityToolkit.Mvvm" Version="{PackageVersions.CommunityToolkitMvvm}" />
            <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="{PackageVersions.MicrosoftExtensionsDependencyInjection}" />
            <PackageReference Include="Microsoft.Extensions.Logging" Version="{PackageVersions.MicrosoftExtensionsLogging}" />
          </ItemGroup>

          <ItemGroup>
            <AvaloniaResource Include="Assets\**" />
            <None Update="Assets\**" CopyToOutputDirectory="PreserveNewest" />
          </ItemGroup>
        </Project>
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, $"{sharedProjectName}.vbproj"), sharedVbprojContent, SourceFileType.ProjectFile));

        // Config.vb
        var configVb = $"""
        ' <auto-generated />
        Namespace {project.RootNamespace}
            Public Module Config
                Public Const AppTitle As String = "{project.Title}"
                Public Const Version As String = "1.0.0"
                Public Const DefaultWindowWidth As Double = {initialDoc.CanvasWidth.ToString(CultureInfo.InvariantCulture)}
                Public Const DefaultWindowHeight As Double = {initialDoc.CanvasHeight.ToString(CultureInfo.InvariantCulture)}
                Public Const CanResize As Boolean = {(initialDoc.CanResize ? "True" : "False")}
                Public Const Topmost As Boolean = {(initialDoc.Topmost ? "True" : "False")}
                Public Const ShowInTaskbar As Boolean = {(initialDoc.ShowInTaskbar ? "True" : "False")}
            End Module
        End Namespace
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Config.vb"), configVb, SourceFileType.ProjectFile));

        // Helpers/BitmapHelper.vb
        var bitmapHelperVb = $$"""
        ' <auto-generated />
        Imports System
        Imports System.IO
        Imports System.Net.Http
        Imports Avalonia
        Imports Avalonia.Media
        Imports Avalonia.Media.Imaging
        Imports Avalonia.Platform

        Namespace {{project.RootNamespace}}.Helpers
            Public NotInheritable Class BitmapHelper
                Private Sub New()
                End Sub

                Public Shared Function CreateInitializedBitmap(width As Double, height As Double, Optional backgroundColor As Color? = Nothing) As WriteableBitmap
                    Dim w = CInt(Math.Max(1.0, width))
                    Dim h = CInt(Math.Max(1.0, height))
                    Dim dpi As New Vector(96.0, 96.0)
                    Dim wb As New WriteableBitmap(New PixelSize(w, h), dpi, PixelFormat.Bgra8888, AlphaFormat.Premul)
                    Dim bg = If(backgroundColor.HasValue, backgroundColor.Value, Color.FromArgb(255, 240, 240, 240))
                    Using fb = wb.Lock()
                        Dim totalBytes = fb.RowBytes * h
                        Dim buffer(totalBytes - 1) As Byte
                        Dim cB = bg.B
                        Dim cG = bg.G
                        Dim cR = bg.R
                        Dim cA = bg.A
                        For y = 0 To h - 1
                            Dim rowOffset = y * fb.RowBytes
                            For x = 0 To w - 1
                                Dim offset = rowOffset + (x * 4)
                                buffer(offset + 0) = cB
                                buffer(offset + 1) = cG
                                buffer(offset + 2) = cR
                                buffer(offset + 3) = cA
                            Next
                        Next
                        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, fb.Address, totalBytes)
                    End Using
                    Return wb
                End Function

                Public Shared Function LoadBitmap(pathOrUri As String) As Bitmap
                    If String.IsNullOrWhiteSpace(pathOrUri) Then Return Nothing
                    Dim trimmed = pathOrUri.Trim()
                    If File.Exists(trimmed) Then
                        Try
                            Return New Bitmap(trimmed)
                        Catch
                            Return Nothing
                        End Try
                    ElseIf trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then
                        Try
                            Using client As New HttpClient()
                                client.Timeout = TimeSpan.FromSeconds(10)
                                Dim bytes = client.GetByteArrayAsync(trimmed).GetAwaiter().GetResult()
                                Using ms As New MemoryStream(bytes)
                                    Return New Bitmap(ms)
                                End Using
                            End Using
                        Catch
                            Return Nothing
                        End Try
                    ElseIf trimmed.StartsWith("avares://", StringComparison.OrdinalIgnoreCase) Then
                        Try
                            Dim uri As New Uri(trimmed)
                            If AssetLoader.Exists(uri) Then
                                Using stream = AssetLoader.Open(uri)
                                    Return New Bitmap(stream)
                                End Using
                            Else
                                Dim resPath = uri.AbsolutePath.TrimStart("/"c)
                                Dim baseDir = AppContext.BaseDirectory
                                Dim cand1 = Path.Combine(baseDir, resPath)
                                Dim cand2 = Path.Combine(baseDir, "Assets", Path.GetFileName(resPath))
                                If File.Exists(cand1) Then Return New Bitmap(cand1)
                                If File.Exists(cand2) Then Return New Bitmap(cand2)
                                Return Nothing
                            End If
                        Catch
                            Return Nothing
                        End Try
                    Else
                        Dim baseDir = AppContext.BaseDirectory
                        Dim cand1 = Path.Combine(baseDir, trimmed)
                        Dim cand2 = Path.Combine(baseDir, "Assets", Path.GetFileName(trimmed))
                        If File.Exists(cand1) Then Return New Bitmap(cand1)
                        If File.Exists(cand2) Then Return New Bitmap(cand2)
                        Return Nothing
                    End If
                End Function
            End Class
        End Namespace
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Helpers", "BitmapHelper.vb"), bitmapHelperVb, SourceFileType.ProjectFile));

        // Controls/MediaPlayerControl.vb
        var mediaPlayerVb = $$"""
        ' <auto-generated />
        Imports System
        Imports System.IO
        Imports Avalonia
        Imports Avalonia.Controls
        Imports Avalonia.Layout
        Imports Avalonia.Media
        Imports Avalonia.Media.Imaging
        Imports Avalonia.Threading
        Imports {{project.RootNamespace}}.Helpers

        Namespace {{project.RootNamespace}}.Controls
            Public Enum MediaState
                Stopped = 0
                Playing = 1
                Paused = 2
                Buffering = 3
                [Error] = 4
            End Enum

            Public Class MediaPlayerControl
                Inherits UserControl

                Public Shared ReadOnly SourceProperty As StyledProperty(Of String) =
                    AvaloniaProperty.Register(Of MediaPlayerControl, String)("Source")

                Public Shared ReadOnly AutoPlayProperty As StyledProperty(Of Boolean) =
                    AvaloniaProperty.Register(Of MediaPlayerControl, Boolean)("AutoPlay", False)

                Public Shared ReadOnly IsLoopingProperty As StyledProperty(Of Boolean) =
                    AvaloniaProperty.Register(Of MediaPlayerControl, Boolean)("IsLooping", False)

                Public Shared ReadOnly VolumeProperty As StyledProperty(Of Double) =
                    AvaloniaProperty.Register(Of MediaPlayerControl, Double)("Volume", 1.0)

                Public Shared ReadOnly PositionProperty As StyledProperty(Of TimeSpan) =
                    AvaloniaProperty.Register(Of MediaPlayerControl, TimeSpan)("Position", TimeSpan.Zero)

                Public Shared ReadOnly DurationProperty As StyledProperty(Of TimeSpan) =
                    AvaloniaProperty.Register(Of MediaPlayerControl, TimeSpan)("Duration", TimeSpan.FromSeconds(10))

                Public Shared ReadOnly StateProperty As StyledProperty(Of MediaState) =
                    AvaloniaProperty.Register(Of MediaPlayerControl, MediaState)("State", MediaState.Stopped)

                Public Shared ReadOnly CurrentFrameProperty As StyledProperty(Of IImage) =
                    AvaloniaProperty.Register(Of MediaPlayerControl, IImage)("CurrentFrame")

                Private ReadOnly _playbackTimer As DispatcherTimer
                Private ReadOnly _frameImage As Image
                Private ReadOnly _titleTextBlock As TextBlock
                Private ReadOnly _btnPlayPause As Button
                Private ReadOnly _btnStop As Button
                Private ReadOnly _seekSlider As Slider
                Private ReadOnly _timeTextBlock As TextBlock

                Public Sub New()
                    Background = New SolidColorBrush(Color.Parse("#09090B"))
                    ClipToBounds = True

                    _frameImage = New Image() With {
                        .HorizontalAlignment = HorizontalAlignment.Stretch,
                        .VerticalAlignment = VerticalAlignment.Stretch
                    }
                    _frameImage.Bind(Image.SourceProperty, Me.GetObservable(CurrentFrameProperty))

                    _titleTextBlock = New TextBlock() With {
                        .Text = "▶ Media Player",
                        .Foreground = New SolidColorBrush(Color.Parse("#F4F4F5")),
                        .FontSize = 13,
                        .FontWeight = FontWeight.SemiBold,
                        .HorizontalAlignment = HorizontalAlignment.Center
                    }

                    _btnPlayPause = New Button() With {.Content = "▶", .FontSize = 11, .Padding = New Thickness(6, 2)}
                    AddHandler _btnPlayPause.Click, Sub()
                                                       If State = MediaState.Playing Then
                                                           Pause()
                                                       Else
                                                           Play()
                                                       End If
                                                   End Sub

                    _btnStop = New Button() With {.Content = "⏹", .FontSize = 11, .Padding = New Thickness(6, 2)}
                    AddHandler _btnStop.Click, Sub() [Stop]()

                    _seekSlider = New Slider() With {.Minimum = 0, .Maximum = 100, .Value = 0, .VerticalAlignment = VerticalAlignment.Center}
                    _timeTextBlock = New TextBlock() With {.Text = "00:00 / 00:10", .FontSize = 10, .Foreground = New SolidColorBrush(Color.Parse("#A1A1AA")), .VerticalAlignment = VerticalAlignment.Center, .Margin = New Thickness(6, 0, 6, 0)}

                    Dim controlsGrid As New Grid()
                    controlsGrid.ColumnDefinitions.Add(New ColumnDefinition(GridLength.Auto))
                    controlsGrid.ColumnDefinitions.Add(New ColumnDefinition(GridLength.Auto))
                    controlsGrid.ColumnDefinitions.Add(New ColumnDefinition(GridLength.Star))
                    controlsGrid.ColumnDefinitions.Add(New ColumnDefinition(GridLength.Auto))

                    Grid.SetColumn(_btnPlayPause, 0)
                    Grid.SetColumn(_btnStop, 1)
                    Grid.SetColumn(_seekSlider, 2)
                    Grid.SetColumn(_timeTextBlock, 3)

                    controlsGrid.Children.Add(_btnPlayPause)
                    controlsGrid.Children.Add(_btnStop)
                    controlsGrid.Children.Add(_seekSlider)
                    controlsGrid.Children.Add(_timeTextBlock)

                    Dim controlsBar As New Border() With {
                        .VerticalAlignment = VerticalAlignment.Bottom,
                        .Background = New SolidColorBrush(Color.Parse("#D918181B")),
                        .BorderBrush = New SolidColorBrush(Color.Parse("#27272A")),
                        .BorderThickness = New Thickness(0, 1, 0, 0),
                        .Padding = New Thickness(8, 4),
                        .Child = controlsGrid
                    }

                    Dim hudPanel As New StackPanel() With {.HorizontalAlignment = HorizontalAlignment.Center, .VerticalAlignment = VerticalAlignment.Center}
                    hudPanel.Children.Add(_titleTextBlock)

                    Dim mainGrid As New Grid()
                    mainGrid.Children.Add(_frameImage)
                    mainGrid.Children.Add(hudPanel)
                    mainGrid.Children.Add(controlsBar)
                    Content = mainGrid

                    _playbackTimer = New DispatcherTimer() With {.Interval = TimeSpan.FromMilliseconds(50)}
                    AddHandler _playbackTimer.Tick, AddressOf OnPlaybackTick
                End Sub

                Public Property Source As String
                    Get
                        Return GetValue(SourceProperty)
                    End Get
                    Set(value As String)
                        SetValue(SourceProperty, value)
                        If Not String.IsNullOrWhiteSpace(value) Then Load(value)
                    End Set
                End Property

                Public Property AutoPlay As Boolean
                    Get
                        Return GetValue(AutoPlayProperty)
                    End Get
                    Set(value As Boolean)
                        SetValue(AutoPlayProperty, value)
                    End Set
                End Property

                Public Property IsLooping As Boolean
                    Get
                        Return GetValue(IsLoopingProperty)
                    End Get
                    Set(value As Boolean)
                        SetValue(IsLoopingProperty, value)
                    End Set
                End Property

                Public Property Volume As Double
                    Get
                        Return GetValue(VolumeProperty)
                    End Get
                    Set(value As Double)
                        SetValue(VolumeProperty, Math.Clamp(value, 0.0, 1.0))
                    End Set
                End Property

                Public Property Position As TimeSpan
                    Get
                        Return GetValue(PositionProperty)
                    End Get
                    Set(value As TimeSpan)
                        SetValue(PositionProperty, value)
                    End Set
                End Property

                Public Property Duration As TimeSpan
                    Get
                        Return GetValue(DurationProperty)
                    End Get
                    Set(value As TimeSpan)
                        SetValue(DurationProperty, value)
                    End Set
                End Property

                Public Property State As MediaState
                    Get
                        Return GetValue(StateProperty)
                    End Get
                    Set(value As MediaState)
                        SetValue(StateProperty, value)
                    End Set
                End Property

                Public Property CurrentFrame As IImage
                    Get
                        Return GetValue(CurrentFrameProperty)
                    End Get
                    Set(value As IImage)
                        SetValue(CurrentFrameProperty, value)
                    End Set
                End Property

                Private Function FormatTime(ts As TimeSpan) As String
                    Return $"{CInt(ts.TotalMinutes):D2}:{ts.Seconds:D2}"
                End Function

                Private Sub UpdatePlaybackUi()
                    _timeTextBlock.Text = $"{FormatTime(Position)} / {FormatTime(Duration)}"
                    If Duration > TimeSpan.Zero Then
                        _seekSlider.Value = (Position.TotalSeconds / Duration.TotalSeconds) * 100.0
                    End If
                    If State = MediaState.Playing Then
                        _btnPlayPause.Content = "⏸"
                    Else
                        _btnPlayPause.Content = "▶"
                    End If
                End Sub

                Private Sub OnPlaybackTick(sender As Object, e As EventArgs)
                    If State <> MediaState.Playing Then Return
                    Dim newPos = Position + TimeSpan.FromMilliseconds(50)
                    If Duration > TimeSpan.Zero AndAlso newPos >= Duration Then
                        If IsLooping Then
                            Position = TimeSpan.Zero
                        Else
                            Position = Duration
                            [Stop]()
                        End If
                    Else
                        Position = newPos
                    End If
                    UpdatePlaybackUi()
                End Sub

                Public Sub Play()
                    If Duration <= TimeSpan.Zero Then Duration = TimeSpan.FromSeconds(30)
                    State = MediaState.Playing
                    _playbackTimer.Start()
                    UpdatePlaybackUi()
                End Sub

                Public Sub Pause()
                    If State = MediaState.Playing Then
                        State = MediaState.Paused
                        _playbackTimer.Stop()
                        UpdatePlaybackUi()
                    End If
                End Sub

                Public Sub [Stop]()
                    State = MediaState.Stopped
                    _playbackTimer.Stop()
                    Position = TimeSpan.Zero
                    UpdatePlaybackUi()
                End Sub

                Public Sub Load(sourcePath As String)
                    If String.IsNullOrWhiteSpace(sourcePath) Then
                        [Stop]()
                        CurrentFrame = Nothing
                        _titleTextBlock.Text = "▶ Media Player"
                    Else
                        Dim bmp = BitmapHelper.LoadBitmap(sourcePath)
                        If bmp IsNot Nothing Then CurrentFrame = bmp
                        Dim name = Path.GetFileName(sourcePath)
                        _titleTextBlock.Text = If(String.IsNullOrWhiteSpace(name), sourcePath, name)
                        If AutoPlay Then Play()
                    End If
                End Sub
            End Class
        End Namespace
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Controls", "MediaPlayerControl.vb"), mediaPlayerVb, SourceFileType.ProjectFile));

        // INavigationService.vb
        var inavVb = $"""
        ' <auto-generated />
        Imports System
        Imports Avalonia.Controls

        Namespace {project.RootNamespace}.Services
            Public Interface INavigationService
                Sub NavigateTo(Of TView As Control)()
                Sub NavigateTo(viewType As Type)
            End Interface
        End Namespace
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "INavigationService.vb"), inavVb, SourceFileType.ProjectFile));

        // NavigationService.vb
        var navVb = $"""
        ' <auto-generated />
        Imports System
        Imports Avalonia.Controls
        Imports Microsoft.Extensions.DependencyInjection
        Imports {project.RootNamespace}

        Namespace {project.RootNamespace}.Services
            Public Class NavigationService
                Implements INavigationService

                Private ReadOnly _serviceProvider As IServiceProvider

                Public Sub New(serviceProvider As IServiceProvider)
                    _serviceProvider = serviceProvider
                End Sub

                Public Sub NavigateTo(Of TView As Control)() Implements INavigationService.NavigateTo
                    NavigateTo(GetType(TView))
                End Sub

                Public Sub NavigateTo(viewType As Type) Implements INavigationService.NavigateTo
                    ArgumentNullException.ThrowIfNull(viewType)
                    Dim view = DirectCast(_serviceProvider.GetRequiredService(viewType), Control)
                    App.SetActiveView(view)
                End Sub
            End Class
        End Namespace
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", "NavigationService.vb"), navVb, SourceFileType.ProjectFile));

        // ViewModels and Views
        var vbRegistrations = new StringBuilder();
        foreach (var doc in project.Documents)
        {
            var viewRes = _vbViewGenerator.Generate(doc);
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Views", viewRes.FileName), viewRes.Content, SourceFileType.View));

            if (doc.ArchitectureMode != ArchitectureMode.CodeBehind)
            {
                var vmRes = _vbViewModelGenerator.Generate(doc);
                files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "ViewModels", vmRes.FileName), vmRes.Content, SourceFileType.ViewModel));

                vbRegistrations.AppendLine($"            services.AddTransient(Of {doc.ViewModelClassName})()");
                vbRegistrations.AppendLine($"            services.AddTransient(Of {doc.ViewClassName})(Function(sp) New {doc.ViewClassName}() With {{ .DataContext = sp.GetRequiredService(Of {doc.ViewModelClassName})() }})");
            }
            else
            {
                vbRegistrations.AppendLine($"            services.AddTransient(Of {doc.ViewClassName})()");
            }
        }

        // App.vb
        var appVb = $"""
        ' <auto-generated />
        Imports System
        Imports Avalonia
        Imports Avalonia.Controls
        Imports Avalonia.Controls.ApplicationLifetimes
        Imports Avalonia.Themes.Fluent
        Imports Microsoft.Extensions.DependencyInjection
        Imports {project.RootNamespace}.Services

        Namespace {project.RootNamespace}
            Public Class App
                Inherits Application

                Private Shared _services As IServiceProvider
                Private Shared _mainWindow As Window
                Private Shared _singleViewLifetime As ISingleViewApplicationLifetime

                Public Shared Property Services As IServiceProvider
                    Get
                        Return _services
                    End Get
                    Private Set(value As IServiceProvider)
                        _services = value
                    End Set
                End Property

                Public Shared Property MainWindow As Window
                    Get
                        Return _mainWindow
                    End Get
                    Private Set(value As Window)
                        _mainWindow = value
                    End Set
                End Property

                Public Shared Sub SetActiveView(view As Control)
                    If _mainWindow IsNot Nothing Then
                        _mainWindow.Content = view
                    ElseIf _singleViewLifetime IsNot Nothing Then
                        _singleViewLifetime.MainView = view
                    End If
                End Sub

                Public Overrides Sub Initialize()
                    Me.Styles.Add(New FluentTheme())
                End Sub

                Public Overrides Sub OnFrameworkInitializationCompleted()
                    Dim services As New ServiceCollection()
                    ConfigureServices(services)
                    _services = services.BuildServiceProvider()

                    If TypeOf ApplicationLifetime Is IClassicDesktopStyleApplicationLifetime Then
                        Dim desktop = DirectCast(ApplicationLifetime, IClassicDesktopStyleApplicationLifetime)
                        Dim initialView = _services.GetRequiredService(Of {initialDoc.ViewClassName})()
                        Dim window As New Window()
                        window.Title = Config.AppTitle
                        window.Width = Config.DefaultWindowWidth
                        window.Height = Config.DefaultWindowHeight
                        window.Content = initialView
                        _mainWindow = window
                        desktop.MainWindow = window
                    ElseIf TypeOf ApplicationLifetime Is ISingleViewApplicationLifetime Then
                        Dim singleView = DirectCast(ApplicationLifetime, ISingleViewApplicationLifetime)
                        _singleViewLifetime = singleView
                        singleView.MainView = _services.GetRequiredService(Of {initialDoc.ViewClassName})()
                    End If

                    MyBase.OnFrameworkInitializationCompleted()
                End Sub

                Private Shared Sub ConfigureServices(services As IServiceCollection)
                    services.AddSingleton(Of INavigationService, NavigationService)()
        {vbRegistrations.ToString().TrimEnd()}
                End Sub
            End Class
        End Namespace
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "App.vb"), appVb, SourceFileType.ProjectFile));

        // 3. .Desktop 專案 (.vbproj)
        var desktopVbprojContent = $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>WinExe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <RootNamespace></RootNamespace>
            <StartupObject>Sub Main</StartupObject>
          </PropertyGroup>

          <ItemGroup>
            <ProjectReference Include="..\..\src\{sharedProjectName}\{sharedProjectName}.vbproj" />
          </ItemGroup>

          <ItemGroup>
            <PackageReference Include="Avalonia.Desktop" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
            <PackageReference Include="Avalonia.Diagnostics" Version="{PackageVersions.Avalonia}" />
          </ItemGroup>
        </Project>
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(desktopDir, $"{desktopProjectName}.vbproj"), desktopVbprojContent, SourceFileType.ProjectFile));

        // Program.vb
        var programVb = $"""
        ' <auto-generated />
        Imports System
        Imports Avalonia
        Imports Avalonia.Fonts.Inter
        Imports {project.RootNamespace}

        Namespace {project.RootNamespace}.Desktop
            Public Module Program
                Public Function BuildAvaloniaApp() As AppBuilder
                    Return AppBuilder.Configure(Of App)() _
                        .UsePlatformDetect() _
                        .WithInterFont() _
                        .LogToTrace()
                End Function

                <STAThread>
                Public Sub Main(args As String())
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)
                End Sub
            End Module
        End Namespace
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(desktopDir, "Program.vb"), programVb, SourceFileType.ProjectFile));

        // 4. 行動端宿主專案：src/{ProjectName}.Android/ (可選)
        if (options.IncludeMobileProject)
        {
            var androidCsprojContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0-android</TargetFramework>
                <SupportedOSPlatformVersion>21</SupportedOSPlatformVersion>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <LangVersion>latest</LangVersion>
                <RootNamespace>{project.RootNamespace}.Android</RootNamespace>
                <ApplicationId>com.afg.{baseProjectName.ToLowerInvariant()}</ApplicationId>
                <ApplicationVersion>1</ApplicationVersion>
                <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
                <AndroidPackageFormat>apk</AndroidPackageFormat>
                <AndroidEnablePreBuildValidation>true</AndroidEnablePreBuildValidation>
                <NoWarn>$(NoWarn);NU1903;XA4211</NoWarn>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="..\{sharedProjectName}\{sharedProjectName}.vbproj" />
                <PackageReference Include="Avalonia.Android" Version="{PackageVersions.Avalonia}" />
                <PackageReference Include="Avalonia.Fonts.Inter" Version="{PackageVersions.Avalonia}" />
              </ItemGroup>
            </Project>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, $"{androidProjectName}.csproj"), androidCsprojContent, SourceFileType.ProjectFile));

            var androidGlobalUsingsCs = $$"""
            // <auto-generated />
            global using System;
            global using Android.App;
            global using Android.Content.PM;
            global using Android.OS;
            global using Avalonia;
            global using Avalonia.Android;
            global using {{project.RootNamespace}};
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "GlobalUsings.cs"), androidGlobalUsingsCs, SourceFileType.ProjectFile));

            var mainActivityCs = $$"""
            // <auto-generated />
            #nullable enable

            namespace {{project.RootNamespace}}.Android;

            [Activity(
                Label = "{{project.Title}}",
                Theme = "@style/MyTheme.NoActionBar",
                Icon = "@drawable/icon",
                MainLauncher = true,
                ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
            public class MainActivity : AvaloniaMainActivity<App>
            {
                protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
                {
                    return base.CustomizeAppBuilder(builder)
                        .WithInterFont();
                }
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "MainActivity.cs"), mainActivityCs, SourceFileType.ProjectFile));

            var stylesXml = """
            <?xml version="1.0" encoding="utf-8" ?>
            <resources>
              <style name="MyTheme.NoActionBar" parent="Theme.AppCompat.DayNight.NoActionBar">
                <item name="android:windowNoTitle">true</item>
                <item name="android:windowActionBar">false</item>
                <item name="android:windowFullscreen">false</item>
              </style>
            </resources>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "Resources", "values", "styles.xml"), stylesXml, SourceFileType.ProjectFile));

            var iconXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="48dp"
                android:height="48dp"
                android:viewportWidth="48"
                android:viewportHeight="48">
              <path
                  android:fillColor="#3B82F6"
                  android:pathData="M24,4C12.95,4 4,12.95 4,24s8.95,20 20,20 20,-8.95 20,-20S35.05,4 24,4z"/>
            </vector>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "Resources", "drawable", "icon.xml"), iconXml, SourceFileType.ProjectFile));

            var androidManifestXml = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <manifest xmlns:android="http://schemas.android.com/apk/res/android"
                      package="com.afg.{{baseProjectName.ToLowerInvariant()}}"
                      android:versionCode="1"
                      android:versionName="1.0">
              <uses-sdk android:minSdkVersion="21" android:targetSdkVersion="34" />
              <application android:label="{{project.Title}}" android:icon="@drawable/icon" android:theme="@style/MyTheme.NoActionBar" />
            </manifest>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "AndroidManifest.xml"), androidManifestXml, SourceFileType.ProjectFile));
        }

        return files;
    }

    /// <summary>
    /// 對專案名稱進行消毒，移除目錄穿越符號、非法字元與路徑分隔符號。
    /// </summary>
    public static string SanitizeProjectName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "AvaloniaApp";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var ch in rawName.Trim())
        {
            if (Array.IndexOf(invalidChars, ch) < 0 && ch is not '/' and not '\\' and not ':' and not '<' and not '>' and not '"' and not '|' and not '?' and not '*')
            {
                sb.Append(ch);
            }
        }

        var result = sb.ToString().Replace("..", string.Empty, StringComparison.Ordinal).Trim('.', ' ');
        return string.IsNullOrWhiteSpace(result) ? "AvaloniaApp" : result;
    }
}
