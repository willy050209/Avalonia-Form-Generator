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
    public void LoadBitmap_WithInvalidHttpUrl_ShouldReturnNullGracefully()
    {
        // Act: try loading an invalid or non-existent URL
        var loaded = BitmapHelper.LoadBitmap("http://127.0.0.1:65534/non_existent_image.png");

        // Assert: should gracefully return null without throwing unhandled exceptions
        loaded.Should().BeNull();
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

    [Fact]
    public void SimdHardware_Properties_ShouldReturnValidHardwareCapabilities()
    {
        SimdHardware.PreferredVectorByteCount.Should().BeGreaterThan(0);
        (SimdHardware.PreferredVectorByteCount is 16 or 32 or 64).Should().BeTrue();
    }

    [Theory]
    [InlineData(PixelProcessingMode.Sequential)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.Parallel)]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    public void ProcessPixels_ShouldProcessAllPixelsCorrectly_AcrossAllModes(PixelProcessingMode mode)
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(23, 17, Color.FromArgb(255, 50, 100, 150));

        // Act: Invert RGB channels
        wb.ProcessPixels((ref byte b, ref byte g, ref byte r, ref byte a) =>
        {
            b = (byte)(255 - b);
            g = (byte)(255 - g);
            r = (byte)(255 - r);
        }, mode);

        // Assert
        for (int y = 0; y < 17; y++)
        {
            for (int x = 0; x < 23; x++)
            {
                var p = wb.GetPixel(x, y);
                p.R.Should().Be((byte)(255 - 50));
                p.G.Should().Be((byte)(255 - 100));
                p.B.Should().Be((byte)(255 - 150));
                p.A.Should().Be(255);
            }
        }
    }

    [Theory]
    [InlineData(PixelProcessingMode.Sequential)]
    [InlineData(PixelProcessingMode.Parallel)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    public void ProcessPixels_WithCoordinates_ShouldApplyLocationBasedTransformation(PixelProcessingMode mode)
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(16, 16, Colors.Black);

        // Act: Set Red = x * 10, Blue = y * 10
        wb.ProcessPixels((int x, int y, ref byte b, ref byte g, ref byte r, ref byte a) =>
        {
            r = (byte)(x * 10);
            b = (byte)(y * 10);
            a = 255;
        }, mode);

        // Assert
        var p0 = wb.GetPixel(0, 0);
        p0.R.Should().Be(0);
        p0.B.Should().Be(0);

        var pMid = wb.GetPixel(5, 7);
        pMid.R.Should().Be(50);
        pMid.B.Should().Be(70);
    }

    [Theory]
    [InlineData(PixelProcessingMode.Sequential)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.Parallel)]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    public void ApplyGrayscale_ShouldConvertToMonochrome_AcrossAllModes(PixelProcessingMode mode)
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(19, 13, Color.FromArgb(255, 200, 100, 50));

        // Act
        BitmapHelper.ApplyGrayscale(wb, mode);

        // Assert: In ITU-R BT.601, Gray = (299*200 + 587*100 + 114*50 + 500) / 1000 = (59800 + 58700 + 5700 + 500) / 1000 = 124700 / 1000 = 124
        var p = wb.GetPixel(5, 5);
        p.R.Should().Be(p.G);
        p.G.Should().Be(p.B);
        p.R.Should().Be(124);
        p.A.Should().Be(255);
    }

    [Fact]
    public void ToGrayscale_ShouldReturnNewGrayscaleWriteableBitmap()
    {
        // Arrange
        var original = BitmapHelper.CreateInitializedBitmap(10, 10, Colors.Red);

        // Act
        var gray = BitmapHelper.ToGrayscale(original);

        // Assert
        gray.Should().NotBeNull();
        gray.PixelSize.Width.Should().Be(10);
        var p = gray.GetPixel(0, 0);
        p.R.Should().Be(p.G);
        p.G.Should().Be(p.B);
        p.R.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DetectEdges_ShouldHighlightIntensityTransitions()
    {
        // Arrange: Left half Black, Right half White
        var wb = BitmapHelper.CreateInitializedBitmap(20, 20, Colors.Black);
        for (int y = 0; y < 20; y++)
        {
            for (int x = 10; x < 20; x++)
            {
                wb.SetPixel(x, y, Colors.White);
            }
        }

        // Act
        var edges = BitmapHelper.DetectEdges(wb, threshold: 50);

        // Assert: The boundary between x=9 and x=10 should have strong edge response
        var edgePixel = edges.GetPixel(9, 10);
        var edgePixel2 = edges.GetPixel(10, 10);
        (edgePixel.R > 0 || edgePixel2.R > 0).Should().BeTrue();

        // Far away from edge (e.g. x=2, y=10) should be 0 (black)
        var flatPixel = edges.GetPixel(2, 10);
        flatPixel.R.Should().Be(0);
    }

    [Fact]
    public void ApplySobel_InPlace_ShouldModifyWriteableBitmap()
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(15, 15, Colors.White);
        wb.SetPixel(7, 7, Colors.Black);

        // Act
        BitmapHelper.ApplySobel(wb, threshold: 0);

        // Assert
        var centerNeighbor = wb.GetPixel(6, 7);
        centerNeighbor.R.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ApplyGaussianBlur_ShouldSmoothPixelTransitions()
    {
        // Arrange: Single bright white pixel on black canvas
        var wb = BitmapHelper.CreateInitializedBitmap(15, 15, Colors.Black);
        wb.SetPixel(7, 7, Colors.White);

        // Act
        BitmapHelper.ApplyGaussianBlur(wb, radius: 2);

        // Assert: Neighbors should now have non-zero diffused intensity
        var neighbor = wb.GetPixel(8, 7);
        neighbor.R.Should().BeGreaterThan(0);
        neighbor.R.Should().BeLessThan(255);
    }

    [Fact]
    public void ApplyBoxBlur_ShouldAverageNeighboringPixels()
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(11, 11, Colors.Black);
        wb.SetPixel(5, 5, Colors.White);

        // Act
        BitmapHelper.ApplyBoxBlur(wb, radius: 1);

        // Assert
        var center = wb.GetPixel(5, 5);
        var neighbor = wb.GetPixel(6, 5);
        center.R.Should().BeGreaterThan(0);
        neighbor.R.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(PixelProcessingMode.Sequential)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.Parallel)]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    public void ProcessPixels_WithVectorTransform_ShouldTransformViaSimdRegisters(PixelProcessingMode mode)
    {
        // Arrange: 17x11 non-aligned dimensions to test both SIMD batching and remainder handling
        var wb = BitmapHelper.CreateInitializedBitmap(17, 11, Color.FromArgb(255, 100, 150, 200));

        // Act: Vector bitwise NOT or 255 - x
        var all255 = new System.Numerics.Vector<byte>(255);
        wb.ProcessPixels(
            vectorTransform: vec => all255 - vec,
            remainderProcessor: (ref byte b, ref byte g, ref byte r, ref byte a) =>
            {
                b = (byte)(255 - b);
                g = (byte)(255 - g);
                r = (byte)(255 - r);
                a = (byte)(255 - a);
            },
            mode: mode);

        // Assert: First pixel and remainder edge pixel should both be inverted
        var p0 = wb.GetPixel(0, 0);
        p0.R.Should().Be(155); // 255 - 100
        p0.G.Should().Be(105); // 255 - 150
        p0.B.Should().Be(55);  // 255 - 200
        p0.A.Should().Be(0);   // 255 - 255

        var pRemainder = wb.GetPixel(16, 10);
        pRemainder.R.Should().Be(155);
        pRemainder.G.Should().Be(105);
        pRemainder.B.Should().Be(55);
        pRemainder.A.Should().Be(0);
    }

    [Theory]
    [InlineData(PixelProcessingMode.Sequential)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.Parallel)]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    public void ProcessPixels_WithVectorPixelProcessor_ShouldProcessSpanBatches(PixelProcessingMode mode)
    {
        // Arrange: 23x13 non-aligned dimensions
        var wb = BitmapHelper.CreateInitializedBitmap(23, 13, Color.FromArgb(255, 50, 80, 120));

        // Act: Vector Span Processor sets all bytes in span to 200
        wb.ProcessPixels(
            vectorProcessor: span =>
            {
                span.Fill(200);
            },
            remainderProcessor: (ref byte b, ref byte g, ref byte r, ref byte a) =>
            {
                b = 200;
                g = 200;
                r = 200;
                a = 200;
            },
            mode: mode);

        // Assert
        var p0 = wb.GetPixel(0, 0);
        p0.R.Should().Be(200);
        p0.G.Should().Be(200);
        p0.B.Should().Be(200);
        p0.A.Should().Be(200);

        var pLast = wb.GetPixel(22, 12);
        pLast.R.Should().Be(200);
        pLast.G.Should().Be(200);
        pLast.B.Should().Be(200);
        pLast.A.Should().Be(200);
    }

    [Theory]
    [InlineData(PixelProcessingMode.Sequential)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.Parallel)]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    public void ProcessPixels_WithVectorPointerProcessor_ShouldProcessUnmanagedPointers(PixelProcessingMode mode)
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(25, 14, Color.FromArgb(255, 30, 60, 90));

        // Act
        unsafe
        {
            wb.ProcessPixels(
                pointerProcessor: (byte* ptr, int byteCount) =>
                {
                    for (int i = 0; i < byteCount; i++)
                    {
                        ptr[i] = 180;
                    }
                },
                remainderProcessor: (ref byte b, ref byte g, ref byte r, ref byte a) =>
                {
                    b = 180;
                    g = 180;
                    r = 180;
                    a = 180;
                },
                mode: mode);
        }

        // Assert
        var p0 = wb.GetPixel(0, 0);
        p0.R.Should().Be(180);
        p0.G.Should().Be(180);
        p0.B.Should().Be(180);
        p0.A.Should().Be(180);

        var pEnd = wb.GetPixel(24, 13);
        pEnd.R.Should().Be(180);
        pEnd.G.Should().Be(180);
        pEnd.B.Should().Be(180);
        pEnd.A.Should().Be(180);
    }

    [Fact]
    public void ApplyInvert_ShouldInvertColorsUsingHardwareSimd()
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(19, 13, Color.FromArgb(255, 10, 20, 30));

        // Act
        BitmapHelper.ApplyInvert(wb);

        // Assert
        var p = wb.GetPixel(5, 5);
        p.R.Should().Be(245); // 255 - 10
        p.G.Should().Be(235); // 255 - 20
        p.B.Should().Be(225); // 255 - 30
        p.A.Should().Be(0);   // 255 - 255
    }

    [Fact]
    public void ProcessPixelsSimdHardware_ShouldExecuteHardwareParallel()
    {
        // Arrange
        var wb = BitmapHelper.CreateInitializedBitmap(16, 16, Color.FromArgb(255, 100, 100, 100));

        // Act
        var mask = new System.Numerics.Vector<byte>(50);
        BitmapHelper.ProcessPixelsSimdHardware(wb, vec => vec + mask);

        // Assert
        var p = wb.GetPixel(3, 3);
        p.R.Should().Be(150);
        p.G.Should().Be(150);
        p.B.Should().Be(150);
    }
}