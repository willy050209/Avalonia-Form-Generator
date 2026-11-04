// filepath: src/AFG.Desktop/MainWindow.axaml.cs
using System.IO;
using AFG.Core.Serialization;
using AFG.Desktop.Services;
using AFG.Shared.ViewModels;
using Avalonia.Controls;

namespace AFG.Desktop;

public partial class MainWindow : Window
{
    public MainWindow() : this(null)
    {
    }

    public MainWindow(string? initialDocumentPath)
    {
        InitializeComponent();

        var fileService = new DesktopFileDialogService(() => this);
        var clipboardService = new DesktopClipboardService(() => this);
        var previewService = new DesktopPreviewWindowService(() => this);

        var viewModel = new MainViewModel(fileService, clipboardService, previewWindowService: previewService);
        RootMainView.DataContext = viewModel;

        if (!string.IsNullOrEmpty(initialDocumentPath) && File.Exists(initialDocumentPath))
        {
            var json = File.ReadAllText(initialDocumentPath);
            var doc = AfgSerializer.DeserializeDocument(json);
            viewModel.Canvas.LoadDocument(doc);
        }
    }
}
