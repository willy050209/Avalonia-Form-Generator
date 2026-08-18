# C# Declarative Markup 生成語法規範 (C# Markup Spec)

本文檔說明 AFG 所產出的純 C# 宣告式 UI（C# Markup / Declarative C# UI）的鏈式語法規則、強型別 Lambda / Compiled Binding 語法、自訂型別 ViewModel、動態 DI 服務注入與多表單導航規範。

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
            .Background(Brush.Parse("#1E293B"))
            .Foreground(Brush.Parse("#FFFFFF"))
            .Command(nameof(LoginFormViewModel.SubmitCommand))
    );
```

### 1.2 強型別編譯綁定 (Compiled / Lambda Bindings) 模式
當 `FormDocument.UseCompiledBindings == true` 時，生成器會產出強型別 Lambda 表達式：
```csharp
Content = new Canvas()
    .Children(
        new TextBox()
            .Text((LoginFormViewModel vm) => vm.Username, BindingMode.TwoWay),
        new Button()
            .Command((LoginFormViewModel vm) => vm.SubmitCommand)
    );
```

### 1.3 擴充方法與資料綁定支援矩陣

| 屬性分類 | 數值賦值擴充方法 | 資料綁定重載 (String / Lambda) | 適用控制項類型 |
| :--- | :--- | :--- | :--- |
| **尺寸與幾何** | `.Width(double)` / `.Height(double)` | `.Width(string path, BindingMode)` | `Control` |
| **外觀與透明度** | `.Opacity(double)` | `.Opacity(string path, BindingMode)` | `Control` |
| **顏色與筆刷** | `.Background(IBrush)` / `.Foreground(IBrush)` | - | `TemplatedControl`, `Panel`, `Border`, `TextBlock` |
| **可見度與啟用** | `.IsEnabled(bool)` / `.IsVisible(bool)` | `.IsEnabled(path, mode)` / `.IsVisible(path, mode)` | `Control` |
| **文字與內容** | `.Content(object)` / `.Text(string)` | `.Content(path, mode)` / `.Text(path, mode)` | `ContentControl`, `TextBlock`, `TextBox` |
| **提示文字** | `.Watermark(string)` | - | `TextBox` |
| **開關與核選** | `.IsChecked(bool)` | `.IsChecked(string path, BindingMode)` | `ToggleButton`, `CheckBox`, `RadioButton` |
| **數值與進度** | `.Value(double)` | `.Value(string path, BindingMode)` | `RangeBase`, `Slider`, `ProgressBar` |
| **字體大小** | `.FontSize(double)` | `.FontSize(string path, BindingMode)` | `TemplatedControl`, `TextBlock` |
| **清單項目來源** | - | `.ItemsSource(string path, BindingMode)` | `ItemsControl`, `ListBox`, `ComboBox` |
| **選取項目** | - | `.SelectedItem(path)` / `.SelectedIndex(path)` | `SelectingItemsControl` |
| **事件命令** | - | `.Command(string path)` / `.Command(Func<TVm, object?> expr)` | `Button` |
| **容器座標** | `.CanvasLeft(double)` / `.CanvasTop(double)` | - | `Canvas` 子項目 |
| **Grid 網格座標** | `.GridRow(int)` / `.GridColumn(int)` | - | `Grid` 子項目 |
| **子項目集合** | `.Children(params Control[])` | - | `Panel` (Canvas, Grid, StackPanel 等) |

---

## 2. 相依性注入 (DI) 與 ViewModel 命令生成規範

AFG 的 ViewModel 生成器嚴格遵循 `CommunityToolkit.Mvvm` 與 `Microsoft.Extensions.DependencyInjection` 標準：

1. **類別宣告與相依性注入**：
   - 類別宣告為 `public partial class <Name>ViewModel : ObservableObject`。
   - 若未配置自訂服務，產出乾淨無參數建構子；若配置自訂服務，自動產出欄位與注入建構子：
     ```csharp
     public partial class OrderFormViewModel : ObservableObject
     {
         private readonly IOrderService? _orderService;

         public OrderFormViewModel(IOrderService orderService)
         {
             _orderService = orderService;
         }

         [ObservableProperty]
         private decimal _totalAmount;

         [ObservableProperty]
         private ObservableCollection<string> _itemsList = [];
     }
     ```
2. **命令生成 (同步 vs 非同步)**：
   - **非同步命令 (預設)**：方法簽章為 `async Task ...Async()`，CommunityToolkit.Mvvm 自動擴展為 `IAsyncRelayCommand` 屬性。
   - **同步命令**：方法簽章為 `void ...()`，自動擴展為 `IRelayCommand` 屬性。

---

## 3. App.cs 容器註冊與多表單導航

```csharp
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private static Window? s_mainWindow;

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var initialView = Services.GetRequiredService<HomeView>();
            s_mainWindow = new Window
            {
                Title = Config.AppTitle,
                Width = Config.DefaultWindowWidth,
                Height = Config.DefaultWindowHeight,
                WindowState = WindowState.Maximized,
                Content = initialView
            };
            desktop.MainWindow = s_mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void SetActiveView(Control view)
    {
        if (s_mainWindow is not null)
        {
            s_mainWindow.Content = view;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<HomeView>(sp => new HomeView { DataContext = sp.GetRequiredService<HomeViewModel>() });
        services.AddTransient<OrderView>(sp => new OrderView { DataContext = sp.GetRequiredService<OrderViewModel>() });
    }
}
```
