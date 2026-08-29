// filepath: src/AFG.Shared/Services/ServiceInterfaces.cs
namespace AFG.Shared.Services;

/// <summary>
/// 檔案對話框服務介面。
/// </summary>
public interface IFileDialogService
{
    Task<string?> OpenFileDialogAsync(string title, string filterExtension = "afg.json", string filterName = "AFG 表單模型");
    Task<string?> OpenImageFileDialogAsync(string title = "選擇圖片檔案");
    Task<string?> OpenMediaFileDialogAsync(string title = "選擇多媒體檔案");
    Task<string?> SaveFileDialogAsync(string title, string defaultFileName, string filterExtension, string filterName);
    Task<string?> OpenFolderDialogAsync(string title);
}

/// <summary>
/// 剪貼簿存取服務介面。
/// </summary>
public interface IClipboardService
{
    Task SetTextAsync(string text);
    Task<string?> GetTextAsync();
}

/// <summary>
/// 使用者提示與通知服務介面。
/// </summary>
public interface INotificationService
{
    void Show(string title, string message, bool isError = false);
}
