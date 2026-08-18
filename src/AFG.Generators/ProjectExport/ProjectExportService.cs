// filepath: src/AFG.Generators/ProjectExport/ProjectExportService.cs
using AFG.Generators.CSharpMarkup;

namespace AFG.Generators.ProjectExport;

/// <summary>
/// 專案匯出選項設定。
/// </summary>
public sealed record ProjectExportOptions(
    bool IncludeMobileProject = true,
    bool IncludeLicense = true,
    string? CustomProjectName = null);

/// <summary>
/// 產出具備相依性注入 (DI) 與跨平台多專案架構 (.Shared, .Desktop, 可選 .Android) 之 Avalonia 現代化方案匯出服務。
/// </summary>
public sealed class ProjectExportService(FormCodeGenerator? codeGenerator = null)
{
    private readonly FormCodeGenerator _codeGenerator = codeGenerator ?? new FormCodeGenerator();

    /// <summary>
    /// 生成包含 Visual Studio 現代化方案檔 (.slnx)、.Shared 共用核心、.Desktop 桌面宿主及可選 .Android 行動端專案的檔案集合。
    /// </summary>
    public IReadOnlyList<GeneratedSourceFile> GenerateFullProject(FormDocument document, ProjectExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new ProjectExportOptions();

        var result = _codeGenerator.GenerateAll(document);
        var files = new List<GeneratedSourceFile>();

        var rawName = string.IsNullOrWhiteSpace(document.ViewClassName)
            ? "GeneratedApp"
            : document.ViewClassName.Replace("View", "", StringComparison.Ordinal) + "App";

        var baseProjectName = string.IsNullOrWhiteSpace(options.CustomProjectName) ? rawName : options.CustomProjectName.Trim();

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
        end_of_line = crlf
        charset = utf-8
        trim_trailing_whitespace = true
        insert_final_newline = true

        [*.cs]
        csharp_prefer_braces = true:suggestion
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
            <RootNamespace>{document.RootNamespace}</RootNamespace>
            <NoWarn>$(NoWarn);NU1903</NoWarn>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Avalonia" Version="11.2.5" />
            <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.5" />
            <PackageReference Include="Avalonia.Fonts.Inter" Version="11.2.5" />
            <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
            <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.2" />
          </ItemGroup>
        </Project>
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, $"{sharedProjectName}.csproj"), sharedCsprojContent, SourceFileType.ProjectFile));

        var services = document.InjectedServices ?? [];

        var serviceRegistrations = services.Count > 0
            ? string.Join("\n", services.Select(s => $"        services.AddSingleton<{s.InterfaceName}, {(!string.IsNullOrEmpty(s.ImplementationName) ? s.ImplementationName : (s.InterfaceName.StartsWith('I') ? s.InterfaceName[1..] : $"{s.InterfaceName}Impl"))}>();")) + "\n"
            : string.Empty;

        var servicesUsing = services.Count > 0
            ? $"global using {document.RootNamespace}.Services;"
            : string.Empty;

        // App.cs（內建 DI 相依性注入與視窗最大化配置）
        var appCs = $$"""
        // <auto-generated />
        using System;
        using Avalonia;
        using Avalonia.Controls;
        using Avalonia.Controls.ApplicationLifetimes;
        using Avalonia.Styling;
        using Avalonia.Themes.Fluent;
        using Microsoft.Extensions.DependencyInjection;

        namespace {{document.RootNamespace}};

        /// <summary>
        /// 應用程式初始化、相依性注入 (DI) 與跨平台 UI 生命週期配置。
        /// </summary>
        public partial class App : Application
        {
            public static IServiceProvider Services { get; private set; } = null!;

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

                // 桌面端生命週期 (視窗最大化啟動)
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainView = Services.GetRequiredService<{{document.ViewClassName}}>();
                    desktop.MainWindow = new Window
                    {
                        Title = Config.AppTitle,
                        Width = Config.DefaultWindowWidth,
                        Height = Config.DefaultWindowHeight,
                        WindowState = WindowState.Maximized,
                        Content = mainView
                    };
                }
                // 行動端生命週期 (Android / iOS 單視圖呈現)
                else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                {
                    singleView.MainView = Services.GetRequiredService<{{document.ViewClassName}}>();
                }

                base.OnFrameworkInitializationCompleted();
            }

            private static void ConfigureServices(IServiceCollection services)
            {
        {{serviceRegistrations}}
                // 註冊檢視模型層 (ViewModels)
                services.AddTransient<{{document.ViewModelClassName}}>();

                // 註冊檢視層 (Views) 並自動綁定 DataContext
                services.AddTransient<{{document.ViewClassName}}>(sp =>
                {
                    var view = new {{document.ViewClassName}}();
                    view.DataContext = sp.GetRequiredService<{{document.ViewModelClassName}}>();
                    return view;
                });
            }
        }
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "App.cs"), appCs, SourceFileType.ProjectFile));

        // Config.cs
        var configCs = $$"""
        // <auto-generated />
        namespace {{document.RootNamespace}};

        /// <summary>
        /// 全域靜態組態配置（視窗大小、標題、版本與目標平台等）。
        /// </summary>
        public static class Config
        {
            public const string AppTitle = "{{document.Title}}";
            public const string Version = "1.0.0";
            public const double DefaultWindowWidth = {{document.CanvasWidth}};
            public const double DefaultWindowHeight = {{document.CanvasHeight}};
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
        global using System.Threading.Tasks;
        global using Avalonia;
        global using Avalonia.Controls;
        global using Avalonia.Data;
        global using Avalonia.Layout;
        global using Avalonia.Media;
        global using Avalonia.Styling;
        global using Avalonia.Themes.Fluent;
        global using CommunityToolkit.Mvvm.ComponentModel;
        global using CommunityToolkit.Mvvm.Input;
        global using Microsoft.Extensions.DependencyInjection;
        global using {{document.RootNamespace}};
        {{servicesUsing}}
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "GlobalUsings.cs"), globalUsingsCs, SourceFileType.ProjectFile));

        // C# Declarative UI 擴充方法：Markup/AvaloniaMarkupExtensions.cs
        files.Add(new GeneratedSourceFile(
            Path.Combine(sharedDir, "Markup", "AvaloniaMarkupExtensions.cs"),
            AvaloniaMarkupExtensionsSource.Code,
            SourceFileType.ProjectFile));

        // 服務層介面與實作：Services/*.cs
        foreach (var svc in services)
        {
            var implName = !string.IsNullOrEmpty(svc.ImplementationName)
                ? svc.ImplementationName
                : (svc.InterfaceName.StartsWith('I') ? svc.InterfaceName[1..] : $"{svc.InterfaceName}Impl");

            var ifaceCs = $$"""
            // <auto-generated />
            namespace {{document.RootNamespace}}.Services;

            public interface {{svc.InterfaceName}}
            {
                string Execute(string input);
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", $"{svc.InterfaceName}.cs"), ifaceCs, SourceFileType.ProjectFile));

            var implCs = $$"""
            // <auto-generated />
            namespace {{document.RootNamespace}}.Services;

            public sealed class {{implName}} : {{svc.InterfaceName}}
            {
                public string Execute(string input) => $"Processed: {input}";
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(sharedDir, "Services", $"{implName}.cs"), implCs, SourceFileType.ProjectFile));
        }

        // 檢視層與檢視模型層：Views 與 ViewModels
        var viewFile = result.Files.FirstOrDefault(f => f.FileType == SourceFileType.View);
        if (viewFile is not null)
        {
            files.Add(new GeneratedSourceFile(
                Path.Combine(sharedDir, "Views", viewFile.FileName),
                viewFile.Content,
                SourceFileType.View));
        }

        var vmFile = result.Files.FirstOrDefault(f => f.FileType == SourceFileType.ViewModel);
        if (vmFile is not null)
        {
            files.Add(new GeneratedSourceFile(
                Path.Combine(sharedDir, "ViewModels", vmFile.FileName),
                vmFile.Content,
                SourceFileType.ViewModel));
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
            <RootNamespace>{document.RootNamespace}.Desktop</RootNamespace>
            <NoWarn>$(NoWarn);NU1903</NoWarn>
          </PropertyGroup>

          <ItemGroup>
            <ProjectReference Include="..\{sharedProjectName}\{sharedProjectName}.csproj" />
            <PackageReference Include="Avalonia.Desktop" Version="11.2.5" />
          </ItemGroup>
        </Project>
        """;
        files.Add(new GeneratedSourceFile(Path.Combine(desktopDir, $"{desktopProjectName}.csproj"), desktopCsprojContent, SourceFileType.ProjectFile));

        var desktopProgramCs = $$"""
        // <auto-generated />
        using System;
        using Avalonia;
        using {{document.RootNamespace}};

        namespace {{document.RootNamespace}}.Desktop;

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
                <RootNamespace>{document.RootNamespace}.Android</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="..\{sharedProjectName}\{sharedProjectName}.csproj" />
                <PackageReference Include="Avalonia.Android" Version="11.2.5" />
              </ItemGroup>
            </Project>
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, $"{androidProjectName}.csproj"), androidCsprojContent, SourceFileType.ProjectFile));

            var mainActivityCs = $$"""
            // <auto-generated />
            using Android.App;
            using Android.Content.PM;
            using Avalonia;
            using Avalonia.Android;
            using {{document.RootNamespace}};

            namespace {{document.RootNamespace}}.Android;

            [Activity(
                Label = "{{document.Title}}",
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

            var splashActivityCs = $$"""
            // <auto-generated />
            using Android.App;
            using Android.Content;
            using Android.OS;
            using Avalonia;
            using Avalonia.Android;
            using {{document.RootNamespace}};

            namespace {{document.RootNamespace}}.Android;

            [Activity(
                Theme = "@style/MyTheme.Splash",
                MainLauncher = false,
                NoHistory = true)]
            public class SplashActivity : AvaloniaSplashActivity<App>
            {
                protected override void OnCreate(Bundle? savedInstanceState)
                {
                    base.OnCreate(savedInstanceState);
                }
            }
            """;
            files.Add(new GeneratedSourceFile(Path.Combine(androidDir, "SplashActivity.cs"), splashActivityCs, SourceFileType.ProjectFile));

            var androidManifestXml = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <manifest xmlns:android="http://schemas.android.com/apk/res/android"
                      package="com.afg.{{baseProjectName.ToLowerInvariant()}}"
                      android:versionCode="1"
                      android:versionName="1.0">
              <uses-sdk android:minSdkVersion="21" android:targetSdkVersion="34" />
              <application android:label="{{document.Title}}" android:theme="@style/MyTheme.NoActionBar" />
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
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var files = GenerateFullProject(document, options);
        foreach (var file in files)
        {
            var filePath = Path.Combine(destinationDirectory, file.FileName);
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            await File.WriteAllTextAsync(filePath, file.Content, Encoding.UTF8, cancellationToken);
        }
    }
}
