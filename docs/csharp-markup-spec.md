# C# Declarative Markup 生成語法規範 (C# Markup Spec)

本文檔說明 AFG 所產出的純 C# 宣告式 UI（C# Markup / Declarative C# UI）的鏈式語法規則、強型別 Lambda / Compiled Binding 語法、自訂型別 ViewModel、動態 DI 服務注入與多表單導航規範。

---

## 1. View 生成語法規則 (C# Markup Syntax)

AFG 將 UI AST 轉譯為純 C# 的宣告式結構，具備高型別安全性、IDE 完整導航 (F12) 與重構支援。

### 1.1 基本鏈式調用結構與物件名稱註解 (Fluent Chaining & Name Comments)
在生成的 View 中，每個控制項與容器的建構子上方均會自動標記該物件名稱註解：
```csharp
// RootCanvas
Content = new Canvas()
    .Width(390)
    .Height(844)
    .Children(
        // UsernameTextBox
        new TextBox()
            .Width(240)
            .Height(35)
            .CanvasLeft(75)
            .CanvasTop(80)
            .Watermark("請輸入使用者名稱")
            .Text(nameof(LoginFormViewModel.Username), BindingMode.TwoWay),
        // LoginButton
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

### 1.2 強型別編譯綁定 (Compiled / Lambda Bindings) 模式 (預設模式)
AFG 預設採用強型別 Lambda 編譯綁定 (`FormDocument.UseCompiledBindings == true`)，生成具備 IDE 型別檢查與編譯期驗證之語法：
```csharp
// RootCanvas
Content = new Canvas()
    .Children(
        // UsernameTextBox
        new TextBox()
            .Text((LoginFormViewModel vm) => vm.Username, BindingMode.TwoWay),
        // LoginButton
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
| **影像來源與縮放** | `.Source(IImage)` / `.Stretch(Stretch)` | `.Source(path, mode)` / `.Stretch(path, mode)` | `Image`, `PictureBox` |
| **多媒體播放控制** | `.Source(string)`, `.AutoPlay(bool)`, `.IsLooping(bool)`, `.Volume(double)`, `.Position(TimeSpan)`, `.Stretch(Stretch)` | `.Source(...)`, `.Volume(...)`, `.Position(...)`, `.CurrentFrame(...)` | `MediaPlayerControl` (`MediaPlayer`) |
| **事件命令綁定** | - | `.Command(string path)` / `.Command(Func<TVm, object?> expr)` | `Button`, `PictureBox`, `Image` |
| **原生事件轉發** | `.OnClick(...)`, `.OnTextChanged(...)`, `.OnSelectionChanged(...)`, `.OnTapped(...)`, `.OnKeyDown(...)`, `.OnMediaOpened(...)`, `.OnMediaEnded(...)`, `.OnFrameCaptured(...)` | 支援無參數、單參數與 `(sender, e)` 雙參數自動轉發至 ViewModel 命令 | `Button`, `TextBox`, `ComboBox`, `MediaPlayerControl`, `Control` |
| **表單生命週期事件** | `Loaded`, `Unloaded`, `Initialized`, `SizeChanged`, `PointerPressed`, `KeyDown` 等 | 在 `InitializeComponent()` 內掛載委派並轉發至 ViewModel 關聯之 RelayCommand | `UserControl` (Form Document View) |
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
2. **命令生成 (單參數 vs 多參數 ValueTuple 與 CanExecute 安全性)**：
   - **非同步命令 (預設)**：方法簽章為 `async Task ...Async()`，CommunityToolkit.Mvvm 自動擴展為 `IAsyncRelayCommand` 屬性。
   - **多參數元組封裝**：當事件包含多個參數（如 `sender` 與 `e`）時，生成器將其封裝為**可為空的 `ValueTuple`** 並提供預設值：
     ```csharp
     [RelayCommand]
     private async Task Button1_ClickAsync((object? sender, RoutedEventArgs? e)? args = null)
     {
         // 透過 args?.sender 與 args?.e 安全存取原生事件來源與參數
         await Task.CompletedTask;
     }
     ```
   - **CanExecute 防禦機制**：參數宣告為可空元組 `(T1, T2)? args = null`，防止 `ValueTuple` 因實值型別拒絕 `null` 而導致 `CanExecute(null)` 回傳 `false` 誤將按鈕禁用的底層問題。

3. **不可視元件與硬體通訊專屬回呼 (Callbacks)**：
    - 自動在 ViewModel 建構子內進行事件掛載，傳遞對應之 `(s, e)` 並調用對應之 RelayCommand：
      ```csharp
      public partial class HardwareFormViewModel : ObservableObject
      {
          private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
          private readonly BackgroundWorker _taskWorker = new();
          private readonly BluetoothClient _bleScanner = new();
          private readonly SerialPortService _serialDevice = new();

          public HardwareFormViewModel()
          {
              _pollTimer.Tick += (s, e) => OnTimerTickCommand.Execute((s, e));
              _taskWorker.DoWork += (s, e) => PerformWorkCommand.Execute((s, e));
              _bleScanner.DataReceived += (s, e) => OnBleDataCommand.Execute((s, e));
              _serialDevice.DataReceived += (s, e) => OnSerialDataCommand.Execute((s, e));
          }

          [RelayCommand]
          private async Task OnTimerTickAsync((object? sender, EventArgs? e)? args = null) { ... }

          [RelayCommand]
          private async Task OnBleDataAsync((object? sender, string? data)? args = null) { ... }
      }
      ```

4. **自動尺寸適應 (AutoSize)**：
   - 當控制項勾選 `AutoSize` 時，生成器略過固定 `.Width(...)` 與 `.Height(...)` 鏈式語法，交由 Avalonia 依據文字與內容自然撐開。

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

---

## 4. PictureBox 點陣圖初始化與 BitmapHelper 擴充規範

### 4.1 PictureBox 初始化程式碼生成
當 PictureBox 勾選「初始化空白點陣圖 (`InitBitmap = true`)」時，View 生成器將產生呼叫 `BitmapHelper.CreateInitializedBitmap` 的鏈式語法：
```csharp
// DrawingCanvas
new Image()
    .Width(400)
    .Height(300)
    .Source(BitmapHelper.CreateInitializedBitmap(400, 300, Brush.Parse("#F0F0F0")))
    .Stretch(Stretch.Uniform)
```

當指定本機圖片並勾選「使用專案相對路徑 (`UseRelativePath = true`)」時，匯出專案時會自動複製圖片至 `Assets/` 資料夾，並生成標準 `avares://` 資源路徑：
```csharp
// LogoPicture
new Image()
    .Width(120)
    .Height(60)
    .Source(BitmapHelper.LoadBitmap("avares://MyApp.Shared/Assets/logo.png"))
    .Stretch(Stretch.Uniform)
```

### 4.2 BitmapHelper 靜態操作類別與擴充方法
匯出的共用核心類別庫內建 `BitmapHelper` 與 `BitmapExtensions`，提供跨平台的高效能像素操作與點陣圖轉換：
```csharp
// 1. 建立已初始化指定尺寸與背景顏色的 WriteableBitmap
var wb = BitmapHelper.CreateInitializedBitmap(300, 200, Color.Parse("#FAFAFA"));

// 2. 格式轉換
var writeable = bitmap.ConvertToWriteableBitmap();
var renderTarget = bitmap.ConvertToRenderTargetBitmap();

// 3. 像素快速讀寫 (具備邊界檢查與安全指針存取)
wb.SetPixel(10, 20, Colors.Red);
Color pixel = wb.GetPixel(10, 20);

// 4. 動態載入點陣圖 (支援本機路徑、avares:// 資源路徑與多層次容錯解析)
var loadedBitmap = BitmapHelper.LoadBitmap("avares://MyApp.Shared/Assets/logo.png");
```

### 4.3 `BitmapHelper.LoadBitmap` 多層次動態容錯解析機制
為了確保在跨平台多專案（`.Desktop`, `.Shared`, `.Browser`, `.Android`, `.iOS`）環境下資源載入的 100% 可靠性，`LoadBitmap` 實作了強韌的多層次解析策略：
1. **本機實體檔案直讀**：若傳入為實體檔案路徑且檔案存在（如設計階段的暫存檔），直接使用 `new Bitmap(path)`。
2. **Avalonia Resource 規範解析 (`avares://`)**：
   - 優先以傳入之 URI 呼叫 `AssetLoader.Exists(uri)`。
   - 若組件名稱不匹配（例如 URI 為 `avares://MyApp/Assets/logo.png`，但實際執行組件為 `MyApp.Shared`），自動提取相對路徑並依序嘗試 `typeof(BitmapHelper).Assembly`、`Assembly.GetEntryAssembly()` 與目前 AppDomain 所有已載入組件進行重定向載入。
3. **純相對路徑字串支援**：傳入 `"Assets/logo.png"` 或 `"logo.png"` 時，自動組合為 `avares://{AssemblyName}/Assets/{fileName}` 進行資源載入。
4. **輸出目錄磁碟後備 (Disk Fallback)**：當嵌入式資源在極端情況下未載入時，在 `AppContext.BaseDirectory` 與執行目錄的 `Assets/` 資料夾中搜尋同名實體檔案作為後備方案。

---

## 5. 表單與視窗控制屬性系統 (Form & Window Control Properties)

### 5.1 View (UserControl) 根背景與尺寸初始化
在生成的 View (`UserControl`) 中，若有自訂 `BackgroundColor`，將在 `InitializeComponent` 內自動套用筆刷：
```csharp
public partial class MainFormView : UserControl
{
    public MainFormView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Background = Brush.Parse("#1E293B");

        // RootCanvas
        Content = new Canvas()
            .Children( ... );
    }
}
```

### 5.2 應用程式主視窗 (App.cs & MainWindow) 視窗屬性配置
在專案匯出服務中，`App.cs` 與 `Config.cs` 將依據 `FormDocument` 所設定的視窗參數自動生成完整的視窗初始化程式碼：
```csharp
// App.cs 桌面端啟動視窗配置
s_mainWindow = new Window
{
    Title = Config.AppTitle,
    Width = Config.DefaultWindowWidth,
    Height = Config.DefaultWindowHeight,
    MinWidth = 800,
    MinHeight = 600,
    MaxWidth = 1920,
    MaxHeight = 1080,
    Background = Brush.Parse("#1E293B"),
    WindowStartupLocation = WindowStartupLocation.CenterScreen,
    WindowState = WindowState.Normal,
    CanResize = Config.CanResize,
    Topmost = Config.Topmost,
    ShowInTaskbar = Config.ShowInTaskbar,
    SystemDecorations = SystemDecorations.Full,
    Icon = new WindowIcon(BitmapHelper.LoadBitmap("Assets/app_icon.ico")!),
    Content = initialView
};
```

### 5.3 Config.cs 全域組態常數
```csharp
public static class Config
{
    public const string AppTitle = "POS 終端收銀系統";
    public const string Version = "1.0.0";
    public const double DefaultWindowWidth = 1280;
    public const double DefaultWindowHeight = 800;
    public const bool CanResize = false;
    public const bool Topmost = true;
    public const bool ShowInTaskbar = true;
    public const bool IsMobileSupported = false;
}
```

---

## 6. 全域命名空間管理 (Global Usings & Clean Architecture)

為了確保生成的程式碼簡潔無冗餘，AFG 在專案匯出時於每個子專案根目錄產出 `GlobalUsings.cs`，統一管理共用命名空間與類型別名，各別實體類別（如 `App.cs`、`Config.cs`、服務層及對話方塊等）皆無需重複宣告 `using`：

```csharp
// GlobalUsings.cs (Shared 核心專案)
global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
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
global using {RootNamespace};
global using {RootNamespace}.Services;
global using OpenFileDialog = {RootNamespace}.Services.OpenFileDialog;
global using SaveFileDialog = {RootNamespace}.Services.SaveFileDialog;
global using MessageBox = {RootNamespace}.Services.MessageBox;
global using LogEntry = {RootNamespace}.Services.LogEntry;
global using InMemoryLogService = {RootNamespace}.Services.InMemoryLogService;
```

---

## 7. 高效能直接記憶體 Bitmap 擴充方法與 SIMD 影像處理 (High-Performance Bitmap Extensions & Image Filters)

AFG 提供極致效能的點陣圖處理套裝（支援於 AFG 內部設計畫布與匯出之應用程式），直接對底層記憶體指標 (`ILockedFramebuffer.Address`) 進行無 GC、零拷貝像素存取與 SIMD 向量化加速。

### 7.1 執行模式 (`PixelProcessingMode`)
像素遍歷與影像處理均支援四種硬體最佳化執行模式：
- `Sequential`：單執行緒循序遍歷，記憶體連續快取命中率高。
- `SequentialVectorized`：單執行緒 + SIMD 向量化展開 (Unrolled Loop)，提升 CPU 管線執行吞吐量。
- `Parallel`：多核心平行排程處理 (`Parallel.For`)，適合高解析度畫面。
- `ParallelVectorized`：多核心平行 + SIMD 向量化展開，釋放多核心與 SIMD 暫存器之極限算力。

### 7.2 SIMD 硬體能力自動偵測 (`SimdHardware`)
自動偵測宿主硬體指令集架構，動態挑選最適合的運算步長：
```csharp
if (SimdHardware.HasVector512) { /* AVX-512 64-byte batch */ }
else if (SimdHardware.HasVector256) { /* AVX2 32-byte batch */ }
else if (SimdHardware.HasVector128) { /* SSE2 / ARM Neon 16-byte batch */ }
```

### 7.3 像素遍歷擴充方法 (`ProcessPixels`)

#### 1. 泛型硬體 SIMD 向量暫存器運算 (`VectorTransform` / `ProcessPixelsSimdHardware`)
直接以非託管指標讀寫 `*(Vector<byte>*)(row + x)` 暫存器進行批次運算，由 JIT 自動依 CPU 硬體指令集對齊暫存器寬度（AVX2 32B / SSE2 16B / ARM NEON 16B）：
```csharp
// 1. 向量反相變換 (Invert Channels via SIMD)
var all255 = new Vector<byte>(255);
writeableBitmap.ProcessPixels(
    vectorTransform: vec => all255 - vec,
    remainderProcessor: (ref byte b, ref byte g, ref byte r, ref byte a) =>
    {
        b = (byte)(255 - b); g = (byte)(255 - g); r = (byte)(255 - r); a = (byte)(255 - a);
    },
    mode: PixelProcessingMode.ParallelVectorized);

// 2. 便捷硬體 SIMD 呼叫
writeableBitmap.ProcessPixelsSimdHardware(vec => vec + new Vector<byte>(10));
```

#### 2. 直接記憶體指標向量處理 (`VectorPointerProcessor`)
直接傳遞底層非託管記憶體指標 `(byte* vectorPtr, int byteCount)`，達到零 GC 與無物件封裝開銷之極限效能：
```csharp
unsafe
{
    writeableBitmap.ProcessPixels(
        pointerProcessor: (byte* ptr, int count) =>
        {
            var vec = *(Vector<byte>*)ptr;
            *(Vector<byte>*)ptr = vec ^ new Vector<byte>(0xFF);
        },
        mode: PixelProcessingMode.ParallelVectorized);
}
```

#### 3. 連續記憶體區塊向量批次處理 (`VectorPixelProcessor`)
自動透過 `SimdHardware.PreferredVectorByteCount` 偵測最佳步長（64B / 32B / 16B），傳遞 `Span<byte>` 進行極速區塊讀寫：
```csharp
writeableBitmap.ProcessPixels(
    vectorProcessor: span =>
    {
        // span 長度精確對齊硬體向量位元組寬度 (e.g. 32 bytes on AVX2)
        span.Fill(255);
    },
    mode: PixelProcessingMode.ParallelVectorized);
```

#### 4. 純像素與帶座標直接記憶體回呼 (`PixelProcessor` / `PixelLocationProcessor`)
自動依硬體向量寬度展開迴圈（Unrolled Loops）：
```csharp
// 1. 顏色色板變換 (Invert RGB)
writeableBitmap.ProcessPixels((ref byte b, ref byte g, ref byte r, ref byte a) =>
{
    b = (byte)(255 - b);
    g = (byte)(255 - g);
    r = (byte)(255 - r);
}, PixelProcessingMode.ParallelVectorized);

// 2. 帶座標變換
writeableBitmap.ProcessPixels((int x, int y, ref byte b, ref byte g, ref byte r, ref byte a) =>
{
    r = (byte)(x * 255 / width);
    b = (byte)(y * 255 / height);
}, PixelProcessingMode.Parallel);
```

### 7.4 常用影像處理函數 (Image Processing Filters)
1. **顏色反相 (`ApplyInvert`)**：
   - 直接採用硬體 SIMD 指令 `*(Vector<byte>*)(row + x) = mask - vec` 批次平行反相：
     ```csharp
     writeableBitmap.ApplyInvert(PixelProcessingMode.ParallelVectorized);
     ```
2. **灰階化 (`ToGrayscale` / `ApplyGrayscale`)**：
   - 採用 ITU-R BT.601 亮度加權演算法：`Gray = (299*R + 587*G + 114*B + 500) / 1000`。
   - 原地修改與新實例建立雙重 API，內部直接以 `Vector128<byte>` / `Vector256<byte>` SIMD 向量指令批次運算：
     ```csharp
     var grayBmp = bitmap.ToGrayscale(PixelProcessingMode.ParallelVectorized);
     writeableBitmap.ApplyGrayscale(PixelProcessingMode.ParallelVectorized);
     ```
3. **邊緣偵測 (`DetectEdges` / `ApplySobel`)**：
   - 採用 3x3 Sobel 梯度卷積核 ($Gx$, $Gy$)，計算梯度幅值 $\sqrt{Gx^2 + Gy^2}$，支援設定過濾門檻值 (`threshold`)。
     ```csharp
     var edgeBmp = bitmap.DetectEdges(threshold: 30.0, PixelProcessingMode.Parallel);
     writeableBitmap.ApplySobel(threshold: 50.0);
     ```
4. **模糊處理 (`ApplyBlur` / `ApplyGaussianBlur` / `ApplyBoxBlur`)**：
   - 採用 2-Pass 1D 可分離卷積（Separable Convolution）結合 `NativeMemory` 零 GC 中間緩衝區，將時間複雜度降至 $O(r \cdot W \cdot H)$：
     ```csharp
     var blurred = bitmap.ApplyBlur(radius: 5, PixelProcessingMode.Parallel);
     writeableBitmap.ApplyGaussianBlur(radius: 3);
     writeableBitmap.ApplyBoxBlur(radius: 2);
     ```





