// filepath: src/AFG.Shared/ViewModels/MainViewModel.cs
using AFG.Generators.ProjectExport;

namespace AFG.Shared.ViewModels;

/// <summary>
/// 主介面協調 ViewModel，整合工具箱、畫布、視覺樹、屬性檢查器、程式碼生成與專案匯出操作。
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IFileDialogService? _fileDialogService;
    private readonly IClipboardService? _clipboardService;
    private readonly INotificationService? _notificationService;
    private readonly FormCodeGenerator _codeGenerator = new();
    private readonly ProjectExportService _exportService = new();

    public CanvasViewModel Canvas { get; }
    public ToolboxViewModel Toolbox { get; }
    public VisualTreeViewModel VisualTree { get; }
    public InspectorViewModel Inspector { get; }

    [ObservableProperty]
    private string _generatedViewCode = string.Empty;

    [ObservableProperty]
    private string _generatedVmCode = string.Empty;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private int _validationErrorCount;

    [ObservableProperty]
    private bool _isLeftPanelVisible = true;

    [ObservableProperty]
    private bool _isRightPanelVisible = true;

    [ObservableProperty]
    private bool _isCodePanelVisible;

    [ObservableProperty]
    private bool _isCustomResolutionDialogVisible;

    [ObservableProperty]
    private double _customResolutionWidth = 800;

    [ObservableProperty]
    private double _customResolutionHeight = 600;

    [ObservableProperty]
    private bool _isProjectNameDialogVisible;

    [ObservableProperty]
    private string _customProjectNameInput = "MainFormApp";

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
        Inspector = new InspectorViewModel();

        // 綁定事件連動
        Canvas.DocumentChanged += OnDocumentChanged;
        Canvas.SelectionChanged += OnCanvasSelectionChanged;
        VisualTree.SelectionChanged += OnVisualTreeSelectionChanged;
        Inspector.NodeUpdated += OnInspectorNodeUpdated;
        Toolbox.ItemDoubleClicked += item => Canvas.AddControlFromToolbox(item);
        Toolbox.ItemDragStarted += item => Canvas.ActiveDraggingItem = item;
        Toolbox.ItemDragEnded += () => Canvas.ActiveDraggingItem = null;

        // 初始化
        VisualTree.RebuildFromDocument(Canvas.Document);
        GeneratePreviewCode();
    }

    private void OnDocumentChanged(FormDocument doc)
    {
        VisualTree.RebuildFromDocument(doc);
        GeneratePreviewCode();
        ValidateWholeDocument(doc);
    }

    private void OnCanvasSelectionChanged(AstNode? node)
    {
        VisualTree.SyncSelection(node?.Id);
        Inspector.LoadNode(node);
    }

    private void OnVisualTreeSelectionChanged(string? nodeId)
    {
        Canvas.SelectNode(nodeId);
    }

    private void OnInspectorNodeUpdated(AstNode node)
    {
        Canvas.UpdateNodeProperties(node);
    }

    private void ValidateWholeDocument(FormDocument doc)
    {
        var validation = AstValidator.ValidateDocument(doc);
        ValidationErrorCount = validation.Errors.Count;
    }

    [RelayCommand]
    private void ToggleLeftPanel() => IsLeftPanelVisible = !IsLeftPanelVisible;

    [RelayCommand]
    private void ToggleRightPanel() => IsRightPanelVisible = !IsRightPanelVisible;

    [RelayCommand]
    private void ToggleCodePanel() => IsCodePanelVisible = !IsCodePanelVisible;

    [RelayCommand]
    private void OpenCustomResolutionDialog()
    {
        CustomResolutionWidth = Canvas.CanvasWidth;
        CustomResolutionHeight = Canvas.CanvasHeight;
        IsCustomResolutionDialogVisible = true;
    }

    [RelayCommand]
    private void ApplyCustomResolution()
    {
        Canvas.CanvasWidth = CustomResolutionWidth;
        Canvas.CanvasHeight = CustomResolutionHeight;
        IsCustomResolutionDialogVisible = false;
    }

    [RelayCommand]
    private void CloseCustomResolutionDialog() => IsCustomResolutionDialogVisible = false;

    [RelayCommand]
    private void OpenProjectNameDialog()
    {
        CustomProjectNameInput = Canvas.ExportProjectName;
        IsProjectNameDialogVisible = true;
    }

    [RelayCommand]
    private void ApplyProjectName()
    {
        if (!string.IsNullOrWhiteSpace(CustomProjectNameInput))
        {
            Canvas.ExportProjectName = CustomProjectNameInput.Trim();
        }
        IsProjectNameDialogVisible = false;
    }

    [RelayCommand]
    private void CloseProjectNameDialog() => IsProjectNameDialogVisible = false;

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
        catch (System.Text.Json.JsonException jex)
        {
            _notificationService?.Show("檔案損毀", $"表單 JSON 格式錯誤 (行 {jex.LineNumber}, 欄 {jex.BytePositionInLine}):\n{jex.Message}", isError: true);
        }
        catch (Exception ex)
        {
            _notificationService?.Show("開啟失敗", $"讀取表單失敗: {ex.Message}", isError: true);
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
    private async Task ExportFullProjectAsync()
    {
        if (_fileDialogService is null) return;

        var folder = await _fileDialogService.OpenFolderDialogAsync("選擇專案匯出資料夾");
        if (string.IsNullOrEmpty(folder)) return;

        try
        {
            var options = new ProjectExportOptions(
                IncludeMobileProject: Canvas.IncludeMobileProject,
                IncludeLicense: Canvas.IncludeLicense,
                CustomProjectName: Canvas.ExportProjectName);
            await _exportService.ExportToFolderAsync(Canvas.Document, folder, options);
            _notificationService?.Show("匯出成功", $"已成功將完整 Avalonia 跨平台專案「{Canvas.ExportProjectName}」匯出至 {folder}");
        }
        catch (Exception ex)
        {
            _notificationService?.Show("匯出失敗", ex.Message, isError: true);
        }
    }

    [RelayCommand]
    private void Undo() => Canvas.Undo();

    [RelayCommand]
    private void Redo() => Canvas.Redo();

    [RelayCommand]
    private void CopyNodes() => Canvas.CopySelectedNodes();

    [RelayCommand]
    private void PasteNodes() => Canvas.PasteNodes();

    [RelayCommand]
    private void AlignLeft() => Canvas.AlignSelectedNodes(NodeAlignmentType.AlignLeft);

    [RelayCommand]
    private void AlignCenter() => Canvas.AlignSelectedNodes(NodeAlignmentType.AlignHorizontalCenter);

    [RelayCommand]
    private void AlignRight() => Canvas.AlignSelectedNodes(NodeAlignmentType.AlignRight);

    [RelayCommand]
    private void AlignTop() => Canvas.AlignSelectedNodes(NodeAlignmentType.AlignTop);

    [RelayCommand]
    private void AlignMiddle() => Canvas.AlignSelectedNodes(NodeAlignmentType.AlignVerticalCenter);

    [RelayCommand]
    private void AlignBottom() => Canvas.AlignSelectedNodes(NodeAlignmentType.AlignBottom);

    [RelayCommand]
    private void DistributeHorizontal() => Canvas.DistributeSelectedNodes(horizontal: true);

    [RelayCommand]
    private void DistributeVertical() => Canvas.DistributeSelectedNodes(horizontal: false);

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
        Canvas.DeleteSelectedNodes();
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
