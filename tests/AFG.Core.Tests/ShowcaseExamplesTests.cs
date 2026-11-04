// filepath: tests/AFG.Core.Tests/ShowcaseExamplesTests.cs
using System.IO;
using AFG.Core.Serialization;

namespace AFG.Core.Tests;

/// <summary>
/// 驗證 7 大擴充實機範例設計檔之產生、存在性與序列化/反序列化 roundtrip 完整度。
/// </summary>
public sealed class ShowcaseExamplesTests
{
    private static string GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "AvaloniaFormGenerator.slnx")))
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return dir;
    }

    static ShowcaseExamplesTests()
    {
        var root = GetSolutionRoot();

        var examples = new (string Folder, string FileName, Func<Models.Ast.FormDocument> Factory)[]
        {
            ("BasicControlsShowcase", "BasicControlsShowcase.afg.json", ShowcaseExamplesDocumentBuilder.CreateBasicControlsDocument),
            ("DialogsShowcase", "DialogsShowcase.afg.json", ShowcaseExamplesDocumentBuilder.CreateDialogsDocument),
            ("PictureBoxShowcase", "PictureBoxShowcase.afg.json", ShowcaseExamplesDocumentBuilder.CreatePictureBoxDocument),
            ("VectorGraphicsShowcase", "VectorGraphicsShowcase.afg.json", ShowcaseExamplesDocumentBuilder.CreateVectorGraphicsDocument),
            ("HighPerformanceDrawingShowcase", "HighPerformanceDrawingShowcase.afg.json", ShowcaseExamplesDocumentBuilder.CreateHighPerformanceDrawingDocument),
            ("MediaPlayerShowcase", "MediaPlayerShowcase.afg.json", ShowcaseExamplesDocumentBuilder.CreateMediaPlayerDocument),
            ("MultiLanguageInteropShowcase", "MultiLanguageInteropShowcase.afg.json", ShowcaseExamplesDocumentBuilder.CreateMultiLanguageInteropDocument)
        };

        foreach (var (folder, fileName, factory) in examples)
        {
            var targetDir = Path.Combine(root, "example", folder);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            var filePath = Path.Combine(targetDir, fileName);
            var doc = factory();
            var json = AfgSerializer.SerializeDocument(doc);
            File.WriteAllText(filePath, json);
        }
    }

    [Theory]
    [InlineData("BasicControlsShowcase", "BasicControlsShowcase.afg.json")]
    [InlineData("DialogsShowcase", "DialogsShowcase.afg.json")]
    [InlineData("PictureBoxShowcase", "PictureBoxShowcase.afg.json")]
    [InlineData("VectorGraphicsShowcase", "VectorGraphicsShowcase.afg.json")]
    [InlineData("HighPerformanceDrawingShowcase", "HighPerformanceDrawingShowcase.afg.json")]
    [InlineData("MediaPlayerShowcase", "MediaPlayerShowcase.afg.json")]
    [InlineData("MultiLanguageInteropShowcase", "MultiLanguageInteropShowcase.afg.json")]
    public void ExampleAfgJson_ShouldExist_AndDeserializeRoundtrip(string folder, string fileName)
    {
        // Arrange
        var root = GetSolutionRoot();
        var filePath = Path.Combine(root, "example", folder, fileName);

        // Assert existence
        File.Exists(filePath).Should().BeTrue($"檔案 {filePath} 必須存在");

        // Act: Deserialize
        var json = File.ReadAllText(filePath);
        var doc = AfgSerializer.DeserializeDocument(json);

        // Assert: Document integrity
        doc.Should().NotBeNull();
        doc.RootNode.Should().NotBeNull();
        doc.RootNode.Children.Count.Should().BeGreaterThan(0);

        // Roundtrip serialization
        var roundtripJson = AfgSerializer.SerializeDocument(doc);
        roundtripJson.Should().NotBeNullOrWhiteSpace();
        var roundtripDoc = AfgSerializer.DeserializeDocument(roundtripJson);
        roundtripDoc.ProjectName.Should().Be(doc.ProjectName);
        roundtripDoc.RootNode.Children.Count.Should().Be(doc.RootNode.Children.Count);
    }
}
