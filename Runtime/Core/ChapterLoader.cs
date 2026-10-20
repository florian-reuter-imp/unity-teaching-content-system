using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    public static class ChapterLoader
    {
        public static ChapterContent LoadChapter(string chapterPath)
        {
            // Gibt es das Kapitel in der gewaehlten Sprache, wird es
            // genommen; sonst bleibt es bei der Grundform (TcsLanguage).
            chapterPath = TcsLanguage.Localize(chapterPath);

            string json = File.ReadAllText(chapterPath);
            ChapterDefinition chapterDef = JsonConvert.DeserializeObject<ChapterDefinition>(json);

            ChapterContent chapter = new ChapterContent
            {
                ChapterName = chapterDef.Chapter,
                Blocks = new List<RuntimeContentBlock>(),
                Nodes = new List<RuntimeContentNode>()
            };

            RuntimeContentNode previousNode = null;

            foreach (var blockDef in chapterDef.Blocks)
            {
                // Normalize text arrays
                if (blockDef.Data.TryGetValue("text", out var rawText))
                {
                    if (rawText is JArray arr)
                        blockDef.Data["text"] = string.Join("\n", arr.ToObject<List<string>>());
                }

                // Create block with LOCAL ID (global prefixing happens in CourseLoader)
                var block = new RuntimeContentBlock
                {
                    Id = blockDef.Id,
                    Type = blockDef.Type,
                    Data = blockDef.Data
                };
                chapter.Blocks.Add(block);

                // Create node with LOCAL ID
                var node = new RuntimeContentNode
                {
                    Id = blockDef.Id + "_node",
                    BlockId = block.Id,
                    PreviousId = previousNode?.Id
                };
                chapter.Nodes.Add(node);

                if (previousNode != null)
                    previousNode.NextIds.Add(node.Id);

                previousNode = node;

                if (chapter.FirstNode == null)
                    chapter.FirstNode = node;

                chapter.LastNode = node;
            }

            return chapter;
        }
    }

    public class ChapterContent
    {
        public string ChapterName;
        public List<RuntimeContentBlock> Blocks;
        public List<RuntimeContentNode> Nodes;

        public RuntimeContentNode FirstNode;
        public RuntimeContentNode LastNode;
    }
}
