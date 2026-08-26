// filepath: tests/AFG.Core.Tests/MediaPlayerControlTests.cs
using System;
using System.IO;
using System.Threading.Tasks;
using AFG.Core.Enums;
using AFG.Shared.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluentAssertions;
using Xunit;
using AvaloniaStretch = Avalonia.Media.Stretch;

namespace AFG.Core.Tests;

public sealed class MediaPlayerControlTests
{
    [Fact]
    public void MediaPlayerControl_DefaultProperties_ShouldMatchSpecification()
    {
        // Arrange & Act
        var player = new MediaPlayerControl();

        // Assert
        player.Volume.Should().Be(1.0);
        player.AutoPlay.Should().BeFalse();
        player.IsLooping.Should().BeFalse();
        player.Position.Should().Be(TimeSpan.Zero);
        player.Duration.Should().Be(TimeSpan.FromSeconds(10));
        player.State.Should().Be(MediaState.Stopped);
        player.Stretch.Should().Be(AvaloniaStretch.Uniform);
        player.SpeedRatio.Should().Be(1.0);
        player.CurrentFrame.Should().BeNull();
        player.Source.Should().BeNull();
    }

    [Fact]
    public void Play_Pause_Stop_ShouldTransitionMediaStateCorrectly()
    {
        // Arrange
        var player = new MediaPlayerControl();
        var wb = BitmapHelper.CreateInitializedBitmap(100, 100, Color.FromArgb(255, 255, 0, 0));
        player.CurrentFrame = wb;

        MediaState? lastState = null;
        player.StateChanged += (_, s) => lastState = s;

        // Act & Assert 1: Play
        player.Play();
        player.State.Should().Be(MediaState.Playing);
        lastState.Should().Be(MediaState.Playing);

        // Act & Assert 2: Pause
        player.Pause();
        player.State.Should().Be(MediaState.Paused);
        lastState.Should().Be(MediaState.Paused);

        // Act & Assert 3: Stop
        player.Stop();
        player.State.Should().Be(MediaState.Stopped);
        player.Position.Should().Be(TimeSpan.Zero);
        lastState.Should().Be(MediaState.Stopped);
    }

    [Fact]
    public void Seek_ShouldClampPositionAndTriggerPositionChanged()
    {
        // Arrange
        var player = new MediaPlayerControl
        {
            Duration = TimeSpan.FromSeconds(60)
        };

        TimeSpan? reportedPosition = null;
        player.PositionChanged += (_, pos) => reportedPosition = pos;

        // Act 1: Seek within duration
        player.Seek(TimeSpan.FromSeconds(30));
        player.Position.Should().Be(TimeSpan.FromSeconds(30));
        reportedPosition.Should().Be(TimeSpan.FromSeconds(30));

        // Act 2: Seek over duration -> should clamp to Duration
        player.Seek(TimeSpan.FromSeconds(100));
        player.Position.Should().Be(TimeSpan.FromSeconds(60));

        // Act 3: Seek negative seconds -> should clamp to Zero
        player.Seek(-10);
        player.Position.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void SetVolume_ShouldClampBetween0And1AndTriggerVolumeChanged()
    {
        // Arrange
        var player = new MediaPlayerControl();
        double? reportedVolume = null;
        player.VolumeChanged += (_, vol) => reportedVolume = vol;

        // Act 1: Valid volume
        player.SetVolume(0.75);
        player.Volume.Should().Be(0.75);
        reportedVolume.Should().Be(0.75);

        // Act 2: Volume > 1.0 -> should clamp to 1.0
        player.SetVolume(2.5);
        player.Volume.Should().Be(1.0);

        // Act 3: Volume < 0.0 -> should clamp to 0.0
        player.SetVolume(-0.5);
        player.Volume.Should().Be(0.0);
    }

    [Fact]
    public void SetSpeedRatio_ShouldSetValidRatio()
    {
        // Arrange
        var player = new MediaPlayerControl();

        // Act
        player.SetSpeedRatio(2.0);

        // Assert
        player.SpeedRatio.Should().Be(2.0);
    }

    [Fact]
    public async Task LoadAsync_WithEmptyOrNull_ShouldStopAndClearFrame()
    {
        // Arrange
        var player = new MediaPlayerControl
        {
            CurrentFrame = BitmapHelper.CreateInitializedBitmap(50, 50)
        };

        // Act
        await player.LoadAsync(null);

        // Assert
        player.CurrentFrame.Should().BeNull();
        player.State.Should().Be(MediaState.Stopped);
    }

    [Fact]
    public void CaptureFrame_WithCurrentFrame_ShouldReturnBitmapAndTriggerFrameCaptured()
    {
        // Arrange
        var player = new MediaPlayerControl();
        var wb = BitmapHelper.CreateInitializedBitmap(64, 64, Color.FromArgb(255, 0, 128, 255));
        player.CurrentFrame = wb;

        Bitmap? capturedResult = null;
        player.FrameCaptured += (_, bmp) => capturedResult = bmp;

        // Act
        var result = player.CaptureFrame();

        // Assert
        result.Should().NotBeNull();
        capturedResult.Should().NotBeNull();
        result!.PixelSize.Width.Should().Be(64);
        result.PixelSize.Height.Should().Be(64);
    }

    [Fact]
    public async Task CaptureFrameAsync_ShouldReturnFrameAsynchronously()
    {
        // Arrange
        var player = new MediaPlayerControl();
        var wb = BitmapHelper.CreateInitializedBitmap(32, 32, Color.FromArgb(255, 100, 200, 50));
        player.CurrentFrame = wb;

        // Act
        var result = await player.CaptureFrameAsync();

        // Assert
        result.Should().NotBeNull();
        result!.PixelSize.Width.Should().Be(32);
        result.PixelSize.Height.Should().Be(32);
    }

    [Fact]
    public void AutoPlay_WhenSet_ShouldAutomaticallyPlayOnLoad()
    {
        // Arrange
        var player = new MediaPlayerControl
        {
            AutoPlay = true
        };

        // Act: Loading a mock resource
        player.Load("mock_media.mp4");

        // Assert
        player.State.Should().Be(MediaState.Playing);
    }
}
