// filepath: src/AFG.Shared/ViewModels/ToolboxViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using AFG.Shared.Models;
using AFG.Shared.Services;

namespace AFG.Shared.ViewModels;

/// <summary>
/// 管理控制項工具箱清單、搜尋篩選與拖曳/雙擊互動的 ViewModel。
/// </summary>
public sealed partial class ToolboxViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ToolboxItem? _selectedItem;

    private readonly List<ToolboxItem> _allItems;

    public ObservableCollection<ToolboxItem> FilteredItems { get; } = [];

    public event Action<ToolboxItem>? ItemDoubleClicked;
    public event Action<ToolboxItem>? ItemDragStarted;
    public event Action? ItemDragEnded;

    public ToolboxViewModel()
    {
        _allItems = ToolboxService.GetAvailableItems().ToList();
        RefreshFilteredItems();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshFilteredItems();
    }

    public void TriggerDoubleClick(ToolboxItem item) => ItemDoubleClicked?.Invoke(item);
    public void StartDrag(ToolboxItem item) => ItemDragStarted?.Invoke(item);
    public void EndDrag() => ItemDragEnded?.Invoke();

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
