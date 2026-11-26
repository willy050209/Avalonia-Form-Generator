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

    private readonly struct InvertVectorFunctor : IVectorTransform
    {
        private static readonly System.Numerics.Vector<byte> Mask = new(255);

        public System.Numerics.Vector<byte> Transform(System.Numerics.Vector<byte> vector) => Mask - vector;

        public void TransformRemainder(ref byte b, ref byte g, ref byte r, ref byte a)
        {
            b = (byte)(255 - b);
            g = (byte)(255 - g);
            r = (byte)(255 - r);
            a = (byte)(255 - a);
        }
    }

    private readonly struct InvertRgbPixelFunctor : IPixelProcessor
    {
        public void Process(ref byte b, ref byte g, ref byte r, ref byte a)
        {
            b = (byte)(255 - b);
            g = (byte)(255 - g);
            r = (byte)(255 - r);
        }
    }

    private readonly struct GradientLocationFunctor : IPixelLocationProcessor
    {
        public void Process(int x, int y, ref byte b, ref byte g, ref byte r, ref byte a)
        {
            b = (byte)((x * 11) & 255);
            g = (byte)((y * 13) & 255);
            r = (byte)(((x + y) * 7) & 255);
            a = 255;
        }
    }

    private readonly struct FillRowFunctor(byte rVal, byte gVal, byte bVal) : IRowPixelProcessor
    {
        public void ProcessRow(int y, Span<byte> rowBytes, int width)
        {
            for (int x = 0; x < width; x++)
            {
                int off = x * 4;
                rowBytes[off + 0] = bVal;
                rowBytes[off + 1] = gVal;
                rowBytes[off + 2] = rVal;
                rowBytes[off + 3] = 255;
            }
        }
    }

    [Theory]
    [InlineData(PixelProcessingMode.Sequential)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.Parallel)]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    public void ProcessPixels_GenericStructFunctors_ShouldMatchExpectedOutput(PixelProcessingMode mode)
    {
        var wbVec = BitmapHelper.CreateInitializedBitmap(19, 13, Color.FromArgb(255, 40, 80, 120));
        wbVec.ProcessPixels(new InvertVectorFunctor(), mode);
        var pv = wbVec.GetPixel(18, 12);
        pv.R.Should().Be(215);
        pv.G.Should().Be(175);
        pv.B.Should().Be(135);
        pv.A.Should().Be(0);

        var wbPx = BitmapHelper.CreateInitializedBitmap(23, 11, Color.FromArgb(200, 40, 80, 120));
        wbPx.ProcessPixelsPerPixel(new InvertRgbPixelFunctor(), mode);
        var pp = wbPx.GetPixel(22, 10);
        pp.R.Should().Be(215);
        pp.G.Should().Be(175);
        pp.B.Should().Be(135);
        pp.A.Should().Be(200);

        var wbLoc = BitmapHelper.CreateInitializedBitmap(17, 9, Colors.Black);
        wbLoc.ProcessPixelsLocation(new GradientLocationFunctor(), mode);
        var pl = wbLoc.GetPixel(5, 3);
        pl.B.Should().Be((byte)((5 * 11) & 255));
        pl.G.Should().Be((byte)((3 * 13) & 255));
        pl.R.Should().Be((byte)(((5 + 3) * 7) & 255));

        var wbRow = BitmapHelper.CreateInitializedBitmap(19, 7, Colors.Black);
        wbRow.ProcessPixelsRow(new FillRowFunctor(10, 20, 30), mode);
        var pr = wbRow.GetPixel(17, 6);
        pr.R.Should().Be(10);
        pr.G.Should().Be(20);
        pr.B.Should().Be(30);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(37)]
    public void ApplyGrayscale_SimdWidths_ShouldMatchScalarAndPreserveAlpha(int width)
    {
        int height = 7;
        var wbScalar = BitmapHelper.CreateInitializedBitmap(width, height, Colors.Black);
        var wbSimd = BitmapHelper.CreateInitializedBitmap(width, height, Colors.Black);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var c = Color.FromArgb((byte)(100 + ((x + y) % 155)), (byte)((x * 17) & 255), (byte)((y * 37) & 255), (byte)(((x + y) * 23) & 255));
                wbScalar.SetPixel(x, y, c);
                wbSimd.SetPixel(x, y, c);
            }
        }

        wbScalar.ApplyGrayscale(PixelProcessingMode.Sequential);
        wbSimd.ApplyGrayscale(PixelProcessingMode.ParallelVectorized);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var s = wbScalar.GetPixel(x, y);
                var v = wbSimd.GetPixel(x, y);
                v.R.Should().Be(v.G);
                v.G.Should().Be(v.B);
                v.A.Should().Be(s.A);
                Math.Abs(v.R - s.R).Should().BeLessThanOrEqualTo(1);
            }
        }
    }

    [Theory]
    [InlineData(PixelProcessingMode.Sequential)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.Parallel)]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    public void DetectEdges_SmallAndUnalignedDimensions_ShouldHandleSafely(PixelProcessingMode mode)
    {
        var wbTiny = BitmapHelper.CreateInitializedBitmap(2, 2, Colors.White);
        var tinyEdges = wbTiny.DetectEdges(0, mode);
        tinyEdges.GetPixel(0, 0).Should().Be(Colors.Black);
        tinyEdges.GetPixel(1, 1).Should().Be(Colors.Black);

        var wbSrc = BitmapHelper.CreateInitializedBitmap(35, 19, Colors.Black);
        for (int y = 0; y < 19; y++)
        {
            for (int x = 15; x < 35; x++)
            {
                wbSrc.SetPixel(x, y, Color.FromArgb(255, (byte)(x * 7), (byte)(y * 12), 200));
            }
        }

        var scalar = wbSrc.DetectEdges(15.0, PixelProcessingMode.Sequential);
        var simd = wbSrc.DetectEdges(15.0, mode);
        for (int y = 0; y < 19; y++)
        {
            for (int x = 0; x < 35; x++)
            {
                Math.Abs(simd.GetPixel(x, y).R - scalar.GetPixel(x, y).R).Should().BeLessThanOrEqualTo(1);
                simd.GetPixel(x, y).A.Should().Be(255);
            }
        }
    }

    [Theory]
    [InlineData(1, 37, 23)]
    [InlineData(2, 37, 23)]
    [InlineData(8, 37, 23)]
    [InlineData(6, 5, 5)]
    public void ApplyGaussianAndBoxBlur_Simd_ShouldMatchScalarWithinOneLsb(int radius, int width, int height)
    {
        var wbG1 = BitmapHelper.CreateInitializedBitmap(width, height, Colors.Black);
        var wbG2 = BitmapHelper.CreateInitializedBitmap(width, height, Colors.Black);
        var wbB1 = BitmapHelper.CreateInitializedBitmap(width, height, Colors.Black);
        var wbB2 = BitmapHelper.CreateInitializedBitmap(width, height, Colors.Black);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var c = Color.FromArgb(255, (byte)((x * 19 + y) & 255), (byte)((y * 29 + x) & 255), (byte)((x ^ y) * 17 & 255));
                wbG1.SetPixel(x, y, c);
                wbG2.SetPixel(x, y, c);
                wbB1.SetPixel(x, y, c);
                wbB2.SetPixel(x, y, c);
            }
        }

        wbG1.ApplyGaussianBlur(radius, PixelProcessingMode.Sequential);
        wbG2.ApplyGaussianBlur(radius, PixelProcessingMode.ParallelVectorized);
        wbB1.ApplyBoxBlur(radius, PixelProcessingMode.Sequential);
        wbB2.ApplyBoxBlur(radius, PixelProcessingMode.ParallelVectorized);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var g1 = wbG1.GetPixel(x, y);
                var g2 = wbG2.GetPixel(x, y);
                Math.Abs(g2.R - g1.R).Should().BeLessThanOrEqualTo(1);
                Math.Abs(g2.G - g1.G).Should().BeLessThanOrEqualTo(1);
                Math.Abs(g2.B - g1.B).Should().BeLessThanOrEqualTo(1);

                var b1 = wbB1.GetPixel(x, y);
                var b2 = wbB2.GetPixel(x, y);
                Math.Abs(b2.R - b1.R).Should().BeLessThanOrEqualTo(1);
                Math.Abs(b2.G - b1.G).Should().BeLessThanOrEqualTo(1);
                Math.Abs(b2.B - b1.B).Should().BeLessThanOrEqualTo(1);
            }
        }
    }

    [Fact]
    public void SharpenAndUnsharpMask_ExtensionsAndStaticForwarders_ShouldMatchAndEnhanceContrast()
    {
        var src = BitmapHelper.CreateInitializedBitmap(33, 21, Color.FromArgb(255, 80, 90, 100));
        for (int y = 0; y < 21; y++)
        {
            for (int x = 16; x < 33; x++)
            {
                src.SetPixel(x, y, Color.FromArgb(255, 170, 180, 190));
            }
        }

        var sharpSeq = BitmapHelper.Sharpen(src, intensity: 0.8f, eightNeighbor: false, PixelProcessingMode.Sequential);
        var sharpSimd = src.Sharpen(intensity: 0.8f, eightNeighbor: false, PixelProcessingMode.ParallelVectorized);

        var sharpInPlace = src.ConvertToWriteableBitmap();
        BitmapHelper.ApplySharpen(sharpInPlace, intensity: 0.8f, eightNeighbor: false, PixelProcessingMode.ParallelVectorized);

        sharpSimd.GetPixel(16, 10).R.Should().BeGreaterThan(170);
        sharpSimd.GetPixel(15, 10).R.Should().BeLessThan(80);
        Math.Abs(sharpSimd.GetPixel(16, 10).R - sharpSeq.GetPixel(16, 10).R).Should().BeLessThanOrEqualTo(1);
        sharpInPlace.GetPixel(16, 10).Should().Be(sharpSimd.GetPixel(16, 10));

        var usmExt = src.UnsharpMask(radius: 2, amount: 1.2f, threshold: 3.0f);
        var usmStatic = BitmapHelper.UnsharpMask(src, radius: 2, amount: 1.2f, threshold: 3.0f);
        var usmInPlace = src.ConvertToWriteableBitmap();
        BitmapHelper.ApplyUnsharpMask(usmInPlace, radius: 2, amount: 1.2f, threshold: 3.0f);

        usmExt.GetPixel(16, 10).R.Should().BeGreaterThan(170);
        usmStatic.GetPixel(16, 10).Should().Be(usmExt.GetPixel(16, 10));
        usmInPlace.GetPixel(16, 10).Should().Be(usmExt.GetPixel(16, 10));
    }
}