// filepath: src/AFG.Shared/Services/CSharpSyntaxColorizer.cs
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AFG.Shared.Services;

/// <summary>
/// 提供高效、支援 C#、F# 與 Visual Basic 之語法著色器，將程式碼轉譯為具備 VS Dark+ 現代配色之 Inlines。
/// </summary>
public static class CSharpSyntaxColorizer
{
    private static readonly IBrush KeywordBrush = new SolidColorBrush(Color.Parse("#569CD6"));
    private static readonly IBrush TypeBrush = new SolidColorBrush(Color.Parse("#4EC9B0"));
    private static readonly IBrush StringBrush = new SolidColorBrush(Color.Parse("#CE9178"));
    private static readonly IBrush NumberBrush = new SolidColorBrush(Color.Parse("#B5CEA8"));
    private static readonly IBrush CommentBrush = new SolidColorBrush(Color.Parse("#6A9955"));
    private static readonly IBrush AttributeBrush = new SolidColorBrush(Color.Parse("#DCDCAA"));
    private static readonly IBrush MethodBrush = new SolidColorBrush(Color.Parse("#DCDCAA"));
    private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#D4D4D4"));

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // C#
        "using", "namespace", "public", "partial", "class", "void", "async", "await",
        "nameof", "new", "private", "get", "set", "return", "true", "false", "null",
        "sealed", "static", "readonly", "override", "if", "else", "switch", "case", "default",
        
        // F#
        "open", "type", "let", "mutable", "inherit", "member", "do", "match", "with",
        "fun", "abstract", "interface", "module", "and", "as", "this", "base", "ignore",
        "unit", "rec", "val", "of",
        
        // VB
        "Imports", "Namespace", "Public", "Private", "Class", "Inherits", "Implements",
        "Interface", "Dim", "As", "New", "Sub", "Function", "End", "Module", "Shared",
        "Property", "Get", "Set", "Overrides", "Overridable", "Option", "Explicit",
        "Strict", "On", "Off", "WithEvents", "AddressOf", "DirectCast", "TypeOf", "Is",
        "IsNot", "True", "False", "Nothing", "Me", "MyBase"
    };

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string", "int", "double", "bool", "float", "decimal", "object", "long", "short", "byte",
        "UserControl", "ObservableObject", "BindingMode", "Thickness", "Brush",
        "HorizontalAlignment", "VerticalAlignment", "Dock", "Orientation", "Task",
        "Button", "TextBox", "TextBlock", "Grid", "StackPanel", "Canvas", "Border",
        "DockPanel", "ScrollViewer", "CheckBox", "RadioButton", "ComboBox", "ProgressBar", "Slider",
        "IRelayCommand", "IAsyncRelayCommand", "RelayCommand", "AsyncRelayCommand", "IServiceProvider"
    };

    private static readonly Regex TokenRegex = new(
        @"(//.*?$|'.*?$|\(\*.*?\*\))|(""(?:\\.|""""|[^""\\])*"")|(\b[0-9]+(?:\.[0-9]+)?\b)|(\b[a-zA-Z_][a-zA-Z0-9_]*\b)|(\.)|([^""\s0-9a-zA-Z_\./]+)|(\s+)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// 將程式碼解析並加入至 InlineCollection 中。
    /// </summary>
    public static void PopulateInlines(InlineCollection inlines, string? code)
    {
        ArgumentNullException.ThrowIfNull(inlines);
        inlines.Clear();

        if (string.IsNullOrEmpty(code))
        {
            return;
        }

        var matches = TokenRegex.Matches(code);
        var isAfterDot = false;

        foreach (Match match in matches)
        {
            var text = match.Value;

            if (match.Groups[1].Success) // 註解 (C# //, VB ', F# (* *))
            {
                inlines.Add(new Run(text) { Foreground = CommentBrush });
                isAfterDot = false;
            }
            else if (match.Groups[2].Success) // 字串
            {
                inlines.Add(new Run(text) { Foreground = StringBrush });
                isAfterDot = false;
            }
            else if (match.Groups[3].Success) // 數字
            {
                inlines.Add(new Run(text) { Foreground = NumberBrush });
                isAfterDot = false;
            }
            else if (match.Groups[4].Success) // 識別碼 / 關鍵字
            {
                if (isAfterDot)
                {
                    inlines.Add(new Run(text) { Foreground = MethodBrush });
                    isAfterDot = false;
                }
                else if (Keywords.Contains(text))
                {
                    inlines.Add(new Run(text) { Foreground = KeywordBrush });
                }
                else if (BuiltInTypes.Contains(text) || (text.Length > 0 && char.IsUpper(text[0])))
                {
                    inlines.Add(new Run(text) { Foreground = TypeBrush });
                }
                else
                {
                    inlines.Add(new Run(text) { Foreground = DefaultBrush });
                }
            }
            else if (match.Groups[5].Success) // 點運算子 .
            {
                inlines.Add(new Run(text) { Foreground = DefaultBrush });
                isAfterDot = true;
            }
            else
            {
                inlines.Add(new Run(text) { Foreground = DefaultBrush });
                if (!match.Groups[7].Success) // 非空白符號重設點旗標
                {
                    isAfterDot = false;
                }
            }
        }
    }
}
