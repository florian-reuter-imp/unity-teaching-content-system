using UnityEngine;
using UnityEngine.UI;

namespace TeachingContentSystem.Runtime.UI
{
    public class DebugInfoUI : MonoBehaviour
    {
        public Text debugText;

        public void Set(string msg)
        {
            debugText.text = msg;
        }
    }
}
