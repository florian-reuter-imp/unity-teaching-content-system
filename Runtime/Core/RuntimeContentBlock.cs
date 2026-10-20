using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Runtime representation of a content block loaded from JSON.
    /// Contains only data, not behavior.
    /// </summary>
    public class RuntimeContentBlock
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public string Type;

        /// <summary>
        /// Arbitrary data dictionary.
        /// Each block type interprets this differently.
        /// </summary>
        [JsonProperty("data")]
        public Dictionary<string, object> Data = new();
    }
}
