using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeachingContentSystem.Runtime.Core
{
    public class ChapterDefinition
    {
        [JsonProperty("chapter")]
        public string Chapter;

        [JsonProperty("blocks")]
        public List<RuntimeContentBlock> Blocks = new();
    }
}
