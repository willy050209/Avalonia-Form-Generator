// filepath: src/AFG.Shared/Views/MainView.axaml.cs
using System;
using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AFG.Shared.ViewModels;

namespace AFG.Shared.Views;

public partial class MainView : UserControl
{
    private TextMate.Installation? _viewTextMate;
    private TextMate.Installation? _vmTextMate;

    public MainView()
    {
        InitializeComponent();

        var viewEditor = this.FindControl<TextEditor>("ViewCodeEditor");
        var vmEditor = this.FindControl<TextEditor>("VmCodeEditor");

        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        if (viewEditor is not null)
        {
            _viewTextMate = viewEditor.InstallTextMate(registryOptions);
            _viewTextMate.SetGrammar(registryOptions.GetScopeByLanguageId("csharp"));
        }

        if (vmEditor is not null)
        {
            _vmTextMate = vmEditor.InstallTextMate(registryOptions);
            _vmTextMate.SetGrammar(registryOptions.GetScopeByLanguageId("csharp"));
        }

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateEditorText(vm);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainViewModel vm)
        {
            if (e.PropertyName == nameof(MainViewModel.GeneratedViewCode) ||
                e.PropertyName == nameof(MainViewModel.GeneratedVmCode))
            {
                UpdateEditorText(vm);
            }
        }
    }

    private void UpdateEditorText(MainViewModel vm)
    {
        var viewEditor = this.FindControl<TextEditor>("ViewCodeEditor");
        var vmEditor = this.FindControl<TextEditor>("VmCodeEditor");

        if (viewEditor is not null && viewEditor.Text != vm.GeneratedViewCode)
        {
            viewEditor.Text = vm.GeneratedViewCode ?? string.Empty;
        }

        if (vmEditor is not null && vmEditor.Text != vm.GeneratedVmCode)
        {
            vmEditor.Text = vm.GeneratedVmCode ?? string.Empty;
        }
    }
}
