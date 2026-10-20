using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Runtime representation of a content node.
    /// Defines structure, navigation, metadata, and tags.
    /// </summary>
    public class RuntimeContentNode
    {
        [JsonProperty("id")]
        public string Id;

        /// <summary>
        /// The block this node references.
        /// </summary>
        [JsonProperty("blockId")]
        public string BlockId;

        /// <summary>
        /// List of next node IDs. Must contain at least one entry.
        /// </summary>
        [JsonProperty("next")]
        public List<string> NextIds = new();

        /// <summary>
        /// ID of the home node (chapter start or global home).
        /// </summary>
        [JsonProperty("home")]
        public string HomeId;

        /// <summary>
        /// Optional previous node ID.
        /// </summary>
        [JsonProperty("previous")]
        public string PreviousId;

        /// <summary>
        /// Arbitrary metadata for sorting, conditions, etc.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata = new();

        /// <summary>
        /// Simple tag list for filtering and analytics.
        /// </summary>
        [JsonProperty("tags")]
        public List<string> Tags = new();
    }
}
