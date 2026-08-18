// filepath: src/AFG.Shared/ViewModels/CanvasViewModel.cs
namespace AFG.Shared.ViewModels;

/// <summary>
/// 管理視覺設計畫布狀態、選取節點、拖曳變更與對齊輔助線的 ViewModel。
/// </summary>
public sealed partial class CanvasViewModel : ObservableObject
{
    [ObservableProperty]
    private FormDocument _document;

    [ObservableProperty]
    private AstNode? _selectedNode;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private double _gridSize = 8.0;

    [ObservableProperty]
    private bool _includeMobileProject = true;

    public IReadOnlyList<CanvasPreset> AvailablePresets { get; } = CanvasPreset.Presets;

    [ObservableProperty]
    private CanvasPreset? _selectedPreset;

    partial void OnSelectedPresetChanged(CanvasPreset? value)
    {
        if (value is not null)
        {
            Document = Document with
            {
                CanvasWidth = value.Width,
                CanvasHeight = value.Height
            };
            DocumentChanged?.Invoke(Document);
        }
    }

    public ObservableCollection<GuideLine> ActiveGuideLines { get; } = [];

    public event Action<FormDocument>? DocumentChanged;
    public event Action<AstNode?>? SelectionChanged;

    public CanvasViewModel()
    {
        _document = FormDocument.CreateDefault();
    }

    public void LoadDocument(FormDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        Document = doc;
        SelectedNode = null;
        ActiveGuideLines.Clear();
        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(null);
    }

    public void SelectNode(string? nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            SelectedNode = null;
        }
        else
        {
            SelectedNode = AstTreeOperations.FindNodeById(Document.RootNode, nodeId);
        }

        ActiveGuideLines.Clear();
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void AddControlFromToolbox(ToolboxItem item, double? left = null, double? top = null, string? targetParentId = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        var parentId = targetParentId ?? Document.RootNode.Id;
        var parentNode = AstTreeOperations.FindNodeById(Document.RootNode, parentId) ?? Document.RootNode;

        var defaultLeft = left ?? 40;
        var defaultTop = top ?? 40;

        if (SnapToGrid)
        {
            defaultLeft = SnappingEngine.SnapToGrid(defaultLeft, GridSize);
            defaultTop = SnappingEngine.SnapToGrid(defaultTop, GridSize);
        }

        var newNode = new AstNode
        {
            Name = $"{item.DisplayName}_{Guid.NewGuid():N}"[..12],
            Type = item.Type,
            Width = item.DefaultWidth,
            Height = item.DefaultHeight,
            Content = item.DefaultContent,
            Text = item.DefaultContent,
            CanvasLeft = parentNode.Type == ControlType.Canvas ? defaultLeft : null,
            CanvasTop = parentNode.Type == ControlType.Canvas ? defaultTop : null
        };

        var newRoot = AstTreeOperations.AddChild(Document.RootNode, parentId, newNode);
        Document = Document with { RootNode = newRoot };
        SelectedNode = newNode;

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void MoveNode(string nodeId, double rawLeft, double rawTop)
    {
        ArgumentNullException.ThrowIfNull(nodeId);

        var node = AstTreeOperations.FindNodeById(Document.RootNode, nodeId);
        if (node is null || node.Id == Document.RootNode.Id)
        {
            return;
        }

        var siblings = Document.RootNode.Children.Where(c => c.Id != nodeId).ToList();
        var snapResult = SnappingEngine.CalculateSnap(
            rawLeft,
            rawTop,
            node.Width ?? 100,
            node.Height ?? 30,
            siblings,
            snapThreshold: 6.0,
            gridSize: GridSize,
            snapToGrid: SnapToGrid);

        ActiveGuideLines.Clear();
        foreach (var guide in snapResult.GuideLines)
        {
            ActiveGuideLines.Add(guide);
        }

        var updatedNode = node with
        {
            CanvasLeft = snapResult.Left,
            CanvasTop = snapResult.Top
        };

        var newRoot = AstTreeOperations.UpdateNode(Document.RootNode, updatedNode);
        Document = Document with { RootNode = newRoot };
        SelectedNode = updatedNode;

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void ResizeNode(string nodeId, double newWidth, double newHeight, double? newLeft = null, double? newTop = null)
    {
        ArgumentNullException.ThrowIfNull(nodeId);

        var node = AstTreeOperations.FindNodeById(Document.RootNode, nodeId);
        if (node is null)
        {
            return;
        }

        var updatedNode = node with
        {
            Width = Math.Max(20, SnapToGrid ? SnappingEngine.SnapToGrid(newWidth, GridSize) : newWidth),
            Height = Math.Max(15, SnapToGrid ? SnappingEngine.SnapToGrid(newHeight, GridSize) : newHeight),
            CanvasLeft = newLeft.HasValue ? (SnapToGrid ? SnappingEngine.SnapToGrid(newLeft.Value, GridSize) : newLeft.Value) : node.CanvasLeft,
            CanvasTop = newTop.HasValue ? (SnapToGrid ? SnappingEngine.SnapToGrid(newTop.Value, GridSize) : newTop.Value) : node.CanvasTop
        };

        var newRoot = AstTreeOperations.UpdateNode(Document.RootNode, updatedNode);
        Document = Document with { RootNode = newRoot };
        SelectedNode = updatedNode;

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void UpdateNodeProperties(AstNode updatedNode)
    {
        ArgumentNullException.ThrowIfNull(updatedNode);

        var newRoot = AstTreeOperations.UpdateNode(Document.RootNode, updatedNode);
        Document = Document with { RootNode = newRoot };
        SelectedNode = updatedNode;

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void DeleteSelectedNode()
    {
        if (SelectedNode is null || SelectedNode.Id == Document.RootNode.Id)
        {
            return;
        }

        var targetId = SelectedNode.Id;
        var newRoot = AstTreeOperations.RemoveChild(Document.RootNode, targetId);
        Document = Document with { RootNode = newRoot };
        SelectedNode = null;
        ActiveGuideLines.Clear();

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(null);
    }
}
