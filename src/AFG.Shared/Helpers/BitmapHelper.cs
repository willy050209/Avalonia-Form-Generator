// filepath: src/AFG.Shared/Helpers/BitmapHelper.cs
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Media.Imaging;

/// <summary>
/// 像素處理執行模式列舉。
/// </summary>
public enum PixelProcessingMode
{
    /// <summary>
    /// 單執行緒循序遍歷 (Sequential)。
    /// </summary>
    Sequential,

    /// <summary>
    /// 單執行緒 SIMD 向量化批次加速 (Sequential Vectorized)。
    /// </summary>
    SequentialVectorized,

    /// <summary>
    /// 多執行緒平行處理 (Parallel)。
    /// </summary>
    Parallel,

    /// <summary>
    /// 多執行緒平行 + SIMD 向量化加速 (Parallel Vectorized)。
    /// </summary>
    ParallelVectorized
}

/// <summary>
/// 像素變換回呼委派（傳遞 BGRA 4 個色板的直接記憶體引用）。
/// </summary>
public delegate void PixelProcessor(ref byte b, ref byte g, ref byte r, ref byte a);

/// <summary>
/// 帶座標之像素變換回呼委派。
/// </summary>
public delegate void PixelLocationProcessor(int x, int y, ref byte b, ref byte g, ref byte r, ref byte a);

/// <summary>
/// 向量化連續像素記憶體區塊處理委派（傳入對齊硬體向量寬度之 Span<byte>）。
/// </summary>
public delegate void VectorPixelProcessor(Span<byte> pixelSpan);

/// <summary>
/// 泛型 SIMD 向量變換委派（輸入硬體向量暫存器資料，回傳運算後之硬體向量暫存器資料）。
/// </summary>
public delegate Vector<byte> VectorTransform(Vector<byte> vector);

/// <summary>
/// 直接對硬體 SIMD 向量暫存器非託管記憶體指標進行操作之委派。
/// </summary>
public unsafe delegate void VectorPointerProcessor(byte* vectorPtr, int byteCount);

/// <summary>
/// 提供底層 SIMD 硬體加速能力偵測與向量輔助工具。
/// </summary>
public static class SimdHardware
{
    /// <summary>
    /// 偵測硬體是否支援泛型 SIMD 向量加速 (System.Numerics.Vector)。
    /// </summary>
    public static bool IsHardwareAccelerated => global::System.Numerics.Vector.IsHardwareAccelerated;

    /// <summary>
    /// 偵測硬體是否支援 512 位元向量暫存器 (AVX-512)。
    /// </summary>
    public static bool HasVector512 => Vector512.IsHardwareAccelerated;

    /// <summary>
    /// 偵測硬體是否支援 256 位元向量暫存器 (AVX2)。
    /// </summary>
    public static bool HasVector256 => Vector256.IsHardwareAccelerated;

    /// <summary>
    /// 偵測硬體是否支援 128 位元向量暫存器 (SSE2 / ARM AdvSIMD)。
    /// </summary>
    public static bool HasVector128 => Vector128.IsHardwareAccelerated;

    /// <summary>
    /// 取得目前裝置支援之最佳向量位元組長度 (512-bit=64B, 256-bit=32B, 128-bit=16B)。
    /// </summary>
    public static int PreferredVectorByteCount
    {
        get
        {
            if (Vector512.IsHardwareAccelerated) return 64;
            if (Vector256.IsHardwareAccelerated) return 32;
            if (Vector128.IsHardwareAccelerated) return 16;
            return global::System.Numerics.Vector<byte>.Count;
        }
    }

    /// <summary>
    /// 取得目前裝置最佳向量可容納的 BGRA 像素數量 (64B=16px, 32B=8px, 16B=4px)。
    /// </summary>
    public static int PreferredPixelBatchCount => Math.Max(1, PreferredVectorByteCount / 4);
}

/// <summary>
/// 提供 Bitmap / WriteableBitmap 高效能直接記憶體像素操作、模式遍歷、向量化加速與影像處理擴充方法。
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

    // ==========================================
    // 1. 高效直接記憶體遍歷方法 (ProcessPixels)
    // ==========================================

    /// <summary>
    /// 透過泛型 SIMD 向量變換委派 (Vector&lt;byte&gt;) 批次處理像素記憶體，直接讀寫硬體向量暫存器 (AVX2/SSE/NEON) 並支援多執行緒加速。
    /// </summary>
    /// <param name="bitmap">目標 WriteableBitmap</param>
    /// <param name="vectorTransform">SIMD 向量運算委派 (傳入硬體暫存器向量並回傳變換後向量)</param>
    /// <param name="remainderProcessor">未對齊向量步長之邊界像素處理回呼（可為 null）</param>
    /// <param name="mode">處理執行模式</param>
    public static void ProcessPixels(
        this WriteableBitmap bitmap,
        VectorTransform vectorTransform,
        PixelProcessor? remainderProcessor = null,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(vectorTransform);

        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int rowBytes = fb.RowBytes;
        int vectorByteSize = global::System.Numerics.Vector<byte>.Count;
        int totalBytes = width * 4;
        int vectorLimit = totalBytes - (totalBytes % vectorByteSize);

        unsafe
        {
            byte* scan0 = (byte*)fb.Address;

            void ProcessRow(int y)
            {
                byte* row = scan0 + y * rowBytes;
                int x = 0;

                // 1. 真實硬體 SIMD 向量指令並行批次運算 (直接指標讀寫暫存器)
                if (SimdHardware.IsHardwareAccelerated)
                {
                    for (; x < vectorLimit; x += vectorByteSize)
                    {
                        var inVec = *(global::System.Numerics.Vector<byte>*)(row + x);
                        var outVec = vectorTransform(inVec);
                        *(global::System.Numerics.Vector<byte>*)(row + x) = outVec;
                    }
                }

                // 2. 處理邊界剩餘無法對齊向量長度的像素
                if (remainderProcessor != null)
                {
                    for (int px = x / 4; px < width; px++)
                    {
                        byte* p = row + px * 4;
                        remainderProcessor(ref p[0], ref p[1], ref p[2], ref p[3]);
                    }
                }
            }

            if (mode is PixelProcessingMode.Parallel or PixelProcessingMode.ParallelVectorized)
            {
                Parallel.For(0, height, ProcessRow);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    ProcessRow(y);
                }
            }
        }
    }

    /// <summary>
    /// 直接傳遞非託管記憶體指標 (byte* vectorPtr) 進行硬體 SIMD 向量運算，達到零 GC 與無封裝之極限效能。
    /// </summary>
    /// <param name="bitmap">目標 WriteableBitmap</param>
    /// <param name="pointerProcessor">直接記憶體指標處理回呼</param>
    /// <param name="remainderProcessor">未對齊向量步長之邊界像素處理回呼（可為 null）</param>
    /// <param name="mode">處理執行模式</param>
    public static void ProcessPixels(
        this WriteableBitmap bitmap,
        VectorPointerProcessor pointerProcessor,
        PixelProcessor? remainderProcessor = null,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(pointerProcessor);

        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int rowBytes = fb.RowBytes;
        int vectorByteSize = SimdHardware.PreferredVectorByteCount;
        int totalBytes = width * 4;
        int vectorLimit = totalBytes - (totalBytes % vectorByteSize);

        unsafe
        {
            byte* scan0 = (byte*)fb.Address;

            void ProcessRow(int y)
            {
                byte* row = scan0 + y * rowBytes;
                int x = 0;

                // 1. 直接以指標進行 SIMD 向量區塊呼叫
                for (; x < vectorLimit; x += vectorByteSize)
                {
                    pointerProcessor(row + x, vectorByteSize);
                }

                // 2. 邊界未對齊像素處理
                if (remainderProcessor != null)
                {
                    for (int px = x / 4; px < width; px++)
                    {
                        byte* p = row + px * 4;
                        remainderProcessor(ref p[0], ref p[1], ref p[2], ref p[3]);
                    }
                }
            }

            if (mode is PixelProcessingMode.Parallel or PixelProcessingMode.ParallelVectorized)
            {
                Parallel.For(0, height, ProcessRow);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    ProcessRow(y);
                }
            }
        }
    }

    /// <summary>
    /// 透過 Span&lt;byte&gt; 向量區塊委派批次處理像素記憶體，自動偵測硬體最佳 SIMD 位元組步長 (64B / 32B / 16B)。
    /// </summary>
    /// <param name="bitmap">目標 WriteableBitmap</param>
    /// <param name="vectorProcessor">向量記憶體區塊處理委派</param>
    /// <param name="remainderProcessor">未對齊向量步長之邊界像素處理回呼（可為 null）</param>
    /// <param name="mode">處理執行模式</param>
    public static void ProcessPixels(
        this WriteableBitmap bitmap,
        VectorPixelProcessor vectorProcessor,
        PixelProcessor? remainderProcessor = null,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(vectorProcessor);

        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int rowBytes = fb.RowBytes;
        int vectorByteSize = SimdHardware.PreferredVectorByteCount;
        int totalBytes = width * 4;
        int vectorLimit = totalBytes - (totalBytes % vectorByteSize);

        unsafe
        {
            byte* scan0 = (byte*)fb.Address;

            void ProcessRow(int y)
            {
                byte* row = scan0 + y * rowBytes;
                int x = 0;

                // 1. 硬體最佳化向量區塊批次呼叫
                for (; x < vectorLimit; x += vectorByteSize)
                {
                    vectorProcessor(new Span<byte>(row + x, vectorByteSize));
                }

                // 2. 邊界未對齊像素處理
                if (remainderProcessor != null)
                {
                    for (int px = x / 4; px < width; px++)
                    {
                        byte* p = row + px * 4;
                        remainderProcessor(ref p[0], ref p[1], ref p[2], ref p[3]);
                    }
                }
            }

            if (mode is PixelProcessingMode.Parallel or PixelProcessingMode.ParallelVectorized)
            {
                Parallel.For(0, height, ProcessRow);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    ProcessRow(y);
                }
            }
        }
    }

    /// <summary>
    /// 直接使用硬體 SIMD 指令 (System.Numerics.Vector) 對像素緩衝區進行高吞吐量向量變換。
    /// </summary>
    public static void ProcessPixelsSimdHardware(
        this WriteableBitmap bitmap,
        VectorTransform vectorTransform,
        PixelProcessor? remainderProcessor = null) =>
        bitmap.ProcessPixels(vectorTransform, remainderProcessor, PixelProcessingMode.ParallelVectorized);

    /// <summary>
    /// 直接對記憶體緩衝區進行像素遍歷處理，支援串列、串列向量化、平行與平行向量化四種執行模式。
    /// </summary>
    /// <param name="bitmap">目標 WriteableBitmap</param>
    /// <param name="processor">像素處理回呼委派 (ref b, ref g, ref r, ref a)</param>
    /// <param name="mode">處理執行模式</param>
    public static void ProcessPixels(
        this WriteableBitmap bitmap,
        PixelProcessor processor,
        PixelProcessingMode mode = PixelProcessingMode.Parallel)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(processor);

        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int rowBytes = fb.RowBytes;
        int batchSize = SimdHardware.PreferredPixelBatchCount;

        unsafe
        {
            byte* scan0 = (byte*)fb.Address;

            void ProcessRow(int y, bool vectorized)
            {
                byte* row = scan0 + y * rowBytes;
                int x = 0;

                if (vectorized && width >= batchSize)
                {
                    int unrollLimit = width - (width % batchSize);
                    for (; x < unrollLimit; x += batchSize)
                    {
                        for (int i = 0; i < batchSize; i++)
                        {
                            byte* p = row + (x + i) * 4;
                            processor(ref p[0], ref p[1], ref p[2], ref p[3]);
                        }
                    }
                }

                for (; x < width; x++)
                {
                    byte* p = row + x * 4;
                    processor(ref p[0], ref p[1], ref p[2], ref p[3]);
                }
            }

            switch (mode)
            {
                case PixelProcessingMode.Sequential:
                    for (int y = 0; y < height; y++) ProcessRow(y, vectorized: false);
                    break;
                case PixelProcessingMode.SequentialVectorized:
                    for (int y = 0; y < height; y++) ProcessRow(y, vectorized: true);
                    break;
                case PixelProcessingMode.Parallel:
                    Parallel.For(0, height, y => ProcessRow(y, vectorized: false));
                    break;
                case PixelProcessingMode.ParallelVectorized:
                    Parallel.For(0, height, y => ProcessRow(y, vectorized: true));
                    break;
            }
        }
    }

    /// <summary>
    /// 直接對記憶體緩衝區進行帶座標之像素遍歷處理。
    /// </summary>
    public static void ProcessPixels(
        this WriteableBitmap bitmap,
        PixelLocationProcessor processor,
        PixelProcessingMode mode = PixelProcessingMode.Parallel)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(processor);

        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int rowBytes = fb.RowBytes;
        int batchSize = SimdHardware.PreferredPixelBatchCount;

        unsafe
        {
            byte* scan0 = (byte*)fb.Address;

            void ProcessRow(int y, bool vectorized)
            {
                byte* row = scan0 + y * rowBytes;
                int x = 0;

                if (vectorized && width >= batchSize)
                {
                    int unrollLimit = width - (width % batchSize);
                    for (; x < unrollLimit; x += batchSize)
                    {
                        for (int i = 0; i < batchSize; i++)
                        {
                            byte* p = row + (x + i) * 4;
                            processor(x + i, y, ref p[0], ref p[1], ref p[2], ref p[3]);
                        }
                    }
                }

                for (; x < width; x++)
                {
                    byte* p = row + x * 4;
                    processor(x, y, ref p[0], ref p[1], ref p[2], ref p[3]);
                }
            }

            switch (mode)
            {
                case PixelProcessingMode.Sequential:
                    for (int y = 0; y < height; y++) ProcessRow(y, vectorized: false);
                    break;
                case PixelProcessingMode.SequentialVectorized:
                    for (int y = 0; y < height; y++) ProcessRow(y, vectorized: true);
                    break;
                case PixelProcessingMode.Parallel:
                    Parallel.For(0, height, y => ProcessRow(y, vectorized: false));
                    break;
                case PixelProcessingMode.ParallelVectorized:
                    Parallel.For(0, height, y => ProcessRow(y, vectorized: true));
                    break;
            }
        }
    }

    // ==========================================
    // 2. 常用影像處理函數 (Grayscale, Edge, Blur)
    // ==========================================

    /// <summary>
    /// 將 Bitmap 轉為灰階 WriteableBitmap（支援 SIMD 向量化與多執行緒加速）。
    /// </summary>
    public static WriteableBitmap ToGrayscale(this Bitmap bitmap, PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var wb = bitmap.ConvertToWriteableBitmap();
        ApplyGrayscale(wb, mode);
        return wb;
    }

    /// <summary>
    /// 原地對 WriteableBitmap 進行灰階化運算（採用 ITU-R BT.601 亮度加權演算法）。
    /// </summary>
    public static void ApplyGrayscale(this WriteableBitmap bitmap, PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int rowBytes = fb.RowBytes;

        unsafe
        {
            byte* scan0 = (byte*)fb.Address;

            switch (mode)
            {
                case PixelProcessingMode.Sequential:
                    for (int y = 0; y < height; y++)
                    {
                        byte* row = scan0 + y * rowBytes;
                        for (int x = 0; x < width; x++)
                        {
                            byte* p = row + x * 4;
                            byte gray = (byte)((299 * p[2] + 587 * p[1] + 114 * p[0] + 500) / 1000);
                            p[0] = gray;
                            p[1] = gray;
                            p[2] = gray;
                        }
                    }
                    break;

                case PixelProcessingMode.SequentialVectorized:
                    for (int y = 0; y < height; y++)
                    {
                        ApplyGrayscaleRowSimd(scan0 + y * rowBytes, width);
                    }
                    break;

                case PixelProcessingMode.Parallel:
                    Parallel.For(0, height, y =>
                    {
                        byte* row = scan0 + y * rowBytes;
                        for (int x = 0; x < width; x++)
                        {
                            byte* p = row + x * 4;
                            byte gray = (byte)((299 * p[2] + 587 * p[1] + 114 * p[0] + 500) / 1000);
                            p[0] = gray;
                            p[1] = gray;
                            p[2] = gray;
                        }
                    });
                    break;

                case PixelProcessingMode.ParallelVectorized:
                    Parallel.For(0, height, y =>
                    {
                        ApplyGrayscaleRowSimd(scan0 + y * rowBytes, width);
                    });
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ApplyGrayscaleRowSimd(byte* row, int width)
    {
        int x = 0;

        if (SimdHardware.HasVector128 && width >= 4)
        {
            int unrollLimit = width - (width % 4);
            for (; x < unrollLimit; x += 4)
            {
                var v = *(Vector128<byte>*)(row + x * 4);
                var low = Vector128.WidenLower(v);
                var high = Vector128.WidenUpper(v);

                byte g0 = (byte)((299 * low[2] + 587 * low[1] + 114 * low[0] + 500) / 1000);
                byte g1 = (byte)((299 * low[6] + 587 * low[5] + 114 * low[4] + 500) / 1000);
                byte g2 = (byte)((299 * high[2] + 587 * high[1] + 114 * high[0] + 500) / 1000);
                byte g3 = (byte)((299 * high[6] + 587 * high[5] + 114 * high[4] + 500) / 1000);

                *(Vector128<byte>*)(row + x * 4) = Vector128.Create(
                    g0, g0, g0, (byte)low[3],
                    g1, g1, g1, (byte)low[7],
                    g2, g2, g2, (byte)high[3],
                    g3, g3, g3, (byte)high[7]);
            }
        }

        for (; x < width; x++)
        {
            byte* p = row + x * 4;
            byte gray = (byte)((299 * p[2] + 587 * p[1] + 114 * p[0] + 500) / 1000);
            p[0] = gray;
            p[1] = gray;
            p[2] = gray;
        }
    }

    /// <summary>
    /// 原地對 WriteableBitmap 進行硬體 SIMD 顏色反相運算 (255 - Channel)。
    /// </summary>
    public static void ApplyInvert(this WriteableBitmap bitmap, PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int rowBytes = fb.RowBytes;
        int vectorSize = global::System.Numerics.Vector<byte>.Count;
        int totalBytes = width * 4;
        int vectorLimit = totalBytes - (totalBytes % vectorSize);

        unsafe
        {
            byte* scan0 = (byte*)fb.Address;
            var mask = new global::System.Numerics.Vector<byte>(255);

            void ProcessRow(int y)
            {
                byte* row = scan0 + y * rowBytes;
                int x = 0;

                if (SimdHardware.IsHardwareAccelerated)
                {
                    for (; x < vectorLimit; x += vectorSize)
                    {
                        var vec = *(global::System.Numerics.Vector<byte>*)(row + x);
                        *(global::System.Numerics.Vector<byte>*)(row + x) = mask - vec;
                    }
                }

                for (; x < totalBytes; x += 4)
                {
                    row[x + 0] = (byte)(255 - row[x + 0]);
                    row[x + 1] = (byte)(255 - row[x + 1]);
                    row[x + 2] = (byte)(255 - row[x + 2]);
                }
            }

            if (mode is PixelProcessingMode.Parallel or PixelProcessingMode.ParallelVectorized)
            {
                Parallel.For(0, height, ProcessRow);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    ProcessRow(y);
                }
            }
        }
    }

    /// <summary>
    /// 執行 3x3 Sobel 邊緣偵測濾鏡，回傳新邊緣點陣圖。
    /// </summary>
    /// <param name="bitmap">原始影像</param>
    /// <param name="threshold">邊緣強度過濾門檻值 (0~255)</param>
    /// <param name="mode">運算模式</param>
    public static WriteableBitmap DetectEdges(
        this Bitmap bitmap,
        double threshold = 0.0,
        PixelProcessingMode mode = PixelProcessingMode.Parallel)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var wb = bitmap.ConvertToWriteableBitmap();
        var result = new WriteableBitmap(
            wb.PixelSize,
            wb.Dpi,
            Platform.PixelFormat.Bgra8888,
            Platform.AlphaFormat.Premul);

        ApplySobelInternal(wb, result, threshold, mode);
        return result;
    }

    /// <summary>
    /// 對 WriteableBitmap 原地套用 Sobel 邊緣偵測運算。
    /// </summary>
    public static void ApplySobel(
        this WriteableBitmap bitmap,
        double threshold = 0.0,
        PixelProcessingMode mode = PixelProcessingMode.Parallel)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var temp = new WriteableBitmap(
            bitmap.PixelSize,
            bitmap.Dpi,
            Platform.PixelFormat.Bgra8888,
            Platform.AlphaFormat.Premul);

        ApplySobelInternal(bitmap, temp, threshold, mode);

        using var srcFb = temp.Lock();
        using var dstFb = bitmap.Lock();
        unsafe
        {
            Buffer.MemoryCopy(
                (void*)srcFb.Address,
                (void*)dstFb.Address,
                (long)dstFb.RowBytes * dstFb.Size.Height,
                (long)srcFb.RowBytes * srcFb.Size.Height);
        }
    }

    private static void ApplySobelInternal(
        WriteableBitmap src,
        WriteableBitmap dst,
        double threshold,
        PixelProcessingMode mode)
    {
        using var srcFb = src.Lock();
        using var dstFb = dst.Lock();

        int width = srcFb.Size.Width;
        int height = srcFb.Size.Height;
        int srcRowBytes = srcFb.RowBytes;
        int dstRowBytes = dstFb.RowBytes;

        unsafe
        {
            byte* srcScan0 = (byte*)srcFb.Address;
            byte* dstScan0 = (byte*)dstFb.Address;

            void ProcessRow(int y)
            {
                byte* rowDst = dstScan0 + y * dstRowBytes;

                if (y == 0 || y == height - 1)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte* p = rowDst + x * 4;
                        p[0] = 0; p[1] = 0; p[2] = 0; p[3] = 255;
                    }
                    return;
                }

                byte* rowSrcPrev = srcScan0 + (y - 1) * srcRowBytes;
                byte* rowSrcCurr = srcScan0 + y * srcRowBytes;
                byte* rowSrcNext = srcScan0 + (y + 1) * srcRowBytes;

                // 邊界 x=0
                rowDst[0] = 0; rowDst[1] = 0; rowDst[2] = 0; rowDst[3] = 255;

                for (int x = 1; x < width - 1; x++)
                {
                    int p00 = GetLuminance(rowSrcPrev + (x - 1) * 4);
                    int p01 = GetLuminance(rowSrcPrev + x * 4);
                    int p02 = GetLuminance(rowSrcPrev + (x + 1) * 4);

                    int p10 = GetLuminance(rowSrcCurr + (x - 1) * 4);
                    int p12 = GetLuminance(rowSrcCurr + (x + 1) * 4);

                    int p20 = GetLuminance(rowSrcNext + (x - 1) * 4);
                    int p21 = GetLuminance(rowSrcNext + x * 4);
                    int p22 = GetLuminance(rowSrcNext + (x + 1) * 4);

                    int gx = (p02 + 2 * p12 + p22) - (p00 + 2 * p10 + p20);
                    int gy = (p20 + 2 * p21 + p22) - (p00 + 2 * p01 + p02);

                    int mag = (int)Math.Sqrt(gx * gx + gy * gy);
                    if (mag > 255) mag = 255;
                    if (mag < threshold) mag = 0;

                    byte* p = rowDst + x * 4;
                    byte val = (byte)mag;
                    p[0] = val;
                    p[1] = val;
                    p[2] = val;
                    p[3] = 255;
                }

                // 邊界 x = width - 1
                byte* pLast = rowDst + (width - 1) * 4;
                pLast[0] = 0; pLast[1] = 0; pLast[2] = 0; pLast[3] = 255;
            }

            if (mode is PixelProcessingMode.Parallel or PixelProcessingMode.ParallelVectorized)
            {
                Parallel.For(0, height, ProcessRow);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    ProcessRow(y);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int GetLuminance(byte* p) =>
        (299 * p[2] + 587 * p[1] + 114 * p[0] + 500) / 1000;

    /// <summary>
    /// 對影像套用高斯模糊，回傳新點陣圖。
    /// </summary>
    /// <param name="bitmap">原始影像</param>
    /// <param name="radius">模糊半徑 (像素，預設 2)</param>
    /// <param name="mode">運算模式</param>
    public static WriteableBitmap ApplyBlur(
        this Bitmap bitmap,
        int radius = 2,
        PixelProcessingMode mode = PixelProcessingMode.Parallel)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var wb = bitmap.ConvertToWriteableBitmap();
        ApplyGaussianBlur(wb, radius, mode);
        return wb;
    }

    /// <summary>
    /// 原地對 WriteableBitmap 進行高斯模糊運算（採用 2-Pass 1D 可分離卷積極速演算法）。
    /// </summary>
    public static void ApplyGaussianBlur(
        this WriteableBitmap bitmap,
        int radius = 2,
        PixelProcessingMode mode = PixelProcessingMode.Parallel)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        radius = Math.Clamp(radius, 1, 50);

        // 建立 1D 高斯核
        int size = radius * 2 + 1;
        float[] kernel = new float[size];
        float sigma = Math.Max((float)radius / 2.0f, 0.5f);
        float twoSigmaSq = 2.0f * sigma * sigma;
        float sum = 0f;

        for (int i = -radius; i <= radius; i++)
        {
            float val = MathF.Exp(-(i * i) / twoSigmaSq);
            kernel[i + radius] = val;
            sum += val;
        }

        for (int i = 0; i < size; i++)
        {
            kernel[i] /= sum;
        }

        ApplySeparableConvolution(bitmap, kernel, radius, mode);
    }

    /// <summary>
    /// 原地對 WriteableBitmap 進行均值模糊 (Box Blur) 運算。
    /// </summary>
    public static void ApplyBoxBlur(
        this WriteableBitmap bitmap,
        int radius = 2,
        PixelProcessingMode mode = PixelProcessingMode.Parallel)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        radius = Math.Clamp(radius, 1, 50);
        int size = radius * 2 + 1;
        float weight = 1.0f / size;
        float[] kernel = new float[size];
        Array.Fill(kernel, weight);

        ApplySeparableConvolution(bitmap, kernel, radius, mode);
    }

    private static unsafe void ApplySeparableConvolution(
        WriteableBitmap bitmap,
        float[] kernel,
        int radius,
        PixelProcessingMode mode)
    {
        using var fb = bitmap.Lock();
        int width = fb.Size.Width;
        int height = fb.Size.Height;
        int rowBytes = fb.RowBytes;

        nuint bufferSize = (nuint)rowBytes * (nuint)height;
        byte* tempScan0 = (byte*)System.Runtime.InteropServices.NativeMemory.Alloc(bufferSize);

        try
        {
            byte* scan0 = (byte*)fb.Address;

            // Pass 1: 水平模糊 (Source -> Temp)
            void HorizontalPass(int y)
            {
                byte* srcRow = scan0 + y * rowBytes;
                byte* dstRow = tempScan0 + y * rowBytes;

                for (int x = 0; x < width; x++)
                {
                    float b = 0, g = 0, r = 0, a = 0;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sampleX = Math.Clamp(x + k, 0, width - 1);
                        byte* p = srcRow + sampleX * 4;
                        float w = kernel[k + radius];
                        b += p[0] * w;
                        g += p[1] * w;
                        r += p[2] * w;
                        a += p[3] * w;
                    }

                    byte* outP = dstRow + x * 4;
                    outP[0] = (byte)Math.Clamp((int)Math.Round(b), 0, 255);
                    outP[1] = (byte)Math.Clamp((int)Math.Round(g), 0, 255);
                    outP[2] = (byte)Math.Clamp((int)Math.Round(r), 0, 255);
                    outP[3] = (byte)Math.Clamp((int)Math.Round(a), 0, 255);
                }
            }

            // Pass 2: 垂直模糊 (Temp -> Destination)
            void VerticalPass(int y)
            {
                byte* dstRow = scan0 + y * rowBytes;

                for (int x = 0; x < width; x++)
                {
                    float b = 0, g = 0, r = 0, a = 0;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sampleY = Math.Clamp(y + k, 0, height - 1);
                        byte* p = tempScan0 + sampleY * rowBytes + x * 4;
                        float w = kernel[k + radius];
                        b += p[0] * w;
                        g += p[1] * w;
                        r += p[2] * w;
                        a += p[3] * w;
                    }

                    byte* outP = dstRow + x * 4;
                    outP[0] = (byte)Math.Clamp((int)Math.Round(b), 0, 255);
                    outP[1] = (byte)Math.Clamp((int)Math.Round(g), 0, 255);
                    outP[2] = (byte)Math.Clamp((int)Math.Round(r), 0, 255);
                    outP[3] = (byte)Math.Clamp((int)Math.Round(a), 0, 255);
                }
            }

            if (mode is PixelProcessingMode.Parallel or PixelProcessingMode.ParallelVectorized)
            {
                Parallel.For(0, height, HorizontalPass);
                Parallel.For(0, height, VerticalPass);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    HorizontalPass(y);
                }
                for (int y = 0; y < height; y++)
                {
                    VerticalPass(y);
                }
            }
        }
        finally
        {
            System.Runtime.InteropServices.NativeMemory.Free(tempScan0);
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

    public static void ProcessPixels(
        WriteableBitmap bitmap,
        VectorTransform vectorTransform,
        PixelProcessor? remainderProcessor = null,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized) =>
        bitmap.ProcessPixels(vectorTransform, remainderProcessor, mode);

    public static void ProcessPixels(
        WriteableBitmap bitmap,
        VectorPointerProcessor pointerProcessor,
        PixelProcessor? remainderProcessor = null,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized) =>
        bitmap.ProcessPixels(pointerProcessor, remainderProcessor, mode);

    public static void ProcessPixels(
        WriteableBitmap bitmap,
        VectorPixelProcessor vectorProcessor,
        PixelProcessor? remainderProcessor = null,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized) =>
        bitmap.ProcessPixels(vectorProcessor, remainderProcessor, mode);

    public static void ProcessPixelsSimdHardware(
        WriteableBitmap bitmap,
        VectorTransform vectorTransform,
        PixelProcessor? remainderProcessor = null) =>
        bitmap.ProcessPixelsSimdHardware(vectorTransform, remainderProcessor);

    public static void ApplyInvert(
        WriteableBitmap bitmap,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized) =>
        bitmap.ApplyInvert(mode);

    public static void ProcessPixels(
        WriteableBitmap bitmap,
        PixelProcessor processor,
        PixelProcessingMode mode = PixelProcessingMode.Parallel) =>
        bitmap.ProcessPixels(processor, mode);

    public static void ProcessPixels(
        WriteableBitmap bitmap,
        PixelLocationProcessor processor,
        PixelProcessingMode mode = PixelProcessingMode.Parallel) =>
        bitmap.ProcessPixels(processor, mode);

    public static WriteableBitmap ToGrayscale(
        Bitmap bitmap,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized) =>
        bitmap.ToGrayscale(mode);

    public static void ApplyGrayscale(
        WriteableBitmap bitmap,
        PixelProcessingMode mode = PixelProcessingMode.ParallelVectorized) =>
        bitmap.ApplyGrayscale(mode);

    public static WriteableBitmap DetectEdges(
        Bitmap bitmap,
        double threshold = 0.0,
        PixelProcessingMode mode = PixelProcessingMode.Parallel) =>
        bitmap.DetectEdges(threshold, mode);

    public static void ApplySobel(
        WriteableBitmap bitmap,
        double threshold = 0.0,
        PixelProcessingMode mode = PixelProcessingMode.Parallel) =>
        bitmap.ApplySobel(threshold, mode);

    public static WriteableBitmap ApplyBlur(
        Bitmap bitmap,
        int radius = 2,
        PixelProcessingMode mode = PixelProcessingMode.Parallel) =>
        bitmap.ApplyBlur(radius, mode);

    public static void ApplyGaussianBlur(
        WriteableBitmap bitmap,
        int radius = 2,
        PixelProcessingMode mode = PixelProcessingMode.Parallel) =>
        bitmap.ApplyGaussianBlur(radius, mode);

    public static void ApplyBoxBlur(
        WriteableBitmap bitmap,
        int radius = 2,
        PixelProcessingMode mode = PixelProcessingMode.Parallel) =>
        bitmap.ApplyBoxBlur(radius, mode);
}