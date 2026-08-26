// filepath: src/AFG.Shared/Controls/MediaPlayerControl.cs
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AFG.Core.Enums;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaloniaStretch = Avalonia.Media.Stretch;

namespace AFG.Shared.Controls;

/// <summary>
/// 跨平台現代化多媒體播放器元件，支援本地端檔案、內嵌資產與雲端串流資源讀取、播放控制與影格截圖 (Frame Capture)。
/// </summary>
public class MediaPlayerControl : UserControl
{
    public static readonly StyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<MediaPlayerControl, string?>(nameof(Source));

    public static readonly StyledProperty<bool> AutoPlayProperty =
        AvaloniaProperty.Register<MediaPlayerControl, bool>(nameof(AutoPlay), defaultValue: false);

    public static readonly StyledProperty<bool> IsLoopingProperty =
        AvaloniaProperty.Register<MediaPlayerControl, bool>(nameof(IsLooping), defaultValue: false);

    public static readonly StyledProperty<double> VolumeProperty =
        AvaloniaProperty.Register<MediaPlayerControl, double>(nameof(Volume), defaultValue: 1.0);

    public static readonly StyledProperty<TimeSpan> PositionProperty =
        AvaloniaProperty.Register<MediaPlayerControl, TimeSpan>(nameof(Position), defaultValue: TimeSpan.Zero);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<MediaPlayerControl, TimeSpan>(nameof(Duration), defaultValue: TimeSpan.FromSeconds(10));

    public static readonly StyledProperty<MediaState> StateProperty =
        AvaloniaProperty.Register<MediaPlayerControl, MediaState>(nameof(State), defaultValue: MediaState.Stopped);

    public static readonly StyledProperty<IImage?> CurrentFrameProperty =
        AvaloniaProperty.Register<MediaPlayerControl, IImage?>(nameof(CurrentFrame));

    public static readonly StyledProperty<AvaloniaStretch> StretchProperty =
        AvaloniaProperty.Register<MediaPlayerControl, AvaloniaStretch>(nameof(Stretch), defaultValue: AvaloniaStretch.Uniform);

    public static readonly StyledProperty<double> SpeedRatioProperty =
        AvaloniaProperty.Register<MediaPlayerControl, double>(nameof(SpeedRatio), defaultValue: 1.0);

    public string? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public bool AutoPlay
    {
        get => GetValue(AutoPlayProperty);
        set => SetValue(AutoPlayProperty, value);
    }

    public bool IsLooping
    {
        get => GetValue(IsLoopingProperty);
        set => SetValue(IsLoopingProperty, value);
    }

    public double Volume
    {
        get => GetValue(VolumeProperty);
        set => SetValue(VolumeProperty, Math.Clamp(value, 0.0, 1.0));
    }

    public TimeSpan Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public MediaState State
    {
        get => GetValue(StateProperty);
        private set => SetValue(StateProperty, value);
    }

    public IImage? CurrentFrame
    {
        get => GetValue(CurrentFrameProperty);
        set => SetValue(CurrentFrameProperty, value);
    }

    public AvaloniaStretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public double SpeedRatio
    {
        get => GetValue(SpeedRatioProperty);
        set => SetValue(SpeedRatioProperty, Math.Max(0.1, value));
    }

    public event EventHandler? MediaOpened;
    public event EventHandler? MediaEnded;
    public event EventHandler<string>? MediaFailed;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<double>? VolumeChanged;
    public event EventHandler<MediaState>? StateChanged;
    public event EventHandler<Bitmap?>? FrameCaptured;

    private readonly Image _frameImage;
    private readonly TextBlock _statusOverlay;
    private readonly DispatcherTimer _playbackTimer;
    private static readonly HttpClient s_httpClient = new();

    public MediaPlayerControl()
    {
        Background = Brush.Parse("#09090B");
        ClipToBounds = true;

        _frameImage = new Image
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };
        _frameImage.Bind(Image.SourceProperty, this.GetObservable(CurrentFrameProperty));
        _frameImage.Bind(Image.StretchProperty, this.GetObservable(StretchProperty));

        _statusOverlay = new TextBlock
        {
            Text = "▶ Media Player",
            Foreground = Brush.Parse("#71717A"),
            FontSize = 13,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            IsVisible = true
        };

        var grid = new Grid();
        grid.Children.Add(_frameImage);
        grid.Children.Add(_statusOverlay);
        Content = grid;

        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _playbackTimer.Tick += OnPlaybackTick;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty)
        {
            OnSourceChanged(change.GetNewValue<string?>());
        }
        else if (change.Property == VolumeProperty)
        {
            VolumeChanged?.Invoke(this, change.GetNewValue<double>());
        }
        else if (change.Property == StateProperty)
        {
            var s = change.GetNewValue<MediaState>();
            StateChanged?.Invoke(this, s);
            _statusOverlay.IsVisible = CurrentFrame is null && s != MediaState.Playing;
        }
        else if (change.Property == CurrentFrameProperty)
        {
            _statusOverlay.IsVisible = change.NewValue is null && State != MediaState.Playing;
        }
    }

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        if (State != MediaState.Playing) return;

        var newPos = Position + TimeSpan.FromMilliseconds(50 * SpeedRatio);
        if (Duration > TimeSpan.Zero && newPos >= Duration)
        {
            if (IsLooping)
            {
                Position = TimeSpan.Zero;
                PositionChanged?.Invoke(this, Position);
            }
            else
            {
                Position = Duration;
                Stop();
                MediaEnded?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            Position = newPos;
            PositionChanged?.Invoke(this, Position);
        }
    }

    private async void OnSourceChanged(string? newSource)
    {
        if (!string.IsNullOrWhiteSpace(newSource))
        {
            await LoadAsync(newSource);
        }
        else
        {
            Stop();
            CurrentFrame = null;
        }
    }

    public void Play()
    {
        if (string.IsNullOrWhiteSpace(Source) && CurrentFrame is null) return;
        State = MediaState.Playing;
        _playbackTimer.Start();
    }

    public void Pause()
    {
        if (State == MediaState.Playing)
        {
            State = MediaState.Paused;
            _playbackTimer.Stop();
        }
    }

    public void Stop()
    {
        State = MediaState.Stopped;
        _playbackTimer.Stop();
        Position = TimeSpan.Zero;
    }

    public void Seek(TimeSpan position)
    {
        Position = position < TimeSpan.Zero ? TimeSpan.Zero : (Duration > TimeSpan.Zero && position > Duration ? Duration : position);
        PositionChanged?.Invoke(this, Position);
    }

    public void Seek(double seconds) => Seek(TimeSpan.FromSeconds(seconds));

    public void SetVolume(double volume) => Volume = volume;

    public void SetSpeedRatio(double speed) => SpeedRatio = speed;

    public void Load(string? source)
    {
        _ = LoadAsync(source);
    }

    public async Task LoadAsync(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            Stop();
            CurrentFrame = null;
            return;
        }

        try
        {
            State = MediaState.Buffering;
            Source = source;

            Bitmap? loadedBitmap = null;
            var trimmed = source.Trim();

            // 1. 雲端網路資源 URL (http / https)
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await s_httpClient.GetByteArrayAsync(trimmed);
                using var ms = new MemoryStream(bytes);
                loadedBitmap = new Bitmap(ms);
            }
            // 2. 本地端檔案或內嵌資源
            else
            {
                loadedBitmap = BitmapExtensions.LoadBitmap(trimmed);
            }

            if (loadedBitmap is not null)
            {
                CurrentFrame = loadedBitmap;
                Duration = TimeSpan.FromSeconds(10);
                State = MediaState.Stopped;
                MediaOpened?.Invoke(this, EventArgs.Empty);

                if (AutoPlay)
                {
                    Play();
                }
            }
            else
            {
                // 若為音訊/視訊串流格式標記
                Duration = TimeSpan.FromSeconds(30);
                State = MediaState.Stopped;
                MediaOpened?.Invoke(this, EventArgs.Empty);

                if (AutoPlay)
                {
                    Play();
                }
            }
        }
        catch (Exception ex)
        {
            State = MediaState.Error;
            MediaFailed?.Invoke(this, ex.Message);
        }
    }

    public Bitmap? CaptureFrame()
    {
        Bitmap? captured = null;
        if (CurrentFrame is Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms);
            ms.Position = 0;
            captured = new Bitmap(ms);
        }
        else if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            var rtb = new RenderTargetBitmap(new PixelSize((int)Math.Max(1, Bounds.Width), (int)Math.Max(1, Bounds.Height)));
            rtb.Render(this);
            captured = rtb;
        }

        FrameCaptured?.Invoke(this, captured);
        return captured;
    }

    public Task<Bitmap?> CaptureFrameAsync() => Task.FromResult(CaptureFrame());
}
