// filepath: src/AFG.Shared/Helpers/BitmapHelper.cs
using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Media.Imaging;

/// <summary>
/// 提供 Bitmap / WriteableBitmap 像素存取、格式轉換與初始化的擴充方法。
/// </summary>
public static class BitmapExtensions
{
    /// <summary>
    /// 建立指定尺寸與背景顏色之已初始化的 WriteableBitmap。
    /// </summary>
    public static WriteableBitmap CreateInitializedBitmap(int width, int height, Color? backgroundColor = null)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var color = backgroundColor ?? Color.Parse("#F0F0F0");
        var wb = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            Platform.PixelFormat.Bgra8888,
            Platform.AlphaFormat.Premul);

        using var fb = wb.Lock();
        unsafe
        {
            byte b = color.B, g = color.G, r = color.R, a = color.A;
            byte* bytePtr = (byte*)fb.Address;
            for (int y = 0; y < height; y++)
            {
                byte* row = bytePtr + y * fb.RowBytes;
                for (int x = 0; x < width; x++)
                {
                    row[x * 4 + 0] = b;
                    row[x * 4 + 1] = g;
                    row[x * 4 + 2] = r;
                    row[x * 4 + 3] = a;
                }
            }
        }
        return wb;
    }

    /// <summary>
    /// 建立指定尺寸與背景顏色之已初始化的 WriteableBitmap。
    /// </summary>
    public static WriteableBitmap CreateInitializedBitmap(double width, double height, Color? backgroundColor = null) =>
        CreateInitializedBitmap((int)Math.Max(1, width), (int)Math.Max(1, height), backgroundColor);

    /// <summary>
    /// 將 Bitmap 轉換為可直接讀寫像素的 WriteableBitmap。
    /// </summary>
    public static WriteableBitmap ConvertToWriteableBitmap(this Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (bitmap is WriteableBitmap wb)
        {
            return wb;
        }

        var writeable = new WriteableBitmap(
            bitmap.PixelSize,
            bitmap.Dpi,
            Platform.PixelFormat.Bgra8888,
            Platform.AlphaFormat.Premul);

        using (var fb = writeable.Lock())
        {
            bitmap.CopyPixels(new PixelRect(PixelPoint.Origin, bitmap.PixelSize), fb.Address, fb.RowBytes * fb.Size.Height, fb.RowBytes);
        }

        return writeable;
    }

    /// <summary>
    /// 將 Bitmap 轉換為 RenderTargetBitmap。
    /// </summary>
    public static RenderTargetBitmap ConvertToRenderTargetBitmap(this Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (bitmap is RenderTargetBitmap rtb)
        {
            return rtb;
        }

        var renderTarget = new RenderTargetBitmap(bitmap.PixelSize, bitmap.Dpi);
        using (var ctx = renderTarget.CreateDrawingContext())
        {
            ctx.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height));
        }
        return renderTarget;
    }

    /// <summary>
    /// 設定 WriteableBitmap 指定座標 (x, y) 的像素顏色。
    /// </summary>
    public static void SetPixel(this WriteableBitmap bitmap, int x, int y, Color color)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        using var fb = bitmap.Lock();
        if (x < 0 || x >= fb.Size.Width || y < 0 || y >= fb.Size.Height) return;

        unsafe
        {
            byte* ptr = (byte*)fb.Address + y * fb.RowBytes + x * 4;
            ptr[0] = color.B;
            ptr[1] = color.G;
            ptr[2] = color.R;
            ptr[3] = color.A;
        }
    }

    /// <summary>
    /// 取得 WriteableBitmap 指定座標 (x, y) 的像素顏色。
    /// </summary>
    public static Color GetPixel(this WriteableBitmap bitmap, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        using var fb = bitmap.Lock();
        if (x < 0 || x >= fb.Size.Width || y < 0 || y >= fb.Size.Height) return Colors.Transparent;

        unsafe
        {
            byte* ptr = (byte*)fb.Address + y * fb.RowBytes + x * 4;
            return Color.FromArgb(ptr[3], ptr[2], ptr[1], ptr[0]);
        }
    }

    /// <summary>
    /// 載入本機檔案路徑或 avares:// 資源路徑之 Bitmap（具備動態組件修正、相對路徑尋訪與磁碟目錄後備容錯機制）。
    /// </summary>
    public static Bitmap? LoadBitmap(string? pathOrUri)
    {
        if (string.IsNullOrWhiteSpace(pathOrUri)) return null;

        var trimmed = pathOrUri.Trim();

        // 1. 若為本機實體檔案路徑且檔案存在，直接自檔案載入
        try
        {
            if (System.IO.File.Exists(trimmed))
            {
                return new Bitmap(trimmed);
            }
        }
        catch
        {
            // 忽略直接讀檔錯誤，進入後續資源/相對路徑解析
        }

        // 2. 解析 avares:// 或相對 Assets 資源路徑
        try
        {
            if (trimmed.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(trimmed);
                if (Platform.AssetLoader.Exists(uri))
                {
                    using var stream = Platform.AssetLoader.Open(uri);
                    return new Bitmap(stream);
                }

                // 若指定的 Assembly 在 URI 中不匹配 (例如 URI 為 avares://MainFormApp/Assets/logo.png 但實際組件名為 MainFormApp.Shared)
                // 提取相對資源路徑 (例如 "Assets/logo.png")
                var resourceRelativePath = uri.AbsolutePath.TrimStart('/');

                // 嘗試透過目前組件 (typeof(BitmapHelper).Assembly) 重新建構 avares:// URI
                var currentAsmName = typeof(BitmapHelper).Assembly.GetName().Name;
                if (!string.IsNullOrEmpty(currentAsmName))
                {
                    var candidateUri = new Uri($"avares://{currentAsmName}/{resourceRelativePath}");
                    if (Platform.AssetLoader.Exists(candidateUri))
                    {
                        using var stream = Platform.AssetLoader.Open(candidateUri);
                        return new Bitmap(stream);
                    }
                }

                // 嘗試透過 EntryAssembly 或 EntryAssembly.Shared 重新建構
                var entryAsm = System.Reflection.Assembly.GetEntryAssembly();
                if (entryAsm != null)
                {
                    var entryName = entryAsm.GetName().Name;
                    if (!string.IsNullOrEmpty(entryName))
                    {
                        var candidateUri1 = new Uri($"avares://{entryName}/{resourceRelativePath}");
                        if (Platform.AssetLoader.Exists(candidateUri1))
                        {
                            using var stream = Platform.AssetLoader.Open(candidateUri1);
                            return new Bitmap(stream);
                        }

                        var candidateUri2 = new Uri($"avares://{entryName}.Shared/{resourceRelativePath}");
                        if (Platform.AssetLoader.Exists(candidateUri2))
                        {
                            using var stream = Platform.AssetLoader.Open(candidateUri2);
                            return new Bitmap(stream);
                        }
                    }
                }

                // 嘗試掃描所有目前已載入的應用程式組件
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    var asmName = asm.GetName().Name;
                    if (string.IsNullOrEmpty(asmName) || asmName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) || asmName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)) continue;

                    var candidateUri = new Uri($"avares://{asmName}/{resourceRelativePath}");
                    if (Platform.AssetLoader.Exists(candidateUri))
                    {
                        using var stream = Platform.AssetLoader.Open(candidateUri);
                        return new Bitmap(stream);
                    }
                }
            }
            else
            {
                // 非 avares:// 開頭之相對路徑字串 (例如 "Assets/logo.png", "logo.png", "assets/images/pic.png")
                var cleanRelative = trimmed.TrimStart('/', '\\').Replace('\\', '/');
                var fileName = System.IO.Path.GetFileName(cleanRelative);
                var possiblePaths = new[]
                {
                    cleanRelative,
                    $"Assets/{cleanRelative}",
                    $"Assets/{fileName}"
                };

                var targetAsmNames = new System.Collections.Generic.List<string>();
                var currentAsmName = typeof(BitmapHelper).Assembly.GetName().Name;
                if (!string.IsNullOrEmpty(currentAsmName)) targetAsmNames.Add(currentAsmName);

                var entryName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
                if (!string.IsNullOrEmpty(entryName))
                {
                    targetAsmNames.Add(entryName);
                    targetAsmNames.Add($"{entryName}.Shared");
                }

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    var name = asm.GetName().Name;
                    if (!string.IsNullOrEmpty(name) && !name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) && !name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) && !targetAsmNames.Contains(name))
                    {
                        targetAsmNames.Add(name);
                    }
                }

                foreach (var asmName in targetAsmNames)
                {
                    foreach (var resPath in possiblePaths)
                    {
                        var candidateUri = new Uri($"avares://{asmName}/{resPath}");
                        if (Platform.AssetLoader.Exists(candidateUri))
                        {
                            using var stream = Platform.AssetLoader.Open(candidateUri);
                            return new Bitmap(stream);
                        }
                    }
                }
            }
        }
        catch
        {
            // 忽略資源解析異常，進入磁碟後備搜尋
        }

        // 3. 後備機制：在應用程式目錄、執行目錄與 Assets 子目錄搜尋實體檔案
        try
        {
            var rawFileName = System.IO.Path.GetFileName(trimmed.Replace('/', System.IO.Path.DirectorySeparatorChar).Replace('\\', System.IO.Path.DirectorySeparatorChar));
            var baseDir = AppContext.BaseDirectory;
            var currentDir = System.IO.Directory.GetCurrentDirectory();

            var diskCandidates = new[]
            {
                System.IO.Path.Combine(baseDir, trimmed),
                System.IO.Path.Combine(baseDir, "Assets", rawFileName),
                System.IO.Path.Combine(baseDir, rawFileName),
                System.IO.Path.Combine(currentDir, trimmed),
                System.IO.Path.Combine(currentDir, "Assets", rawFileName),
                System.IO.Path.Combine(currentDir, rawFileName)
            };

            foreach (var diskPath in diskCandidates)
            {
                if (System.IO.File.Exists(diskPath))
                {
                    return new Bitmap(diskPath);
                }
            }
        }
        catch
        {
            // 防禦性忽略磁碟讀取例外
        }

        return null;
    }
}

/// <summary>
/// 提供操作 Bitmap 的靜態類別便捷呼叫方法。
/// </summary>
public static class BitmapHelper
{
    public static WriteableBitmap CreateInitializedBitmap(int width, int height, Color? backgroundColor = null) =>
        BitmapExtensions.CreateInitializedBitmap(width, height, backgroundColor);

    public static WriteableBitmap CreateInitializedBitmap(double width, double height, Color? backgroundColor = null) =>
        BitmapExtensions.CreateInitializedBitmap((int)Math.Max(1, width), (int)Math.Max(1, height), backgroundColor);

    public static WriteableBitmap ConvertToWriteableBitmap(Bitmap bitmap) => bitmap.ConvertToWriteableBitmap();

    public static RenderTargetBitmap ConvertToRenderTargetBitmap(Bitmap bitmap) => bitmap.ConvertToRenderTargetBitmap();

    public static void SetPixel(WriteableBitmap bitmap, int x, int y, Color color) => bitmap.SetPixel(x, y, color);

    public static Color GetPixel(WriteableBitmap bitmap, int x, int y) => bitmap.GetPixel(x, y);

    public static Bitmap? LoadBitmap(string? pathOrUri) => BitmapExtensions.LoadBitmap(pathOrUri);
}