// filepath: src/AFG.Desktop/MainWindow.axaml.cs
using AFG.Desktop.Services;
using AFG.Shared.ViewModels;
using Avalonia.Controls;

namespace AFG.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var fileService = new DesktopFileDialogService(() => this);
        var clipboardService = new DesktopClipboardService(() => this);
        var previewService = new DesktopPreviewWindowService(() => this);

        var viewModel = new MainViewModel(fileService, clipboardService, previewWindowService: previewService);
        RootMainView.DataContext = viewModel;
    }
}
