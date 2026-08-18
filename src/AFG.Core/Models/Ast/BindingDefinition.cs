// filepath: src/AFG.Core/Models/Ast/BindingDefinition.cs
namespace AFG.Core.Models.Ast;

/// <summary>
/// 表示 View 控制項屬性與 ViewModel 屬性之間的綁定定義。
/// </summary>
/// <param name="TargetProperty">View 端控制項屬性名稱（例如 Text, IsEnabled, SelectedItem）。</param>
/// <param name="ViewModelProperty">ViewModel 端的屬性名稱（例如 Username, IsBusy）。</param>
/// <param name="Mode">綁定模式（預設、單向、雙向等）。</param>
/// <param name="CustomDataType">自訂 C# 資料型別（例如 int, decimal, ObservableCollection&lt;T&gt; 等，若為 null 則自動推斷）。</param>
/// <param name="Converter">轉換器型別名稱或標籤（可為 null）。</param>
/// <param name="StringFormat">字串格式化規則（例如 "{0:C}"）。</param>
/// <param name="FallbackValue">綁定失敗時的預設值。</param>
public sealed record BindingDefinition(
    string TargetProperty,
    string ViewModelProperty,
    BindingMode Mode = BindingMode.Default,
    string? CustomDataType = null,
    string? Converter = null,
    string? StringFormat = null,
    string? FallbackValue = null)
{
    public BindingDefinition() : this(string.Empty, string.Empty) { }
}

/// <summary>
/// 表示注入至 ViewModel 的服務相依性定義。
/// </summary>
/// <param name="InterfaceName">服務介面名稱（例如 IOrderService, IUserService）。</param>
/// <param name="ImplementationName">服務實作類別名稱（例如 OrderService, UserService）。</param>
/// <param name="Lifetime">生命週期（Singleton / Transient / Scoped）。</param>
public sealed record ServiceDependencyDefinition(
    string InterfaceName,
    string? ImplementationName = null,
    string Lifetime = "Singleton")
{
    public ServiceDependencyDefinition() : this(string.Empty) { }
}

/// <summary>
/// 表示 View 控制項事件至 ViewModel RelayCommand 的映射定義（支援同步與非同步模式）。
/// </summary>
/// <param name="EventName">事件名稱（例如 Click, SelectionChanged, Tapped）。</param>
/// <param name="CommandProperty">ViewModel 端 Command 屬性名稱（例如 SubmitCommand）。</param>
/// <param name="CommandParameterProperty">Command 傳遞的參數屬性路徑（可為 null）。</param>
/// <param name="IsAsync">是否生成非同步命令 Task 方法（預設為 true，符合現代非同步開發標準）。</param>
public sealed record EventMappingDefinition(
    string EventName,
    string CommandProperty,
    string? CommandParameterProperty = null,
    bool IsAsync = true)
{
    public EventMappingDefinition() : this(string.Empty, string.Empty, null, true) { }
}
