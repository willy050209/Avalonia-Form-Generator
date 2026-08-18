// filepath: tests/AFG.Core.Tests/HistoryManagerTests.cs
using AFG.Shared.History;

namespace AFG.Core.Tests;

/// <summary>
/// 驗證 HistoryManager Memento 快照與 Undo/Redo 歷史堆疊行為。
/// </summary>
public sealed class HistoryManagerTests
{
    [Fact]
    public void PushSnapshot_ShouldEnableCanUndoAndDisableCanRedo()
    {
        var history = new HistoryManager();
        var doc1 = FormDocument.CreateDefault();
        var doc2 = doc1 with { CanvasWidth = 900 };

        history.PushSnapshot(doc1);

        history.CanUndo.Should().BeTrue();
        history.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void UndoAndRedo_ShouldRestorePreviousAndNextSnapshotsCorrectly()
    {
        var history = new HistoryManager();
        var doc1 = FormDocument.CreateDefault();
        var doc2 = doc1 with { CanvasWidth = 900 };
        var doc3 = doc1 with { CanvasWidth = 1000 };

        history.PushSnapshot(doc1);
        history.PushSnapshot(doc2);

        // Act: Undo from doc3
        var previous = history.Undo(doc3);

        // Assert
        previous.Should().NotBeNull();
        previous!.CanvasWidth.Should().Be(900);
        history.CanUndo.Should().BeTrue();
        history.CanRedo.Should().BeTrue();

        // Act: Redo
        var next = history.Redo(previous);
        next.Should().NotBeNull();
        next!.CanvasWidth.Should().Be(1000);
        history.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void PushSnapshot_AfterUndo_ShouldClearRedoStack()
    {
        var history = new HistoryManager();
        var doc1 = FormDocument.CreateDefault();
        var doc2 = doc1 with { CanvasWidth = 900 };
        var doc3 = doc1 with { CanvasWidth = 1000 };

        history.PushSnapshot(doc1);
        history.Undo(doc2);
        history.CanRedo.Should().BeTrue();

        // Act: 新動作發布快照
        history.PushSnapshot(doc3);

        // Assert: Redo 堆疊應被清空
        history.CanRedo.Should().BeFalse();
    }
}
