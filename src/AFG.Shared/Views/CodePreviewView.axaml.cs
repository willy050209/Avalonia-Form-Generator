// filepath: src/AFG.Shared/Views/CodePreviewView.axaml.cs
using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using AFG.Shared.Services;
using AFG.Shared.ViewModels;

namespace AFG.Shared.Views;

public partial class CodePreviewView : UserControl
{
    private SelectableTextBlock? _viewTextBlock;
    private SelectableTextBlock? _vmTextBlock;

    private MainViewModel? _observedVm;
    private string _currentViewCode = string.Empty;
    private string _currentVmCode = string.Empty;

    private readonly Avalonia.Threading.DispatcherTimer _codeDebounceTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(150)
    };

    public CodePreviewView()
    {
        InitializeComponent();

        _codeDebounceTimer.Tick += (_, _) =>
        {
            _codeDebounceTimer.Stop();
            if (DataContext is MainViewModel vm)
            {
                SetCode(vm.GeneratedViewCode, vm.GeneratedVmCode);
            }
        };

        Loaded += (_, _) =>
        {
            InitControls();
            if (DataContext is MainViewModel vm)
            {
                SetCode(vm.GeneratedViewCode, vm.GeneratedVmCode);
            }
            else if (!string.IsNullOrEmpty(_currentViewCode) || !string.IsNullOrEmpty(_currentVmCode))
            {
                SetCode(_currentViewCode, _currentVmCode);
            }
        };

        DataContextChanged += (_, _) =>
        {
            if (_observedVm is not null)
            {
                _observedVm.PropertyChanged -= OnViewModelPropertyChanged;
                _observedVm = null;
            }

            if (DataContext is MainViewModel vm)
            {
                _observedVm = vm;
                vm.PropertyChanged += OnViewModelPropertyChanged;
                SetCode(vm.GeneratedViewCode, vm.GeneratedVmCode);
            }
        };

        if (DataContext is MainViewModel initialVm)
        {
            _observedVm = initialVm;
            initialVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void InitControls()
    {
        _viewTextBlock ??= this.FindControl<SelectableTextBlock>("ViewCodeTextBlock");
        _vmTextBlock ??= this.FindControl<SelectableTextBlock>("VmCodeTextBlock");
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.GeneratedViewCode) ||
            e.PropertyName == nameof(MainViewModel.GeneratedVmCode))
        {
            _codeDebounceTimer.Stop();
            _codeDebounceTimer.Start();
        }
    }

    /// <summary>
    /// 設定並即時著色 View 與 ViewModel 程式碼。
    /// </summary>
    public void SetCode(string viewCode, string vmCode)
    {
        InitControls();
        _currentViewCode = viewCode ?? string.Empty;
        _currentVmCode = vmCode ?? string.Empty;

        if (_viewTextBlock?.Inlines is not null)
        {
            CSharpSyntaxColorizer.PopulateInlines(_viewTextBlock.Inlines, _currentViewCode);
        }

        if (_vmTextBlock?.Inlines is not null)
        {
            CSharpSyntaxColorizer.PopulateInlines(_vmTextBlock.Inlines, _currentVmCode);
        }
    }

    private async void OnCopyViewClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_currentViewCode))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is not null)
            {
                await topLevel.Clipboard.SetTextAsync(_currentViewCode);
            }
        }
    }

    private async void OnCopyVmClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_currentVmCode))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is not null)
            {
                await topLevel.Clipboard.SetTextAsync(_currentVmCode);
            }
        }
    }
}
