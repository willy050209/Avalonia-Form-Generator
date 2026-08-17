// filepath: tests/AFG.Generators.Tests/RoslynCompilerServiceTests.cs
namespace AFG.Generators.Tests;

/// <summary>
/// 驗證 Roslyn 格式化工具與記憶體編譯診斷服務。
/// </summary>
public sealed class RoslynCompilerServiceTests
{
    private readonly RoslynCompilerService _service = new();

    [Fact]
    public void Format_ShouldNormalizeIndentationAndSpacing()
    {
        // Arrange
        const string unformatted = "namespace Foo{public class Bar{public void Do(){int a=1+2;}}}";

        // Act
        var formatted = _service.FormatCode(unformatted);

        // Assert
        formatted.Should().NotBeNullOrWhiteSpace();
        formatted.Should().Contain(Environment.NewLine);
        formatted.Should().Contain("public void Do()");
    }

    [Fact]
    public async Task CompileInMemoryAsync_ShouldSucceed_ForValidCSharpCode()
    {
        // Arrange
        const string validSource = """
        namespace TestApp;

        public class Calculator
        {
            public int Add(int a, int b) => a + b;
        }
        """;

        var files = new[]
        {
            new GeneratedSourceFile("Calculator.cs", validSource, SourceFileType.ViewModel)
        };

        // Act
        var result = await _service.CompileInMemoryAsync(files);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AssemblyBytes.Should().NotBeNull();
        result.AssemblyBytes!.Length.Should().BeGreaterThan(0);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task CompileInMemoryAsync_ShouldReportDiagnostics_ForInvalidCSharpCode()
    {
        // Arrange
        const string invalidSource = """
        namespace TestApp;

        public class BrokenClass
        {
            // 語法錯誤：缺少分號與型別
            invalid syntax here !
        }
        """;

        var files = new[]
        {
            new GeneratedSourceFile("BrokenClass.cs", invalidSource, SourceFileType.ViewModel)
        };

        // Act
        var result = await _service.CompileInMemoryAsync(files);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void CheckSyntaxDiagnostics_ShouldDetectSyntaxErrors()
    {
        // Arrange
        const string brokenCode = "public class A { void M() { int a = ; } }";

        // Act
        var errors = RoslynCompilerService.CheckSyntaxDiagnostics(brokenCode);

        // Assert
        errors.Should().NotBeEmpty();
    }
}
