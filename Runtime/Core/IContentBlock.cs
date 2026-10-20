using System;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Base interface for all runtime content blocks.
    /// Blocks define behavior only, not structure or navigation.
    /// </summary>
    public interface IContentBlock
    {
        /// <summary>
        /// Unique identifier for this block.
        /// Must match the ID used in JSON and ScriptableObject definitions.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Duration in seconds before auto-advancing.
        /// 0 means: wait for user input or external trigger.
        /// </summary>
        float Duration { get; }

        /// <summary>
        /// Called when this block becomes active.
        /// </summary>
        void Enter();

        /// <summary>
        /// Called every frame while this block is active.
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Called when leaving this block.
        /// </summary>
        void Exit();
    }
}
