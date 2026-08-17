// filepath: src/AFG.Shared/ViewModels/TreeNodeViewModel.cs
namespace AFG.Shared.ViewModels;

/// <summary>
/// 視覺樹 (Visual Tree) 中的節點 ViewModel。
/// </summary>
public sealed partial class TreeNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private AstNode _node;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<TreeNodeViewModel> Children { get; } = [];

    public string DisplayTitle => string.IsNullOrWhiteSpace(Node.Name)
        ? $"{Node.Type} ({Node.Id[..6]})"
        : $"{Node.Name} ({Node.Type})";

    public string IconGlyph => Node.Type switch
    {
        ControlType.Grid => "▦",
        ControlType.Canvas => "🎨",
        ControlType.StackPanel => "📑",
        ControlType.Button => "🔘",
        ControlType.TextBox => "📝",
        ControlType.TextBlock => "🔤",
        ControlType.CheckBox => "☑️",
        _ => "📦"
    };

    public TreeNodeViewModel(AstNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _node = node;
        RebuildChildren();
    }

    public void UpdateNode(AstNode newNode)
    {
        ArgumentNullException.ThrowIfNull(newNode);
        Node = newNode;
        OnPropertyChanged(nameof(DisplayTitle));
        RebuildChildren();
    }

    private void RebuildChildren()
    {
        Children.Clear();
        foreach (var child in Node.Children)
        {
            Children.Add(new TreeNodeViewModel(child));
        }
    }
}
