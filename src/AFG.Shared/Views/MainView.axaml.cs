using System;
using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaGridUnitType = Avalonia.Controls.GridUnitType;
using AFG.Shared.Services;
using AFG.Shared.ViewModels;

namespace AFG.Shared.Views;

public partial class MainView : UserControl
{
    private SelectableTextBlock? _viewTextBlock;
    private SelectableTextBlock? _vmTextBlock;
    private Grid? _mainWorkspaceGrid;
    private Grid? _centerGrid;

    private double _savedLeftPanelWidth = 260;
    private double _savedRightPanelWidth = 320;
    private double _savedCodePanelHeight = 240;

    private readonly Avalonia.Threading.DispatcherTimer _codeDebounceTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(150)
    };

    public MainView()
    {
        InitializeComponent();

        _codeDebounceTimer.Tick += (_, _) =>
        {
            _codeDebounceTimer.Stop();
            UpdateViewCode();
            UpdateVmCode();
        };

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
        _viewTextBlock ??= this.FindControl<SelectableTextBlock>("ViewCodeTextBlock");
        _vmTextBlock ??= this.FindControl<SelectableTextBlock>("VmCodeTextBlock");
        _mainWorkspaceGrid ??= this.FindControl<Grid>("MainWorkspaceGrid");
        _centerGrid ??= this.FindControl<Grid>("CenterGrid");
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.GeneratedViewCode) ||
            e.PropertyName == nameof(MainViewModel.GeneratedVmCode))
        {
            _codeDebounceTimer.Stop();
            _codeDebounceTimer.Start();
        }
        else if (e.PropertyName is nameof(MainViewModel.IsLeftPanelVisible) or
                                   nameof(MainViewModel.IsRightPanelVisible) or
                                   nameof(MainViewModel.IsCodePanelVisible))
        {
            UpdateWorkspaceLayout();
        }
    }

    private void RefreshAll()
    {
        InitControls();
        UpdateViewCode();
        UpdateVmCode();
        UpdateWorkspaceLayout();
    }

    private void UpdateViewCode()
    {
        InitControls();
        if (_viewTextBlock is not null && DataContext is MainViewModel vm)
        {
            CSharpSyntaxColorizer.PopulateInlines(_viewTextBlock.Inlines!, vm.GeneratedViewCode);
        }
    }

    private void UpdateVmCode()
    {
        InitControls();
        if (_vmTextBlock is not null && DataContext is MainViewModel vm)
        {
            CSharpSyntaxColorizer.PopulateInlines(_vmTextBlock.Inlines!, vm.GeneratedVmCode);
        }
    }

    private void UpdateWorkspaceLayout()
    {
        InitControls();
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        // 1. 左側與右側邊欄 Grid 佈局更新（確保摺疊時畫布擴展至 100% 寬度）
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

        // 2. 中央區域程式碼預覽面板（支援自由拉伸高度與摺疊時畫布擴展至 100% 高度）
        if (_centerGrid is not null && _centerGrid.RowDefinitions.Count >= 3)
        {
            _centerGrid.RowDefinitions[0].Height = new GridLength(1, AvaloniaGridUnitType.Star);

            if (vm.IsCodePanelVisible)
            {
                _centerGrid.RowDefinitions[1].Height = GridLength.Auto;
                _centerGrid.RowDefinitions[2].Height = new GridLength(_savedCodePanelHeight, AvaloniaGridUnitType.Pixel);
            }
            else
            {
                if (_centerGrid.RowDefinitions[2].Height.Value > 50)
                {
                    _savedCodePanelHeight = _centerGrid.RowDefinitions[2].Height.Value;
                }
                _centerGrid.RowDefinitions[1].Height = new GridLength(0, AvaloniaGridUnitType.Pixel);
                _centerGrid.RowDefinitions[2].Height = new GridLength(0, AvaloniaGridUnitType.Pixel);
            }
        }
    }
}
