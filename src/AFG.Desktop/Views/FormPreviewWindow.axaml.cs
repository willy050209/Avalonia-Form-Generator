// filepath: src/AFG.Desktop/Views/FormPreviewWindow.axaml.cs
using System;
using AFG.Core.Models.Ast;
using AFG.Shared.Services;
using Avalonia.Controls;
using Avalonia.Media;

namespace AFG.Desktop.Views;

public partial class FormPreviewWindow : Window
{
    public FormPreviewWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 載入並套用表單文件配置與控制項樹。
    /// </summary>
    /// <param name="doc">表單設計模型。</param>
    public void LoadDocument(FormDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ApplyDocumentProperties(doc);
        RebuildControlTree(doc);
    }

    /// <summary>
    /// 重新載入表單文件（即時同步最新編輯狀態）。
    /// </summary>
    /// <param name="doc">表單設計模型。</param>
    public void ReloadDocument(FormDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ApplyDocumentProperties(doc);
        RebuildControlTree(doc);
    }

    private void ApplyDocumentProperties(FormDocument doc)
    {
        Title = $"[即時預覽] {doc.ViewClassName} - {doc.Title}";

        if (doc.CanvasWidth > 0)
        {
            Width = doc.CanvasWidth;
        }
        if (doc.CanvasHeight > 0)
        {
            Height = doc.CanvasHeight;
        }

        if (doc.MinWidth.HasValue && doc.MinWidth.Value > 0)
        {
            MinWidth = doc.MinWidth.Value;
        }
        if (doc.MinHeight.HasValue && doc.MinHeight.Value > 0)
        {
            MinHeight = doc.MinHeight.Value;
        }
        if (doc.MaxWidth.HasValue && doc.MaxWidth.Value > 0)
        {
            MaxWidth = doc.MaxWidth.Value;
        }
        if (doc.MaxHeight.HasValue && doc.MaxHeight.Value > 0)
        {
            MaxHeight = doc.MaxHeight.Value;
        }

        // 同步啟動位置與視窗狀態
        if (Enum.TryParse<WindowStartupLocation>(doc.WindowStartupLocation.ToString(), out var startupLoc))
        {
            WindowStartupLocation = startupLoc;
        }
        else
        {
            WindowStartupLocation = (WindowStartupLocation)doc.WindowStartupLocation;
        }

        if (Enum.TryParse<WindowState>(doc.WindowState.ToString(), out var state))
        {
            WindowState = state;
        }
        else
        {
            WindowState = (WindowState)doc.WindowState;
        }

        // 背景色同步
        if (!string.IsNullOrWhiteSpace(doc.BackgroundColor) && Color.TryParse(doc.BackgroundColor, out var color))
        {
            Background = new SolidColorBrush(color);
        }
        else
        {
            Background = new SolidColorBrush(Color.Parse("#18181B"));
        }
    }

    private void RebuildControlTree(FormDocument doc)
    {
        var runtimeTree = FormRuntimePreviewBuilder.BuildRuntimeControlTree(doc);
        PreviewScrollViewer.Content = runtimeTree;
    }
}
