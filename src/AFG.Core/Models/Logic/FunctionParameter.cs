// filepath: src/AFG.Core/Models/Logic/FunctionParameter.cs
namespace AFG.Core.Models.Logic;

/// <summary>
/// 表示邏輯函數的單一參數定義。
/// </summary>
public sealed record FunctionParameter
{
    /// <summary>
    /// 參數名稱（例如 "amount", "orderId"）。
    /// </summary>
    public string Name { get; init; } = "param";

    /// <summary>
    /// 參數型態（例如 "string", "int", "double", "decimal", "bool", "object" 或自訂型別）。
    /// </summary>
    public string Type { get; init; } = "string";

    /// <summary>
    /// 參數預設值（選填，例如 "0", "\"\"", "null"）。
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// 參數摘要說明註解。
    /// </summary>
    public string? Description { get; init; }
}
