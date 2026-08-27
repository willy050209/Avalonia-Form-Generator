// filepath: src/AFG.Core/Validation/AstValidator.cs
using System.Text.RegularExpressions;

namespace AFG.Core.Validation;

/// <summary>
/// 提供針對 UI AST 模型與 FormDocument 進行語意、命名規範與結構防禦性驗證的純函數驗證器。
/// </summary>
public static partial class AstValidator
{
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex CSharpIdentifierRegex();

    /// <summary>
    /// 驗證整份 FormDocument 結構與設定。
    /// </summary>
    /// <param name="document">要驗證的表單文件。</param>
    /// <returns>包含所有錯誤與警告的驗證結果。</returns>
    /// <exception cref="ArgumentNullException">當 document 為 null 時擲出。</exception>
    public static ValidationResult ValidateDocument(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(document.ViewClassName))
        {
            errors.Add(new("AFG001", "View 類別名稱不可為空。"));
        }
        else if (!IsValidCSharpIdentifier(document.ViewClassName))
        {
            errors.Add(new("AFG002", $"View 類別名稱 '{document.ViewClassName}' 不是合法的 C# 識別碼。"));
        }

        if (string.IsNullOrWhiteSpace(document.ViewModelClassName))
        {
            errors.Add(new("AFG003", "ViewModel 類別名稱不可為空。"));
        }
        else if (!IsValidCSharpIdentifier(document.ViewModelClassName))
        {
            errors.Add(new("AFG004", $"ViewModel 類別名稱 '{document.ViewModelClassName}' 不是合法的 C# 識別碼。"));
        }

        // 驗證表單層級事件
        foreach (var evt in document.Events)
        {
            if (string.IsNullOrWhiteSpace(evt.EventName))
            {
                errors.Add(new("AFG301", "表單事件名稱不得為空。"));
            }

            if (string.IsNullOrWhiteSpace(evt.CommandProperty))
            {
                errors.Add(new("AFG302", "表單映射的 Command 屬性名稱不得為空。"));
            }
            else if (!IsValidCSharpIdentifier(evt.CommandProperty))
            {
                errors.Add(new("AFG303", $"表單 Command 屬性名稱 '{evt.CommandProperty}' 不符合 C# 識別碼規範。"));
            }
        }

        // 驗證 AST 樹
        var nodeErrors = ValidateTree(document.RootNode);
        errors.AddRange(nodeErrors.Items);

        return new ValidationResult(errors.ToImmutableList());
    }

    /// <summary>
    /// 驗證 AST 語意樹中的節點完整性與約束。
    /// </summary>
    /// <param name="root">AST 根節點。</param>
    /// <returns>驗證結果。</returns>
    /// <exception cref="ArgumentNullException">當 root 為 null 時擲出。</exception>
    public static ValidationResult ValidateTree(AstNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var errors = new List<ValidationError>();
        var allNodes = AstTreeOperations.Flatten(root);

        // 1. 檢查 Id 重複性
        var idGroups = allNodes.GroupBy(n => n.Id).Where(g => g.Count() > 1);
        foreach (var group in idGroups)
        {
            errors.Add(new("AFG101", $"發現重複的節點 ID: '{group.Key}'。", group.Key));
        }

        // 2. 檢查各節點細部約束
        foreach (var node in allNodes)
        {
            ValidateSingleNode(node, errors);
        }

        return new ValidationResult(errors.ToImmutableList());
    }

    private static void ValidateSingleNode(AstNode node, List<ValidationError> errors)
    {
        // 檢查 Name 識別碼合法性（若有填寫）
        if (!string.IsNullOrWhiteSpace(node.Name) && !IsValidCSharpIdentifier(node.Name))
        {
            errors.Add(new("AFG102", $"控制項名稱 '{node.Name}' 不是合法的 C# 識別碼。", node.Id));
        }

        // 檢查非容器控制項是否擁有子節點
        if (!node.IsContainer && node.Children.Count > 0)
        {
            errors.Add(new("AFG103", $"控制項類型 '{node.Type}' 不是容器，無法包含子節點。", node.Id));
        }

        // 檢查尺寸合理性
        if (node.Width.HasValue && node.Width.Value < 0)
        {
            errors.Add(new("AFG104", "Width 不得為負數。", node.Id));
        }

        if (node.Height.HasValue && node.Height.Value < 0)
        {
            errors.Add(new("AFG105", "Height 不得為負數。", node.Id));
        }

        // 檢查綁定設定
        foreach (var binding in node.Bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.TargetProperty))
            {
                errors.Add(new("AFG201", "綁定的 TargetProperty 不得為空。", node.Id));
            }
            else if (!ControlBindingCatalog.IsPropertySupported(node.Type, binding.TargetProperty))
            {
                errors.Add(new("AFG204", $"控制項類型 '{node.Type}' 不支援可綁定屬性 '{binding.TargetProperty}'。", node.Id));
            }

            if (string.IsNullOrWhiteSpace(binding.ViewModelProperty))
            {
                errors.Add(new("AFG202", "綁定的 ViewModelProperty 不得為空。", node.Id));
            }
            else if (!IsValidCSharpIdentifier(binding.ViewModelProperty))
            {
                errors.Add(new("AFG203", $"ViewModel 綁定屬性名稱 '{binding.ViewModelProperty}' 不符合 C# 屬性命名規範。", node.Id));
            }

            if (!string.IsNullOrWhiteSpace(binding.TargetProperty) && !string.IsNullOrWhiteSpace(binding.CustomDataType))
            {
                if (!ControlBindingCatalog.IsDataTypeCompatible(binding.TargetProperty, binding.CustomDataType, node.Type))
                {
                    errors.Add(new("AFG205", $"目標屬性 '{binding.TargetProperty}' 與指定型別 '{binding.CustomDataType}' 不相容。", node.Id));
                }
            }
        }

        // 檢查事件映射
        foreach (var evt in node.Events)
        {
            if (string.IsNullOrWhiteSpace(evt.EventName))
            {
                errors.Add(new("AFG301", "事件名稱不得為空。", node.Id));
            }

            if (string.IsNullOrWhiteSpace(evt.CommandProperty))
            {
                errors.Add(new("AFG302", "映射的 Command 屬性名稱不得為空。", node.Id));
            }
            else if (!IsValidCSharpIdentifier(evt.CommandProperty))
            {
                errors.Add(new("AFG303", $"Command 屬性名稱 '{evt.CommandProperty}' 不符合 C# 識別碼規範。", node.Id));
            }
        }
    }

    /// <summary>
    /// 檢查字串是否為合法的 C# 識別碼。
    /// </summary>
    public static bool IsValidCSharpIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        return CSharpIdentifierRegex().IsMatch(identifier);
    }
}
