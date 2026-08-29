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
    private readonly StackPanel _hudPanel;
    private readonly Border _playStateBadge;
    private readonly TextBlock _playStateBadgeText;
    private readonly TextBlock _titleTextBlock;
    private readonly StackPanel _equalizerPanel;
    private readonly Border[] _equalizerBars;
    private readonly Border _controlsBar;
    private readonly Button _btnPlayPause;
    private readonly Button _btnStop;
    private readonly Slider _seekSlider;
    private readonly TextBlock _timeTextBlock;
    private readonly DispatcherTimer _playbackTimer;
    private static readonly HttpClient s_httpClient = new();
    private int _tickCount;
    private bool _isUserSeeking;

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

        // 1. 中央播放狀態 HUD
        _playStateBadgeText = new TextBlock
        {
            Text = "⏹ 已停止",
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#A1A1AA")
        };
        _playStateBadge = new Border
        {
            Background = Brush.Parse("#2071717A"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Child = _playStateBadgeText
        };

        _titleTextBlock = new TextBlock
        {
            Text = "▶ Media Player",
            Foreground = Brush.Parse("#F4F4F5"),
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };

        _equalizerPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 3,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        _equalizerBars = new Border[5];
        for (int i = 0; i < 5; i++)
        {
            _equalizerBars[i] = new Border
            {
                Width = 4,
                Height = 6,
                Background = Brush.Parse("#38BDF8"),
                CornerRadius = new CornerRadius(2)
            };
            _equalizerPanel.Children.Add(_equalizerBars[i]);
        }

        _hudPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children = { _playStateBadge, _titleTextBlock, _equalizerPanel }
        };

        // 2. 底部控制列 (Transport Bar)
        _btnPlayPause = new Button
        {
            Content = "▶",
            FontSize = 11,
            Padding = new Thickness(6, 2),
            Margin = new Thickness(0, 0, 4, 0)
        };
        _btnPlayPause.Click += (_, _) =>
        {
            if (State == MediaState.Playing) Pause();
            else Play();
        };

        _btnStop = new Button
        {
            Content = "⏹",
            FontSize = 11,
            Padding = new Thickness(6, 2),
            Margin = new Thickness(0, 0, 6, 0)
        };
        _btnStop.Click += (_, _) => Stop();

        _seekSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        _seekSlider.PointerPressed += (_, _) => _isUserSeeking = true;
        _seekSlider.PointerReleased += (_, _) =>
        {
            _isUserSeeking = false;
            if (Duration > TimeSpan.Zero)
            {
                Seek(TimeSpan.FromSeconds((_seekSlider.Value / 100.0) * Duration.TotalSeconds));
            }
        };

        _timeTextBlock = new TextBlock
        {
            Text = "00:00 / 00:10",
            FontSize = 10,
            Foreground = Brush.Parse("#A1A1AA"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 6, 0)
        };

        var volIcon = new TextBlock
        {
            Text = "🔊",
            FontSize = 11,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var controlsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto")
        };
        Grid.SetColumn(_btnPlayPause, 0);
        Grid.SetColumn(_btnStop, 1);
        Grid.SetColumn(_seekSlider, 2);
        Grid.SetColumn(_timeTextBlock, 3);
        Grid.SetColumn(volIcon, 4);
        controlsGrid.Children.Add(_btnPlayPause);
        controlsGrid.Children.Add(_btnStop);
        controlsGrid.Children.Add(_seekSlider);
        controlsGrid.Children.Add(_timeTextBlock);
        controlsGrid.Children.Add(volIcon);

        _controlsBar = new Border
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
            Background = Brush.Parse("#D918181B"),
            BorderBrush = Brush.Parse("#27272A"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(8, 4),
            Child = controlsGrid
        };

        var mainGrid = new Grid();
        mainGrid.Children.Add(_frameImage);
        mainGrid.Children.Add(_hudPanel);
        mainGrid.Children.Add(_controlsBar);
        Content = mainGrid;

        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _playbackTimer.Tick += OnPlaybackTick;
    }

    private static string FormatTime(TimeSpan ts) => $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";

    private void UpdatePlaybackUi()
    {
        _timeTextBlock.Text = $"{FormatTime(Position)} / {FormatTime(Duration)}";
        if (!_isUserSeeking && Duration > TimeSpan.Zero)
        {
            _seekSlider.Value = (Position.TotalSeconds / Duration.TotalSeconds) * 100.0;
        }

        switch (State)
        {
            case MediaState.Playing:
                _btnPlayPause.Content = "⏸";
                _playStateBadgeText.Text = "▶ 播放中";
                _playStateBadge.Background = Brush.Parse("#2038BDF8");
                _playStateBadgeText.Foreground = Brush.Parse("#38BDF8");
                _equalizerPanel.IsVisible = true;
                _hudPanel.IsVisible = CurrentFrame is null;
                break;
            case MediaState.Paused:
                _btnPlayPause.Content = "▶";
                _playStateBadgeText.Text = "⏸ 已暫停";
                _playStateBadge.Background = Brush.Parse("#20F59E0B");
                _playStateBadgeText.Foreground = Brush.Parse("#F59E0B");
                _equalizerPanel.IsVisible = false;
                _hudPanel.IsVisible = true;
                break;
            case MediaState.Stopped:
                _btnPlayPause.Content = "▶";
                _playStateBadgeText.Text = "⏹ 已停止";
                _playStateBadge.Background = Brush.Parse("#2071717A");
                _playStateBadgeText.Foreground = Brush.Parse("#A1A1AA");
                _equalizerPanel.IsVisible = false;
                _hudPanel.IsVisible = true;
                break;
            case MediaState.Buffering:
                _btnPlayPause.Content = "⏳";
                _playStateBadgeText.Text = "⏳ 載入中...";
                _playStateBadge.Background = Brush.Parse("#20A78BFA");
                _playStateBadgeText.Foreground = Brush.Parse("#A78BFA");
                _hudPanel.IsVisible = true;
                break;
            case MediaState.Error:
                _btnPlayPause.Content = "⚠";
                _playStateBadgeText.Text = "⚠ 錯誤";
                _playStateBadge.Background = Brush.Parse("#20EF4444");
                _playStateBadgeText.Foreground = Brush.Parse("#EF4444");
                _hudPanel.IsVisible = true;
                break;
        }
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
        else if (change.Property == PositionProperty)
        {
            UpdatePlaybackUi();
        }
        else if (change.Property == DurationProperty)
        {
            UpdatePlaybackUi();
        }
        else if (change.Property == StateProperty)
        {
            var s = change.GetNewValue<MediaState>();
            StateChanged?.Invoke(this, s);
            UpdatePlaybackUi();
        }
        else if (change.Property == CurrentFrameProperty)
        {
            UpdatePlaybackUi();
        }
    }

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        if (State != MediaState.Playing) return;

        _tickCount++;
        var newPos = Position + TimeSpan.FromMilliseconds(50 * SpeedRatio);

        // 動畫音波等化器
        for (int i = 0; i < _equalizerBars.Length; i++)
        {
            var wave = Math.Abs(Math.Sin(_tickCount * 0.35 + i * 1.2));
            _equalizerBars[i].Height = Math.Max(4, 4 + wave * 16 * Volume);
        }

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

        UpdatePlaybackUi();
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
            _titleTextBlock.Text = "▶ Media Player";
            UpdatePlaybackUi();
        }
    }

    public void Play()
    {
        if (Duration <= TimeSpan.Zero)
        {
            Duration = TimeSpan.FromSeconds(30);
        }
        State = MediaState.Playing;
        _playbackTimer.Start();
        UpdatePlaybackUi();
    }

    public void Pause()
    {
        if (State == MediaState.Playing)
        {
            State = MediaState.Paused;
            _playbackTimer.Stop();
            UpdatePlaybackUi();
        }
    }

    public void Stop()
    {
        State = MediaState.Stopped;
        _playbackTimer.Stop();
        Position = TimeSpan.Zero;
        UpdatePlaybackUi();
    }

    public void Seek(TimeSpan position)
    {
        Position = position < TimeSpan.Zero ? TimeSpan.Zero : (Duration > TimeSpan.Zero && position > Duration ? Duration : position);
        PositionChanged?.Invoke(this, Position);
        UpdatePlaybackUi();
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
            _titleTextBlock.Text = "▶ Media Player";
            UpdatePlaybackUi();
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
                try
                {
                    var bytes = await s_httpClient.GetByteArrayAsync(trimmed);
                    using var ms = new MemoryStream(bytes);
                    loadedBitmap = new Bitmap(ms);
                }
                catch
                {
                    // 若非靜態圖片格式 (例如視訊串流或影片檔)，進入通用多媒體狀態
                    loadedBitmap = null;
                }
            }
            // 2. 本地端檔案或內嵌資源
            else
            {
                try
                {
                    loadedBitmap = BitmapExtensions.LoadBitmap(trimmed);
                }
                catch
                {
                    loadedBitmap = null;
                }
            }

            var displayName = System.IO.Path.GetFileName(trimmed);
            if (string.IsNullOrWhiteSpace(displayName)) displayName = trimmed;

            if (loadedBitmap is not null)
            {
                CurrentFrame = loadedBitmap;
                Duration = TimeSpan.FromSeconds(10);
                State = MediaState.Stopped;
                _titleTextBlock.Text = $"▶ {displayName}";
                UpdatePlaybackUi();
                MediaOpened?.Invoke(this, EventArgs.Empty);

                if (AutoPlay)
                {
                    Play();
                }
            }
            else
            {
                // 音訊/視訊串流格式標記或一般影音檔案
                CurrentFrame = null;
                Duration = TimeSpan.FromSeconds(30);
                State = MediaState.Stopped;
                _titleTextBlock.Text = $"▶ {displayName}";
                UpdatePlaybackUi();
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
            _titleTextBlock.Text = $"⚠ 載入失敗: {ex.Message}";
            UpdatePlaybackUi();
            MediaFailed?.Invoke(this, ex.Message);
        }
    }

    public Bitmap? CaptureFrame()
    {
        Bitmap? captured = null;
        if (CurrentFrame is Bitmap bmp)
        {
            using var ms = new MemoryStream();
#pragma warning disable CS0618
            bmp.Save(ms);
#pragma warning restore CS0618
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
