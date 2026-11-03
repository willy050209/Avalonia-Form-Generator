using System;
using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaGridUnitType = Avalonia.Controls.GridUnitType;
using AFG.Shared.ViewModels;

namespace AFG.Shared.Views;

public partial class MainView : UserControl
{
    private Grid? _mainWorkspaceGrid;

    private double _savedLeftPanelWidth = 260;
    private double _savedRightPanelWidth = 320;

    public MainView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            InitControls();
            RefreshAll();
        };

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged -= OnViewModelPropertyChanged;
                vm.PropertyChanged += OnViewModelPropertyChanged;
                RefreshAll();
            }
        };

        if (DataContext is MainViewModel initialVm)
        {
            initialVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void InitControls()
    {
        _mainWorkspaceGrid ??= this.FindControl<Grid>("MainWorkspaceGrid");
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsLeftPanelVisible) or
                              nameof(MainViewModel.IsRightPanelVisible))
        {
            UpdateWorkspaceLayout();
        }
    }

    private void RefreshAll()
    {
        InitControls();
        UpdateWorkspaceLayout();
    }

    private void UpdateWorkspaceLayout()
    {
        InitControls();
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        // 左側與右側邊欄 Grid 佈局更新（確保摺疊時畫布擴展至 100% 寬度）
        if (_mainWorkspaceGrid is not null && _mainWorkspaceGrid.ColumnDefinitions.Count >= 5)
        {
            // 左側面板 (Column 0: 內容, Column 1: GridSplitter)
            if (vm.IsLeftPanelVisible)
            {
                _mainWorkspaceGrid.ColumnDefinitions[0].Width = new GridLength(_savedLeftPanelWidth, AvaloniaGridUnitType.Pixel);
                _mainWorkspaceGrid.ColumnDefinitions[1].Width = GridLength.Auto;
            }
            else
            {
                if (_mainWorkspaceGrid.ColumnDefinitions[0].Width.Value > 50)
                {
                    _savedLeftPanelWidth = _mainWorkspaceGrid.ColumnDefinitions[0].Width.Value;
                }
                _mainWorkspaceGrid.ColumnDefinitions[0].Width = new GridLength(0, AvaloniaGridUnitType.Pixel);
                _mainWorkspaceGrid.ColumnDefinitions[1].Width = new GridLength(0, AvaloniaGridUnitType.Pixel);
            }

            // 中央畫布區永遠佔滿所有剩餘彈性寬度 (Column 2: Star)
            _mainWorkspaceGrid.ColumnDefinitions[2].Width = new GridLength(1, AvaloniaGridUnitType.Star);

            // 右側面板 (Column 3: GridSplitter, Column 4: 內容)
            if (vm.IsRightPanelVisible)
            {
                _mainWorkspaceGrid.ColumnDefinitions[3].Width = GridLength.Auto;
                _mainWorkspaceGrid.ColumnDefinitions[4].Width = new GridLength(_savedRightPanelWidth, AvaloniaGridUnitType.Pixel);
            }
            else
            {
                if (_mainWorkspaceGrid.ColumnDefinitions[4].Width.Value > 50)
                {
                    _savedRightPanelWidth = _mainWorkspaceGrid.ColumnDefinitions[4].Width.Value;
                }
                _mainWorkspaceGrid.ColumnDefinitions[3].Width = new GridLength(0, AvaloniaGridUnitType.Pixel);
                _mainWorkspaceGrid.ColumnDefinitions[4].Width = new GridLength(0, AvaloniaGridUnitType.Pixel);
            }
        }
    }
}
