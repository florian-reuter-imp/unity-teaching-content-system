using System.Collections.Generic;

namespace TeachingContentSystem.Runtime.Core.Navigation
{
    /// <summary>
    /// Holds the current navigation state for the ContentManager.
    /// </summary>
    public class NavigationState
    {
        public string CurrentNodeId;
        public float TimeInNode;

        /// <summary>
        /// Optional stack for back navigation.
        /// </summary>
        public Stack<string> History = new();
    }
}
