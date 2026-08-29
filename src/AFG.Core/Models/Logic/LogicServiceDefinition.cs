// filepath: src/AFG.Core/Models/Logic/LogicServiceDefinition.cs
using System;
using System.Collections.Immutable;
using AFG.Core.Enums;

namespace AFG.Core.Models.Logic;

/// <summary>
/// 表示包含多個邏輯函數之獨立業務邏輯服務/模組定義。
/// 完全與 View / ViewModel 解耦，可作為獨立 Class Library 或透過 DI 依賴注入使用。
/// </summary>
public sealed record LogicServiceDefinition
{
    /// <summary>
    /// 唯一識別碼。
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 服務/類別/模組名稱（例如 "OrderCalculationService", "PaymentProcessor", "CryptoEngine"）。
    /// </summary>
    public string ServiceName { get; init; } = "LogicService";

    /// <summary>
    /// 介面名稱（自動由 ServiceName 衍生，例如 "IOrderCalculationService"）。
    /// </summary>
    public string InterfaceName => ServiceName.StartsWith('I') && ServiceName.Length > 1 && char.IsUpper(ServiceName[1])
        ? ServiceName
        : $"I{ServiceName}";

    /// <summary>
    /// 所屬命名空間（例如 "MyApp.Services", "Finance.Logic"）。
    /// </summary>
    public string Namespace { get; init; } = "App.Services";

    /// <summary>
    /// 目標程式語言（C#, F#, VB, C++）。
    /// </summary>
    public TargetLanguage Language { get; init; } = TargetLanguage.CSharp;

    /// <summary>
    /// 服務整體說明註解。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 包含於此服務之邏輯函數清單。
    /// </summary>
    public ImmutableList<LogicFunctionDefinition> Functions { get; init; } = [];
}
