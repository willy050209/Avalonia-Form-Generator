// filepath: src/AFG.Shared/Views/MainView.axaml.cs
using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AFG.Shared.ViewModels;

namespace AFG.Shared.Views;

public partial class MainView : UserControl
{
    private readonly RegistryOptions _registryOptions = new(ThemeName.DarkPlus);
    private TextMate.Installation? _viewTextMate;
    private TextMate.Installation? _vmTextMate;

    public MainView()
    {
        InitializeComponent();

        Loaded += (_, _) => RefreshAllEditors();
        AttachedToVisualTree += (_, _) => RefreshAllEditors();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged -= OnViewModelPropertyChanged;
                vm.PropertyChanged += OnViewModelPropertyChanged;
                RefreshAllEditors();
            }
        };

        if (DataContext is MainViewModel initialVm)
        {
            initialVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshAllEditors();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainViewModel vm)
        {
            if (e.PropertyName == nameof(MainViewModel.GeneratedViewCode) ||
                e.PropertyName == nameof(MainViewModel.GeneratedVmCode))
            {
                RefreshAllEditors();
            }
        }
    }

    private void RefreshAllEditors()
    {
        if (DataContext is not MainViewModel vm) return;

        var viewEditor = this.FindControl<TextEditor>("ViewCodeEditor");
        var vmEditor = this.FindControl<TextEditor>("VmCodeEditor");

        EnsureEditor(viewEditor, ref _viewTextMate, vm.GeneratedViewCode);
        EnsureEditor(vmEditor, ref _vmTextMate, vm.GeneratedVmCode);
    }

    private void EnsureEditor(TextEditor? editor, ref TextMate.Installation? textMate, string? content)
    {
        if (editor is null) return;

        if (textMate is null)
        {
            try
            {
                textMate = editor.InstallTextMate(_registryOptions);
                textMate.SetGrammar(_registryOptions.GetScopeByLanguageId("csharp"));
            }
            catch
            {
                // 忽略 TextMate 重複安裝異常
            }
        }

        var targetText = content ?? string.Empty;
        if (editor.Text != targetText)
        {
            editor.Text = targetText;
        }
    }
}
