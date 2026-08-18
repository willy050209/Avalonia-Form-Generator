# UI AST Schema 規格與資料模型 (AST Metadata Schema)

本文檔定義 **Avalonia Form Generator (AFG)** 中介語意樹 (UI Metadata AST) 的 JSON 結構規範、節點定義與儲存格式 (`.afg.json`)。

---

## 1. 根文件綱要 (FormDocument Schema)

`.afg.json` 為 AFG 專案儲存的中介格式，定義了表單的全域設定與根節點：

```json
{
  "schemaVersion": "1.0",
  "rootNamespace": "GeneratedApp.Views",
  "viewClassName": "UserFormView",
  "viewModelClassName": "UserFormViewModel",
  "title": "使用者表單",
  "canvasWidth": 390.0,
  "canvasHeight": 844.0,
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
| `rootNode` | `AstNode` | *(Canvas 根節點)* | 頂層容器節點 |

---

## 2. 節點綱要 (AstNode Schema)

每個 UI 控制項或容器皆表示為一個不可變的 `AstNode` 結構：

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

---

## 3. 資料綁定與事件映射結構

### 3.1 `BindingDefinition`
- `targetProperty`: View 端屬性（如 `Text`, `IsEnabled`, `IsChecked`, `Value`, `Width`, `Height`, `Opacity`）。
- `viewModelProperty`: ViewModel 端屬性名稱（如 `Username`, `CanSubmit`）。
- `mode`: `default` | `oneWay` | `twoWay` | `oneTime` | `oneWayToSource`。

### 3.2 `EventMappingDefinition`
- `eventName`: View 事件名稱（如 `Click`, `Tapped`, `SelectionChanged`）。
- `commandProperty`: ViewModel 端的 Command 屬性名稱（如 `SubmitCommand`）。
- `isAsync`: `bool`（預設 `true`）。指定是否生成非同步 `async Task ...Async()` 方法或同步 `void ...()` 方法。
