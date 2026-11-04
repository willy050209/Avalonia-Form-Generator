# 多語言交互協同運算展示 (Multi-Language Interop Showcase Example)

> 🎨 **Avalonia Form Generator (AFG)** 官方實機展示專案  
> 示範由 AFG 獨創之跨語言協同運算架構：**C# 主介面與視圖模型 + F# 函數式計算專案 + VB.NET 商業規則專案 + C++ 原生模組 (DllImport)** 的全方位整合。

---

## 📸 實機畫面展示 (Showcase Gallery)

### 1. AFG 設計器視覺化編輯畫面 (Editor Design View)
呈現於 AFG 設計器中載入 `MultiLanguageInteropShowcase.afg.json` 之視覺化畫布、多語言運算卡片與 3 組非視覺化邏輯函數元件：

![AFG Editor Design View](screenshots/editor_design_view.png)

### 2. 獨立實機運行與多語言協同運算結果 (Runtime Result)
呈現跨平台桌面端實機執行結果，展示點擊「一鍵執行多語言協同運算」後，由 F#、VB.NET 與 C++ 原生函式庫即時平行運算之結果：

![Runtime Result](screenshots/runtime_result.png)

---

## 🌟 專案特色與架構 (Key Features & Architecture)

1. **多語言分散式方案架構 (.slnx)**：
   * **C# 主專案 (`MultiLanguageInteropShowcase.Shared` / `.Desktop`)**：負責現代 Fluent C# Markup 介面、MVVM 狀態管理與相依性注入容器。
   * **F# 函數式計算專案 (`MultiLanguageInteropShowcase.Logic.FSharp`)**：提供強型別函數式不可變模型、模式匹配與高精度複利現值計算 (`FSharpFinancialService.CalculateCompoundGrowth`)。
   * **VB.NET 商業規則專案 (`MultiLanguageInteropShowcase.Logic.VB`)**：提供企業稅額驗證、大寫貨幣格式化與欄位規則審核 (`VisualBasicOrderService.ValidateAndFormatOrder`)。
   * **C++ 原生運算模組 (`MultiLanguageInteropShowcase.Logic.Cpp`)**：提供底層 SIMD 位元演算與硬體指令極速校驗碼計算 (`CppNativeMathService.ComputeFastChecksum`)，透過 C# `DllImport` 零負擔呼叫。
2. **自動化相依性注入 (Automatic DI)**：AFG 匯出服務自動於 `App.cs` 註冊各語言服務介面，ViewModel 無需關心底層是由何種語言實作。
3. **無縫整合建置 (Seamless Unified Build)**：整個方案由標準 `dotnet build MultiLanguageInteropShowcase.slnx` 一鍵完成跨語言編譯與組裝，保持 **0 Warning, 0 Error**。

---

## 📁 檔案結構 (Directory Structure)

```text
example/MultiLanguageInteropShowcase/
├── MultiLanguageInteropShowcase.afg.json        # [核心 AST] AFG 視覺化設計專案檔
├── README.md                                    # [說明文件] 專案導覽與實機操作手冊
├── screenshots/                                 # [實機截圖]
│   ├── editor_design_view.png                   # AFG 視覺化編輯器真實畫面
│   └── runtime_result.png                       # 桌面端實機運行畫面
├── MultiLanguageInteropShowcase.slnx            # Visual Studio 現代方案檔 (多專案)
└── src/                                         # [匯出專案] 完整跨平台可編譯原始碼
    ├── MultiLanguageInteropShowcase.Shared/     # C# 跨平台核心共用專案
    │   ├── MultiLanguageInteropShowcase.Shared.csproj
    │   ├── App.cs                               # DI 容器註冊 F#、VB 與 C++ 服務
    │   ├── Services/
    │   │   ├── ICppNativeMathService.cs         # C++ 介面
    │   │   └── CppNativeMathServiceNativeBridge.cs # C# DllImport 橋接器
    │   ├── ViewModels/
    │   │   └── MultiLanguageInteropShowcaseViewModel.cs # 協同調用三種語言服務
    │   └── Views/
    │       └── MultiLanguageInteropShowcaseView.cs      # Fluent C# Markup 介面
    ├── MultiLanguageInteropShowcase.Logic.FSharp/ # F# 函數式運算專案
    │   ├── MultiLanguageInteropShowcase.Logic.FSharp.fsproj
    │   └── FSharpFinancialService.fs            # F# 核心邏輯
    ├── MultiLanguageInteropShowcase.Logic.VB/   # VB.NET 商業規則專案
    │   ├── MultiLanguageInteropShowcase.Logic.VB.vbproj
    │   ├── IVisualBasicOrderService.vb
    │   └── VisualBasicOrderService.vb           # VB.NET 核心邏輯
    ├── MultiLanguageInteropShowcase.Logic.Cpp/  # C++ 原生運算模組
    │   ├── CMakeLists.txt                       # CMake 配置
    │   ├── CppNativeMathService.h               # C++ 標頭檔
    │   └── CppNativeMathService.cpp             # C++ 演算法實作
    └── MultiLanguageInteropShowcase.Desktop/    # 桌面端進入點專案
        ├── MultiLanguageInteropShowcase.Desktop.csproj
        └── Program.cs
```

---

## 🧩 UI 元件階層樹 (Component Hierarchy)

| 元件識別碼 (ID) | 控制項類型 | 座標 / 尺寸 | 綁定屬性 / 事件映射 | 說明 |
| :--- | :--- | :--- | :--- | :--- |
| **RootCanvas** | `Canvas` | `640 × 840` | — | 深色背景畫布 (`#18181B`) |
| **headerCard** | `Border` | `(20, 20) 600 × 70` | — | 頂部標題卡片 |
| ├── `txtHeaderTitle` | `TextBlock` | `(20, 12)` | Text: `"多語言交互協同運算展示"` | 主標題 (20pt Bold, `#818CF8`) |
| └── `txtHeaderSubtitle` | `TextBlock` | `(20, 40)` | Text: `"C# Host + F# Logic + VB.NET Logic + C++ Native"` | 副標題 (12pt, `#94A3B8`) |
| **fsharpCard** | `Border` | `(20, 105) 600 × 120`| — | F# 函數式運算卡片 (亮紫邊框 `#6366F1`) |
| ├── `lblF1` / `lblF2` | `TextBlock` | `(20, 15)` / `(20, 42)` | Text: 說明 F# 複利模型與本金參數 | 函數式計算說明標籤 |
| └── `txtFSharpResult` | `TextBlock` | `(20, 78)` | `Text` 綁定 `FSharpResultText` | F# 複利現值計算結果標籤 |
| **vbCard** | `Border` | `(20, 240) 600 × 120`| — | VB.NET 商業規則卡片 (天藍邊框 `#0EA5E9`) |
| ├── `lblV1` / `lblV2` | `TextBlock` | `(20, 15)` / `(20, 42)` | Text: 說明 VB.NET 訂單檢驗規則 | 商業檢驗說明標籤 |
| └── `txtVbResult` | `TextBlock` | `(20, 78)` | `Text` 綁定 `VbResultText` | VB.NET 稅額核可計算結果標籤 |
| **cppCard** | `Border` | `(20, 375) 600 × 120`| — | C++ 原生運算卡片 (琥珀橙邊框 `#F97316`) |
| ├── `lblC1` / `lblC2` | `TextBlock` | `(20, 15)` / `(20, 42)` | Text: 說明 C++ 原生位元運算與迭代 | 原生加速說明標籤 |
| └── `txtCppResult` | `TextBlock` | `(20, 78)` | `Text` 綁定 `CppResultText` | C++ 迭代校驗碼運算結果標籤 |
| **btnRunAll** | `Button` | `(20, 510) 600 × 45` | `Click` 映射 `RunAllCommand` | 一鍵協同運算按鈕 (`#4F46E5`) |
| **summaryCard** | `Border` | `(20, 575) 600 × 245`| — | 多語言分散式架構拓撲與狀態總結面板 |
| └── `txtSummary` | `TextBlock` | `(20, 80) 560 × 90` | Text: 說明 DI 連線與協同運算機制 | 顯示跨語言相依性注入連線狀態 |
| **fnFSharpFinancial** | `LogicFunction` | — | `CalculateCompoundGrowth` (F#) | F# 函數式複利服務元件 |
| **fnVisualBasicOrder**| `LogicFunction` | — | `ValidateAndFormatOrder` (VB) | VB.NET 商業規則驗證服務元件 |
| **fnCppNativeMath** | `LogicFunction` | — | `ComputeFastChecksum` (Cpp) | C++ 原生位元校驗運算服務元件 |

---

## 🚀 如何執行本範例 (How to Run)

### 1. 於 AFG 視覺化設計器開啟
```powershell
dotnet run --project src/AFG.Desktop/AFG.Desktop.csproj "D:\P\CS\Avalonia Form Generator\example\MultiLanguageInteropShowcase\MultiLanguageInteropShowcase.afg.json"
```

### 2. 獨立編譯並啟動實機應用程式
```powershell
dotnet run --project example/MultiLanguageInteropShowcase/src/MultiLanguageInteropShowcase.Desktop/MultiLanguageInteropShowcase.Desktop.csproj
```
