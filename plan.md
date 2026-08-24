# Avalonia Form Generator (AFG) 專案執行計劃書

視覺化拖曳式 Avalonia MVVM 介面與程式碼生成工具  
遵照 `csharp-master-architect` 規範與 .NET 10 / C# 14 最高標準

---

## 1. 系統架構與專案結構設計 (.NET 10 / C# 14)

專案遵循 **模式 A（多專案跨平台與分層架構）** 與 **SRP 單一職責原則**，將核心 AST 模型、Roslyn 程式碼生成引擎、視覺化設計器 UI、展示層與平台進入點完全解耦。

```text
AvaloniaFormGenerator/
├── src/
│   ├── AFG.Core/                         # [核心層] UI AST 中介模型、驗證器、序列化合約（純 C#，無 UI 依賴）
│   │   ├── Models/Ast/                   # UI 節點定義 (AstNode, BindingDefinition, EventMapping, FormDocument)
│   │   ├── Enums/                        # 控制項類型、佈局模式、對齊等列舉
│   │   ├── Validation/                   # AST 語法與防禦性驗證器 (Fail-Fast)
│   │   └── Serialization/                # AfgSerializer (.afg.json 專案檔讀寫與容錯)
│   │
│   ├── AFG.Generators/                   # [程式碼生成引擎] Roslyn 格式化、C# Markup 生成器、ViewModel 生成器
│   │   ├── Abstractions/                 # ICodeGenerator, IRoslynCompilerService 介面
│   │   ├── CSharpMarkup/                 # Fluent C# Markup (View) 鏈式代碼生成器 (支援 Lambda/Compiled Binding)
│   │   ├── Mvvm/                         # CommunityToolkit.Mvvm (ViewModel) 生成器 (支援動態 DI 與自訂型別)
│   │   ├── ProjectExport/                # 跨平台多專案方案 (.slnx, .Shared, .Desktop, .Android) 匯出服務
│   │   └── Roslyn/                       # Roslyn 記憶體編譯、語法樹格式化與即時診斷服務
│   │
│   ├── AFG.Shared/                       # [跨平台共用 UI] 視覺化畫布、工具箱、屬性檢查器、Adorner 系統
│   │   ├── Controls/                     # 自訂設計畫布 (DesignCanvas - 支援遞迴容器渲染、Zoom/Pan 矩陣、多選橡皮筋)
│   │   ├── History/                      # Undo/Redo 歷史堆疊管理器 (Memento 模式)
│   │   ├── Views/                        # 主視窗、畫布視圖、屬性檢查器、代碼預覽視圖
│   │   ├── ViewModels/                   # MainViewModel, CanvasViewModel, InspectorViewModel
│   │   └── Services/                     # 剪貼簿、檔案對話框、熱重載通知介面定義 (IFileDialogService, etc.)
│   │
│   └── AFG.Desktop/                      # [桌面端進入點] Windows / macOS / Linux 桌面執行環境
│       ├── Services/                     # 桌面端 IFileDialogService, DesktopClipboardService 實作
│       └── Program.cs                    # 應用程式進入點 (ClassicDesktopStyleLifetime)
│
├── tests/
│   ├── AFG.Core.Tests/                   # AST 模型、節點階層操作、驗證器之單元測試 (xUnit)
│   └── AFG.Generators.Tests/             # C# Markup 轉譯、ViewModel 生成、Roslyn 格式化與記憶體編譯測試 (xUnit)
│
├── .github/workflows/                    # GitHub Actions 跨平台 CI/CD 工作流 (Ubuntu, Windows, macOS)
├── .editorconfig                         # 統一 C# 14 / .NET 10 編碼風格與 UTF-8 BOM 規則
├── .gitattributes                        # Git 換行與檔案屬性控管
├── .gitignore                            # .NET 10 / Visual Studio / JetBrains 忽略檔
└── AvaloniaFormGenerator.slnx            # 方案主檔
```

---

## 2. 核心技術選型與規範

| 模組領域 | 選用技術 / 函式庫 | 規範與用途說明 |
| :--- | :--- | :--- |
| **目標運行時** | `.NET 10` / `C# 14` | 全面啟用 `<Nullable>enable</Nullable>`、Primary Constructors、集合表達式 `[]`、File-scoped namespaces |
| **UI 基礎框架** | `Avalonia 11.2.5+ / 12.x` | 跨平台渲染底層、佈局引擎與視覺樹，集中版本號常數管理 |
| **C# Markup 規範** | Fluent Extension Builder / Declarative UI | 生成純 C# Declarative UI View，支援雙向資料綁定與強型別 Lambda 綁定模式 |
| **MVVM 架構** | `CommunityToolkit.Mvvm (8.x+)` | 產出 `[ObservableProperty]` 與 `[RelayCommand]`，支援同步與非同步命令及動態 DI 注入 |
| **相依性注入** | `Microsoft.Extensions.DependencyInjection` | 匯出專案內建標準服務容器生命週期配置 |
| **代碼分析與生成** | `Microsoft.CodeAnalysis.CSharp` (Roslyn) | 代碼格式化、AST 轉譯、背景記憶體編譯診斷（即時綁定語法檢查） |
| **測試框架** | `xUnit` + `FluentAssertions` | 針對所有純函式與生成引擎進行邊界測試與防禦性測試 |
| **CI / CD** | GitHub Actions | 多作業系統矩陣 (Ubuntu, Windows, macOS) 自動化建置與測試 |

---

## 3. 專案開發階段與進度追蹤 (Phased Milestones & Tracking)

### 🔹 階段 1 至 階段 5：基礎建設與第一期功能 (已完成)
- [x] **階段 1：環境基建與核心 AST 模型 (`AFG.Core`)**
- [x] **階段 2：程式碼生成引擎與 Roslyn 診斷 (`AFG.Generators`)**
- [x] **階段 3：視覺化設計畫布與 Adorner 裝飾器系統 (`AFG.Shared`)**
- [x] **階段 4：屬性與事件檢查器 (`AFG.Shared`)**
- [x] **階段 5：即時代碼預覽、匯出與端到端整合 (`ProjectExportService`)**
- [x] **增強項目：相依性注入 (DI)、自訂解析度、同步/非同步命令與 MIT 授權**

---

### 🔹 階段 6：視覺化畫布與互動體驗重大升級 (Canvas & UX Core Overhaul)
- [x] **階段狀態：已完成**
- **目標**：解決畫布無法嵌套容器、缺乏復原重做、無法多選/對齊、縮放座標偏移與缺乏快捷鍵之核心 UX 缺陷。
- **任務清單**：
  - [x] 6.1 **巢狀容器遞迴渲染**：重構 `DesignCanvas.cs` 中的 `CreateControlFromNode` 與 `RebuildElements`，當節點為容器（`IsContainer == true` 或 Grid/StackPanel/Canvas）時，遞迴建立子節點控制項並正確加入父容器的 Children 集合。
  - [x] 6.2 **復原 / 重做 (Undo / Redo) 歷史堆疊**：
    - 引入基於不可變 `FormDocument` 的 `HistoryManager`（Memento 模式），維護 `UndoStack` 與 `RedoStack`。
    - 在控制項移動結束、尺寸縮放完成、屬性修改、節點增刪時發布快照。
    - 支援快捷鍵 `Ctrl + Z`（復原）與 `Ctrl + Y` / `Ctrl + Shift + Z`（重做），並在頂部工具列提供對應按鈕。
  - [x] 6.3 **多選支援與對齊工具列**：
    - 將選取狀態升級為多節點支援 (`SelectedNodeIds`)，支援 `Ctrl + 左鍵` 多選與畫布橡皮筋選取框（Rubberband Selection Box）。
    - 新增對齊與分佈工具列：靠左對齊、水平居中、靠右對齊、靠頂對齊、垂直居中、靠底對齊、水平均勻分佈、垂直均勻分佈。
  - [x] 6.4 **畫布縮放平移與座標矩陣修正**：
    - 整合 `ZoomLevel` 與平移（Pan），外層支援 `Ctrl + 滑鼠滾輪` 縮放與 `滑鼠中鍵 / 空白鍵 + 拖曳` 平移。
    - 修正滑鼠點擊與 8 點手柄縮放計算，套用逆變換矩陣（Inverse Matrix Transform）確保任何縮放比率下零座標偏移。
  - [x] 6.5 **鍵盤快捷鍵與微調 (Nudge & Hotkeys)**：
    - 方向鍵微調（上下左右鍵移動 1px；`Shift + 方向鍵` 移動 8px/10px 網格）。
    - `Delete` / `Backspace` 鍵刪除選取控制項。
    - `Ctrl + C`（複製）、`Ctrl + V`（貼上）節點與 AST 複製純函式。
  - [x] 6.6 **畫布邊界與負數座標防禦機制**：防止控制項被拖出負數座標或超出畫布邊界過多。
- **驗證方式**：
  - 撰寫單元測試驗證 `HistoryManager` 堆疊操作、`AstTreeOperations` 批次對齊與多選複製純函式；手動操作畫布驗證巢狀容器渲染、快捷鍵與縮放座標精準度。

---

### 🔹 階段 7：屬性檢查器、MVVM 綁定機制與視覺化強化 (Inspector & MVVM Enhancement)
- [x] **階段狀態：已完成**
- **目標**：解耦寫死的服務注入、支援自訂 ViewModel 資料型別、提供視覺化調色盤。
- **任務清單**：
  - [x] 7.1 **解耦寫死之 DI 服務注入**：
    - 移除 ViewModel 與 `App.cs` 中強制的 `IGreetingService` 依賴。
    - 在 FormDocument 或 Inspector 中提供「相依性注入配置」清單，讓使用者自由勾選是否啟用 DI，並可自訂注入的服務介面名稱（例如 `IOrderService`, `IAuthService`）；預設產出乾淨無參數 ViewModel。
  - [x] 7.2 **自訂 ViewModel 資料型別**：
    - 在屬性檢查器的「資料綁定」分頁中，允許使用者手動輸入或下拉選擇自訂 C# 型別（如 `int`, `decimal`, `DateTime?`, `ObservableCollection<T>`, `List<string>` 等），覆寫自動推斷機制。
  - [x] 7.3 **視覺化調色盤與筆刷設定 (Color Picker)**：
    - 在外觀屬性中整合視覺化顏色選擇器與十六進制色碼（`#RRGGBB` / `#AARRGGBB`）輸入框，支援 Background 與 Foreground 顏色自訂。
- **驗證方式**：
  - 測試 ViewModel 生成器產出自訂型別與自訂 DI 服務；透過 Roslyn 編譯器驗證自訂型別 ViewModel 之語法正確性。

---

### 🔹 階段 8：程式碼生成引擎現代化與工具箱擴充 (Code Generation & Components)
- [x] **階段狀態：已完成**
- **目標**：統一套件版本管理、支援強型別 Compiled/Lambda Bindings、擴充不可視與通訊控制項。
- **任務清單**：
  - [x] 8.1 **統一套件依賴版本管理**：
    - 在全域常數中抽離 Avalonia 與 CommunityToolkit.Mvvm 之版本號（建立 `PackageVersions.cs` 於 `AFG.Generators`），消除各處寫死字串的維護隱患。
  - [x] 8.2 **C# Markup 強型別 Lambda 綁定支援**：
    - 擴充 View 生成器，支援切換輸出為強型別 Compiled Binding / Lambda 語法（例如 `.Text((ViewModel vm) => vm.Username)`）與標準字串 binding 模式（`nameof(ViewModel.Property)`）。
    - 在 `FormDocument` 增加 `bool UseCompiledBindings { get; init; }` 開關。
  - [x] 8.3 **擴充工具箱控制項 (不可視元件與通訊控制項)**：
    - 在 `ControlType.cs` 與 `ToolboxService.cs` 增加不可視元件與通訊服務模型：
      - `DispatcherTimer` (計時器元件)
      - `BackgroundWorker` (背景工作元件)
      - `BluetoothClient` (藍牙客戶端)
      - `SerialPortService` (序列埠通訊)
    - 在畫布上提供專屬元件卡片 Badge 渲染與綁定支援。
- **驗證方式**：
  - 驗證生成的 C# 代碼可切換輸出強型別 Lambda 綁定並無語法警告；驗證新增不可視控制項於工具箱與畫布可正常操作與序列化。

---

### 🔹 階段 9：多表單導航、可摺疊工作區排版、例外容錯與 CI/CD (Multi-Form, UI Polish & CI/CD)
- [x] **階段狀態：已完成**
- **目標**：支援多表單架構、最佳化工作區佈局（可摺疊面板）、完善例外提示，並建立 GitHub Actions 跨平台 CI/CD。
- **任務清單**：
  - [x] 9.1 **多表單 (Multi-Form / Multi-View Navigation) 支援**
  - [x] 9.2 **畫面排版重構（可摺疊/可隱藏面板）**
  - [x] 9.3 **例外與容錯處理 (Defensive & Friendly Errors)**
  - [x] 9.4 **GitHub Actions 跨平台 CI/CD 建置**

---

### 🔹 階段 10：物件名稱註解與對話方塊元件整合 (Object Name Comments & Dialogs Integration)
- [x] **階段狀態：已完成**
- **目標**：在生成的 View 中為每個控制項建構子上方產生該物件名稱註解；實作開檔 (`OpenFileDialog`)、存檔 (`SaveFileDialog`) 與 `MessageBox` 對話方塊並加入工具箱與程式碼生成器中。
- **任務清單**：
  - [x] 10.1 **Phase 1: 物件名稱註解生成 (Object Name Comments in Generated Views)**
    - 在 `CSharpMarkupViewGenerator.GenerateNodeCode` 中，於每個控制項/容器建構子（`new Button()`, `new TextBox()`, `new Canvas()` 等）上方加入 `// {node.Name}` 註解。
    - 撰寫單元測試驗證所有控制項與巢狀容器均包含精準的名稱註解。
  - [x] 10.2 **Phase 2: 對話方塊 AST 模型、事件目錄與工具箱擴充 (Dialogs in AST & Toolbox)**
    - 在 `ControlType` 加入 `OpenFileDialog`, `SaveFileDialog`, `MessageBox`。
    - 在 `ToolboxService` 新增「對話方塊」分類，包含三種對話方塊工具箱項目。
    - 在 `DesignCanvas` 支援不可視對話方塊徽章卡片渲染（`[OpenFileDialog]`, `[SaveFileDialog]`, `[MessageBox]`）。
    - 在 `ControlEventCatalog` 註冊對話方塊專屬事件（`FileOk`, `Confirmed` 等）與預設回呼參數。
    - 撰寫單元測試驗證對話方塊的事件目錄、參數及工具箱整合。
  - [x] 10.3 **Phase 3: 專案匯出對話方塊服務與 ViewModel 整合 (Dialog Services & CodeGen Integration)**
    - 在 `ProjectExportService` 產出跨平台 `IDialogService.cs`、`DialogService.cs` 與純 C# 原生現代化 `MessageBoxWindow.cs`。
    - 在 `MvvmViewModelGenerator` 支援不可視對話方塊欄位宣告與事件建構子掛載。
    - 撰寫單元與整合測試，包含匯出包含對話方塊的專案並驗證 `dotnet build`。
  - [x] 10.4 **Phase 4: 技術文件更新與全專案驗證 (Documentation & Final Verification)**
    - 更新 `README.md`、`docs/architecture.md`、`docs/ast-schema.md`、`docs/csharp-markup-spec.md`、`docs/user-guide.md`。
    - 執行全專案 100% 測試驗證並完成各階段 Git Commit。

---

### 🔹 階段 11：內嵌 Debug Console / Log Console 元件整合 (Embedded Debug Console & Logging Integration)
- [x] **階段狀態：已完成 (Completed)**
- **目標**：提供開箱即用的「內嵌 Debug Console」元件 (`DebugConsole`)，支援 `Microsoft.Extensions.Logging` 攔截、繼承 `System.IO.TextWriter` 支援標準輸出重定向、畫布視覺渲染、C# Declarative UI 綁定與 ViewModel 自動相依性注入。
- **任務清單**：
  - [x] 11.1 **Phase 1: AST 模型、工具箱與畫布視覺支援 (AST, Toolbox & Canvas for DebugConsole)**
    - 在 `ControlType` 加入 `DebugConsole`。
    - 在 `ToolboxService` 新增「除錯工具」分類，包含 `DebugConsole` 工具箱項目。
    - 在 `DesignCanvas` 支援 `DebugConsole` 畫布深色面板視覺渲染（包含標題列、Clear 按鈕與日誌列表）。
    - 撰寫單元測試驗證 AST、工具箱與事件目錄整合。
  - [x] 11.2 **Phase 2: View 與 ViewModel 程式碼生成支援 (View & ViewModel CodeGen for DebugConsole)**
    - 在 `CSharpMarkupViewGenerator` 將 `DebugConsole` 轉譯為 C# Declarative UI 結構（Border, Grid, Header, Clear Button, ListBox ItemsSource）。
    - 在 `MvvmViewModelGenerator` 當偵測到 `DebugConsole` 時，自動注入 `InMemoryLogService` 與 `ILogger<TViewModel>`，宣告 `LogEntries` 與 `ClearLogsCommand`。
    - 撰寫單元測試驗證 View 與 ViewModel 生成及 Roslyn 語法診斷。
  - [x] 11.3 **Phase 3: 專案匯出服務、Logging DI 與 ConsoleRedirectWriter 實作 (Project Export Services, Logging & TextWriter Redirection)**
    - 在 `PackageVersions` 定義 `MicrosoftExtensionsLogging = "9.0.2"`。
    - 在 `ProjectExportService` 匯出 `LogEntry.cs`、`InMemoryLogService.cs`、`InMemoryLoggerProvider.cs` 與 `ConsoleRedirectWriter.cs`（繼承 `System.IO.TextWriter` 支援 `Console.Out` / `Console.Error` 重定向）。
    - 在 `App.cs` 與 `GlobalUsings.cs` 配置全域 `Microsoft.Extensions.Logging` 與 `InMemoryLoggerProvider`。
    - 撰寫匯出專案實體端到端編譯整合測試（執行 `dotnet build` 驗證 0 錯誤 0 警告成功通過）。
  - [x] 11.4 **Phase 4: 技術文件更新與全專案驗證 (Documentation & Final Verification)**
    - 更新 `README.md`、`docs/architecture.md`、`docs/ast-schema.md`、`docs/csharp-markup-spec.md`、`docs/user-guide.md`。
    - 執行全專案 100% 測試驗證並完成各階段 Git Commit。

---

## 4. 驗證標準與品質指標

1. **單元測試覆蓋率**：所有純函式、AST 操作、歷史堆疊、生成器與序列化演算法 100% 覆蓋。
2. **零警告與零錯誤**：`dotnet build` 與 `dotnet test` 保持 0 Error, 0 Warning。
3. **實體跨平台編譯保障**：匯出之專案在 Windows / macOS / Linux 平台均可一鍵執行 `dotnet run` 成功啟動。
