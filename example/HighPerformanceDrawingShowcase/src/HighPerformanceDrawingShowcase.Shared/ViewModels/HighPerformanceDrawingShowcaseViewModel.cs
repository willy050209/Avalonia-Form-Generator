#nullable enable

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighPerformanceDrawingShowcase.Services;

namespace HighPerformanceDrawingShowcase;

/// <summary>
/// 高效能示波器繪圖展示 ViewModel。
/// 整合 Avalonia WriteableBitmap 雙緩衝區記憶體直接鎖定 (Zero-Allocation Double Buffering)，
/// 透過 IFastSignalRendererService 實現 60 FPS 實時流暢訊號波形渲染與延遲微秒級監控。
/// </summary>
public partial class HighPerformanceDrawingShowcaseViewModel : ObservableObject, IDisposable
{
    public const int BitmapWidth = 620;
    public const int BitmapHeight = 290;

    private readonly IFastSignalRendererService _rendererService;
    private readonly WriteableBitmap _bitmapA;
    private readonly WriteableBitmap _bitmapB;
    private readonly DispatcherTimer _renderTimer;

    private bool _usingBufferA;
    private double _phase;
    private double _lastRenderUs = 120.0;
    private double _currentFps = 60.0;
    private int _frameCount;
    private long _lastFpsTimestamp;
    private bool _disposed;

    [ObservableProperty]
    private Avalonia.Media.IImage? _waveformImageSource;

    [ObservableProperty]
    private string _frequencyLabel = "訊號頻率 (Frequency: 5.0 Hz)：";

    [ObservableProperty]
    private double _frequencyValue = 5;

    [ObservableProperty]
    private string _amplitudeLabel = "訊號振幅 (Amplitude: 80 px)：";

    [ObservableProperty]
    private double _amplitudeValue = 80;

    [ObservableProperty]
    private bool _isSineWave = true;

    [ObservableProperty]
    private bool _isSquareWave = false;

    [ObservableProperty]
    private bool _isNoiseWave = false;

    [ObservableProperty]
    private string _telemetryText = "即時效能監控：60.0 FPS | 渲染延遲：< 150 μs | 採樣緩衝區：620 × 290 (179,800 像素) | 橋接方式：C++ DllImport SIMD 向量化計算";

    public HighPerformanceDrawingShowcaseViewModel()
        : this(null)
    {
    }

    public HighPerformanceDrawingShowcaseViewModel(IFastSignalRendererService? rendererService)
    {
        _rendererService = rendererService ?? new FastSignalRendererServiceNativeBridge();

        // 預先建立兩組未託管像素緩衝區實現零記憶體回收 (Zero-GC) 雙緩衝 Ping-Pong 機制
        var pixelSize = new PixelSize(BitmapWidth, BitmapHeight);
        var dpi = new Avalonia.Vector(96, 96);
        _bitmapA = new WriteableBitmap(pixelSize, dpi, PixelFormat.Bgra8888, AlphaFormat.Opaque);
        _bitmapB = new WriteableBitmap(pixelSize, dpi, PixelFormat.Bgra8888, AlphaFormat.Opaque);

        _lastFpsTimestamp = Stopwatch.GetTimestamp();

        // 配置 60 FPS 渲染計時器 (約 16.6 毫秒間隔)
        _renderTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16.666)
        };
        _renderTimer.Tick += OnRenderTimerTick;

        // 繪製初始幀，確保視窗剛載入時立即呈現波形
        RenderFrame();

        // 預設自動啟動即時 60 FPS 刷新
        _renderTimer.Start();
    }

    private int CurrentWaveType => IsSquareWave ? 1 : IsNoiseWave ? 2 : 0;

    partial void OnFrequencyValueChanged(double value)
    {
        FrequencyLabel = $"訊號頻率 (Frequency: {value:F1} Hz)：";
        if (!_renderTimer.IsEnabled)
        {
            RenderFrame();
        }
    }

    partial void OnAmplitudeValueChanged(double value)
    {
        AmplitudeLabel = $"訊號振幅 (Amplitude: {value:F0} px)：";
        if (!_renderTimer.IsEnabled)
        {
            RenderFrame();
        }
    }

    partial void OnIsSineWaveChanged(bool value)
    {
        if (value)
        {
            IsSquareWave = false;
            IsNoiseWave = false;
            if (!_renderTimer.IsEnabled) RenderFrame();
        }
    }

    partial void OnIsSquareWaveChanged(bool value)
    {
        if (value)
        {
            IsSineWave = false;
            IsNoiseWave = false;
            if (!_renderTimer.IsEnabled) RenderFrame();
        }
    }

    partial void OnIsNoiseWaveChanged(bool value)
    {
        if (value)
        {
            IsSineWave = false;
            IsSquareWave = false;
            if (!_renderTimer.IsEnabled) RenderFrame();
        }
    }

    /// <summary>
    /// 啟動 60 FPS 即時示波器動畫刷新。
    /// </summary>
    [RelayCommand]
    private void StartRefresh()
    {
        if (!_renderTimer.IsEnabled)
        {
            _lastFpsTimestamp = Stopwatch.GetTimestamp();
            _frameCount = 0;
            _renderTimer.Start();
            UpdateTelemetryString();
        }
    }

    /// <summary>
    /// 停止即時刷新計時器。
    /// </summary>
    [RelayCommand]
    private void StopRefresh()
    {
        if (_renderTimer.IsEnabled)
        {
            _renderTimer.Stop();
            TelemetryText = $"即時效能監控：已暫停 (Paused) | 渲染延遲：{_lastRenderUs:F0} μs | 採樣緩衝區：{BitmapWidth} × {BitmapHeight} ({BitmapWidth * BitmapHeight:N0} 像素) | 橋接方式：C++ DllImport SIMD 向量化計算";
        }
    }

    private void OnRenderTimerTick(object? sender, EventArgs e)
    {
        // 依據當前頻率推進相位
        _phase += 0.08 * (FrequencyValue / 5.0);
        if (_phase > Math.PI * 2000.0) _phase -= Math.PI * 2000.0;

        RenderFrame();

        // 統計即時 FPS 與延遲指標
        _frameCount++;
        var now = Stopwatch.GetTimestamp();
        var elapsedSec = (double)(now - _lastFpsTimestamp) / Stopwatch.Frequency;
        if (elapsedSec >= 0.25)
        {
            _currentFps = _frameCount / elapsedSec;
            _frameCount = 0;
            _lastFpsTimestamp = now;
            UpdateTelemetryString();
        }
    }

    /// <summary>
    /// 執行單格幀渲染：直接鎖定 Framebuffer，寫入像素並將 ImageSource 切換至剛完成繪製之緩衝區。
    /// </summary>
    private void RenderFrame()
    {
        var targetBitmap = _usingBufferA ? _bitmapB : _bitmapA;

        var startTick = Stopwatch.GetTimestamp();
        using (var lockedFb = targetBitmap.Lock())
        {
            _rendererService.ComputeWaveformBuffer(
                (long)lockedFb.Address,
                BitmapWidth,
                BitmapHeight,
                FrequencyValue,
                AmplitudeValue,
                _phase,
                CurrentWaveType);
        }
        var elapsedUs = (double)(Stopwatch.GetTimestamp() - startTick) * 1_000_000.0 / Stopwatch.Frequency;
        _lastRenderUs = elapsedUs;

        // 切換雙緩衝指標，觸發 Avalonia CompiledBinding 重新擷取 Image Source 並引發繪製
        _usingBufferA = !_usingBufferA;
        WaveformImageSource = targetBitmap;
    }

    private void UpdateTelemetryString()
    {
        TelemetryText = $"即時效能監控：{_currentFps:F1} FPS | 渲染延遲：{_lastRenderUs:F0} μs | 採樣緩衝區：{BitmapWidth} × {BitmapHeight} ({BitmapWidth * BitmapHeight:N0} 像素) | 橋接方式：C++ DllImport SIMD 向量化計算";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _renderTimer.Stop();
        _bitmapA.Dispose();
        _bitmapB.Dispose();
        GC.SuppressFinalize(this);
    }
}
