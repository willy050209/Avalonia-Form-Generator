// filepath: src/AFG.Shared/ViewModels/MainViewModel.cs
namespace AFG.Shared.ViewModels;

/// <summary>
/// 主介面協調 ViewModel，整合工具箱、畫布、視覺樹、程式碼生成與儲存檔案操作。
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IFileDialogService? _fileDialogService;
    private readonly IClipboardService? _clipboardService;
    private readonly INotificationService? _notificationService;
    private readonly FormCodeGenerator _codeGenerator = new();

    public CanvasViewModel Canvas { get; }
    public ToolboxViewModel Toolbox { get; }
    public VisualTreeViewModel VisualTree { get; }

    [ObservableProperty]
    private string _generatedViewCode = string.Empty;

    [ObservableProperty]
    private string _generatedVmCode = string.Empty;

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainViewModel(
        IFileDialogService? fileDialogService = null,
        IClipboardService? clipboardService = null,
        INotificationService? notificationService = null)
    {
        _fileDialogService = fileDialogService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;

        Canvas = new CanvasViewModel();
        Toolbox = new ToolboxViewModel();
        VisualTree = new VisualTreeViewModel();

        // 綁定事件連動
        Canvas.DocumentChanged += OnDocumentChanged;
        Canvas.SelectionChanged += OnCanvasSelectionChanged;
        VisualTree.SelectionChanged += OnVisualTreeSelectionChanged;

        // 初始化視覺樹
        VisualTree.RebuildFromDocument(Canvas.Document);
        GeneratePreviewCode();
    }

    private void OnDocumentChanged(FormDocument doc)
    {
        VisualTree.RebuildFromDocument(doc);
        GeneratePreviewCode();
    }

    private void OnCanvasSelectionChanged(AstNode? node)
    {
        VisualTree.SyncSelection(node?.Id);
    }

    private void OnVisualTreeSelectionChanged(string? nodeId)
    {
        Canvas.SelectNode(nodeId);
    }

    [RelayCommand]
    private void NewDocument()
    {
        Canvas.LoadDocument(FormDocument.CreateDefault());
        _notificationService?.Show("新表單", "已成功建立新表單畫布。");
    }

    [RelayCommand]
    private async Task OpenDocumentAsync()
    {
        if (_fileDialogService is null) return;

        var path = await _fileDialogService.OpenFileDialogAsync("開啟表單設計檔");
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var doc = AfgSerializer.DeserializeDocument(json);
            Canvas.LoadDocument(doc);
            _notificationService?.Show("開啟成功", $"已成功載入 {System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            _notificationService?.Show("開啟失敗", ex.Message, isError: true);
        }
    }

    [RelayCommand]
    private async Task SaveDocumentAsync()
    {
        if (_fileDialogService is null) return;

        var path = await _fileDialogService.SaveFileDialogAsync(
            "儲存表單設計檔",
            $"{Canvas.Document.ViewClassName}.afg.json",
            "afg.json",
            "AFG 表單模型");

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var json = AfgSerializer.SerializeDocument(Canvas.Document);
            await File.WriteAllTextAsync(path, json);
            _notificationService?.Show("儲存成功", $"已儲存至 {path}");
        }
        catch (Exception ex)
        {
            _notificationService?.Show("儲存失敗", ex.Message, isError: true);
        }
    }

    [RelayCommand]
    private void AddSelectedToolboxItem()
    {
        if (Toolbox.SelectedItem is not null)
        {
            Canvas.AddControlFromToolbox(Toolbox.SelectedItem);
        }
    }

    [RelayCommand]
    private void DeleteSelectedNode()
    {
        Canvas.DeleteSelectedNode();
    }

    [RelayCommand]
    public void GeneratePreviewCode()
    {
        try
        {
            var result = _codeGenerator.GenerateAll(Canvas.Document);
            if (result.IsSuccess)
            {
                var viewFile = result.Files.FirstOrDefault(f => f.FileType == SourceFileType.View);
                var vmFile = result.Files.FirstOrDefault(f => f.FileType == SourceFileType.ViewModel);

                GeneratedViewCode = viewFile?.Content ?? string.Empty;
                GeneratedVmCode = vmFile?.Content ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            GeneratedViewCode = $"// 代碼生成失敗: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CopyViewCodeAsync()
    {
        if (_clipboardService is not null && !string.IsNullOrEmpty(GeneratedViewCode))
        {
            await _clipboardService.SetTextAsync(GeneratedViewCode);
            _notificationService?.Show("複製成功", "View C# Markup 程式碼已複製到剪貼簿。");
        }
    }

    [RelayCommand]
    private async Task CopyVmCodeAsync()
    {
        if (_clipboardService is not null && !string.IsNullOrEmpty(GeneratedVmCode))
        {
            await _clipboardService.SetTextAsync(GeneratedVmCode);
            _notificationService?.Show("複製成功", "ViewModel 程式碼已複製到剪貼簿。");
        }
    }
}
