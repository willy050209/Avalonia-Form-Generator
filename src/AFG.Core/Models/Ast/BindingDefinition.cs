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
/// 表示事件或命令之單一參數定義（支援名稱、C# 型別、常數值或 ViewModel 綁定路徑）。
/// </summary>
/// <param name="Name">參數變數名稱（例如 sender, e, id, keyword）。</param>
/// <param name="Type">C# 型別（例如 object?, RoutedEventArgs, int, string）。</param>
/// <param name="ValueOrPath">綁定路徑或常數值（若為 null 則代表由事件原生提供或無需額外傳入）。</param>
/// <param name="IsConstant">是否為靜態常數值。</param>
public sealed record EventParameterDefinition(
    string Name,
    string Type = "object?",
    string? ValueOrPath = null,
    bool IsConstant = false)
{
    public EventParameterDefinition() : this("parameter", "object?", null, false) { }
}

/// <summary>
/// 表示 View 控制項事件至 ViewModel RelayCommand 的映射定義（支援多參數傳遞、同步/非同步與 CommandParameter）。
/// </summary>
/// <param name="EventName">事件名稱（例如 Click, SelectionChanged, Tapped）。</param>
/// <param name="CommandProperty">ViewModel 端 Command 屬性名稱（例如 SubmitCommand, DeleteItemCommand）。</param>
/// <param name="CommandParameterProperty">單一參數之傳遞屬性路徑或常數值（向後相容欄位）。</param>
/// <param name="IsAsync">是否生成非同步命令 Task 方法（預設為 true）。</param>
/// <param name="ParameterType">單一參數之 C# 型別（向後相容欄位）。</param>
/// <param name="IsConstantParameter">單一參數是否為靜態常數值（向後相容欄位）。</param>
/// <param name="Parameters">多參數配置清單（若非 null 且有元素則優先採用）。</param>
public sealed record EventMappingDefinition(
    string EventName,
    string CommandProperty,
    string? CommandParameterProperty = null,
    bool IsAsync = true,
    string? ParameterType = null,
    bool IsConstantParameter = false,
    ImmutableList<EventParameterDefinition>? Parameters = null)
{
    public EventMappingDefinition() : this(string.Empty, string.Empty, null, true, null, false, []) { }

    /// <summary>
    /// 取得所有有效的參數定義清單（若 Parameters 為空且存在舊版單一參數設定，自動向後相容轉譯）。
    /// </summary>
    public ImmutableList<EventParameterDefinition> GetEffectiveParameters()
    {
        if (Parameters is { Count: > 0 })
        {
            return Parameters;
        }

        if (!string.IsNullOrWhiteSpace(ParameterType) || !string.IsNullOrWhiteSpace(CommandParameterProperty))
        {
            var paramType = !string.IsNullOrWhiteSpace(ParameterType) ? ParameterType.Trim() : "object?";
            var paramName = ControlEventCatalog.GetDefaultParameterName(paramType, !string.IsNullOrWhiteSpace(CommandParameterProperty) ? CommandParameterProperty : null);
            return [new EventParameterDefinition(paramName, paramType, CommandParameterProperty, IsConstantParameter)];
        }

        return [];
    }
}
