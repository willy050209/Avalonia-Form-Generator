// filepath: src/AFG.Desktop/Services/DesktopServices.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AFG.Shared.Services;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

namespace AFG.Desktop.Services;

public sealed class DesktopFileDialogService(Func<TopLevel?> topLevelProvider) : IFileDialogService
{
    private readonly Func<TopLevel?> _topLevelProvider = topLevelProvider ?? throw new ArgumentNullException(nameof(topLevelProvider));

    public async Task<string?> OpenFileDialogAsync(string title, string filterExtension = "afg.json", string filterName = "AFG 表單模型")
    {
        var topLevel = _topLevelProvider();
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(filterName)
                {
                    Patterns = [$"{filterExtension}", "*.json", "*.*"]
                }
            ]
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<string?> OpenImageFileDialogAsync(string title = "選擇圖片檔案")
    {
        var topLevel = _topLevelProvider();
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("所有支援的圖片檔案")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp", "*.ico"]
                },
                new FilePickerFileType("PNG 影像檔 (*.png)") { Patterns = ["*.png"] },
                new FilePickerFileType("JPEG 影像檔 (*.jpg;*.jpeg)") { Patterns = ["*.jpg", "*.jpeg"] },
                new FilePickerFileType("點陣圖 (*.bmp)") { Patterns = ["*.bmp"] },
                new FilePickerFileType("所有檔案 (*.*)") { Patterns = ["*.*"] }
            ]
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<string?> SaveFileDialogAsync(string title, string defaultFileName, string filterExtension, string filterName)
    {
        var topLevel = _topLevelProvider();
        if (topLevel is null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName,
            DefaultExtension = filterExtension,
            FileTypeChoices =
            [
                new FilePickerFileType(filterName)
                {
                    Patterns = [$"*.{filterExtension}", "*.json"]
                }
            ]
        });

        return file?.Path.LocalPath;
    }

    public async Task<string?> OpenFolderDialogAsync(string title)
    {
        var topLevel = _topLevelProvider();
        if (topLevel is null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}

public sealed class DesktopClipboardService(Func<TopLevel?> topLevelProvider) : IClipboardService
{
    private readonly Func<TopLevel?> _topLevelProvider = topLevelProvider ?? throw new ArgumentNullException(nameof(topLevelProvider));

    public async Task SetTextAsync(string text)
    {
        var topLevel = _topLevelProvider();
        if (topLevel?.Clipboard is not null)
        {
            await topLevel.Clipboard.SetTextAsync(text);
        }
    }

    public async Task<string?> GetTextAsync()
    {
        var topLevel = _topLevelProvider();
        if (topLevel?.Clipboard is not null)
        {
            return await topLevel.Clipboard.TryGetTextAsync();
        }

        return null;
    }
}
