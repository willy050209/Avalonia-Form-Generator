// filepath: src/AFG.Core/Models/Ast/BindingDefinition.cs
namespace AFG.Core.Models.Ast;

/// <summary>
/// 表示 View 控制項屬性與 ViewModel 屬性之間的綁定定義。
/// </summary>
/// <param name="TargetProperty">View 端控制項屬性名稱（例如 Text, IsEnabled, SelectedItem）。</param>
/// <param name="ViewModelProperty">ViewModel 端的屬性名稱（例如 Username, IsBusy）。</param>
/// <param name="Mode">綁定模式（預設、單向、雙向等）。</param>
/// <param name="Converter">轉換器型別名稱或標籤（可為 null）。</param>
/// <param name="StringFormat">字串格式化規則（例如 "{0:C}"）。</param>
/// <param name="FallbackValue">綁定失敗時的預設值。</param>
public sealed record BindingDefinition(
    string TargetProperty,
    string ViewModelProperty,
    BindingMode Mode = BindingMode.Default,
    string? Converter = null,
    string? StringFormat = null,
    string? FallbackValue = null)
{
    public BindingDefinition() : this(string.Empty, string.Empty) { }
}

/// <summary>
/// 表示 View 控制項事件至 ViewModel RelayCommand 的映射定義。
/// </summary>
/// <param name="EventName">事件名稱（例如 Click, SelectionChanged, Tapped）。</param>
/// <param name="CommandProperty">ViewModel 端 Command 屬性名稱（例如 SubmitCommand）。</param>
/// <param name="CommandParameterProperty">Command 傳遞的參數屬性路徑（可為 null）。</param>
public sealed record EventMappingDefinition(
    string EventName,
    string CommandProperty,
    string? CommandParameterProperty = null)
{
    public EventMappingDefinition() : this(string.Empty, string.Empty) { }
}
