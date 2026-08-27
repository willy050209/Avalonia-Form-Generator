# UI AST Schema 規格與資料模型 (AST Metadata Schema)

本文檔定義 **Avalonia Form Generator (AFG)** 中介語意樹 (UI Metadata AST) 的 JSON 結構規範、節點定義、多表單專案定義與儲存格式 (`.afg.json`)。

---

## 1. 根文件綱要 (FormDocument Schema)

`.afg.json` 為 AFG 專案儲存單一表單的中介格式，定義了表單的全域設定與根節點：

```json
{
  "schemaVersion": "1.0",
  "projectName": "InventoryApp",
  "rootNamespace": "InventoryApp.Views",
  "viewClassName": "UserFormView",
  "viewModelClassName": "UserFormViewModel",
  "title": "使用者表單",
  "backgroundColor": "#FFFFFF",
  "canvasWidth": 390.0,
  "canvasHeight": 844.0,
  "minWidth": 320.0,
  "minHeight": 480.0,
  "maxWidth": null,
  "maxHeight": null,
  "windowStartupLocation": "CenterScreen",
  "windowState": "Normal",
  "canResize": true,
  "topmost": false,
  "showInTaskbar": true,
  "icon": "Assets/app_icon.ico",
  "systemDecorations": "Full",
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
| `projectName` | `string?` | `null` | 匯出方案與專案名稱（若未指定則自動由 `viewClassName` 推斷，例如 `MainFormApp`） |
| `rootNamespace` | `string` | `"GeneratedApp.Views"` | 生成 C# 類別的命名空間 |
| `viewClassName` | `string` | `"MainFormView"` | 生成的 View 類別名稱 |
| `viewModelClassName` | `string` | `"MainFormViewModel"` | 生成的 ViewModel 類別名稱 |
| `title` | `string` | `"Avalonia Form"` | 視窗/表單標題 |
| `backgroundColor` | `string?` | `null` | 表單與視窗背景色彩代碼（例如 `#FFFFFF`, `#1E1E2E`） |
| `canvasWidth` | `double` | `800.0` | 設計畫布寬度 (px) |
| `canvasHeight` | `double` | `600.0` | 設計畫布高度 (px) |
| `minWidth` | `double?` | `null` | 視窗最小寬度限制 (px) |
| `minHeight` | `double?` | `null` | 視窗最小高度限制 (px) |
| `maxWidth` | `double?` | `null` | 視窗最大寬度限制 (px) |
| `maxHeight` | `double?` | `null` | 視窗最大高度限制 (px) |
| `windowStartupLocation` | `WindowStartupLocation` | `CenterScreen` | 視窗啟動定位位置 (`CenterScreen`, `CenterOwner`, `Manual`) |
| `windowState` | `WindowState` | `Normal` | 視窗初始顯示狀態 (`Normal`, `Maximized`, `Minimized`, `FullScreen`) |
| `canResize` | `bool` | `true` | 是否允許使用者拉伸縮放視窗大小 |
| `topmost` | `bool` | `false` | 是否視窗永遠置頂 |
| `showInTaskbar` | `bool` | `true` | 是否在作業系統工作列中顯示圖示與標籤 |
| `icon` | `string?` | `null` | 視窗圖示路徑（支援本機檔案或 Assets 相對資源） |
| `systemDecorations` | `SystemDecorations` | `Full` | 系統標題列與邊框裝飾樣式 (`Full`, `None`, `BorderOnly`) |
| `enableDependencyInjection` | `bool` | `true` | 是否在此表單啟用相依性注入架構配置 |
| `useCompiledBindings` | `bool` | `true` | 是否生成強型別編譯綁定 (Compiled / Lambda Bindings) 語法（預設為 true） |
| `injectedServices` | `Array<ServiceDependencyDefinition>` | `[]` | 注入至此 ViewModel 的自訂服務相依性清單 |
| `events` | `Array<EventMappingDefinition>` | `[]` | 表單與視窗層級全域生命週期與互動事件掛載清單（例如 `Loaded`, `PointerPressed`, `SizeChanged`） |
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
  "borderBrush": "#3B82F6",
  "borderThickness": {
    "left": 2.0,
    "top": 2.0,
    "right": 2.0,
    "bottom": 2.0
  },
  "cornerRadius": {
    "topLeft": 8.0,
    "topRight": 8.0,
    "bottomRight": 8.0,
    "bottomLeft": 8.0
  },
  "boxShadow": {
    "offsetX": 0.0,
    "offsetY": 4.0,
    "blur": 12.0,
    "spread": 0.0,
    "color": "#40000000",
    "isInset": false
  },
  "canvasLeft": 100.0,
  "canvasTop": 80.0,
  "gridRow": 0,
  "gridColumn": 0,
  "gridRowSpan": 1,
  "gridColumnSpan": 1,
  "content": "登入",
  "source": "assets/logo.png",
  "useRelativePath": true,
  "initBitmap": false,
  "bitmapBackgroundColor": "#F0F0F0",
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

- **基本控制項**：`Button`, `TextBox`, `TextBlock`, `CheckBox`, `RadioButton`, `ComboBox`, `ListBox`, `DatePicker`, `TimePicker`, `Slider`, `ProgressBar`, `DataGrid`, `Image`, `PictureBox`, `Border`。
- **多媒體控制項**：`MediaPlayer`（支援本地與雲端 URL 串流資源、播放、暫停、停止、音量、進度與當前影格截圖 `CaptureFrame` 轉 `Bitmap`）。
- **佈局容器**：`Canvas`, `Grid`, `StackPanel`, `DockPanel`, `WrapPanel`, `ScrollViewer`。
- **除錯與日誌元件**：DebugConsole（內建深色主控台外觀、Clear 按鈕與日誌 ItemsSource 綁定）
- **對話方塊元件**：`OpenFileDialog`, `SaveFileDialog`, `MessageBox`。
- **不可視元件與通訊服務**：`DispatcherTimer`, `BackgroundWorker`, `BluetoothClient`, `SerialPortService`。

---

## 4. 資料綁定與事件映射結構

### 4.1 `BindingDefinition`
- `targetProperty`: View 端屬性（如 `Text`, `IsEnabled`, `IsChecked`, `Value`, `Width`, `Height`, `Opacity`, `Source`, `Stretch`, `Background`, `Foreground`）。
- `viewModelProperty`: ViewModel 端屬性名稱（如 `Username`, `CanSubmit`, `TotalAmount`, `ItemsList`, `UserProfileImage`）。
- `customDataType`: 自訂 C# 型別（如 `string`, `int`, `decimal`, `bool`, `Avalonia.Media.IImage`, `Avalonia.Media.Stretch`, `DateTime?`, `ObservableCollection<string>`）。若未指定則自動根據 TargetProperty 推斷。
- `mode`: `default` | `oneWay` | `twoWay` | `oneTime` | `oneWayToSource`。

### 4.2 `EventMappingDefinition`、`EventParameterDefinition` 與專屬事件目錄 (`ControlEventCatalog`)

#### `EventParameterDefinition` 參數定義
```csharp
public sealed record EventParameterDefinition(
    string Name,                     // 參數識別名稱（如 "sender", "e", "filePath", "result"）
    string Type = "object?",         // C# 型別（如 "object?", "RoutedEventArgs", "string?", "bool?"）
    string? ValueOrPath = null,      // 傳遞常數值或 ViewModel 屬性路徑（若為 null 則傳遞原生事件參數）
    bool IsConstant = false          // 是否為常數字串
);
```

#### 控制項專屬事件與預設參數清單
每個控制項僅提供其專屬支援的事件清單與對應的專屬 EventArgs 型別，防止跨事件選取無關的參數型別：
- **`Button`**：`Click`, `Tapped`, `DoubleTapped`, `PointerPressed`, `PointerReleased`, `KeyDown`, `KeyUp`（預設包含 `(sender, object?)` 與 `(e, RoutedEventArgs)`）
- **`TextBox`**：`TextChanged`, `KeyDown`, `KeyUp`, `GotFocus`, `LostFocus`, `PointerPressed`（預設包含 `(sender, object?)` 與 `(e, TextChangedEventArgs)` 或專屬事件參數）
- **`TextBlock`**：`Tapped`, `DoubleTapped`, `PointerPressed`, `PointerReleased`（預設包含 `(sender, object?)` 與 `(e, RoutedEventArgs)`）
- **`CheckBox` / `RadioButton`**：`IsCheckedChanged`, `Checked`, `Unchecked`, `Click`（預設包含 `(sender, object?)` 與 `(e, RoutedEventArgs)`）
- **`ComboBox`**：`SelectionChanged`, `DropDownOpened`, `DropDownClosed`（預設包含 `(sender, object?)` 與 `(e, SelectionChangedEventArgs)`）
- **`ListBox` / `DataGrid`**：`SelectionChanged`, `DoubleTapped`, `CellEditEnded`
- **`DatePicker` / `TimePicker`**：`SelectedDateChanged` / `SelectedTimeChanged`
- **`Slider` / `ProgressBar`**：`ValueChanged`（預設包含 `(sender, object?)` 與 `(e, RangeBaseValueChangedEventArgs)`）
- **`ScrollViewer`**：`ScrollChanged`, `PointerPressed`, `PointerReleased`
- **`PictureBox`** (圖片方塊 / Image)：`Click`, `DoubleClick`, `Tapped`, `DoubleTapped`, `PointerPressed`, `PointerReleased`, `LoadCompleted`, `SizeModeChanged`
- **`Border` / 佈局容器**：`PointerPressed`, `PointerReleased`, `Tapped`, `DoubleTapped`

#### 除錯主控台專屬事件
- **DebugConsole** (內嵌日誌主控台)：
  - Cleared：日誌清除回呼事件，預設帶入 (sender, object?)。
  - Tapped / PointerPressed：點擊日誌面板回呼。

#### 對話方塊元件專屬事件
- **`OpenFileDialog`** (開啟檔案對話方塊)：
  - `FileOk`：檔案選擇確認回呼（預設傳入 `(sender, object?)` 與 `(filePath, string?)`）。
- **`SaveFileDialog`** (儲存檔案對話方塊)：
  - `FileOk`：儲存路徑確認回呼（預設傳入 `(sender, object?)` 與 `(filePath, string?)`）。
- **`MessageBox`** (訊息方塊)：
  - `Confirmed`：對話框按鈕確認回呼（預設傳入 `(sender, object?)` 與 `(result, bool?)`）。

#### 不可視元件與通訊硬體專屬回呼 (Callbacks)
- **`DispatcherTimer`** (計時器)：
  - `Tick`：定時觸發回呼（預設傳入 `(sender, object?)` 與 `(e, EventArgs)`）。
- **`BackgroundWorker`** (背景工作執行緒)：
  - `DoWork`：背景工作執行回呼（預設傳入 `(sender, object?)` 與 `(e, DoWorkEventArgs)`）。
  - `ProgressChanged`：工作進度回報回呼（預設傳入 `(sender, object?)` 與 `(e, ProgressChangedEventArgs)`）。
  - `RunWorkerCompleted`：背景作業完成回呼（預設傳入 `(sender, object?)` 與 `(e, RunWorkerCompletedEventArgs)`）。
- **`BluetoothClient`** (跨平台低功耗藍牙 BLE)：
  - `DeviceDiscovered`：發現周邊裝置回呼。
  - `Connected`：連線成功建立回呼。
  - `Disconnected`：連線中斷回呼。
  - `DataReceived`：接收到藍牙特徵值傳輸資料回呼（預設傳入 `(sender, object?)` 與 `(data, string)`）。
- **`SerialPortService`** (序列埠 RS-232 / UART)：
  - `DataReceived`：收到序列埠串流資料回呼（預設傳入 `(sender, object?)` 與 `(data, string)`）。
  - `ErrorReceived`：序列埠通訊錯誤回呼。
  - `PinChanged`：Pin 狀態訊號變更回呼。

- `commandProperty`: ViewModel 端的 Command 屬性名稱（如 `SubmitCommand`, `DataReceivedCommand`, `FileOpenedCommand`）。
- `parameters`: 多參數配置清單。生成器會自動將多參數包裝為可空的 `ValueTuple`，並在 View 端透過 `ExecuteCommandWithArgs` 或 ViewModel 建構子精準傳遞。
- `isAsync`: `bool`（預設 `true`）。指定是否生成非同步 `async Task ...Async()` 方法或同步 `void ...()` 方法。不可視元件與對話方塊事件將在 ViewModel 建構子內自動訂閱並調用對應之 RelayCommand。
