// filepath: src/AFG.Core/Models/Logic/LogicServiceAggregator.cs
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AFG.Core.Enums;
using AFG.Core.Models.Ast;

namespace AFG.Core.Models.Logic;

/// <summary>
/// 業務邏輯服務聚合器：
/// 遍歷 AST 節點樹中的所有 LogicFunction 節點，依據 OutputPath (輸出路徑)、ServiceName (服務名稱)、Namespace (命名空間)
/// 與 TargetLanguage (目標語言) 進行智能合併或獨立分離，產出結構完整的 LogicServiceDefinition 清單。
/// </summary>
public static class LogicServiceAggregator
{
    /// <summary>
    /// 從 FormDocument 中聚合所有 LogicFunction 節點以及既有的 doc.LogicServices。
    /// </summary>
    public static ImmutableList<LogicServiceDefinition> AggregateFromDocument(FormDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var docLogicServices = document.LogicServices ?? [];
        var nodes = FlattenNodes(document.RootNode);
        var aggregatedFromNodes = AggregateFromNodes(nodes, document.RootNamespace, document.TargetLanguage);

        // 合併 doc.LogicServices 與從節點聚合的服務（若同名且同語言則進一步合併函數清單）
        var combinedDict = new Dictionary<(string Name, string Ns, TargetLanguage Lang), List<LogicFunctionDefinition>>();

        foreach (var svc in docLogicServices)
        {
            var key = (svc.ServiceName.Trim(), svc.Namespace.Trim(), svc.Language);
            if (!combinedDict.TryGetValue(key, out var list))
            {
                list = [];
                combinedDict[key] = list;
            }
            list.AddRange(svc.Functions);
        }

        foreach (var svc in aggregatedFromNodes)
        {
            var key = (svc.ServiceName.Trim(), svc.Namespace.Trim(), svc.Language);
            if (!combinedDict.TryGetValue(key, out var list))
            {
                list = [];
                combinedDict[key] = list;
            }
            // 避免重複加入相同 Name 的函數
            foreach (var fn in svc.Functions)
            {
                if (!list.Any(existing => existing.Name.Equals(fn.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(fn);
                }
            }
        }

        var result = combinedDict.Select(kvp => new LogicServiceDefinition
        {
            ServiceName = kvp.Key.Name,
            Namespace = kvp.Key.Ns,
            Language = kvp.Key.Lang,
            Functions = kvp.Value.ToImmutableList()
        }).ToImmutableList();

        return result;
    }

    /// <summary>
    /// 從節點集合中聚合所有 LogicFunction 節點。
    /// </summary>
    public static ImmutableList<LogicServiceDefinition> AggregateFromNodes(
        IEnumerable<AstNode> nodes,
        string defaultNamespace = "App.Services",
        TargetLanguage defaultLanguage = TargetLanguage.CSharp)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var logicNodes = nodes.Where(n => n.Type == ControlType.LogicFunction).ToList();
        if (logicNodes.Count == 0)
        {
            return [];
        }

        // 依據 (OutputPath, ServiceName, Namespace, Language) 進行分組
        var groups = logicNodes.GroupBy(n =>
        {
            var svcName = !string.IsNullOrWhiteSpace(n.Name) ? n.Name.Trim() : "LogicService";
            var ns = !string.IsNullOrWhiteSpace(n.TargetNamespace) ? n.TargetNamespace.Trim() : defaultNamespace;
            var lang = n.TargetLanguage ?? n.LogicFunction?.Language ?? defaultLanguage;
            var outPath = !string.IsNullOrWhiteSpace(n.OutputPath) ? n.OutputPath.Trim().Replace('\\', '/').TrimEnd('/') : string.Empty;

            return (OutputPath: outPath, ServiceName: svcName, Namespace: ns, Language: lang);
        });

        var resultList = new List<LogicServiceDefinition>();

        foreach (var grp in groups)
        {
            var functionsList = new List<LogicFunctionDefinition>();

            foreach (var node in grp)
            {
                LogicFunctionDefinition fn;
                if (node.LogicFunction is not null)
                {
                    fn = node.LogicFunction;
                }
                else
                {
                    fn = new LogicFunctionDefinition
                    {
                        Name = !string.IsNullOrWhiteSpace(node.Text) ? node.Text.Trim() : "Execute",
                        ReturnType = "void",
                        IsAsync = false,
                        Language = grp.Key.Language
                    };
                }

                // 避免同一服務內函數名稱衝突（若同名自動編號）
                var finalFnName = fn.Name;
                int counter = 2;
                while (functionsList.Any(f => f.Name.Equals(finalFnName, StringComparison.OrdinalIgnoreCase)))
                {
                    finalFnName = $"{fn.Name}_{counter++}";
                }

                functionsList.Add(fn with { Name = finalFnName });
            }

            resultList.Add(new LogicServiceDefinition
            {
                ServiceName = grp.Key.ServiceName,
                Namespace = grp.Key.Namespace,
                Language = grp.Key.Language,
                Functions = functionsList.ToImmutableList()
            });
        }

        return resultList.ToImmutableList();
    }

    /// <summary>
    /// 包含 OutputPath 資訊之完整聚合清單。
    /// </summary>
    public static ImmutableList<(string OutputPath, LogicServiceDefinition Service)> AggregateWithOutputPath(
        AstNode rootNode,
        string defaultNamespace = "App.Services",
        TargetLanguage defaultLanguage = TargetLanguage.CSharp)
    {
        ArgumentNullException.ThrowIfNull(rootNode);

        var nodes = FlattenNodes(rootNode).Where(n => n.Type == ControlType.LogicFunction).ToList();
        if (nodes.Count == 0)
        {
            return [];
        }

        var groups = nodes.GroupBy(n =>
        {
            var svcName = !string.IsNullOrWhiteSpace(n.Name) ? n.Name.Trim() : "LogicService";
            var ns = !string.IsNullOrWhiteSpace(n.TargetNamespace) ? n.TargetNamespace.Trim() : defaultNamespace;
            var lang = n.TargetLanguage ?? n.LogicFunction?.Language ?? defaultLanguage;
            var outPath = !string.IsNullOrWhiteSpace(n.OutputPath) ? n.OutputPath.Trim().Replace('\\', '/').TrimEnd('/') : string.Empty;

            return (OutputPath: outPath, ServiceName: svcName, Namespace: ns, Language: lang);
        });

        var results = new List<(string OutputPath, LogicServiceDefinition Service)>();

        foreach (var grp in groups)
        {
            var functionsList = new List<LogicFunctionDefinition>();

            foreach (var node in grp)
            {
                var fn = node.LogicFunction ?? new LogicFunctionDefinition
                {
                    Name = !string.IsNullOrWhiteSpace(node.Text) ? node.Text.Trim() : "Execute",
                    ReturnType = "void",
                    IsAsync = false,
                    Language = grp.Key.Language
                };

                var finalFnName = fn.Name;
                int counter = 2;
                while (functionsList.Any(f => f.Name.Equals(finalFnName, StringComparison.OrdinalIgnoreCase)))
                {
                    finalFnName = $"{fn.Name}_{counter++}";
                }

                functionsList.Add(fn with { Name = finalFnName });
            }

            var svc = new LogicServiceDefinition
            {
                ServiceName = grp.Key.ServiceName,
                Namespace = grp.Key.Namespace,
                Language = grp.Key.Language,
                Functions = functionsList.ToImmutableList()
            };

            results.Add((grp.Key.OutputPath, svc));
        }

        return results.ToImmutableList();
    }

    private static List<AstNode> FlattenNodes(AstNode root)
    {
        var list = new List<AstNode>();
        void Traverse(AstNode node)
        {
            list.Add(node);
            foreach (var child in node.Children)
            {
                Traverse(child);
            }
        }
        Traverse(root);
        return list;
    }
}
