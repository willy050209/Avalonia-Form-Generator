# 視覺化設計器操作手冊 (User Guide)

本文檔為 **Avalonia Form Generator (AFG)** 的完整使用者操作指南，包含畫布互動、裝置解析度設定、屬性檢查、MVVM 綁定配置、可摺疊面板操作與跨平台多專案匯出流程。

---

## 1. 介面工作區佈局 (Workspace Layout)

啟動 AFG 後，介面主要分為五大區域（支援面板獨立摺疊與展開）：

```text
+---------------------------------------------------------------------------------------------------------------+
| 頂部工具列: [新增] [開啟] [儲存] [匯出整包專案] | [復原/重做] [複製/貼上] [左/中/右/頂/中/底對齊/均分] [網格吸附]
|             [解析度: 9:19.5] [寬: 390] [高: 844] [行動端] [授權] | [工具箱] [程式碼] [屬性欄] [複製 View] [複製 VM]  |
+------------------+----------------------------------------------------------------------------+---------------+
| 左側邊欄          | 中央畫布區                                                                  | 右側邊欄      |
| [工具箱]         |                                                                            | [屬性檢查器]  |
| - 常用控制項     |  +---------------------------------------------------------------------+   | - 外觀 (顏色) |
| - 佈局容器       |  |                                                                     |   | - 排版 (幾何) |
| - 不可視/通訊元件|  |                     [ 設計畫布 (Design Canvas) ]                    |   | - 資料綁定    |
| [元件樹]         |  |                                                                     |   | - 事件命令    |
| - 階層式節點     |  +---------------------------------------------------------------------+   |               |
|                  | -------------------------------------------------------------------------- |               |
|                  | 底部即時程式碼預覽: [View (C# Markup)] [ViewModel (CommunityToolkit.Mvvm)]  |               |
+------------------+----------------------------------------------------------------------------+---------------+
| 底部狀態列: 目前選取節點 ID | 語法診斷狀態                                                                      |
+---------------------------------------------------------------------------------------------------------------+
```

---

## 2. 操作步驟說明 (Step-by-Step Instructions)

### 2.1 快捷鍵與歷史操作 (Undo / Redo & Shortcuts)
- **復原 / 重做**：按下 `Ctrl + Z`（復原）或 `Ctrl + Y`（重做）。
- **複製 / 貼上**：選中節點後按下 `Ctrl + C`，按下 `Ctrl + V` 在原位置偏移處貼上包含子樹的完整複本。
- **微調位移 (Nudge)**：選取控制項後，使用鍵盤方向鍵可進行 1px 精細微調；按住 `Shift + 方向鍵` 進行 8px 快速吸附網格微調。
- **刪除控制項**：選取控制項後按 `Delete` 或 `Backspace` 鍵。
- **縮放畫布**：按住 `Ctrl + 滑鼠滾輪` 可在 20% ~ 300% 間平滑縮放畫布。

### 2.2 橡皮筋多選、對齊均分與容器拖曳重新排序 (Container Drag-Reordering)
- **多選控制項**：在畫布空白處按住滑鼠左鍵拖曳拉出「橡皮筋框選盒」，即可一次圈選多個控制項；或點選節點時按住 `Ctrl` 鍵進行多重選取。
- **對齊工具列**：
  - 靠左對齊 / 水平置中 / 靠右對齊
  - 靠頂對齊 / 垂直置中 / 靠底對齊
  - 水平均勻分佈 / 垂直均勻分佈
- **容器內拖曳重新排序**：
  - 在 `StackPanel`, `DockPanel`, `WrapPanel` 等容器內拖曳子元件時，畫布會即時計算目標插入位置，並呈現**亮藍色插入指示線（Blue Insertion Indicator Line）與端點光暈圓點**。
  - 放開滑鼠後自動調換 AST Children 索引，並推入歷史紀錄堆疊（支援 `Ctrl+Z` 復原）。

### 2.3 可摺疊面板與工作區自訂
在頂部選單列「檢視(_V)」中提供三個面板切換開關：
- **「工具箱 / 元件樹」**：顯示或隱藏左側工具箱與元件樹面板。
- **「即時程式碼預覽」**：顯示或隱藏底部即時 C# 程式碼預覽區域。
- **「屬性檢查器」**：顯示或隱藏右側屬性檢查器。
隱藏側邊欄後，中央設計畫布會自動佔滿可用視窗空間。

### 2.4 頂部選單列整合操作 (Integrated Menu Bar)
所有裝置規格、畫布尺寸、架構配置與專案匯出功能已全面整合至頂部選單列中，提供乾淨全高設計畫布：
- **「檔案(_F)」**：新增表單 (`Ctrl+N`)、開啟專案 (`Ctrl+O`)、儲存專案 (`Ctrl+S`)、匯出完整跨平台專案 (`Ctrl+Shift+E`)。
- **「編輯(_E)」**：復原 (`Ctrl+Z`)、重做 (`Ctrl+Y`)、複製節點 (`Ctrl+C`)、貼上節點 (`Ctrl+V`)、刪除選取 (`Delete`)。
- **「排版對齊(_A)」**：靠左/中/右對齊、頂/中/底對齊、水平均分、垂直均分。
- **「檢視(_V)」**：面板開關切換、一鍵複製 View C# Markup 代碼、一鍵複製 ViewModel C# 代碼。
- **「裝置與規格(_D)」**：
  - **桌面端解析度**：快速切換 Desktop 1080p, 720p, Standard (800x600), Small (960x600)。
  - **行動手機解析度**：快速切換 Phone 9:19.5 (390x844), 9:20 (412x915), 9:16 (360x640), FHD+ 9:20 (1080x2400)。
  - **平板電腦解析度**：快速切換 iPad Standard (768x1024), Tablet 16:10 (800x1280)。
  - **自訂解析度...**：點選開啟自訂畫布解析度視窗，自由輸入任意像素寬度與高度（200px ~ 4000px）並一鍵套用。
  - **網格吸附 (Snap to Grid)**：即時開關 8px 網格吸附。
- **「專案與架構(_P)」**：
  - **自訂專案名稱...**：點選開啟自訂專案名稱視窗，自由指定匯出方案與各專案名稱（如 `InventoryApp`、`PosSystem`）。
  - **包含行動端專案 (.Android)**：即時開關匯出時是否產出 Android 宿主專案。
  - **包含授權文件 (LICENSE)**：開關匯出時是否產出 MIT License（預設為關閉/不生成）。
  - **使用強型別編譯綁定 (Compiled Bindings)**：切換 C# Markup 產出模式（Lambda 編譯綁定或 nameof 字串綁定）。
  - **啟用相依性注入架構 (Dependency Injection)**：控制是否生成 DI 容器與服務注入機制。
  - **匯出完整跨平台專案... (`Ctrl+Shift+E`)**。

在底部狀態列 (Status Bar) 則即時顯示當前選取節點 ID、語法問題數、當前裝置規格、畫布解析度寬高與專案名稱。

### 2.5 設定控制項屬性、顏色、資料綁定與多參數事件命令
在右側 **「屬性檢查器」** 中：
- **流暢輸入與焦點保護**：在編輯 ViewModel 屬性名稱或 Command 命令名稱時，系統具備防重入與焦點保護機制，連續打字輸入絕不丟失鍵盤焦點。
- **「外觀」分頁**：支援設定名稱 (x:Name)、標題 (Content)、文字 (Text)、提示文字 (PlaceholderText)、背景色 (Background) 與前景色 (Foreground)（支援 `#RRGGBB` 色碼或顏色名稱）。
  - **PictureBox 圖片方塊操作**：
    - **視覺化檔案選擇**：點選「瀏覽...」按鈕開啟檔案對話框選取圖片檔案（支援 `.png`, `.jpg`, `.jpeg`, `.bmp`, `.gif`, `.webp`, `.ico` 等）。
    - **即時圖片預覽與資訊**：屬性面板即時呈現選取檔案的檔名與檔案大小，且設計畫布上立即載入並即時預覽該圖片。
    - **相對路徑 / 絕對路徑模式**：勾選「使用專案相對路徑」後，專案匯出時系統會自動將實體圖片複製至生成專案的 `Assets/` 資料夾，並生成 `avares://{AppName}.Shared/Assets/{filename}` 資源路徑；取消勾選則使用本機絕對路徑。
    - **初始化空白點陣圖 (Init Bitmap)**：勾選後可快速建立空白點陣圖，指定背景顏色（預設 `#F0F0F0`）與寬高，表單載入時自動透過 `BitmapHelper.CreateInitializedBitmap()` 初始化。
- **「排版」分頁**：設定寬度、高度、Canvas 座標、Grid 網格座標、對齊方式與外距 Margin。不可視元件（如 Timer、Worker、BLE、COM）在畫布上具備獨立的預覽卡片，支援自由拖曳排版與縮放。
- **「資料綁定」分頁**：
  - **View 屬性 (TargetProperty)**：提供下拉選單（`Text`, `Content`, `IsChecked`, `Value`, `IsEnabled`, `IsVisible`, `Source`, `Stretch`, `ItemsSource`, `SelectedItem`, `SelectedIndex`, `Header` 等），選取時自動同步推斷合適的 C# 資料型別，徹底避免手動打字拼錯。
  - **ViewModel 屬性名稱**：指定綁定的 ViewModel 屬性。
  - **文字連動綁定 (TextBox 綁定至 TextBlock)**：只要將 `TextBox` 的 `Text` 屬性（TwoWay 模式）與 `TextBlock` 的 `Text` 屬性（OneWay 或 Default 模式）指定為**同一個 ViewModel 屬性名稱**（例如 `UserName`），在執行期於文字輸入框打字時，標籤文字就會即時同步更新呈現！
  - **資料型別 (C# DataType)**：提供常見型別下拉選單（`string`, `bool`, `double`, `int`, `Avalonia.Media.IImage`, `Avalonia.Media.Stretch`, `DateTime?`, `ObservableCollection<string>` 等）。
  - **綁定模式 (Mode)**：下拉切換 `Default`, `TwoWay`, `OneWay`, `OneWayToSource`, `OneTime`。
- **「事件命令」分頁**：
  - **智慧專屬事件下拉選單 (EventName)**：系統依據所選元件類型自動過濾僅顯示該控制項或硬體通訊元件支援的專屬事件（例如 Button 僅顯示 `Click` / `Tapped` 等；Slider 僅顯示 `ValueChanged`；OpenFileDialog / SaveFileDialog 專屬提供 `FileOk` 回呼；MessageBox 專屬提供 `Confirmed` 回呼；BluetoothClient 專屬提供 `DeviceDiscovered`, `Connected`, `Disconnected`, `DataReceived` 回呼；SerialPortService 專屬提供 `DataReceived`, `ErrorReceived`, `PinChanged` 回呼；BackgroundWorker 提供 `DoWork`, `ProgressChanged`, `RunWorkerCompleted` 回呼；DispatcherTimer 提供 `Tick` 回呼），徹底消除拼寫與無效事件錯誤。
  - **多參數配置與型別過濾 (Multi-Parameter Configuration)**：
    - 事件預設自動帶入專屬的雙參數（如 `(sender, object?)` 與 `(e, RoutedEventArgs)`，`FileOk` 帶入 `(sender, object?)` 與 `(filePath, string?)`，`Confirmed` 帶入 `(sender, object?)` 與 `(result, bool?)`，`Tick` 帶入 `(sender, object?)` 與 `(e, EventArgs)`）。
    - 支援透過「新增參數」與「移除參數」自訂多個傳遞參數。
    - 參數型別下拉選單會依據當前事件自動限制為**專屬 EventArgs 型別與通用基底型別**，防止跨事件誤選不相容的 EventArgs（例如 Click 事件無法選擇 TextChangedEventArgs）。
  - **ViewModel Command 名稱**：指定映射的 ViewModel RelayCommand 方法。
  - **非同步開關**：勾選後將自動在 ViewModel 產出 `async Task ...Async()` 簽章，不可視元件、對話方塊與硬體通訊事件將在 ViewModel 建構子內自動訂閱掛載。

### 2.6 即時預覽與跨平台方案匯出 (即時反應式預覽與專案匯出)
- **即時反應式預覽**：底部預覽區直連 AST 與生成引擎，無論畫布新增、刪除、移動控制項或修改資料綁定，皆能**零延遲即時更新** View (C# Markup) 與 ViewModel (CommunityToolkit.Mvvm) 程式碼。
- **單檔代碼一鍵複製**：在底部預覽區各分頁右上角提供「複製 View 程式碼」與「複製 ViewModel 程式碼」按鈕，點選即可將 Roslyn 格式化後的 C# 代碼複製至剪貼簿並彈出成功通知。
- **專案檔儲存/開啟**：點選「儲存」輸出 `.afg.json`（保留自訂專案名稱與完整畫布狀態）；若載入損毀檔案，系統會顯示行數與欄位的詳細診斷提示。
- **一鍵匯出跨平台專案**：點選「檔案 > 匯出完整跨平台專案...」或按 `Ctrl+Shift+E`，系統自動產出包含自訂專案名稱的 `.slnx` 方案、`.Shared` 核心（含 `INavigationService` 導航與動態 DI 容器）、`.Desktop` 與可選的 `.Android` 宿主專案！
  - **Android 專案支援與 APK 編譯**：生成的 `.Android` 專案內建完整的 Android 資源階層（`Resources/values/styles.xml`、`Resources/drawable/icon.xml` 與 `AndroidManifest.xml`），並配置 `<AndroidPackageFormat>apk</AndroidPackageFormat>`。在安裝有 Android SDK 的環境下，可透過 `dotnet build -c Release -t:SignAndroidPackage` 或 `dotnet publish -c Release` 直接編譯產出 Signed APK 安裝包。

### 2.7 目標語言選擇 (C# / F# / Visual Basic .NET / C++)
- **多語言程式碼生成切換**：在右側屬性檢查器的「表單屬性 > 外觀 > 專案與類別架構」中，提供「目標語言 (Target Language)」下拉選單：
  - **`CSharp`** (C# 14 / .NET 10)：產出現代化 C# Declarative UI View、`CommunityToolkit.Mvvm` ViewModel、強型別 Lambda 綁定與完整 `.csproj` 方案。
  - **`FSharp`** (F# 9 / .NET 10)：產出純 F# Avalonia View (`UserControl`)、`ObservableObject` ViewModel、`App.fs`、`Config.fs` 與依照嚴格編譯相依性排序之 `.fsproj` 方案。
  - **`VisualBasic`** (Visual Basic .NET 10)：產出標準 VB.NET View (`WithEvents` 控制項欄位、`InitializeComponent`)、`ObservableObject` ViewModel、`Config.vb` 與 `.vbproj` 方案。
  - **`Cpp`** (C++ 20 / CMake / P/Invoke)：產出原生 C++ 標頭檔、實作檔、`CMakeLists.txt` 與 C#/F#/VB 之 P/Invoke Bridge 互通類別。
- **即時代碼預覽與語法高亮**：切換目標語言後，底部代碼預覽區立即切換為對應語言之原始碼，並由 `CSharpSyntaxColorizer` 自動套用 VS Dark+ 語法著色高亮。
- **一鍵多語言專案匯出**：匯出專案時，系統會依據表單所選之目標語言產出對應的 `.csproj` / `.fsproj` / `.vbproj` 跨平台專案，所有專案皆支援 `dotnet build` 與 `dotnet run` 直接編譯執行。

### 2.8 表單與視窗控制屬性系統 (Form & Window Control Properties)
- **進入表單屬性模式**：點擊畫布空白處、選取樹狀圖根節點或在控制項屬性頂部點擊「表單屬性」按鈕，屬性檢查器將自動切換為「表單與視窗屬性 (Form)」面板。
- **「外觀」分頁**：
  - **視窗標題 (Title)**：設定應用程式視窗標題列文字。
  - **表單背景顏色 (BackgroundColor)**：提供自訂 `#RRGGBB` 色碼輸入與色票即時預覽，並附帶常用快捷色票按鈕（純白、淺灰、米色、深灰、夜黑、暗藍），點選後畫布背景即時同步渲染。
  - **視窗圖示 (Icon)**：點選「瀏覽...」按鈕可視覺化選取視窗圖示檔案（`.ico` / `.png`），專案匯出時自動複製至 `.Shared/Assets/`。
  - **專案與類別架構**：設定 View 類別名稱、ViewModel 類別名稱、目標語言 (CSharp / FSharp / VisualBasic) 與命名空間 (RootNamespace)。
- **「尺寸」分頁**：
  - **預設畫布寬高**：設定表單預設尺寸 (CanvasWidth / CanvasHeight)。
  - **快速切換解析度**：提供 800x600, 1024x768, 1280x720, 1920x1080 快捷切換按鈕。
  - **尺寸約束極限**：自由設定 MinWidth、MinHeight、MaxWidth 與 MaxHeight 視窗拉伸約束。
- **「行為」分頁**：
  - **視窗啟動位置 (WindowStartupLocation)**：下拉選取 `CenterScreen`（螢幕置中）、`CenterOwner` 或 `Manual`。
  - **初始視窗狀態 (WindowState)**：下拉選取 `Normal`（一般視窗）、`Maximized`（最大化）、`Minimized`（最小化）或 `FullScreen`（全螢幕）。
  - **系統邊框裝飾 (SystemDecorations)**：下拉選取 `Full`、`None`（無邊框視窗）或 `BorderOnly`。
  - **視窗行為開關**：設定「允許調整大小 (CanResize)」、「視窗永遠置頂 (Topmost)」與「在工作列顯示 (ShowInTaskbar)」。
- **即時連動與復原支援**：所有表單屬性變更皆會即時反映至畫布與底部 View / MainWindow 程式碼預覽，並自動推入 `Ctrl+Z` 歷史復原堆疊。

### 2.9 多語言業務邏輯函數生成與跨語言專案配置 (Multi-Language Logic Functions)
- **View / ViewModel 徹底解耦**：業務邏輯獨立生成於服務層，ViewModel 透過依賴注入取得服務實例，保持關注點分離。
- **靈活配置函數與參數**：支援為邏輯服務指定獨立命名空間 (Namespace)、函數名稱、回傳型態、非同步 (`IsAsync`) 標記與強型別參數清單（含型態與預設值）。
- **跨語言獨立專案自動配置**：
  - 當邏輯語言與主專案相同時，直接生成至 `.Shared/Services/`。
  - 當邏輯語言不同時（如 C# 專案搭配 F#/VB.NET 邏輯），自動生成獨立類別庫專案（如 `src/{ProjectName}.Logic.FSharp/`、`src/{ProjectName}.Logic.VB/`），自動加入方案檔 `.slnx` 與主專案 `<ProjectReference>`。
  - 若選擇 C++ 語言，自動產出原生模組（含 `CMakeLists.txt`、`.h`、`.cpp`）與主專案 P/Invoke Interop Bridge 類別。
  - 在 `App` 入口點自動註冊依賴注入 (`services.AddSingleton`)。

### 2.10 跨平台二進位版本發布 (v1.0.0 Release Matrix)
- **主流 4 大架構發布產物**：
  - `AFG-win-x64.zip` (Windows 64 位元)
  - `AFG-linux-x64.tar.gz` (Linux x64)
  - `AFG-osx-x64.tar.gz` (macOS Intel x64)
  - `AFG-osx-arm64.tar.gz` (macOS Apple Silicon M 系列)
- **CI/CD 自動化安全驗證**：GitHub Actions 在 Windows、Linux、macOS 全平台單元與整合測試（325 項測試）100% 通過後自動產出 Single-File 自包含執行檔並發布至 GitHub Releases。
