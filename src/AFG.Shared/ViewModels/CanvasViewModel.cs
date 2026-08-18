// filepath: src/AFG.Shared/ViewModels/CanvasViewModel.cs
using System.Collections.Generic;
using AFG.Core.Enums;
using AFG.Shared.History;

namespace AFG.Shared.ViewModels;

/// <summary>
/// 管理視覺設計畫布狀態、多選節點、歷史堆疊 (Undo/Redo)、批次對齊與對齊輔助線的 ViewModel。
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
    private double _canvasWidth = 800;

    [ObservableProperty]
    private double _canvasHeight = 600;

    [ObservableProperty]
    private bool _includeMobileProject = true;

    [ObservableProperty]
    private bool _includeLicense = true;

    [ObservableProperty]
    private ToolboxItem? _activeDraggingItem;

    public HistoryManager History { get; } = new();

    public ObservableCollection<string> SelectedNodeIds { get; } = [];

    private readonly List<AstNode> _clipboard = [];

    public IReadOnlyList<CanvasPreset> AvailablePresets { get; } = CanvasPreset.Presets;

    [ObservableProperty]
    private CanvasPreset? _selectedPreset;

    partial void OnSelectedPresetChanged(CanvasPreset? value)
    {
        if (value is not null && !value.IsCustom)
        {
            CanvasWidth = value.Width;
            CanvasHeight = value.Height;

            PushHistory();
            Document = Document with
            {
                CanvasWidth = value.Width,
                CanvasHeight = value.Height
            };
            DocumentChanged?.Invoke(Document);
        }
    }

    partial void OnCanvasWidthChanged(double value)
    {
        if (Math.Abs(Document.CanvasWidth - value) > 0.1)
        {
            PushHistory();
            Document = Document with { CanvasWidth = value };
            MatchOrSetCustomPreset();
            DocumentChanged?.Invoke(Document);
        }
    }

    partial void OnCanvasHeightChanged(double value)
    {
        if (Math.Abs(Document.CanvasHeight - value) > 0.1)
        {
            PushHistory();
            Document = Document with { CanvasHeight = value };
            MatchOrSetCustomPreset();
            DocumentChanged?.Invoke(Document);
        }
    }

    private void MatchOrSetCustomPreset()
    {
        var matched = AvailablePresets.FirstOrDefault(p => !p.IsCustom && Math.Abs(p.Width - Document.CanvasWidth) < 0.5 && Math.Abs(p.Height - Document.CanvasHeight) < 0.5);
        SelectedPreset = matched ?? CanvasPreset.Custom;
    }

    public ObservableCollection<GuideLine> ActiveGuideLines { get; } = [];

    public event Action<FormDocument>? DocumentChanged;
    public event Action<AstNode?>? SelectionChanged;

    public CanvasViewModel()
    {
        _document = FormDocument.CreateDefault();
        CanvasWidth = _document.CanvasWidth;
        CanvasHeight = _document.CanvasHeight;
        MatchOrSetCustomPreset();
    }

    public void PushHistory()
    {
        History.PushSnapshot(Document);
    }

    public void Undo()
    {
        var previous = History.Undo(Document);
        if (previous is not null)
        {
            Document = previous;
            CanvasWidth = previous.CanvasWidth;
            CanvasHeight = previous.CanvasHeight;
            MatchOrSetCustomPreset();
            ValidateSelection();
            DocumentChanged?.Invoke(Document);
            SelectionChanged?.Invoke(SelectedNode);
        }
    }

    public void Redo()
    {
        var next = History.Redo(Document);
        if (next is not null)
        {
            Document = next;
            CanvasWidth = next.CanvasWidth;
            CanvasHeight = next.CanvasHeight;
            MatchOrSetCustomPreset();
            ValidateSelection();
            DocumentChanged?.Invoke(Document);
            SelectionChanged?.Invoke(SelectedNode);
        }
    }

    public void LoadDocument(FormDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        History.Clear();
        Document = doc;
        CanvasWidth = doc.CanvasWidth;
        CanvasHeight = doc.CanvasHeight;
        MatchOrSetCustomPreset();
        SelectedNodeIds.Clear();
        SelectedNode = null;
        ActiveGuideLines.Clear();
        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(null);
    }

    public void SelectNode(string? nodeId, bool isToggle = false)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            if (!isToggle)
            {
                SelectedNodeIds.Clear();
                SelectedNode = null;
            }
        }
        else
        {
            if (isToggle)
            {
                if (!SelectedNodeIds.Remove(nodeId))
                {
                    SelectedNodeIds.Add(nodeId);
                }
            }
            else
            {
                SelectedNodeIds.Clear();
                SelectedNodeIds.Add(nodeId);
            }

            var primaryId = SelectedNodeIds.LastOrDefault();
            SelectedNode = primaryId is not null ? AstTreeOperations.FindNodeById(Document.RootNode, primaryId) : null;
        }

        ActiveGuideLines.Clear();
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void SelectNodes(IEnumerable<string> nodeIds)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);
        SelectedNodeIds.Clear();
        foreach (var id in nodeIds)
        {
            if (!string.IsNullOrEmpty(id) && AstTreeOperations.FindNodeById(Document.RootNode, id) is not null)
            {
                SelectedNodeIds.Add(id);
            }
        }

        var primaryId = SelectedNodeIds.LastOrDefault();
        SelectedNode = primaryId is not null ? AstTreeOperations.FindNodeById(Document.RootNode, primaryId) : null;
        ActiveGuideLines.Clear();
        SelectionChanged?.Invoke(SelectedNode);
    }

    private void ValidateSelection()
    {
        var validIds = SelectedNodeIds
            .Where(id => AstTreeOperations.FindNodeById(Document.RootNode, id) is not null)
            .ToList();

        SelectedNodeIds.Clear();
        foreach (var id in validIds)
        {
            SelectedNodeIds.Add(id);
        }

        var primaryId = SelectedNodeIds.LastOrDefault();
        SelectedNode = primaryId is not null ? AstTreeOperations.FindNodeById(Document.RootNode, primaryId) : null;
    }

    public void AddControlFromToolbox(ToolboxItem item, double? left = null, double? top = null, string? targetParentId = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        PushHistory();

        var parentId = targetParentId;
        if (parentId is null)
        {
            // 1. 若目前選取的節點本身是容器，優先將元件放入該容器
            if (SelectedNode is not null && SelectedNode.IsContainer)
            {
                parentId = SelectedNode.Id;
            }
            // 2. 若提供了滑鼠放置座標，命中測試畫布中該位置最深層的容器
            else if (left.HasValue && top.HasValue)
            {
                var container = FindInnermostContainerAt(Document.RootNode, left.Value, top.Value);
                if (container is not null)
                {
                    parentId = container.Id;
                }
            }
        }

        parentId ??= Document.RootNode.Id;
        var parentNode = AstTreeOperations.FindNodeById(Document.RootNode, parentId) ?? Document.RootNode;

        var defaultLeft = left ?? 40;
        var defaultTop = top ?? 40;

        if (SnapToGrid)
        {
            defaultLeft = SnappingEngine.SnapToGrid(defaultLeft, GridSize);
            defaultTop = SnappingEngine.SnapToGrid(defaultTop, GridSize);
        }

        var (clampedLeft, clampedTop) = AstTreeOperations.ClampCoordinates(
            defaultLeft, defaultTop, item.DefaultWidth, item.DefaultHeight, Document.CanvasWidth, Document.CanvasHeight);

        var isParentCanvas = parentNode.Type == ControlType.Canvas;
        var newNode = new AstNode
        {
            Name = $"{item.DisplayName}_{Guid.NewGuid():N}"[..12],
            Type = item.Type,
            Width = isParentCanvas ? item.DefaultWidth : null,
            Height = isParentCanvas ? item.DefaultHeight : null,
            Content = item.DefaultContent,
            Text = item.DefaultContent,
            CanvasLeft = isParentCanvas ? clampedLeft : null,
            CanvasTop = isParentCanvas ? clampedTop : null,
            HorizontalAlignment = isParentCanvas ? Core.Enums.HorizontalAlignment.Left : Core.Enums.HorizontalAlignment.Stretch,
            VerticalAlignment = isParentCanvas ? Core.Enums.VerticalAlignment.Top : Core.Enums.VerticalAlignment.Stretch
        };

        var newRoot = AstTreeOperations.AddChild(Document.RootNode, parentId, newNode);
        Document = Document with { RootNode = newRoot };
        SelectNode(newNode.Id);

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void ReorderChild(string parentId, string childId, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(parentId);
        ArgumentNullException.ThrowIfNull(childId);

        PushHistory();
        var newRoot = AstTreeOperations.ReorderChild(Document.RootNode, parentId, childId, newIndex);
        Document = Document with { RootNode = newRoot };
        SelectedNode = AstTreeOperations.FindNodeById(Document.RootNode, childId);

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public static AstNode? FindInnermostContainerAt(AstNode root, double x, double y)
    {
        for (var i = root.Children.Count - 1; i >= 0; i--)
        {
            var child = root.Children[i];
            if (!child.IsContainer)
            {
                continue;
            }

            var left = child.CanvasLeft ?? 0;
            var top = child.CanvasTop ?? 0;
            var width = child.Width ?? 200;
            var height = child.Height ?? 150;

            if (x >= left && x <= left + width && y >= top && y <= top + height)
            {
                var deeper = FindInnermostContainerAt(child, x - left, y - top);
                return deeper ?? child;
            }
        }

        return null;
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

        var (clampedLeft, clampedTop) = AstTreeOperations.ClampCoordinates(
            snapResult.Left, snapResult.Top, node.Width ?? 100, node.Height ?? 30, Document.CanvasWidth, Document.CanvasHeight);

        var updatedNode = node with
        {
            CanvasLeft = clampedLeft,
            CanvasTop = clampedTop
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

        var (clampedLeft, clampedTop) = (newLeft, newTop);
        if (newLeft.HasValue && newTop.HasValue)
        {
            var clamped = AstTreeOperations.ClampCoordinates(
                newLeft.Value, newTop.Value, newWidth, newHeight, Document.CanvasWidth, Document.CanvasHeight);
            clampedLeft = clamped.Left;
            clampedTop = clamped.Top;
        }

        var updatedNode = node with
        {
            Width = Math.Max(20, SnapToGrid ? SnappingEngine.SnapToGrid(newWidth, GridSize) : newWidth),
            Height = Math.Max(15, SnapToGrid ? SnappingEngine.SnapToGrid(newHeight, GridSize) : newHeight),
            CanvasLeft = clampedLeft.HasValue ? (SnapToGrid ? SnappingEngine.SnapToGrid(clampedLeft.Value, GridSize) : clampedLeft.Value) : node.CanvasLeft,
            CanvasTop = clampedTop.HasValue ? (SnapToGrid ? SnappingEngine.SnapToGrid(clampedTop.Value, GridSize) : clampedTop.Value) : node.CanvasTop
        };

        var newRoot = AstTreeOperations.UpdateNode(Document.RootNode, updatedNode);
        Document = Document with { RootNode = newRoot };
        SelectedNode = updatedNode;

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void NudgeSelectedNodes(double deltaX, double deltaY)
    {
        if (SelectedNodeIds.Count == 0) return;
        PushHistory();

        var currentRoot = Document.RootNode;
        foreach (var id in SelectedNodeIds)
        {
            var node = AstTreeOperations.FindNodeById(currentRoot, id);
            if (node is not null && node.Id != currentRoot.Id && node.CanvasLeft.HasValue && node.CanvasTop.HasValue)
            {
                var newX = Math.Max(0, node.CanvasLeft.Value + deltaX);
                var newY = Math.Max(0, node.CanvasTop.Value + deltaY);
                currentRoot = AstTreeOperations.UpdateNode(currentRoot, node with { CanvasLeft = newX, CanvasTop = newY });
            }
        }

        Document = Document with { RootNode = currentRoot };
        ValidateSelection();
        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void AlignSelectedNodes(NodeAlignmentType alignment)
    {
        if (SelectedNodeIds.Count < 2) return;
        PushHistory();

        var newRoot = AstTreeOperations.AlignNodes(Document.RootNode, SelectedNodeIds.ToList(), alignment);
        Document = Document with { RootNode = newRoot };
        ValidateSelection();
        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void DistributeSelectedNodes(bool horizontal)
    {
        if (SelectedNodeIds.Count < 3) return;
        PushHistory();

        var newRoot = AstTreeOperations.DistributeNodes(Document.RootNode, SelectedNodeIds.ToList(), horizontal);
        Document = Document with { RootNode = newRoot };
        ValidateSelection();
        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(SelectedNode);
    }

    public void CopySelectedNodes()
    {
        _clipboard.Clear();
        foreach (var id in SelectedNodeIds)
        {
            var node = AstTreeOperations.FindNodeById(Document.RootNode, id);
            if (node is not null && node.Id != Document.RootNode.Id)
            {
                _clipboard.Add(node);
            }
        }
    }

    public void PasteNodes()
    {
        if (_clipboard.Count == 0) return;
        PushHistory();

        var newIds = new List<string>();
        var currentRoot = Document.RootNode;

        foreach (var originalNode in _clipboard)
        {
            var cloned = AstTreeOperations.CloneSubtree(originalNode, offset: 24);
            currentRoot = AstTreeOperations.AddChild(currentRoot, Document.RootNode.Id, cloned);
            newIds.Add(cloned.Id);
        }

        Document = Document with { RootNode = currentRoot };
        SelectNodes(newIds);
        DocumentChanged?.Invoke(Document);
    }

    public void UpdateNodeProperties(AstNode updatedNode)
    {
        ArgumentNullException.ThrowIfNull(updatedNode);
        PushHistory();

        var newRoot = AstTreeOperations.UpdateNode(Document.RootNode, updatedNode);
        Document = Document with { RootNode = newRoot };
        SelectedNode = updatedNode;

        DocumentChanged?.Invoke(Document);
    }

    public void DeleteSelectedNode() => DeleteSelectedNodes();

    public void DeleteSelectedNodes()
    {
        if (SelectedNodeIds.Count == 0)
        {
            return;
        }

        PushHistory();
        var currentRoot = Document.RootNode;
        foreach (var id in SelectedNodeIds.ToList())
        {
            if (id != Document.RootNode.Id)
            {
                try
                {
                    currentRoot = AstTreeOperations.RemoveChild(currentRoot, id);
                }
                catch { }
            }
        }

        Document = Document with { RootNode = currentRoot };
        SelectedNodeIds.Clear();
        SelectedNode = null;
        ActiveGuideLines.Clear();

        DocumentChanged?.Invoke(Document);
        SelectionChanged?.Invoke(null);
    }
}
