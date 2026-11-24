using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    public static class CourseLoader
    {
        /// <summary>
        /// Laedt einen Kurs ueber die Quellen aus
        /// <see cref="TcsContentLocations"/>; ohne Einstellung ist das
        /// StreamingAssets allein, wie bisher. Der Pfad ist relativ zu den
        /// Quellen, etwa <c>TCS/ExampleCourse/course.json</c>.
        ///
        /// Ein absoluter Pfad wird wie frueher direkt aus dem Dateisystem
        /// gelesen, mit Kapiteln relativ zu seinem Ordner.
        /// </summary>
        public static RuntimeContentGraph LoadCourse(string courseJsonPath)
        {
            if (!string.IsNullOrEmpty(courseJsonPath) && Path.IsPathRooted(courseJsonPath))
            {
                var folder = new FolderSource(Path.GetDirectoryName(courseJsonPath));
                return LoadCourse(Path.GetFileName(courseJsonPath), new ContentResolver(folder));
            }

            return LoadCourse(courseJsonPath, TcsContentLocations.Resolver);
        }

        /// <summary>
        /// Laedt einen Kurs ueber eine bestimmte Liste von Quellen. Jede
        /// Datei (Gliederung und jedes Kapitel) wird einzeln gesucht: Die
        /// erste Quelle, die sie hat, gewinnt, in der aktuellen Sprache wenn
        /// vorhanden.
        /// </summary>
        public static RuntimeContentGraph LoadCourse(string courseJsonPath, ContentResolver resolver)
        {
            string coursePath = ContentResolver.Normalize(courseJsonPath);

            // Auch die Gliederung darf uebersetzt vorliegen, etwa wenn ein
            // Kurs in einer Sprache zusaetzliche Kapitel hat.
            string json = resolver.Read(coursePath);

            CourseDefinition course = JsonConvert.DeserializeObject<CourseDefinition>(json);
            string courseDirectory = ContentResolver.DirectoryOf(coursePath);

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
                // Relativ zum Ordner der Gliederung, und je Datei aufgeloest:
                // Ein Kapitel kann aus einer anderen Quelle kommen als die
                // Gliederung.
                string chapterPath = ContentResolver.Combine(courseDirectory, chapterFile);
                ChapterContent chapter = ChapterLoader.LoadChapter(chapterPath, resolver);
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
