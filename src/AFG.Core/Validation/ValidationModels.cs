// filepath: src/AFG.Core/Validation/ValidationModels.cs
namespace AFG.Core.Validation;

/// <summary>
/// 驗證問題嚴重層級。
/// </summary>
public enum ValidationSeverity
{
    Warning,
    Error
}

/// <summary>
/// 表示單一 AST 語意或結構驗證錯誤或警告項目。
/// </summary>
public sealed record ValidationError(
    string ErrorCode,
    string Message,
    string? NodeId = null,
    ValidationSeverity Severity = ValidationSeverity.Error);

/// <summary>
/// AST 驗證結果封裝。
/// </summary>
public sealed record ValidationResult(ImmutableList<ValidationError> Items)
{
    public static ValidationResult Success => new([]);

    public bool IsValid => Items.All(i => i.Severity != ValidationSeverity.Error);

    public ImmutableList<ValidationError> Errors => Items.Where(i => i.Severity == ValidationSeverity.Error).ToImmutableList();
    public ImmutableList<ValidationError> Warnings => Items.Where(i => i.Severity == ValidationSeverity.Warning).ToImmutableList();
}
