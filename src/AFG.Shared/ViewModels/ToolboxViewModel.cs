// filepath: src/AFG.Shared/ViewModels/ToolboxViewModel.cs
namespace AFG.Shared.ViewModels;

/// <summary>
/// 管理控制項工具箱清單與搜尋篩選的 ViewModel。
/// </summary>
public sealed partial class ToolboxViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ToolboxItem? _selectedItem;

    private readonly List<ToolboxItem> _allItems;

    public ObservableCollection<ToolboxItem> FilteredItems { get; } = [];

    public ToolboxViewModel()
    {
        _allItems = ToolboxService.GetAvailableItems().ToList();
        RefreshFilteredItems();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshFilteredItems();
    }

    private void RefreshFilteredItems()
    {
        FilteredItems.Clear();
        var query = SearchText.Trim();

        var matches = string.IsNullOrEmpty(query)
            ? _allItems
            : _allItems.Where(i => i.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var item in matches)
        {
            FilteredItems.Add(item);
        }
    }
}
