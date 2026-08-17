// filepath: src/AFG.Shared/ViewModels/VisualTreeViewModel.cs
namespace AFG.Shared.ViewModels;

/// <summary>
/// 視覺樹 DOM 樹狀導覽的 ViewModel。
/// </summary>
public sealed partial class VisualTreeViewModel : ObservableObject
{
    [ObservableProperty]
    private TreeNodeViewModel? _root;

    [ObservableProperty]
    private TreeNodeViewModel? _selectedItem;

    public event Action<string?>? SelectionChanged;

    public void RebuildFromDocument(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Root = new TreeNodeViewModel(document.RootNode);
    }

    public void SyncSelection(string? nodeId)
    {
        if (Root is null || string.IsNullOrEmpty(nodeId))
        {
            SelectedItem = null;
            return;
        }

        SelectedItem = FindTreeNode(Root, nodeId);
    }

    partial void OnSelectedItemChanged(TreeNodeViewModel? value)
    {
        SelectionChanged?.Invoke(value?.Node.Id);
    }

    private static TreeNodeViewModel? FindTreeNode(TreeNodeViewModel current, string targetId)
    {
        if (current.Node.Id == targetId)
        {
            return current;
        }

        foreach (var child in current.Children)
        {
            var found = FindTreeNode(child, targetId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
