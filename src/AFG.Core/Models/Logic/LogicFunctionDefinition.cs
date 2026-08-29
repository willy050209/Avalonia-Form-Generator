// filepath: src/AFG.Core/Models/Logic/LogicFunctionDefinition.cs
using System;
using System.Collections.Immutable;
using AFG.Core.Enums;

namespace AFG.Core.Models.Logic;

/// <summary>
/// 表示獨立業務邏輯函數之完整結構定義。
/// </summary>
public sealed record LogicFunctionDefinition
{
    /// <summary>
    /// 唯一識別碼。
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 函數/方法名稱（例如 "CalculateTotal", "ValidateOrder", "ProcessPayment"）。
    /// </summary>
    public string Name { get; init; } = "Execute";

    /// <summary>
    /// 函數傳回型態（例如 "void", "int", "string", "bool", "double", "decimal", "byte[]" 或自訂類別）。
    /// 若為非同步函數，生成器將自動封裝為 Task, Task&lt;T&gt;, Async&lt;'T&gt; 或 Task(Of T)。
    /// </summary>
    public string ReturnType { get; init; } = "void";

    /// <summary>
    /// 是否採用非同步模式（async/await）。
    /// </summary>
    public bool IsAsync { get; init; }

    /// <summary>
    /// 函數指定之目標程式語言（選填，若未指定則繼承所屬 LogicService 之語言）。
    /// </summary>
    public TargetLanguage? Language { get; init; }

    /// <summary>
    /// 函數說明註解（將生成 XML Doc 或註解）。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 自訂實作邏輯程式碼區塊（選填，若為空則產出標準 stub 範本）。
    /// </summary>
    public string? CustomImplementation { get; init; }

    /// <summary>
    /// 函數參數清單。
    /// </summary>
    public ImmutableList<FunctionParameter> Parameters { get; init; } = [];
}
