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

---

## 8. MediaPlayerControl 多媒體播放器元件規範

### 8.1 元件架構與核心特性
`MediaPlayerControl` 為跨平台（桌面端、行動端與 Web）設計之現代化多媒體播放控制項，支援本地端媒體檔案、內嵌資源（`avares://`）與雲端串流網址（HTTP/HTTPS）之讀取、播放控制與當前影格截圖。

- **命名空間**：`AFG.Shared.Controls.MediaPlayerControl`（共用核心專案與生成專案之 `Markup/AvaloniaMarkupExtensions.cs`）。
- **狀態列舉 (`MediaState`)**：`Stopped`, `Playing`, `Paused`, `Buffering`, `Error`。

### 8.2 屬性與方法 API
| 屬性 / 方法 | 型別 / 簽章 | 說明 |
| :--- | :--- | :--- |
| `Source` | `string?` | 媒體來源（支援本地路徑、avares:// 與 http/https 雲端 URL） |
| `AutoPlay` | `bool` | 是否在載入完成後自動播放（預設 false） |
| `IsLooping` | `bool` | 是否循環播放（預設 false） |
| `Volume` | `double` | 播放音量（0.0 ~ 1.0，預設 1.0） |
| `Position` | `TimeSpan` | 當前播放進度時間戳 |
| `Duration` | `TimeSpan` | 媒體總長度（預設 10 秒） |
| `State` | `MediaState` | 當前播放狀態 |
| `CurrentFrame` | `IImage?` | 當前播放影格影像 |
| `Stretch` | `Stretch` | 畫面填滿拉伸模式（預設 Uniform） |
| `SpeedRatio` | `double` | 播放速率倍數（預設 1.0） |
| `Play()` | `void` | 開始/繼續播放 |
| `Pause()` | `void` | 暫停播放 |
| `Stop()` | `void` | 停止播放並重置進度為 0 |
| `Seek(TimeSpan/double)` | `void` | 跳轉至指定時間戳或秒數 |
| `LoadAsync(string?)` | `Task` | 非同步載入媒體資源 |
| `CaptureFrame()` / `CaptureFrameAsync()` | `Bitmap?` / `Task<Bitmap?>` | 將當前影格轉為點陣圖並觸發 `FrameCaptured` 事件 |

### 8.3 C# Declarative Markup Fluent 鏈式調用範例
```csharp
// 建立自訂多媒體播放器
new MediaPlayerControl()
    .Width(640)
    .Height(360)
    .Source("https://example.com/video.mp4")
    .AutoPlay(true)
    .IsLooping(true)
    .Volume(0.8)
    .Stretch(Stretch.Uniform)
    .OnMediaOpened((MyViewModel vm) => vm.VideoOpenedCommand)
    .OnMediaEnded((MyViewModel vm) => vm.VideoEndedCommand)
    .OnFrameCaptured((MyViewModel vm) => vm.FrameCapturedCommand);
```

---

## 9. 控制項專屬資料綁定白名單目錄與強型別約束規範 (Control Binding Catalog & Type Constraints)

### 9.1 控制項資料綁定屬性白名單 (`ControlBindingCatalog`)
為避免各元件出現不存在之屬性綁定（例如在 `MediaPlayerControl` 綁定 `Text`，或在 `TextBox` 綁定 `AutoPlay`），AFG 透過 `ControlBindingCatalog` 建立嚴格之屬性白名單：

| 控制項類型 | 專屬支援可綁定屬性清單 | 屏蔽之屬性範例 |
| :--- | :--- | :--- |
| `MediaPlayerControl` | `Source`, `AutoPlay`, `IsLooping`, `Volume`, `Position`, `Duration`, `State`, `CurrentFrame`, `Stretch`, `SpeedRatio`, `IsEnabled`, `IsVisible`, `Width`, `Height`, `Opacity` | `Text`, `Content`, `Watermark`, `Header`, `IsChecked`, `ItemsSource` |
| `TextBox` | `Text`, `Watermark`, `FontSize`, `Background`, `Foreground`, `IsEnabled`, `IsVisible`, `Width`, `Height`, `Opacity` | `AutoPlay`, `IsLooping`, `CurrentFrame`, `IsChecked`, `Value` |
| `Button` | `Text`, `Content`, `Background`, `Foreground`, `IsEnabled`, `IsVisible`, `Width`, `Height`, `Opacity` | `Source`, `CurrentFrame`, `Interval`, `Watermark` |
| `CheckBox` / `RadioButton` | `IsChecked`, `Text`, `Content`, `Foreground`, `Background`, `IsEnabled`, `IsVisible`, `Width`, `Height`, `Opacity` | `Value`, `CurrentFrame`, `AutoPlay`, `ItemsSource` |
| `Slider` / `ProgressBar` | `Value`, `IsEnabled`, `IsVisible`, `Width`, `Height`, `Opacity` | `Text`, `Content`, `IsChecked`, `Source` |
| `Image` / `PictureBox` | `Source`, `Stretch`, `IsEnabled`, `IsVisible`, `Width`, `Height`, `Opacity` | `Text`, `Content`, `Watermark`, `IsChecked` |
| `DispatcherTimer` | `Interval`, `IsEnabled` | 幾何與排版屬性（非視覺化元件） |

### 9.2 資料綁定強型別約束規則 (Type Compatibility Rules)
AFG 在 AST 驗證器 (`AstValidator`) 與屬性檢查器 (`InspectorViewModel` / `BindingItemViewModel`) 內建強型別約束，嚴格防止不相容型態綁定：
1. **影像型別約束**：`CurrentFrame` 僅相容 `Bitmap?`, `Bitmap`, `IImage?`, `IImage`, `WriteableBitmap?`，**嚴禁**綁定 `bool`, `int`, `string` 或自訂不相容物件（違反時觸發 `AFG205` 驗證錯誤）。
2. **布林型別約束**：`IsChecked`, `IsEnabled`, `IsVisible`, `AutoPlay`, `IsLooping` 等僅相容 `bool` 與 `bool?`。
3. **數值型別約束**：`Value`, `Volume`, `Opacity`, `SpeedRatio`, `FontSize` 僅相容 `double`, `float`, `int`, `decimal`。
4. **時間型別約束**：`Position`, `Duration`, `SelectedTime` 僅相容 `TimeSpan` 與 `TimeSpan?`。
5. **列舉狀態約束**：`State` 僅相容 `MediaState` 與 `string`。

---

## 10. 三大代碼架構模式規範 (Three Architecture Modes Specification)

為了兼顧現代化 MVVM 資料驅動架構、WinForms / WPF 快速原型驗證與跨平台專案需求，AFG 提供三大代碼生成架構模式：

```
+----------------------------------------------------------------------------------------------------+
|                                      AFG 代碼生成架構模式                                           |
+----------------------------------------------------------------------------------------------------+
| 1. Code-Behind / Event-Driven 模式 | 極致輕量、零 MVVM 心智負擔，事件直連 View 處理器 (WinForms/WPF 原型) |
| 2. Pure MVVM 模式                   | 標準企業架構，Inline View + ViewModel，具備完整單元測試性 (Unit Test)  |
| 3. 混合模式 (Hybrid - 預設模式)     | 兼顧 MVVM 雙向綁定與 Code-Behind 強型別欄位直接存取，滿足大多數場景  |
+----------------------------------------------------------------------------------------------------+
```

### 10.1 模式一：Code-Behind / Event-Driven 模式 (`ArchitectureMode.CodeBehind`)
- **定位**：快速驗證、Demo 原型測試或 WinForms 習慣開發者。
- **產出物**：僅產出 `MainView.generated.cs`（宣告可視強型別欄位如 `_btnSubmit`、`_txtInput`，以及不可視元件、對話方塊與硬體通訊欄位）與使用者事件處理常式 Method Stubs。**不生成 `ViewModel.cs`**。
- **不可視元件與硬體通訊**：
  - 在 View 內自動產出 `DispatcherTimer`, `BackgroundWorker`, `BluetoothClient`, `SerialPortService`, `OpenFileDialog`, `SaveFileDialog`, `MessageBox` 之私有欄位與初始化。
  - 在 `InitializeComponent()` 內自動掛載事件監聽（如 `_pollTimer.Tick += PollTimer_Tick;`, `_bleClient.DataReceived += BleClient_DataReceived;`）。
  - 若配置了自訂注入服務，自動產出 View 的多載相依性注入建構子（`public MainView(ISensorService sensor) : this()`）。
- **事件處理**：直接在 `InitializeComponent()` 內透過 `.OnClick(BtnSubmit_Click)` 或 `Loaded += MainView_Loaded;` 綁定 View 內部事件方法。
- **DI / 啟動設定**：`App.cs` 與 DI 容器直接註冊 `services.AddTransient<MainView>()`，無需 `DataContext` 依賴。

```csharp
// <auto-generated />
#nullable enable

namespace MyApp.Views;

public partial class MainView : UserControl
{
    // 提供編譯期強型別欄位，方便 Code-Behind 使用者 (Code-Behind Friendly Fields)
    private TextBox _txtUsername;
    private Button _btnSubmit;

    // 不可視元件、對話方塊與硬體通訊 (Non-Visual Components, Dialogs & Hardware Services)
    private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly BackgroundWorker _backgroundWorker = new();
    private readonly BluetoothClient _bleClient = new();
    private readonly SerialPortService _serialPort = new();
    private readonly OpenFileDialog _openFileDialog = new();
    private readonly MessageBox _messageBox = new();

    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Background = Brush.Parse("#FFFFFF");
        // RootCanvas
        Content = new Canvas()
            .Children(
                // txtUsername
                _txtUsername = new TextBox()
                    .Name("txtUsername")
                    .Width(200)
                    .Text("預設文字"),
                // btnSubmit
                _btnSubmit = new Button()
                    .Name("btnSubmit")
                    .Text("送出")
                    .OnClick(BtnSubmit_Click)
            );

        // 不可視元件與通訊事件監聽 (Non-Visual & Hardware Events)
        _pollTimer.Tick += PollTimer_Tick;
        _backgroundWorker.DoWork += BackgroundWorker_DoWork;
        _bleClient.DataReceived += BleClient_DataReceived;
        _serialPort.DataReceived += SerialPort_DataReceived;
        _openFileDialog.FileOk += OpenFileDialog_FileOk;
        _messageBox.Confirmed += MessageBox_Confirmed;
    }

    #region 事件處理常式 (Event Handlers)
    private void BtnSubmit_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: 在此實作 BtnSubmit 的 Click 事件邏輯
    }

    private void PollTimer_Tick(object? sender, EventArgs e)
    {
        // TODO: 在此實作 PollTimer 的 Tick 事件邏輯
    }

    private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        // TODO: 在此實作 BackgroundWorker 的 DoWork 事件邏輯
    }

    private void BleClient_DataReceived(object? sender, byte[] e)
    {
        // TODO: 在此實作 BleClient 的 DataReceived 事件邏輯
    }
    #endregion
}
```

---

### 10.2 模式二：Pure MVVM 模式 (`ArchitectureMode.PureMvvm`)
- **定位**：解耦良好、具備可測試性 (Unit Testable)，適合正式專案與團隊協作。
- **產出物**：`MainView.generated.cs` + `MainViewModel.generated.cs`。
- **事件與資料**：將事件轉化為 `[RelayCommand]`，具名控制項的屬性轉化為 `[ObservableProperty]`。
- **View 特性**：維持純 Inline 宣告（**不宣告私有強型別欄位**），保持 View 代碼極致純粹。

```csharp
// MainView.cs (Pure MVVM)
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Content = new StackPanel()
            .Children(
                new TextBox()
                    .Text((MainViewModel vm) => vm.Username, BindingMode.TwoWay),
                new Button()
                    .Text("送出")
                    .OnClick((MainViewModel vm) => vm.SubmitCommand)
            );
    }
}
```

---

### 10.3 模式三：混合模式 (`ArchitectureMode.Hybrid` - 預設模式)
- **定位**：介於 Pure Code-Behind 與 Pure MVVM 之間，兼顧 MVVM 數據綁定與 Code-Behind 直接操控控制項的便利性，適合大多數使用者。
- **產出物**：`MainView.generated.cs` + `MainViewModel.generated.cs`。
- **事件與資料**：將事件轉化為 `[RelayCommand]`，屬性轉化為 `[ObservableProperty]`，同時在 View 中宣告強型別私有欄位並透過 `.Name(...)` 自動註冊進 Avalonia `NameScope`。

```csharp
// MainView.cs (Hybrid 模式)
public partial class MainView : UserControl
{
    // 提供編譯期強型別欄位，方便 Code-Behind 使用者 (Code-Behind Friendly Fields)
    private TextBox _txtUsername;
    private Button _btnSubmit;

    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Content = new StackPanel()
            .Children(
                _txtUsername = new TextBox()
                    .Name("txtUsername")
                    .Text((MainViewModel vm) => vm.Username, BindingMode.TwoWay),

                _btnSubmit = new Button()
                    .Name("btnSubmit")
                    .Text("送出")
                    .OnClick((MainViewModel vm) => vm.SubmitCommand)
            );
    }
}
```

---

### 10.4 欄位命名清理與衝突保護 (Conflict Resolution)
1. **智慧命名過濾**：
   - 僅對具名或有互動之控制項生成欄位；
   - 匿名/無自訂 ID 的純佈局容器（如無自訂名稱的 `Grid`, `StackPanel`, `Canvas`）與裝飾用 `TextBlock` 維持 Inline 宣告。
2. **命名清理**：
   - 透過 `CSharpSyntaxSanitizer.SanitizeIdentifier` 清除非法識別碼字元，並轉為符合 C# 欄位慣例之 `_camelCase`。
3. **命名衝突保護**：
   - 若 AST 中存在多個相同名稱之節點，生成器會自動以 `node.Id` 作為後綴（如 `_txtUsername_b2c3`），並輸出防護提示註解：
     ```csharp
     // 提示: 控制項名稱 'txtUsername' 發生重複，已自動附加 ID 後綴 'b2c3' 以消除衝突保護編譯安全
     private TextBox _txtUsername_b2c3;
     ```

---

## 11. 對話方塊同步呼叫與非同步事件常式支援 (Dialog Synchronous Show & Async Events)

### 11.1 對話方塊同步 `Show()` 與靜態方法
為了滿足 WinForms 轉移者與純 Code-Behind 開發者習慣，對話方塊元件（`OpenFileDialog`, `SaveFileDialog`, `MessageBox`）與 `IDialogService` 同時提供**同步 `Show()`** 與 **非同步 `ShowAsync()`**：

```csharp
// 1. 訊息框實例與靜態同步呼叫 (Classic WinForms Style)
MessageBox.Show("儲存成功！", "系統提示");
_messageBox.Show();

// 2. 開啟檔案對話方塊同步選取
var selectedFile = OpenFileDialog.Show("請選擇文字檔", "*.txt");
var path = _openFileDialog.Show();

// 3. 儲存檔案對話方塊同步呼叫
var savePath = SaveFileDialog.Show("Report.csv", "匯出報表", "*.csv");
var target = _saveFileDialog.Show("Data.json");
```

> [!NOTE]
> 內部透過 Avalonia `DispatcherFrame` 嵌套訊息泵（Nested Message Loop），在 UI 執行緒執行同步等待時避免死結（Deadlock），保持 UI 渲染與視窗回應性。

### 11.2 Code-Behind 模式下的非同步事件常式 (`async void`)
在 Code-Behind 模式下，當事件標記為非同步 (`IsAsync = true`) 時，生成器自動產出符合 C# 事件標準的 `async void` 事件處理常式 Method Stubs：

```csharp
#region 事件處理常式 (Event Handlers)
private async void BtnFetchData_Click(object? sender, RoutedEventArgs e)
{
    // TODO: 在此實作 BtnFetchData 的非同步 Click 事件邏輯
    await Task.CompletedTask;
}

private async void BgWorker_DoWork(object? sender, DoWorkEventArgs e)
{
    // TODO: 在此實作 BgWorker 的非同步 DoWork 事件邏輯
    await Task.CompletedTask;
}
#endregion
```

---

## 12. 控制項邊框 (Border) 與陰影效果 (BoxShadow / DropShadow) 系統

### 12.1 邊框與圓角規格
AFG 支援控制項與容器設定可選且可自訂的邊框筆刷 (`BorderBrush`)、粗細 (`BorderThickness`) 與圓角弧度 (`CornerRadius`)：
- **`BorderBrush`**：支援 16 進位顏色代碼（如 `#3B82F6`）或顏色名稱，透過 `Brush.Parse(...)` 解析。
- **`BorderThickness`**：支援統一或四邊獨立邊距（`Left, Top, Right, Bottom`），以 `new Thickness(...)` 輸出。
- **`CornerRadius`**：支援統一或四角獨立半徑（`TopLeft, TopRight, BottomRight, BottomLeft`），以 `new CornerRadius(...)` 輸出。

### 12.2 陰影規格 (`BoxShadowModel` & `BoxShadows`)
AFG 提供完整的陰影效果配置，支援偏移量、模糊半徑、擴展大小、色彩與內外陰影：
- **`OffsetX` / `OffsetY`**：水平與垂直位移像素。
- **`Blur`**：陰影模糊半徑。
- **`Spread`**：陰影擴展半徑。
- **`Color`**：含 Alpha 通道之顏色代碼（如 `#40000000`）。
- **`IsInset`**：是否為內陰影（Inset）。

### 12.3 C# Declarative Markup Fluent 語法範例
```csharp
// 建立具備圓角邊框與柔和陰影之卡片容器
new Border()
    .Name("cardPanel")
    .Width(320)
    .Height(200)
    .Background(Brush.Parse("#FFFFFF"))
    .BorderBrush(Brush.Parse("#E2E8F0"))
    .BorderThickness(new Thickness(1.5, 1.5, 1.5, 1.5))
    .CornerRadius(new CornerRadius(12, 12, 12, 12))
    .BoxShadow(BoxShadows.Parse("0 8 24 0 #1A000000"))
    .Child(
        new Button()
            .Name("btnSubmit")
            .Text("送出")
            .BorderBrush(Brush.Parse("#2563EB"))
            .BorderThickness(new Thickness(1, 1, 1, 1))
            .CornerRadius(new CornerRadius(6, 6, 6, 6))
            .BoxShadow(BoxShadows.Parse("0 2 6 0 #20000000"))
    );
```

---

## 13. 多語言業務邏輯函數生成與跨語言專案配置規範 (Multi-Language Logic Services & Functions)

### 13.1 架構解耦原則 (Decoupled Logic Architecture)
為了使業務計算、演算法、外部 API 串接與硬體處理徹底獨立於 View / ViewModel，AFG 提供專屬之邏輯服務生成系統：
- **命名空間獨立**：可為每個邏輯服務定義獨立之 Namespace（如 `Company.Accounting.Services`）。
- **服務介面與實作分離**：自動產出 `I{ServiceName}` 介面與 `{ServiceName}` 實作。
- **支援非同步與取消 Token**：非同步方法自動補全 `CancellationToken cancellationToken = default` 與 `Task` / `Task<T>`。
- **依賴注入友善**：在 `App` 容器自動以單例模式 (`AddSingleton`) 註冊，支援 ViewModel 建構子相依性注入。

### 13.2 多語言生成代碼範例

#### 1. C# 邏輯服務 (`CSharpLogicGenerator`)
```csharp
// ICalculationService.cs
namespace App.Services;

public interface ICalculationService
{
    decimal CalculateTotal(decimal unitPrice, int quantity, decimal discountRate = 0m);
    Task<bool> ProcessPaymentAsync(string orderId, decimal amount, CancellationToken cancellationToken = default);
}

// CalculationService.cs
namespace App.Services;

public class CalculationService : ICalculationService
{
    public decimal CalculateTotal(decimal unitPrice, int quantity, decimal discountRate = 0m)
    {
        return (unitPrice * quantity) * (1m - discountRate);
    }

    public async Task<bool> ProcessPaymentAsync(string orderId, decimal amount, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return true;
    }
}
```

#### 2. F# 邏輯服務 (`FSharpLogicGenerator`)
```fsharp
// CalculationService.fs
#nowarn "3261" "3262" "3263" "1183" "0020"
namespace App.Services

open System
open System.Threading
open System.Threading.Tasks

type ICalculationService =
    abstract member CalculateTotal : unitPrice: decimal * quantity: int * ?discountRate: decimal -> decimal
    abstract member ProcessPaymentAsync : orderId: string * amount: decimal * ?cancellationToken: CancellationToken -> Task<bool>

type CalculationService() =
    interface ICalculationService with
        member this.CalculateTotal(unitPrice: decimal, quantity: int, ?discountRate: decimal) =
            let discountRate = defaultArg discountRate 0m
            (unitPrice * decimal quantity) * (1m - discountRate)

        member this.ProcessPaymentAsync(orderId: string, amount: decimal, ?cancellationToken: CancellationToken) =
            task {
                do! Task.CompletedTask
                return true
            }
```

#### 3. VB.NET 邏輯服務 (`VisualBasicLogicGenerator`)
```vb
' ICalculationService.vb
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Namespace App.Services
    Public Interface ICalculationService
        Function CalculateTotal(ByVal unitPrice As Decimal, ByVal quantity As Integer, Optional ByVal discountRate As Decimal = 0m) As Decimal
        Function ProcessPaymentAsync(ByVal orderId As String, ByVal amount As Decimal, Optional ByVal cancellationToken As CancellationToken = Nothing) As Task(Of Boolean)
    End Interface
End Namespace

' CalculationService.vb
Namespace App.Services
    Public Class CalculationService
        Implements ICalculationService

        Public Function CalculateTotal(ByVal unitPrice As Decimal, ByVal quantity As Integer, Optional ByVal discountRate As Decimal = 0m) As Decimal Implements ICalculationService.CalculateTotal
            Return (unitPrice * quantity) * (1m - discountRate)
        End Function

        Public Async Function ProcessPaymentAsync(ByVal orderId As String, ByVal amount As Decimal, Optional ByVal cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements ICalculationService.ProcessPaymentAsync
            Await Task.CompletedTask
            Return True
        End Function
    End Class
End Namespace
```

#### 4. C++ 原生模組與各語言 P/Invoke Bridge (`CppLogicGenerator`)
- **原生標頭檔 (`NativeCrypto.h`)**：
  ```cpp
  #pragma once
  #include <cstdint>
  #define AFG_API extern "C" __declspec(dllexport)

  AFG_API int32_t NativeCrypto_EncryptData(int32_t key, int32_t data);
  ```
- **原生實作檔 (`NativeCrypto.cpp`)**：
  ```cpp
  #include "NativeCrypto.h"

  AFG_API int32_t NativeCrypto_EncryptData(int32_t key, int32_t data) {
      return key ^ data;
  }
  ```
- **C# P/Invoke Bridge (`NativeCryptoNativeBridge.cs`)**：
  ```csharp
  namespace Security.Native;

  public class NativeCryptoNativeBridge : INativeCrypto
  {
      private const string LibName = "SecurityNativeLib";

      [DllImport(LibName, EntryPoint = "NativeCrypto_EncryptData", CallingConvention = CallingConvention.Cdecl)]
      private static extern int NativeCrypto_EncryptData(int key, int data);

      public int EncryptData(int key, int data)
      {
          return NativeCrypto_EncryptData(key, data);
      }
  }
  ```

### 13.3 跨語言專案配置與方案整合
當邏輯服務語言與專案主語言不同時（例如 C# 主專案搭配 F# 或 VB 邏輯模組）：
1. **獨立類別庫專案產出**：
   - `src/{ProjectName}.Logic.FSharp/{ProjectName}.Logic.FSharp.fsproj`
   - `src/{ProjectName}.Logic.VB/{ProjectName}.Logic.VB.vbproj`
   - `src/{ProjectName}.Logic.Cpp/`（內含 `CMakeLists.txt`）
2. **方案檔 (.slnx) 自動配置**：自動於 `.slnx` 中加入 `<Project Path="..." />`。
3. **主專案相依性引用**：在主專案（`.Shared.csproj` 或 `.Shared.fsproj`）中自動配置 `<ProjectReference Include="..\..\{ProjectName}.Logic.{Lang}\{ProjectName}.Logic.{Lang}.{proj}" />`。
4. **DI 容器自動裝配**：於 `App.cs/fs/vb` 的 `ConfigureServices` 中自動注入對應服務實例。








