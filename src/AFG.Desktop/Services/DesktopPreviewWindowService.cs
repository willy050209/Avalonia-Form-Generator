// filepath: src/AFG.Desktop/Services/DesktopPreviewWindowService.cs
using System;
using AFG.Core.Models.Ast;
using AFG.Desktop.Views;
using AFG.Shared.Services;
using AFG.Shared.ViewModels;
using Avalonia.Controls;

namespace AFG.Desktop.Services;

/// <summary>
/// 桌面平台獨立預覽視窗管理服務，負責代碼預覽與表單即時預覽之單例控制、焦點喚醒與即時同步。
/// </summary>
public sealed class DesktopPreviewWindowService : IPreviewWindowService
{
    private readonly Func<Window?> _mainWindowProvider;
    private CodePreviewWindow? _codePreviewWindow;
    private FormPreviewWindow? _formPreviewWindow;

    public DesktopPreviewWindowService(Func<Window?> mainWindowProvider)
    {
        _mainWindowProvider = mainWindowProvider ?? throw new ArgumentNullException(nameof(mainWindowProvider));
    }

    public DesktopPreviewWindowService(Func<TopLevel?> topLevelProvider)
        : this(() => topLevelProvider?.Invoke() as Window)
    {
    }

    public bool IsCodePreviewOpen => _codePreviewWindow is not null;
    public bool IsFormPreviewOpen => _formPreviewWindow is not null;

    /// <summary>
    /// 開啟或聚焦獨立程式碼預覽視窗。
    /// </summary>
    public void OpenOrFocusCodePreview(MainViewModel mainVm)
    {
        ArgumentNullException.ThrowIfNull(mainVm);

        if (_codePreviewWindow is not null)
        {
            _codePreviewWindow.SetCode(mainVm.GeneratedViewCode, mainVm.GeneratedVmCode);
            _codePreviewWindow.Activate();
            return;
        }

        var window = new CodePreviewWindow
        {
            DataContext = mainVm
        };

        window.Closed += (_, _) => _codePreviewWindow = null;
        _codePreviewWindow = window;

        window.SetCode(mainVm.GeneratedViewCode, mainVm.GeneratedVmCode);

        var owner = _mainWindowProvider();
        if (owner is not null)
        {
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
    }

    /// <summary>
    /// 開啟或聚焦獨立表單執行期即時預覽視窗。
    /// </summary>
    public void OpenOrFocusFormPreview(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_formPreviewWindow is not null)
        {
            _formPreviewWindow.ReloadDocument(document);
            _formPreviewWindow.Activate();
            return;
        }

        var window = new FormPreviewWindow();
        window.Closed += (_, _) => _formPreviewWindow = null;
        _formPreviewWindow = window;

        window.LoadDocument(document);

        var owner = _mainWindowProvider();
        if (owner is not null)
        {
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
    }

    /// <summary>
    /// 當獨立程式碼視窗開啟時，即時更新其顯示內容。
    /// </summary>
    public void UpdateCodePreview(string viewCode, string vmCode)
    {
        if (_codePreviewWindow is not null)
        {
            _codePreviewWindow.SetCode(viewCode, vmCode);
        }
    }

    /// <summary>
    /// 當獨立表單預覽視窗開啟時，即時更新其控制項結構與樣式。
    /// </summary>
    public void UpdateFormPreview(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_formPreviewWindow is not null)
        {
            _formPreviewWindow.ReloadDocument(document);
        }
    }
}
