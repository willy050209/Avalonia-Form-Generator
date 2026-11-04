#nullable enable

using System;

namespace HighPerformanceDrawingShowcase.Services;

/// <summary>
/// 高效訊號渲染器服務介面，定義 C++ 原生向量運算與託管高效降級實作契約。
/// </summary>
public interface IFastSignalRendererService
{
    /// <summary>
    /// 使用 C++ 原生向量運算或高速託管降級演算法填充示波器像素緩衝區。
    /// </summary>
    /// <param name="bufferPtr">像素緩衝區記憶體指標 (未託管記憶體位址)</param>
    /// <param name="width">畫布寬度 (像素)</param>
    /// <param name="height">畫布高度 (像素)</param>
    /// <param name="frequency">訊號頻率 (Hz)</param>
    /// <param name="amplitude">訊號振幅 (像素)</param>
    /// <param name="phase">訊號目前相位角 (徑度)</param>
    /// <param name="waveType">波形類型：0: 正弦波 (Sine), 1: 方波 (Square), 2: 諧波雜訊 (Noise)</param>
    void ComputeWaveformBuffer(long bufferPtr, int width, int height, double frequency, double amplitude, double phase, int waveType = 0);
}
