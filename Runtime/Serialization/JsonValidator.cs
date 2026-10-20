using UnityEngine;
using System.Collections.Generic;
using TeachingContentSystem.Runtime.Core;

namespace TeachingContentSystem.Runtime.Serialization
{
    /// <summary>
    /// Validates a RuntimeContentGraph and ensures all references are valid.
    /// Missing nodes or blocks are replaced with error fallbacks.
    /// </summary>
    public static class JsonValidator
    {
        public static void Validate(RuntimeContentGraph graph)
        {
            var nodeMap = new Dictionary<string, RuntimeContentNode>();
            var blockMap = new Dictionary<string, RuntimeContentBlock>();

            foreach (var node in graph.Nodes)
                nodeMap[node.Id] = node;

            foreach (var block in graph.Blocks)
                blockMap[block.Id] = block;

            // Validate node references
            foreach (var node in graph.Nodes)
            {
                // Validate block reference
                if (!blockMap.ContainsKey(node.BlockId))
                {
                    Debug.LogError($"JsonValidator: Missing block {node.BlockId} for node {node.Id}");
                    var fallback = ErrorFallbackFactory.CreateErrorBlock(node.BlockId);
                    graph.Blocks.Add(fallback);
                    blockMap[node.BlockId] = fallback;
                }

                // Validate next nodes
                for (int i = 0; i < node.NextIds.Count; i++)
                {
                    string nextId = node.NextIds[i];
                    if (!nodeMap.ContainsKey(nextId))
                    {
                        Debug.LogError($"JsonValidator: Missing next node {nextId} referenced by {node.Id}");
                        var fallback = ErrorFallbackFactory.CreateErrorNode(nextId);
                        graph.Nodes.Add(fallback);
                        nodeMap[nextId] = fallback;
                    }
                }

                // Validate home node
                if (!string.IsNullOrEmpty(node.HomeId) && !nodeMap.ContainsKey(node.HomeId))
                {
                    Debug.LogError($"JsonValidator: Missing home node {node.HomeId} for node {node.Id}");
                    var fallback = ErrorFallbackFactory.CreateErrorNode(node.HomeId);
                    graph.Nodes.Add(fallback);
                    nodeMap[node.HomeId] = fallback;
                }

                // Validate previous node
                if (!string.IsNullOrEmpty(node.PreviousId) && !nodeMap.ContainsKey(node.PreviousId))
                {
                    Debug.LogWarning($"JsonValidator: Missing previous node {node.PreviousId} for node {node.Id}");
                }
            }
        }
    }
}
