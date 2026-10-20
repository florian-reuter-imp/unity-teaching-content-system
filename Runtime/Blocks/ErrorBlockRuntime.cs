using UnityEngine;
using TeachingContentSystem.Runtime.Core;

namespace TeachingContentSystem.Runtime.Blocks
{
    /// <summary>
    /// Fallback block used when JSON references missing or invalid content.
    /// </summary>
    public class ErrorBlockRuntime : IContentBlock
    {
        private readonly RuntimeContentBlock _data;

        public string Id => _data.Id;
        public float Duration => 0f;

        public ErrorBlockRuntime(RuntimeContentBlock data)
        {
            _data = data;
        }

        public void Enter()
        {
            Debug.LogError($"[ErrorBlock] Missing or invalid block: {_data.Id}");
        }

        public void Update(float deltaTime) { }

        public void Exit() { }
    }
}
