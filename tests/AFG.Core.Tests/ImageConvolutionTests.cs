// filepath: tests/AFG.Core.Tests/ImageConvolutionTests.cs
using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluentAssertions;
using Xunit;

namespace AFG.Core.Tests;

public class ImageConvolutionTests
{
    static ImageConvolutionTests()
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

    private readonly struct InvertAndMaskCheckerPostProcessor : IConvolutionPostProcessor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PostProcess(int x, int y, ref float b, ref float g, ref float r, ref float a)
        {
            if (((x + y) & 1) == 0)
            {
                r = 255f - r;
                g = 255f - g;
                b = 255f - b;
            }
        }
    }

    private static WriteableBitmap CreatePatternBitmap(int width, int height, bool varyingAlpha = false)
    {
        var wb = BitmapHelper.CreateInitializedBitmap(width, height, Colors.Black);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = (byte)((x * 23 + y * 7) & 255);
                byte g = (byte)((y * 31 + x * 5) & 255);
                byte b = (byte)(((x ^ y) * 19 + 40) & 255);
                byte a = varyingAlpha ? (byte)((x & 1) == 0 ? 120 : 240) : (byte)255;
                wb.SetPixel(x, y, Color.FromArgb(a, r, g, b));
            }
        }
        return wb;
    }

    private static void AssertBitmapsMatchWithin(WriteableBitmap actual, WriteableBitmap expected, int tolerance)
    {
        actual.PixelSize.Should().Be(expected.PixelSize);
        int w = actual.PixelSize.Width;
        int h = actual.PixelSize.Height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var p1 = actual.GetPixel(x, y);
                var p2 = expected.GetPixel(x, y);
                Math.Abs(p1.R - p2.R).Should().BeLessThanOrEqualTo(tolerance, $"R mismatch at ({x},{y})");
                Math.Abs(p1.G - p2.G).Should().BeLessThanOrEqualTo(tolerance, $"G mismatch at ({x},{y})");
                Math.Abs(p1.B - p2.B).Should().BeLessThanOrEqualTo(tolerance, $"B mismatch at ({x},{y})");
                Math.Abs(p1.A - p2.A).Should().BeLessThanOrEqualTo(tolerance, $"A mismatch at ({x},{y})");
            }
        }
    }

    [Fact]
    public void ConvolutionKernel_Validation_ShouldThrowOnInvalidDimensionsOrNulls()
    {
        Action null2D = () => ConvolutionKernel.Create2D(null!);
        null2D.Should().Throw<ArgumentNullException>();

        Action even2D = () => ConvolutionKernel.Create2D(new float[2, 3]);
        even2D.Should().Throw<ArgumentException>();

        Action empty1D = () => ConvolutionKernel.CreateSeparable([], [1f]);
        empty1D.Should().Throw<ArgumentException>();

        Action even1D = () => ConvolutionKernel.CreateSeparable([0.5f, 0.5f], [1f]);
        even1D.Should().Throw<ArgumentException>();

        Action mismatchedGradient = () => ConvolutionKernel.CreateGradient(new float[3, 3], new float[5, 5]);
        mismatchedGradient.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ConvolutionKernel_ImmutabilityAndTransforms_ShouldWorkCorrectly()
    {
        var rectKernel = ConvolutionKernel.Create2D(
            new float[,]
            {
                { 1, 2, 1, 2, 1 },
                { 2, 4, 2, 4, 2 },
                { 1, 2, 1, 2, 1 }
            },
            factor: 1.0f,
            bias: 5.0f,
            normalize: false,
            name: "Rect3x5");

        rectKernel.Kind.Should().Be(ConvolutionKernelKind.Matrix2D);
        rectKernel.Width.Should().Be(5);
        rectKernel.Height.Should().Be(3);
        rectKernel.RadiusX.Should().Be(2);
        rectKernel.RadiusY.Should().Be(1);

        var scaled = rectKernel.WithFactor(0.5f);
        var shifted = scaled.WithBias(12.0f);
        var normalized = shifted.Normalized();

        rectKernel.Factor.Should().Be(1.0f);
        rectKernel.Bias.Should().Be(5.0f);
        scaled.Factor.Should().Be(0.5f);
        shifted.Bias.Should().Be(12.0f);
        normalized.Factor.Should().BeApproximately(1.0f / 28.0f, 1e-5f);
    }

    [Fact]
    public void Matrix2D_Identity_ShouldPreservePixelsExactly()
    {
        var src = CreatePatternBitmap(33, 21);
        var dst = src.Convolve(ConvolutionKernel.Identity);

        AssertBitmapsMatchWithin(dst, src, 0);
    }

    [Theory]
    [InlineData(PixelProcessingMode.ParallelVectorized)]
    [InlineData(PixelProcessingMode.SequentialVectorized)]
    [InlineData(PixelProcessingMode.Parallel)]
    public void Matrix2D_BuiltInAndCustomRectKernels_SimdShouldMatchSequential(PixelProcessingMode mode)
    {
        var src = CreatePatternBitmap(37, 23);
        var rect3x5 = ConvolutionKernel.Create2D(
            new float[,]
            {
                {  0, -1,  0, -1,  0 },
                { -1,  2,  5,  2, -1 },
                {  0, -1,  0, -1,  0 }
            },
            factor: 1.0f / 5.0f,
            bias: 2.0f,
            name: "Custom3x5");

        ConvolutionKernel[] kernels =
        [
            ConvolutionKernel.Sharpen,
            ConvolutionKernel.StrongSharpen,
            ConvolutionKernel.Emboss,
            ConvolutionKernel.EdgeDetect,
            ConvolutionKernel.Laplacian4,
            ConvolutionKernel.Laplacian8,
            ConvolutionKernel.UnsharpMask5x5,
            rect3x5
        ];

        var seqOptions = new ConvolutionOptions(ProcessingMode: PixelProcessingMode.Sequential);
        var targetOptions = new ConvolutionOptions(ProcessingMode: mode);

        foreach (var kernel in kernels)
        {
            var expected = src.Convolve(kernel, seqOptions);
            var actual = src.Convolve(kernel, targetOptions);
            AssertBitmapsMatchWithin(actual, expected, 1);

            // 原地 ApplyConvolution 應與非破壞性 Convolve 結果一致
            var inPlace = src.ConvertToWriteableBitmap();
            inPlace.ApplyConvolution(kernel, targetOptions);
            AssertBitmapsMatchWithin(inPlace, actual, 0);
        }
    }

    [Fact]
    public void Matrix2D_AllFourBorderModes_ShouldResolveLeftEdgeDistinctly()
    {
        // 建立 4x4 橫向色帶：[Red, Green, Blue, White]
        var src = BitmapHelper.CreateInitializedBitmap(4, 4, Colors.Black);
        Color[] cols = [Colors.Red, Colors.Lime, Colors.Blue, Colors.White];
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                src.SetPixel(x, y, cols[x]);
            }
        }

        // 取樣 (x - 2, y) 的 5x1 單位位移算子：x=0 處會讀取 x = -2
        var shiftRightTwo = ConvolutionKernel.Create2D(
            new float[,] { { 1f, 0f, 0f, 0f, 0f } },
            name: "SampleXMinus2");

        var clampOut = src.Convolve(shiftRightTwo, new ConvolutionOptions(BorderMode: ConvolutionBorderMode.Clamp));
        var zeroOut = src.Convolve(shiftRightTwo, new ConvolutionOptions(BorderMode: ConvolutionBorderMode.Zero));
        var reflectOut = src.Convolve(shiftRightTwo, new ConvolutionOptions(BorderMode: ConvolutionBorderMode.Reflect));
        var wrapOut = src.Convolve(shiftRightTwo, new ConvolutionOptions(BorderMode: ConvolutionBorderMode.Wrap));

        // x=-2 依四種邊界策略應分別對應：Clamp->x=0(Red), Zero->Black, Reflect->x=1(Lime), Wrap->x=2(Blue)
        clampOut.GetPixel(0, 1).Should().Be(Colors.Red);
        var zeroPixel = zeroOut.GetPixel(0, 1);
        zeroPixel.R.Should().Be(0);
        zeroPixel.G.Should().Be(0);
        zeroPixel.B.Should().Be(0);
        reflectOut.GetPixel(0, 1).Should().Be(Colors.Lime);
        wrapOut.GetPixel(0, 1).Should().Be(Colors.Blue);
    }

    [Fact]
    public void ConvolutionAlphaMode_PreserveVsConvolve_ShouldBehaveAsConfigured()
    {
        var src = CreatePatternBitmap(16, 12, varyingAlpha: true);
        var box3 = ConvolutionKernel.CreateBox(1);

        var preserved = src.Convolve(
            ConvolutionKernel.Sharpen,
            new ConvolutionOptions(AlphaMode: ConvolutionAlphaMode.Preserve));
        var convolved = src.Convolve(
            box3,
            new ConvolutionOptions(AlphaMode: ConvolutionAlphaMode.Convolve));

        // Preserve 模式：x=0 (原A=120) 與 x=1 (原A=240) 的 Alpha 應維持原值
        preserved.GetPixel(4, 4).A.Should().Be(120);
        preserved.GetPixel(5, 4).A.Should().Be(240);

        // Convolve 模式：交錯 120 / 240 經 3x3 盒狀平均後 Alpha 會平滑為中間值 (約 160~200)
        int blurredAlpha = convolved.GetPixel(4, 4).A;
        blurredAlpha.Should().BeInRange(155, 205);
    }

    [Fact]
    public void GradientDual2D_SobelPrewittScharr_ModesAndThreshold_ShouldProduceExpectedEdges()
    {
        // 左半純紅、右半純綠（水平亮度接近但色彩通道劇烈跳變）
        var src = BitmapHelper.CreateInitializedBitmap(24, 16, Colors.Black);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                src.SetPixel(x, y, x < 12 ? Color.FromArgb(255, 180, 20, 20) : Color.FromArgb(255, 20, 180, 20));
            }
        }

        // Prewitt / Scharr / Sobel 三種梯度算子皆應可正常執行並偵測垂直邊界
        foreach (var gKernel in new[] { ConvolutionKernel.Sobel, ConvolutionKernel.Prewitt, ConvolutionKernel.Scharr })
        {
            var seq = src.Convolve(gKernel, new ConvolutionOptions(ProcessingMode: PixelProcessingMode.Sequential));
            var simd = src.Convolve(gKernel, new ConvolutionOptions(ProcessingMode: PixelProcessingMode.ParallelVectorized));
            AssertBitmapsMatchWithin(simd, seq, 1);

            var lumPixel = simd.GetPixel(12, 8);
            lumPixel.R.Should().Be(lumPixel.G);
            lumPixel.G.Should().Be(lumPixel.B);
            lumPixel.R.Should().BeGreaterThan(30);
        }

        // PerChannelRgb 應保留各色彩通道獨立響應（R/G 梯度強、B 梯度為 0）
        var rgbEdges = src.Convolve(
            ConvolutionKernel.Sobel,
            new ConvolutionOptions(
                GradientChannelMode: GradientChannelMode.PerChannelRgb,
                GradientNormMode: GradientNormMode.L2));
        var edgeAtBoundary = rgbEdges.GetPixel(12, 8);
        edgeAtBoundary.R.Should().BeGreaterThan(100);
        edgeAtBoundary.G.Should().BeGreaterThan(100);
        edgeAtBoundary.B.Should().Be(0);

        // L1 vs L2 在對角線邊界上應滿足 L1 >= L2
        var diag = BitmapHelper.CreateInitializedBitmap(16, 16, Colors.Black);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                if (x + y >= 15) diag.SetPixel(x, y, Colors.White);

        var l1Out = diag.Convolve(ConvolutionKernel.Sobel, new ConvolutionOptions(GradientNormMode: GradientNormMode.L1));
        var l2Out = diag.Convolve(ConvolutionKernel.Sobel, new ConvolutionOptions(GradientNormMode: GradientNormMode.L2));
        l1Out.GetPixel(8, 7).R.Should().BeGreaterThanOrEqualTo(l2Out.GetPixel(8, 7).R);

        // 高 Threshold 應將弱邊界歸零
        var suppressed = src.Convolve(
            ConvolutionKernel.Prewitt,
            new ConvolutionOptions(GradientChannelMode: GradientChannelMode.Luminance, GradientThreshold: 255.0f));
        suppressed.GetPixel(12, 8).R.Should().Be(0);
    }

    [Fact]
    public void GenericPostProcessor_ShouldTransformOutputPixelsZeroOverhead()
    {
        var src = BitmapHelper.CreateInitializedBitmap(10, 10, Color.FromArgb(255, 30, 60, 90));
        var result = src.Convolve(ConvolutionKernel.Identity, new InvertAndMaskCheckerPostProcessor());

        // (0, 0) 為偶數座標 -> 反相；(1, 0) 為奇數座標 -> 保持原色
        var evenPx = result.GetPixel(0, 0);
        evenPx.R.Should().Be(225);
        evenPx.G.Should().Be(195);
        evenPx.B.Should().Be(165);

        var oddPx = result.GetPixel(1, 0);
        oddPx.R.Should().Be(30);
        oddPx.G.Should().Be(60);
        oddPx.B.Should().Be(90);

        // 原地 ApplyConvolution<TPost> 應獲得相同結果
        var inPlace = src.ConvertToWriteableBitmap();
        inPlace.ApplyConvolution(ConvolutionKernel.Identity, new InvertAndMaskCheckerPostProcessor());
        AssertBitmapsMatchWithin(inPlace, result, 0);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 5)]
    public void TinyBitmaps_AllKernelKinds_ShouldExecuteWithoutErrors(int width, int height)
    {
        var src = CreatePatternBitmap(width, height);
        ConvolutionKernel[] kernels =
        [
            ConvolutionKernel.Sharpen,
            ConvolutionKernel.UnsharpMask5x5,
            ConvolutionKernel.CreateGaussian(2),
            ConvolutionKernel.CreateBox(2),
            ConvolutionKernel.Sobel
        ];

        foreach (var kernel in kernels)
        {
            var res = src.Convolve(kernel);
            res.PixelSize.Should().Be(new PixelSize(width, height));
        }
    }
}
