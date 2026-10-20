using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    public class ContentDebugUI : MonoBehaviour
    {
        public ContentManager Manager;

        private void OnGUI()
        {
            if (Manager == null || Manager.Graph == null)
                return;

            GUI.Box(new Rect(10, 10, 300, 200), "Content Debug");

            var state = Manager.GetState();
            var node = Manager.GetCurrentNode();

            GUI.Label(new Rect(20, 40, 280, 20), $"Node: {node.Id}");
            GUI.Label(new Rect(20, 60, 280, 20), $"Block: {node.BlockId}");
            GUI.Label(new Rect(20, 80, 280, 20), $"History: {state.History.Count}");

            if (GUI.Button(new Rect(20, 110, 80, 30), "Next"))
                Manager.GoToNext();

            if (GUI.Button(new Rect(110, 110, 80, 30), "Back"))
                Manager.GoBack();

            if (GUI.Button(new Rect(200, 110, 80, 30), "Home"))
                Manager.GoHome();

            if (GUI.Button(new Rect(20, 150, 260, 30), "Jump to First Chapter"))
            {
                string firstChapter = Manager.Graph.ChapterOrder[0];
                Manager.GoToChapter(firstChapter);
            }
        }
    }
}
