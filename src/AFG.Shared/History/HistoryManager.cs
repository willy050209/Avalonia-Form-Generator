// filepath: src/AFG.Shared/History/HistoryManager.cs
using System.Collections.Generic;
using AFG.Core.Models.Ast;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AFG.Shared.History;

/// <summary>
/// 基於不可變 FormDocument 快照的復原/重做 (Undo/Redo) 歷史管理器 (Memento 模式)。
/// </summary>
public sealed partial class HistoryManager : ObservableObject
{
    private readonly Stack<FormDocument> _undoStack = new();
    private readonly Stack<FormDocument> _redoStack = new();
    private readonly int _maxHistoryCount;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    public HistoryManager(int maxHistoryCount = 50)
    {
        _maxHistoryCount = Math.Max(5, maxHistoryCount);
    }

    /// <summary>
    /// 在使用者執行可復原之變更前或變更後發布歷史快照。
    /// </summary>
    public void PushSnapshot(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_undoStack.Count > 0 && _undoStack.Peek().Equals(document))
        {
            return;
        }

        _undoStack.Push(document);
        _redoStack.Clear();

        if (_undoStack.Count > _maxHistoryCount)
        {
            // 堆疊超過最大上限時拋棄最舊快照
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (var i = items.Length - 2; i >= 0; i--)
            {
                _undoStack.Push(items[i]);
            }
        }

        UpdateCanExecute();
    }

    /// <summary>
    /// 執行復原操作，回傳上一個歷史快照。
    /// </summary>
    public FormDocument? Undo(FormDocument currentDocument)
    {
        if (_undoStack.Count == 0)
        {
            return null;
        }

        _redoStack.Push(currentDocument);
        var previous = _undoStack.Pop();
        UpdateCanExecute();
        return previous;
    }

    /// <summary>
    /// 執行重做操作，回傳重做後的快照。
    /// </summary>
    public FormDocument? Redo(FormDocument currentDocument)
    {
        if (_redoStack.Count == 0)
        {
            return null;
        }

        _undoStack.Push(currentDocument);
        var next = _redoStack.Pop();
        UpdateCanExecute();
        return next;
    }

    /// <summary>
    /// 清空所有歷史紀錄。
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateCanExecute();
    }

    private void UpdateCanExecute()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }
}
