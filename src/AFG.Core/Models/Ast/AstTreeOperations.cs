// filepath: src/AFG.Core/Models/Ast/AstTreeOperations.cs
namespace AFG.Core.Models.Ast;

/// <summary>
/// 提供針對 UI AST 語意樹進行不可變操作與查詢的純函數集合。
/// </summary>
public static class AstTreeOperations
{
    /// <summary>
    /// 在 AST 樹中根據節點 Id 尋找特定節點。
    /// </summary>
    /// <param name="root">AST 根節點。</param>
    /// <param name="id">要尋找的節點 Id。</param>
    /// <returns>找到的節點，若不存在則回傳 null。</returns>
    /// <exception cref="ArgumentNullException">當 root 或 id 為 null 時擲出。</exception>
    public static AstNode? FindNodeById(AstNode root, string id)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(id);

        if (root.Id == id)
        {
            return root;
        }

        return root.Children
            .Select(child => FindNodeById(child, id))
            .FirstOrDefault(found => found is not null);
    }

    /// <summary>
    /// 尋找指定子節點的父節點。
    /// </summary>
    /// <param name="root">AST 根節點。</param>
    /// <param name="childId">子節點 Id。</param>
    /// <returns>父節點，若為根節點或不存在則回傳 null。</returns>
    /// <exception cref="ArgumentNullException">當 root 或 childId 為 null 時擲出。</exception>
    public static AstNode? FindParentNode(AstNode root, string childId)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(childId);

        if (root.Children.Any(c => c.Id == childId))
        {
            return root;
        }

        return root.Children
            .Select(child => FindParentNode(child, childId))
            .FirstOrDefault(found => found is not null);
    }

    /// <summary>
    /// 將所有節點展平成唯讀清單（深度優先遍歷）。
    /// </summary>
    /// <param name="root">AST 根節點。</param>
    /// <returns>包含所有節點的清單。</returns>
    /// <exception cref="ArgumentNullException">當 root 為 null 時擲出。</exception>
    public static IReadOnlyList<AstNode> Flatten(AstNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var list = new List<AstNode> { root };
        foreach (var child in root.Children)
        {
            list.AddRange(Flatten(child));
        }

        return list.ToImmutableList();
    }

    /// <summary>
    /// 取得從根節點到目標節點的路徑節點清單（祖先清單，依序由根至父節點）。
    /// </summary>
    /// <param name="root">AST 根節點。</param>
    /// <param name="targetId">目標節點 Id。</param>
    /// <returns>祖先節點清單。</returns>
    public static IReadOnlyList<AstNode> GetAncestors(AstNode root, string targetId)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(targetId);

        var path = new List<AstNode>();
        if (BuildPath(root, targetId, path))
        {
            // 移除目標自身，保留祖先
            if (path.Count > 0)
            {
                path.RemoveAt(path.Count - 1);
            }
        }

        return path.ToImmutableList();

        static bool BuildPath(AstNode current, string id, List<AstNode> currentPath)
        {
            currentPath.Add(current);
            if (current.Id == id)
            {
                return true;
            }

            foreach (var child in current.Children)
            {
                if (BuildPath(child, id, currentPath))
                {
                    return true;
                }
            }

            currentPath.RemoveAt(currentPath.Count - 1);
            return false;
        }
    }

    /// <summary>
    /// 新增子節點至指定父節點下，回傳新的根節點（不可變更新）。
    /// </summary>
    /// <param name="root">目前 AST 根節點。</param>
    /// <param name="parentId">父節點 Id。</param>
    /// <param name="child">要新增的子節點。</param>
    /// <param name="index">插入索引位置（若為 null 則加至末尾）。</param>
    /// <returns>更新後的新根節點。</returns>
    /// <exception cref="ArgumentNullException">當參數為 null 時擲出。</exception>
    /// <exception cref="InvalidOperationException">當父節點不存在時擲出。</exception>
    public static AstNode AddChild(AstNode root, string parentId, AstNode child, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(parentId);
        ArgumentNullException.ThrowIfNull(child);

        if (root.Id == parentId)
        {
            var newChildren = index.HasValue && index.Value >= 0 && index.Value <= root.Children.Count
                ? root.Children.Insert(index.Value, child)
                : root.Children.Add(child);

            return root with { Children = newChildren };
        }

        var updatedChildren = root.Children
            .Select(c => AddChildInternal(c, parentId, child, index, out var modified))
            .ToImmutableList();

        return root with { Children = updatedChildren };

        static AstNode AddChildInternal(AstNode current, string targetParentId, AstNode newChild, int? targetIndex, out bool wasModified)
        {
            if (current.Id == targetParentId)
            {
                wasModified = true;
                var list = targetIndex.HasValue && targetIndex.Value >= 0 && targetIndex.Value <= current.Children.Count
                    ? current.Children.Insert(targetIndex.Value, newChild)
                    : current.Children.Add(newChild);
                return current with { Children = list };
            }

            var childrenList = new List<AstNode>(current.Children.Count);
            wasModified = false;

            foreach (var c in current.Children)
            {
                var newC = AddChildInternal(c, targetParentId, newChild, targetIndex, out var childMod);
                if (childMod)
                {
                    wasModified = true;
                }
                childrenList.Add(newC);
            }

            return wasModified ? current with { Children = childrenList.ToImmutableList() } : current;
        }
    }

    /// <summary>
    /// 移除指定子節點，回傳新的根節點（不可變更新）。
    /// </summary>
    /// <param name="root">目前 AST 根節點。</param>
    /// <param name="childId">要移除的節點 Id。</param>
    /// <returns>更新後的新根節點。</returns>
    /// <exception cref="ArgumentNullException">當參數為 null 時擲出。</exception>
    public static AstNode RemoveChild(AstNode root, string childId)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(childId);

        if (root.Id == childId)
        {
            throw new InvalidOperationException("無法從根節點移除根節點自身。");
        }

        return RemoveInternal(root, childId);

        static AstNode RemoveInternal(AstNode current, string targetId)
        {
            if (current.Children.Any(c => c.Id == targetId))
            {
                return current with
                {
                    Children = current.Children.Where(c => c.Id != targetId).ToImmutableList()
                };
            }

            var updatedChildren = current.Children
                .Select(c => RemoveInternal(c, targetId))
                .ToImmutableList();

            return current with { Children = updatedChildren };
        }
    }

    /// <summary>
    /// 更新指定節點的內容，回傳新的根節點。
    /// </summary>
    /// <param name="root">目前 AST 根節點。</param>
    /// <param name="updatedNode">更新後的節點模型。</param>
    /// <returns>更新後的新根節點。</returns>
    /// <exception cref="ArgumentNullException">當參數為 null 時擲出。</exception>
    public static AstNode UpdateNode(AstNode root, AstNode updatedNode)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(updatedNode);

        if (root.Id == updatedNode.Id)
        {
            return updatedNode;
        }

        var newChildren = root.Children
            .Select(child => UpdateNode(child, updatedNode))
            .ToImmutableList();

        return root with { Children = newChildren };
    }

    /// <summary>
    /// 將節點移動至新的父節點與索引位置。
    /// </summary>
    /// <param name="root">目前 AST 根節點。</param>
    /// <param name="nodeId">要移動的節點 Id。</param>
    /// <param name="newParentId">目標父節點 Id。</param>
    /// <param name="newIndex">目標索引位置（可為 null）。</param>
    /// <returns>更新後的新根節點。</returns>
    /// <exception cref="ArgumentNullException">當參數為 null 時擲出。</exception>
    /// <exception cref="InvalidOperationException">當目標節點為自身或其子孫時擲出（防止循環結構）。</exception>
    public static AstNode MoveChild(AstNode root, string nodeId, string newParentId, int? newIndex = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(nodeId);
        ArgumentNullException.ThrowIfNull(newParentId);

        if (nodeId == root.Id)
        {
            throw new InvalidOperationException("無法移動根節點。");
        }

        if (nodeId == newParentId)
        {
            throw new InvalidOperationException("無法將節點移動至自己底下。");
        }

        var nodeToMove = FindNodeById(root, nodeId)
            ?? throw new InvalidOperationException($"找不到要移動的節點: {nodeId}");

        // 檢查目標父節點是否在要移動節點的子樹中
        if (FindNodeById(nodeToMove, newParentId) is not null)
        {
            throw new InvalidOperationException("無法將節點移動至自己的子孫節點中（循環結構錯誤）。");
        }

        // 先從舊位置移除
        var removedTree = RemoveChild(root, nodeId);

        // 再加入至新位置
        return AddChild(removedTree, newParentId, nodeToMove, newIndex);
    }
}
