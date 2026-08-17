# Avalonia Form Generator (AFG) 專案執行計劃書

視覺化拖曳式 Avalonia MVVM 介面與程式碼生成工具  
遵照 `csharp-master-architect` 規範與 .NET 10 / C# 14 最高標準

---

## 1. 系統架構與專案結構設計 (.NET 10 / C# 14)

專案遵循 **模式 A（多專案跨平台與分層架構）** 與 **SRP 單一職責原則**，將核心 AST 模型、Roslyn 程式碼生成引擎、視覺化設計器 UI、展示層與平台進入點完全解耦。

```
AvaloniaFormGenerator/
├── src/
│   ├── AFG.Core/                         # [核心核心層] UI AST 中介模型、驗證器、序列化合約（純 C#，無 UI 依賴）
│   │   ├── Models/Ast/                   # UI 節點定義 (ComponentNode, LayoutNode, BindingDefinition, EventMapping)
│   │   ├── Enums/                        # 控制項類型、佈局模式、對齊等列舉
│   │   ├── Validation/                   # AST 語法與防禦性驗證器 (Fail-Fast)
│   │   └── GlobalUsings.cs
│   │
│   ├── AFG.Generators/                   # [程式碼生成引擎] Roslyn 格式化、C# Markup 生成器、ViewModel 生成器
│   │   ├── Abstractions/                 # ICodeGenerator, IRoslynCompilerService 介面
│   │   ├── CSharpMarkup/                 # Fluent C# Markup (View) 鏈式代碼生成器
│   │   ├── Mvvm/                         # CommunityToolkit.Mvvm (ViewModel) 生成器
│   │   ├── Roslyn/                       # Roslyn 記憶體編譯、語法樹格式化與即時診斷服務
│   │   └── GlobalUsings.cs
│   │
│   ├── AFG.Shared/                       # [跨平台共用 UI] 視覺化畫布、工具箱、屬性檢查器、Adorner 系統
│   │   ├── Controls/                     # 自訂設計畫布 (DesignCanvas)、裝飾器 (Adorners)、輔助對齊線
│   │   ├── Views/                        # 主視窗、畫布視圖、屬性檢查器、代碼預覽視圖
│   │   ├── ViewModels/                   # MainViewModel, CanvasViewModel, InspectorViewModel, CodePreviewViewModel
│   │   ├── Services/                     # 剪貼簿、檔案對話框、熱重載通知介面定義 (IFileDialogService, etc.)
│   │   └── GlobalUsings.cs
│   │
│   └── AFG.Desktop/                      # [桌面端進入點] Windows / macOS / Linux 桌面執行環境
│       ├── Services/                     # 桌面端 IFileDialogService, IClipboardService 實作
│       ├── Program.cs                    # 應用程式進入點 (ClassicDesktopStyleLifetime)
│       └── GlobalUsings.cs
│
├── tests/
│   ├── AFG.Core.Tests/                   # AST 模型、節點階層操作、驗證器之單元測試 (xUnit)
│   └── AFG.Generators.Tests/             # C# Markup 轉譯、ViewModel 生成、Roslyn 格式化與記憶體編譯測試 (xUnit)
│
├── .editorconfig                         # 統一 C# 14 / .NET 10 編碼風格與 UTF-8 BOM 規則
├── .gitattributes                        # Git 換行與檔案屬性控管
├── .gitignore                            # .NET 10 / Visual Studio / JetBrains 忽略檔
└── AvaloniaFormGenerator.sln             # 方案主檔
```

---

## 2. 核心技術選型與規範

| 模組領域 | 選用技術 / 函式庫 | 規範與用途說明 |
| :--- | :--- | :--- |
| **目標運行時** | `.NET 10` / `C# 14` | 全面啟用 `<Nullable>enable</Nullable>`、Primary Constructors、集合表達式 `[]`、File-scoped namespaces |
| **UI 基礎框架** | `Avalonia 11.x+` | 跨平台渲染底層、佈局引擎與視覺樹 |
| **C# Markup 規範** | `Avalonia.Markup.Declarative` / Fluent Extension Builder | 生成純 C# Declarative UI View，支援雙向資料綁定與強型別 Lambda |
| **MVVM 架構** | `CommunityToolkit.Mvvm (8.x+)` | 產出 `[ObservableProperty]` 與 `[RelayCommand]`，ViewModel 零 UI 依賴 |
| **代碼分析與生成** | `Microsoft.CodeAnalysis.CSharp` (Roslyn) | 代碼格式化、AST 轉譯、背景記憶體編譯診斷（即時綁定語法檢查） |
| **測試框架** | `xUnit` + `FluentAssertions` + `Moq` | 針對所有純函式與生成引擎進行邊界測試與防禦性測試 |

---

## 3. 專案開發階段與進度追蹤 (Phased Milestones & Tracking)

### 🔹 階段 1：環境基建與核心 AST 模型 (`AFG.Core`)
- [ ] **階段狀態：未開始**
- **任務清單**：
  - [ ] 1.1 初始化 Git 儲存庫、建立 `.gitignore`、`.gitattributes`、`.editorconfig`
  - [ ] 1.2 建立 .NET 10 多專案方案結構與全域設定 (`Directory.Build.props`)
  - [ ] 1.3 定義 UI AST 中介資料結構 (`AstNode`, `PropertyBinding`, `EventMapping`, `ContainerNode`)
  - [ ] 1.4 實作 AST 操作純函式（節點新增、刪除、移動、階層樹遍歷）
  - [ ] 1.5 實作 AST JSON 序列化與反序列化（支援 `.afg.json` 專案檔讀寫）
- **驗證方式**：
  - [ ] `AFG.Core.Tests` 撰寫單元測試，測試 AST 增刪改查、階層樹循環引用檢查、防禦性 Null 檢查（100% 通過）

---

### 🔹 階段 2：程式碼生成引擎與 Roslyn 診斷 (`AFG.Generators`)
- [ ] **階段狀態：未開始**
- **任務清單**：
  - [ ] 2.1 實作 C# Markup View 生成器（遞迴遍歷 AST 產出 Fluent 鏈式調用程式碼）
  - [ ] 2.2 實作 ViewModel 生成器（產生符合 `CommunityToolkit.Mvvm` 規範之 partial class）
  - [ ] 2.3 整合 Roslyn 程式碼格式化工具 (`CSharpSyntaxTree`, `Formatter`)
  - [ ] 2.4 實作 Roslyn 記憶體編譯診斷服務（驗證產生的 C# 程式碼是否能順利編譯通過）
- **驗證方式**：
  - [ ] `AFG.Generators.Tests`：針對各種表單情境（包含 Grid、StackPanel、雙向綁定、Command 映射）進行產出測試，並由 Roslyn 進行 In-Memory Compilation 驗證無編譯錯誤

---

### 🔹 階段 3：視覺化設計畫布與 Adorner 裝飾器系統 (`AFG.Shared`)
- [ ] **階段狀態：未開始**
- **任務清單**：
  - [ ] 3.1 實作設計畫布（支援自由畫布 Canvas 與流式佈局容器 Grid / StackPanel / DockPanel）
  - [ ] 3.2 實作控制項工具箱（Toolbox）與拖曳放置（Drag & Drop）機制
  - [ ] 3.3 實作選取裝飾器（Selection Adorner）：8 點縮放、尺寸調整、即時外距/內距調整
  - [ ] 3.4 實作吸附對齊演算法（Snap to Grid、邊界與中心輔助線）
  - [ ] 3.5 實作 Visual Tree Explorer（視覺化節點樹狀導航與拖曳層級調整）
- **驗證方式**：
  - [ ] 執行桌面端 App，手動拖曳測試控制項定位、縮放、容器嵌套以及對齊輔助線吸附行為

---

### 🔹 階段 4：屬性與事件檢查器 (`AFG.Shared`)
- [ ] **階段狀態：未開始**
- **任務清單**：
  - [ ] 4.1 實作屬性檢查器（Property Inspector），動態反映當前選取節點之外觀、佈局與文字屬性
  - [ ] 4.2 實作 Binding Builder：視覺化配置屬性與 ViewModel 欄位之雙向綁定
  - [ ] 4.3 實作 Event-to-Command Mapping：將 Click / SelectionChanged 等事件映射為 RelayCommand
  - [ ] 4.4 連動 Inspector 與 AST 狀態變更（變更即時反映在畫布與中介樹上）
- **驗證方式**：
  - [ ] 點選畫布上的按鈕或文字框，修改屬性並驗證畫布即時更新，且對應 AST 節點同步更新

---

### 🔹 階段 5：即時代碼預覽、匯出與端到端整合
- [ ] **階段狀態：未開始**
- **任務清單**：
  - [ ] 5.1 實作即時代碼預覽分頁（支援 C# 語法高亮）
  - [ ] 5.2 實作單檔複製與整包 Avalonia 模組專案導出功能（包含 View.cs, ViewModel.cs, 專案檔或組件註冊）
  - [ ] 5.3 整合即時錯誤警示：若綁定屬性名稱不合法，在 Inspector 與預覽區給予即時標記
- **驗證方式**：
  - [ ] 設計一個完整的 CRUD 表單（包含 DataGrid, TextBox, DatePicker, Button），將生成的 C# 檔案直接導入全新 Avalonia 專案中編譯並執行成功

---

## 4. Git 環境與協同規範

1. **版本控制初始化**：
   - 預設分支：`main`
   - 換行與編碼控管：透過 `.gitattributes` 強制所有 `*.cs`, `*.axaml`, `*.props` 檔案統一使用 UTF-8 (含 BOM) 與 LF/CRLF 規範。
2. **Commit 訊息規範 (Conventional Commits)**：
   - `feat: <description>`：新增功能（如 AST 節點、C# Markup 生成器）
   - `fix: <description>`：修復 Bug
   - `test: <description>`：新增或修改單元測試
   - `refactor: <description>`：架構重構（無行為變更）
   - `chore: <description>`：環境或依賴套件配置

---

## 5. 設計確認與待決策項目 (Decisions Log)

- [ ] **Q1. C# Markup 語法擴充偏好**：
  - 選項 A: 採用社群標準 `Avalonia.Markup.Declarative` 套件
  - 選項 B: 內建輕量化 Fluent Extension 生成模板（零外部擴充依賴）
- [ ] **Q2. 目標執行平台**：先以 Desktop (Windows / macOS / Linux) 為核心設計器宿主
- [ ] **Q3. 專案儲存格式**：支援導出 C# 程式碼與儲存/載入 `.afg.json` 中介檔
