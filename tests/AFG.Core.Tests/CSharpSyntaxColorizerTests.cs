// filepath: tests/AFG.Core.Tests/CSharpSyntaxColorizerTests.cs
using Avalonia.Controls.Documents;
using AFG.Shared.Services;
using FluentAssertions;

namespace AFG.Core.Tests;

public sealed class CSharpSyntaxColorizerTests
{
    [Fact]
    public void PopulateInlines_WithValidCSharpCode_ShouldGenerateColoredInlines()
    {
        // Arrange
        var inlines = new InlineCollection();
        var code = "public class MainFormView : UserControl\n{\n    // Comment\n    string text = \"Hello World\";\n}";

        // Act
        CSharpSyntaxColorizer.PopulateInlines(inlines, code);

        // Assert
        inlines.Should().NotBeEmpty();
        inlines.Should().Contain(i => i is Run && ((Run)i).Text == "public");
        inlines.Should().Contain(i => i is Run && ((Run)i).Text == "class");
        inlines.Should().Contain(i => i is Run && ((Run)i).Text == "UserControl");
        inlines.Should().Contain(i => i is Run && ((Run)i).Text == "\"Hello World\"");
        inlines.OfType<Run>().Should().Contain(run => run.Text != null && run.Text.Contains("// Comment"));
    }

    [Fact]
    public void PopulateInlines_WithEmptyCode_ShouldClearInlines()
    {
        // Arrange
        var inlines = new InlineCollection();
        inlines.Add(new Run("test"));

        // Act
        CSharpSyntaxColorizer.PopulateInlines(inlines, "");

        // Assert
        inlines.Should().BeEmpty();
    }
}
