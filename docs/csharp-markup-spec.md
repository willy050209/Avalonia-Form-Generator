# C# Declarative Markup 生成語法規範 (C# Markup Spec)

本文檔說明 AFG 所產出的純 C# 宣告式 UI（C# Markup / Declarative C# UI）的鏈式語法規則、資料綁定表達式、相依性注入 (DI) 與同步/非同步 ViewModel 生成規範。

---

## 1. View 生成語法規則 (C# Markup Syntax)

AFG 將 UI AST 轉譯為純 C# 的宣告式結構，具備高型別安全性、IDE 完整導航 (F12) 與重構支援。

### 1.1 基本鏈式調用結構 (Fluent Chaining)
```csharp
Content = new Canvas()
    .Width(390)
    .Height(844)
    .Children(
        new TextBox()
            .Width(240)
            .Height(35)
            .CanvasLeft(75)
            .CanvasTop(80)
            .Watermark("請輸入使用者名稱")
            .Text(nameof(LoginFormViewModel.Username), BindingMode.TwoWay),
        new Button()
            .Width(120)
            .Height(35)
            .CanvasLeft(75)
            .CanvasTop(130)
            .Content("登入")
            .Command(nameof(LoginFormViewModel.SubmitCommand))
    );
```

### 1.2 擴充方法與資料綁定支援矩陣

| 屬性分類 | 數值賦值擴充方法 | 資料綁定重載 (String / Path) | 適用控制項類型 |
| :--- | :--- | :--- | :--- |
| **尺寸與幾何** | `.Width(double)` / `.Height(double)` | `.Width(string path, BindingMode)` | `Control` |
| **外觀與透明度** | `.Opacity(double)` | `.Opacity(string path, BindingMode)` | `Control` |
| **可見度與啟用** | `.IsEnabled(bool)` / `.IsVisible(bool)` | `.IsEnabled(path, mode)` / `.IsVisible(path, mode)` | `Control` |
| **文字與內容** | `.Content(object)` / `.Text(string)` | `.Content(path, mode)` / `.Text(path, mode)` | `ContentControl`, `TextBlock`, `TextBox` |
| **提示文字** | `.Watermark(string)` | - | `TextBox` |
| **開關與核選** | `.IsChecked(bool)` | `.IsChecked(string path, BindingMode)` | `ToggleButton`, `CheckBox`, `RadioButton` |
| **數值與進度** | `.Value(double)` | `.Value(string path, BindingMode)` | `RangeBase`, `Slider`, `ProgressBar` |
| **字體大小** | `.FontSize(double)` | `.FontSize(string path, BindingMode)` | `TemplatedControl`, `TextBlock` |
| **清單項目來源** | - | `.ItemsSource(string path, BindingMode)` | `ItemsControl`, `ListBox`, `ComboBox` |
| **選取項目** | - | `.SelectedItem(path)` / `.SelectedIndex(path)` | `SelectingItemsControl` |
| **事件命令** | - | `.Command(string path)` | `Button` |
| **容器座標** | `.CanvasLeft(double)` / `.CanvasTop(double)` | - | `Canvas` 子項目 |
| **Grid 網格座標** | `.GridRow(int)` / `.GridColumn(int)` | - | `Grid` 子項目 |
| **子項目集合** | `.Children(params Control[])` | - | `Panel` (Canvas, Grid, StackPanel 等) |

---

## 2. 相依性注入 (DI) 與 ViewModel 命令生成規範

AFG 的 ViewModel 生成器嚴格遵循 `CommunityToolkit.Mvvm` 與 `Microsoft.Extensions.DependencyInjection` 標準：

1. **類別宣告與相依性注入**：
   - 類別宣告為 `public partial class <Name>ViewModel : ObservableObject`。
   - 提供無參數建構子與支援服務注入的建構子：
     ```csharp
     public partial class MainFormViewModel : ObservableObject
     {
         private readonly IGreetingService? _greetingService;

         public MainFormViewModel()
         {
         }

         public MainFormViewModel(IGreetingService greetingService)
         {
             _greetingService = greetingService;
         }
         ...
     }
     ```
2. **屬性生成**：
   - 根據綁定目標自動推斷型別（例如 `IsChecked` / `IsEnabled` -> `bool`, `Value` -> `double`, `Text` -> `string`）。
   - 欄位加上 `[ObservableProperty]`，採用下劃線駝峰命名（如 `_username`），原始碼生成器自動生成公開屬性 `Username`。
3. **命令生成 (同步 vs 非同步)**：
   - **非同步命令 (預設)**：方法簽章為 `async Task ...Async()`，CommunityToolkit.Mvvm 自動擴展為 `IAsyncRelayCommand` 屬性。
     ```csharp
     [RelayCommand]
     private async Task SubmitAsync()
     {
         // TODO: 實作非同步命令業務邏輯
         await Task.CompletedTask;
     }
     ```
   - **同步命令**：方法簽章為 `void ...()`，自動擴展為 `IRelayCommand` 屬性。
     ```csharp
     [RelayCommand]
     private void Reset()
     {
         // TODO: 實作同步命令業務邏輯
     }
     ```

---

## 3. App.cs 容器註冊與生命週期

```csharp
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainView = Services.GetRequiredService<MainFormView>();
            desktop.MainWindow = new Window
            {
                Title = Config.AppTitle,
                Width = Config.DefaultWindowWidth,
                Height = Config.DefaultWindowHeight,
                WindowState = WindowState.Maximized,
                Content = mainView
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = Services.GetRequiredService<MainFormView>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IGreetingService, GreetingService>();
        services.AddTransient<MainFormViewModel>();
        services.AddTransient<MainFormView>(sp =>
        {
            var view = new MainFormView();
            view.DataContext = sp.GetRequiredService<MainFormViewModel>();
            return view;
        });
    }
}
```
