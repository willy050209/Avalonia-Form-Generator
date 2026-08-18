# UI AST Schema 規格與資料模型 (AST Metadata Schema)

本文檔定義 **Avalonia Form Generator (AFG)** 中介語意樹 (UI Metadata AST) 的 JSON 結構規範、節點定義、多表單專案定義與儲存格式 (`.afg.json`)。

---

## 1. 根文件綱要 (FormDocument Schema)

`.afg.json` 為 AFG 專案儲存單一表單的中介格式，定義了表單的全域設定與根節點：

```json
{
  "schemaVersion": "1.0",
  "rootNamespace": "GeneratedApp.Views",
  "viewClassName": "UserFormView",
  "viewModelClassName": "UserFormViewModel",
  "title": "使用者表單",
  "canvasWidth": 390.0,
  "canvasHeight": 844.0,
  "enableDependencyInjection": true,
  "useCompiledBindings": false,
  "injectedServices": [
    {
      "interfaceName": "IUserService",
      "implementationName": "UserService"
    }
  ],
  "rootNode": { ... }
}
```

### 欄位定義

| 屬性名稱 | 型別 | 預設值 | 說明 |
| :--- | :--- | :--- | :--- |
| `schemaVersion` | `string` | `"1.0"` | AFG Schema 版本號 |
| `rootNamespace` | `string` | `"GeneratedApp.Views"` | 生成 C# 類別的命名空間 |
| `viewClassName` | `string` | `"MainFormView"` | 生成的 View 類別名稱 |
| `viewModelClassName` | `string` | `"MainFormViewModel"` | 生成的 ViewModel 類別名稱 |
| `title` | `string` | `"Avalonia Form"` | 視窗/表單標題 |
| `canvasWidth` | `double` | `800.0` | 設計畫布寬度 (px) |
| `canvasHeight` | `double` | `600.0` | 設計畫布高度 (px) |
| `enableDependencyInjection` | `bool` | `true` | 是否在此表單啟用相依性注入架構配置 |
| `useCompiledBindings` | `bool` | `false` | 是否生成強型別編譯綁定 (Compiled / Lambda Bindings) 語法 |
| `injectedServices` | `Array<ServiceDependencyDefinition>` | `[]` | 注入至此 ViewModel 的自訂服務相依性清單 |
| `rootNode` | `AstNode` | *(Canvas 根節點)* | 頂層容器節點 |

---

## 2. 多表單專案綱要 (FormProjectDefinition Schema)

多表單專案聚合多個 `FormDocument`，並在匯出時建立 `INavigationService` 支援跨表單導航：

```csharp
public sealed record FormProjectDefinition
{
    public string ProjectName { get; init; } = "MainFormApp";
    public string RootNamespace { get; init; } = "MainFormApp";
    public string Title { get; init; } = "Avalonia Application";
    public string InitialFormName { get; init; } = "MainFormView";
    public ImmutableList<FormDocument> Documents { get; init; } = [];
}
```

---

## 3. 節點綱要 (AstNode Schema)

每個 UI 控制項、容器或不可視元件皆表示為一個不可變的 `AstNode` 結構：

```json
{
  "id": "e9b20723a1b44c688849b2053158cda8",
  "name": "loginButton",
  "type": "button",
  "width": 120.0,
  "height": 35.0,
  "margin": {
    "left": 10.0,
    "top": 5.0,
    "right": 10.0,
    "bottom": 5.0
  },
  "horizontalAlignment": "center",
  "verticalAlignment": "center",
  "opacity": 1.0,
  "isEnabled": true,
  "isVisible": true,
  "background": "#1E293B",
  "foreground": "#FFFFFF",
  "canvasLeft": 100.0,
  "canvasTop": 80.0,
  "gridRow": 0,
  "gridColumn": 0,
  "gridRowSpan": 1,
  "gridColumnSpan": 1,
  "content": "登入",
  "bindings": [
    {
      "targetProperty": "IsEnabled",
      "viewModelProperty": "CanLogin",
      "customDataType": "bool",
      "mode": "oneWay"
    }
  ],
  "events": [
    {
      "eventName": "Click",
      "commandProperty": "LoginCommand",
      "isAsync": true
    }
  ],
  "children": []
}
```

### 支援之控制項類型 (`ControlType`)

- **基本控制項**：`Button`, `TextBox`, `TextBlock`, `CheckBox`, `RadioButton`, `ComboBox`, `ListBox`, `DatePicker`, `TimePicker`, `Slider`, `ProgressBar`, `DataGrid`, `Image`, `Border`。
- **佈局容器**：`Canvas`, `Grid`, `StackPanel`, `DockPanel`, `WrapPanel`, `ScrollViewer`。
- **不可視元件與通訊服務**：`DispatcherTimer`, `BackgroundWorker`, `BluetoothClient`, `SerialPortService`。

---

## 4. 資料綁定與事件映射結構

### 4.1 `BindingDefinition`
- `targetProperty`: View 端屬性（如 `Text`, `IsEnabled`, `IsChecked`, `Value`, `Width`, `Height`, `Opacity`, `Background`, `Foreground`）。
- `viewModelProperty`: ViewModel 端屬性名稱（如 `Username`, `CanSubmit`, `TotalAmount`, `ItemsList`）。
- `customDataType`: 自訂 C# 型別（如 `string`, `int`, `decimal`, `bool`, `DateTime?`, `ObservableCollection<string>`）。若未指定則自動根據 TargetProperty 推斷。
- `mode`: `default` | `oneWay` | `twoWay` | `oneTime` | `oneWayToSource`。

### 4.2 `EventMappingDefinition`
- `eventName`: View 事件名稱（如 `Click`, `Tapped`, `SelectionChanged`）。
- `commandProperty`: ViewModel 端的 Command 屬性名稱（如 `SubmitCommand`）。
- `isAsync`: `bool`（預設 `true`）。指定是否生成非同步 `async Task ...Async()` 方法或同步 `void ...()` 方法。
