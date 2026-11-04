#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HighPerformanceDrawingShowcase.Services;

/// <summary>
/// C++ 原生向量運算庫與託管高效降級橋接器。
/// 支援 Direct P/Invoke 向量化運算，並具備微秒級 (Microsecond-level) 零配置 Unsafe 記憶體直接寫入降級機制。
/// </summary>
public sealed class FastSignalRendererServiceNativeBridge : IFastSignalRendererService
{
    private const string LibName = "HighPerformanceDrawingShowcase.Logic.Cpp";
    private static bool s_nativeUnavailable;

    [DllImport(LibName, EntryPoint = "FastSignalRendererService_ComputeWaveformBuffer", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Native_ComputeWaveformBuffer(long bufferPtr, int width, int height, double frequency, double amplitude, double phase);

    /// <summary>
    /// 執行示波器緩衝區高速渲染。優先嘗試載入 C++ 原生 SIMD 動態庫，若無原生二進位庫則自動降級為 Unsafe 託管高效渲染。
    /// </summary>
    public void ComputeWaveformBuffer(long bufferPtr, int width, int height, double frequency, double amplitude, double phase, int waveType = 0)
    {
        if (bufferPtr == 0 || width <= 0 || height <= 0) return;

        if (!s_nativeUnavailable && waveType == 0)
        {
            try
            {
                Native_ComputeWaveformBuffer(bufferPtr, width, height, frequency, amplitude, phase);
                return;
            }
            catch (DllNotFoundException)
            {
                s_nativeUnavailable = true;
            }
            catch (EntryPointNotFoundException)
            {
                s_nativeUnavailable = true;
            }
            catch (BadImageFormatException)
            {
                s_nativeUnavailable = true;
            }
        }

        // 託管高效降級實作 (Zero-Allocation Unsafe Framebuffer Fill)
        RenderWaveformBufferManaged(bufferPtr, width, height, frequency, amplitude, phase, waveType);
    }

    /// <summary>
    /// 零配置 (Zero-Allocation) 極速託管渲染演算法，以 Unsafe 直接記憶體指標寫入像素緩衝區。
    /// 渲染包含深色背景、數位示波器座標格線、刻度點以及抗鋸齒霓虹綠發光訊號波形。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static unsafe void RenderWaveformBufferManaged(long bufferPtr, int width, int height, double frequency, double amplitude, double phase, int waveType)
    {
        var pixels = (uint*)bufferPtr;
        var totalPixels = width * height;

        // 1. 示波器調色盤常數 (Bgra8888 格式)
        const uint ColorBg = 0xFF0A0F1D;         // 深色暗黑藍背景
        const uint ColorCenterAxis = 0xFF1C2C3D;   // 中央主座標軸
        const uint ColorGridMajor = 0xFF121B28;    // 主要格線 (每 50 像素)
        const uint ColorTick = 0xFF2B4158;         // 刻度標記點
        const uint ColorWaveCore = 0xFF4ADE80;     // 核心波形：高亮度霓虹綠
        const uint ColorWaveGlow1 = 0xFF22C55E;    // 一級暈光：翠綠色
        const uint ColorWaveGlow2 = 0xFF15803D;    // 二級暈光：深綠色

        var centerX = width / 2;
        var centerY = height / 2;

        // 2. 清除背景並繪製示波器網格 (Grid & Reticle)
        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * width;
            var isCenterY = y == centerY;
            var isGridY = (y % 50) == 0;

            for (var x = 0; x < width; x++)
            {
                var isCenterX = x == centerX;
                var isGridX = (x % 50) == 0;

                uint color;
                if (isCenterY || isCenterX)
                {
                    // 座標軸刻度點 (每 10 像素一個刻度)
                    if ((isCenterY && (x % 10 == 0)) || (isCenterX && (y % 10 == 0)))
                    {
                        color = ColorTick;
                    }
                    else
                    {
                        color = ColorCenterAxis;
                    }
                }
                else if (isGridY || isGridX)
                {
                    color = ColorGridMajor;
                }
                else
                {
                    color = ColorBg;
                }

                pixels[rowOffset + x] = color;
            }
        }

        // 3. 繪製動態訊號波形 (Waveform Rendering with Phosphor Glow)
        var prevY = CalculateSampleY(0, width, height, frequency, amplitude, phase, waveType, centerY);

        for (var x = 0; x < width; x++)
        {
            var curY = CalculateSampleY(x, width, height, frequency, amplitude, phase, waveType, centerY);

            var minY = Math.Min(prevY, curY);
            var maxY = Math.Max(prevY, curY);

            // 垂直連線繪製，避免高頻或大振幅時產生離散斷點
            for (var y = minY; y <= maxY; y++)
            {
                if (y >= 0 && y < height)
                {
                    pixels[y * width + x] = ColorWaveCore;

                    // 上下光暈 (Phosphor Persistence Glow)
                    if (y - 1 >= 0) pixels[(y - 1) * width + x] = BlendColor(pixels[(y - 1) * width + x], ColorWaveGlow1);
                    if (y + 1 < height) pixels[(y + 1) * width + x] = BlendColor(pixels[(y + 1) * width + x], ColorWaveGlow1);
                    if (y - 2 >= 0) pixels[(y - 2) * width + x] = BlendColor(pixels[(y - 2) * width + x], ColorWaveGlow2);
                    if (y + 2 < height) pixels[(y + 2) * width + x] = BlendColor(pixels[(y + 2) * width + x], ColorWaveGlow2);
                }
            }

            prevY = curY;
        }
    }

    /// <summary>
    /// 計算特定 X 座標點對應的訊號 Y 座標值。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateSampleY(int x, int width, int height, double frequency, double amplitude, double phase, int waveType, int centerY)
    {
        var normalizedX = (double)x / width;
        var theta = 2.0 * Math.PI * frequency * normalizedX + phase;

        double signal;
        switch (waveType)
        {
            case 1: // 方波 (Square Wave)
                var rawSin = Math.Sin(theta);
                signal = rawSin >= 0 ? 0.95 : -0.95;
                break;

            case 2: // 諧波雜訊 (Harmonics & Noise)
                var s1 = 0.65 * Math.Sin(theta);
                var s2 = 0.22 * Math.Sin(theta * 3.0 + phase * 0.4);
                var s3 = 0.10 * Math.Sin(theta * 7.0 - phase * 0.8);
                // 輕量偽隨機高頻雜訊
                var noise = (Math.Sin(x * 12.9898 + phase * 78.233) * 43758.5453 % 1.0 - 0.5) * 0.08;
                signal = s1 + s2 + s3 + noise;
                break;

            case 0: // 正弦波 (Sine Wave)
            default:
                signal = Math.Sin(theta);
                break;
        }

        var sampleY = (int)Math.Round(centerY - amplitude * signal);
        return Math.Clamp(sampleY, 2, height - 3);
    }

    /// <summary>
    /// 簡易發光顏色覆蓋混合。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint BlendColor(uint current, uint glow)
    {
        // 若當前像素已是核心亮度，保留核心色
        if (current == 0xFF4ADE80) return current;
        return glow;
    }
}
