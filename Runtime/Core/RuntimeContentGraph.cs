using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Runtime representation of a full content graph.
    /// Built by CourseLoader and consumed by ContentManager + NavigationService.
    /// </summary>
    public class RuntimeContentGraph
    {
        /// <summary>
        /// Unique identifier for the graph (usually the course name).
        /// </summary>
        [JsonProperty("graphId")]
        public string GraphId;

        /// <summary>
        /// All blocks used in this graph (global IDs).
        /// </summary>
        [JsonProperty("blocks")]
        public List<RuntimeContentBlock> Blocks = new();

        /// <summary>
        /// All nodes in this graph (global IDs).
        /// </summary>
        [JsonProperty("nodes")]
        public List<RuntimeContentNode> Nodes = new();

        /// <summary>
        /// ID of the starting node (global).
        /// </summary>
        [JsonProperty("startNodeId")]
        public string StartNodeId;

        public List<string> ChapterOrder = new();


        // -----------------------------
        // NEW STRUCTURES FOR NAVIGATION
        // -----------------------------

        /// <summary>
        /// Fast lookup: globalNodeId -> node.
        /// </summary>
        public Dictionary<string, RuntimeContentNode> NodesById = new();

        /// <summary>
        /// Fast lookup: globalBlockId -> block.
        /// </summary>
        public Dictionary<string, RuntimeContentBlock> BlocksById = new();

        /// <summary>
        /// ChapterName-> first node of that chapter.
        /// </summary>
        public Dictionary<string, RuntimeContentNode> FirstNodeOfChapter = new();

        /// <summary>
        /// ChapterName -> last node of that chapter.
        /// </summary>
        public Dictionary<string, RuntimeContentNode> LastNodeOfChapter = new();

        /// <summary>
        /// ChapterName -> ordered list of nodes in that chapter.
        /// </summary>
        public Dictionary<string, List<RuntimeContentNode>> NodesByChapter = new();
    }
}
