using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    public static class CourseLoader
    {
        public static RuntimeContentGraph LoadCourse(string courseJsonPath)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, courseJsonPath);

            // Auch die Gliederung darf uebersetzt vorliegen, etwa wenn ein
            // Kurs in einer Sprache zusaetzliche Kapitel hat.
            fullPath = TcsLanguage.Localize(fullPath);

            string json = File.ReadAllText(fullPath);

            CourseDefinition course = JsonConvert.DeserializeObject<CourseDefinition>(json);

            RuntimeContentGraph graph = new RuntimeContentGraph
            {
                GraphId = course.course,
                Nodes = new List<RuntimeContentNode>(),
                Blocks = new List<RuntimeContentBlock>(),
                NodesById = new Dictionary<string, RuntimeContentNode>(),
                BlocksById = new Dictionary<string, RuntimeContentBlock>(),
                FirstNodeOfChapter = new Dictionary<string, RuntimeContentNode>(),
                LastNodeOfChapter = new Dictionary<string, RuntimeContentNode>(),
                NodesByChapter = new Dictionary<string, List<RuntimeContentNode>>()
            };

            RuntimeContentNode previousNode = null;

            foreach (string chapterFile in course.chapters)
            {
                string chapterPath = Path.Combine(Path.GetDirectoryName(fullPath), chapterFile);

                // Load chapter using the new ChapterLoader
                ChapterContent chapter = ChapterLoader.LoadChapter(chapterPath);
                string chapterName = chapter.ChapterName;
                graph.ChapterOrder.Add(chapterName);


                graph.NodesByChapter[chapterName] = new List<RuntimeContentNode>();

                RuntimeContentNode firstNodeOfChapter = null;
                RuntimeContentNode lastNodeOfChapter = null;

                foreach (var localBlock in chapter.Blocks)
                {
                    // Build global IDs
                    string globalBlockId = $"{course.course}.{chapterName}.{localBlock.Id}";
                    string globalNodeId = $"{globalBlockId}_node";

                    // Create global block
                    RuntimeContentBlock block = new RuntimeContentBlock
                    {
                        Id = globalBlockId,
                        Type = localBlock.Type,
                        Data = localBlock.Data
                    };

                    graph.Blocks.Add(block);
                    graph.BlocksById[globalBlockId] = block;

                    // Create global node
                    RuntimeContentNode node = new RuntimeContentNode
                    {
                        Id = globalNodeId,
                        BlockId = globalBlockId,
                        PreviousId = previousNode?.Id,
                        HomeId = graph.StartNodeId ?? globalNodeId
                    };

                    graph.Nodes.Add(node);
                    graph.NodesById[globalNodeId] = node;
                    graph.NodesByChapter[chapterName].Add(node);

                    // Link previous -> current
                    if (previousNode != null)
                        previousNode.NextIds.Add(node.Id);

                    previousNode = node;

                    // Set global start node
                    if (graph.StartNodeId == null)
                        graph.StartNodeId = node.Id;

                    // Track chapter boundaries
                    if (firstNodeOfChapter == null)
                        firstNodeOfChapter = node;

                    lastNodeOfChapter = node;
                }

                // Store chapter boundaries
                graph.FirstNodeOfChapter[chapterName] = firstNodeOfChapter;
                graph.LastNodeOfChapter[chapterName] = lastNodeOfChapter;
            }

            return graph;
        }
    }
}
