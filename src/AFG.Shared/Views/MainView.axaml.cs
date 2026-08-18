// filepath: src/AFG.Shared/Views/MainView.axaml.cs
using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Layout;
using AFG.Shared.Services;
using AFG.Shared.ViewModels;

namespace AFG.Shared.Views;

public partial class MainView : UserControl
{
    private SelectableTextBlock? _viewTextBlock;
    private SelectableTextBlock? _vmTextBlock;
    private Grid? _centerGrid;

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
        _viewTextBlock ??= this.FindControl<SelectableTextBlock>("ViewCodeTextBlock");
        _vmTextBlock ??= this.FindControl<SelectableTextBlock>("VmCodeTextBlock");
        _centerGrid ??= this.FindControl<Grid>("CenterGrid");
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.GeneratedViewCode))
        {
            UpdateViewCode();
        }
        else if (e.PropertyName == nameof(MainViewModel.GeneratedVmCode))
        {
            UpdateVmCode();
        }
        else if (e.PropertyName == nameof(MainViewModel.IsCodePanelVisible))
        {
            UpdateCenterGridLayout();
        }
    }

    private void RefreshAll()
    {
        InitControls();
        UpdateViewCode();
        UpdateVmCode();
        UpdateCenterGridLayout();
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

    private void UpdateCenterGridLayout()
    {
        InitControls();
        if (_centerGrid is null || _centerGrid.RowDefinitions.Count < 3 || DataContext is not MainViewModel vm)
        {
            return;
        }

        // 確保在隱藏代碼預覽區時，畫布區域 (Row 0) 能夠立即擴展至 100% 完整垂直高度
        _centerGrid.RowDefinitions[0].Height = new GridLength(1, Avalonia.Controls.GridUnitType.Star);

        if (!vm.IsCodePanelVisible)
        {
            _centerGrid.RowDefinitions[1].Height = new GridLength(0, Avalonia.Controls.GridUnitType.Pixel);
            _centerGrid.RowDefinitions[2].Height = new GridLength(0, Avalonia.Controls.GridUnitType.Pixel);
        }
        else
        {
            _centerGrid.RowDefinitions[1].Height = GridLength.Auto;
            _centerGrid.RowDefinitions[2].Height = GridLength.Auto;
        }
    }
}
