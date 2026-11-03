// filepath: src/AFG.Desktop/Views/CodePreviewWindow.axaml.cs
using Avalonia.Controls;
using AFG.Shared.ViewModels;

namespace AFG.Desktop.Views;

public partial class CodePreviewWindow : Window
{
    public CodePreviewWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 即時更新預覽之 View 與 ViewModel 程式碼。
    /// </summary>
    public void SetCode(string viewCode, string vmCode)
    {
        PreviewView.SetCode(viewCode, vmCode);
    }
}
