// filepath: tests/AFG.Core.Tests/BitmapHelperTests.cs
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluentAssertions;
using Xunit;

namespace AFG.Core.Tests;

public class BitmapHelperTests
{
    static BitmapHelperTests()
    {
        try
        {
            Avalonia.Skia.SkiaPlatform.Initialize();
        }
        catch
        {
            // Ignore if already initialized
        }
    }

    [Fact]
    public void CreateInitializedBitmap_ShouldCreateValidBitmapWithMatchingDimensionsAndDefaultColor()
    {
        // Act
        var bitmap = BitmapHelper.CreateInitializedBitmap(50, 40);

        // Assert
        bitmap.Should().NotBeNull();
        bitmap.PixelSize.Width.Should().Be(50);
        bitmap.PixelSize.Height.Should().Be(40);

        // Verify pixel color (default #F0F0F0 -> R=240, G=240, B=240, A=255)
        var pixel = bitmap.GetPixel(0, 0);
        pixel.R.Should().Be(240);
        pixel.G.Should().Be(240);
        pixel.B.Should().Be(240);
        pixel.A.Should().Be(255);
    }

    [Fact]
    public void CreateInitializedBitmap_WithCustomColor_ShouldFillBitmapWithSpecifiedColor()
    {
        // Arrange
        var customColor = Color.FromArgb(255, 100, 150, 200);

        // Act
        var bitmap = BitmapHelper.CreateInitializedBitmap(30, 20, customColor);

        // Assert
        bitmap.PixelSize.Width.Should().Be(30);
        bitmap.PixelSize.Height.Should().Be(20);

        var centerPixel = bitmap.GetPixel(15, 10);
        centerPixel.R.Should().Be(100);
        centerPixel.G.Should().Be(150);
        centerPixel.B.Should().Be(200);
        centerPixel.A.Should().Be(255);
    }

    [Fact]
    public void SetPixel_And_GetPixel_ShouldCorrectlyWriteAndReadPixel()
    {
        // Arrange
        var bitmap = BitmapHelper.CreateInitializedBitmap(20, 20, Colors.White);
        var targetColor = Color.FromArgb(255, 255, 0, 128);

        // Act
        bitmap.SetPixel(5, 5, targetColor);
        var readColor = bitmap.GetPixel(5, 5);

        // Assert
        readColor.R.Should().Be(255);
        readColor.G.Should().Be(0);
        readColor.B.Should().Be(128);
        readColor.A.Should().Be(255);
    }

    [Fact]
    public void SetPixel_OutOfBounds_ShouldNotThrowException()
    {
        // Arrange
        var bitmap = BitmapHelper.CreateInitializedBitmap(10, 10);

        // Act & Assert
        var actNegative = () => bitmap.SetPixel(-1, -1, Colors.Red);
        var actOverflow = () => bitmap.SetPixel(20, 20, Colors.Red);

        actNegative.Should().NotThrow();
        actOverflow.Should().NotThrow();
    }

    [Fact]
    public void GetPixel_OutOfBounds_ShouldReturnTransparentColor()
    {
        // Arrange
        var bitmap = BitmapHelper.CreateInitializedBitmap(10, 10);

        // Act
        var colorNeg = bitmap.GetPixel(-5, -5);
        var colorOver = bitmap.GetPixel(50, 50);

        // Assert
        colorNeg.Should().Be(Colors.Transparent);
        colorOver.Should().Be(Colors.Transparent);
    }

    [Fact]
    public void ConvertToWriteableBitmap_FromWriteableBitmap_ShouldReturnInstance()
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(10, 10);

        // Act
        var converted = wb.ConvertToWriteableBitmap();

        // Assert
        converted.Should().BeSameAs(wb);
    }

    [Fact]
    public void LoadBitmap_WithNullOrWhitespace_ShouldReturnNull()
    {
        BitmapHelper.LoadBitmap(null).Should().BeNull();
        BitmapHelper.LoadBitmap("   ").Should().BeNull();
    }

    [Fact]
    public void LoadBitmap_WithValidLocalFilePath_ShouldLoadBitmap()
    {
        // Arrange: Create a minimal 1x1 png image
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_img_" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
#pragma warning disable CS0618
            var wb = BitmapHelper.CreateInitializedBitmap(16, 16, Colors.Red);
            wb.Save(tempFile);
#pragma warning restore CS0618

            // Act
            var loaded = BitmapHelper.LoadBitmap(tempFile);

            // Assert
            loaded.Should().NotBeNull();
            loaded!.PixelSize.Width.Should().Be(16);
            loaded.PixelSize.Height.Should().Be(16);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                try { System.IO.File.Delete(tempFile); } catch { }
            }
        }
    }

    [Fact]
    public void LoadBitmap_WithDiskFallback_ShouldFindImageInBaseOrAssetsDirectory()
    {
        // Arrange
        var assetsDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets");
        System.IO.Directory.CreateDirectory(assetsDir);
        var testAssetFile = System.IO.Path.Combine(assetsDir, "fallback_test_logo.png");
        try
        {
#pragma warning disable CS0618
            var wb = BitmapHelper.CreateInitializedBitmap(32, 32, Colors.Blue);
            wb.Save(testAssetFile);
#pragma warning restore CS0618

            // Act: load via relative string "fallback_test_logo.png" or "Assets/fallback_test_logo.png"
            var loaded1 = BitmapHelper.LoadBitmap("fallback_test_logo.png");
            var loaded2 = BitmapHelper.LoadBitmap("Assets/fallback_test_logo.png");
            var loaded3 = BitmapHelper.LoadBitmap("avares://NonExistentApp.Shared/Assets/fallback_test_logo.png");

            // Assert
            loaded1.Should().NotBeNull();
            loaded1!.PixelSize.Width.Should().Be(32);

            loaded2.Should().NotBeNull();
            loaded2!.PixelSize.Width.Should().Be(32);

            loaded3.Should().NotBeNull();
            loaded3!.PixelSize.Width.Should().Be(32);
        }
        finally
        {
            if (System.IO.File.Exists(testAssetFile))
            {
                try { System.IO.File.Delete(testAssetFile); } catch { }
            }
        }
    }
}