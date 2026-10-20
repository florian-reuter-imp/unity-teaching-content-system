using TeachingContentSystem.Runtime.Core;

namespace TeachingContentSystem.Runtime.Serialization
{
    public static class ErrorFallbackFactory
    {
        public static RuntimeContentGraph CreateErrorGraph(string id)
        {
            return new RuntimeContentGraph
            {
                GraphId = id,
                StartNodeId = "error_node",
                Nodes = new()
                {
                    CreateErrorNode("error_node")
                },
                Blocks = new()
                {
                    CreateErrorBlock("error_block")
                }
            };
        }

        public static RuntimeContentNode CreateErrorNode(string id)
        {
            return new RuntimeContentNode
            {
                Id = id,
                BlockId = "error_block",
                NextIds = new() // no next
            };
        }

        public static RuntimeContentBlock CreateErrorBlock(string id)
        {
            return new RuntimeContentBlock
            {
                Id = id,
                Type = "Error",
                Data = new()
            };
        }
    }
}
