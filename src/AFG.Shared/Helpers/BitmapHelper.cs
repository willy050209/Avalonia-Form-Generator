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
    /// 載入本機檔案路徑或 avares:// 資源路徑之 Bitmap。
    /// </summary>
    public static Bitmap? LoadBitmap(string? pathOrUri)
    {
        if (string.IsNullOrWhiteSpace(pathOrUri)) return null;

        try
        {
            if (pathOrUri.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(pathOrUri);
                if (Platform.AssetLoader.Exists(uri))
                {
                    using var stream = Platform.AssetLoader.Open(uri);
                    return new Bitmap(stream);
                }
            }
            else if (System.IO.File.Exists(pathOrUri))
            {
                return new Bitmap(pathOrUri);
            }
        }
        catch
        {
            // 防禦性忽略載入異常
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