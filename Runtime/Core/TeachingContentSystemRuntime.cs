using TeachingContentSystem.Runtime.Core;
using TeachingContentSystem.Runtime.Serialization;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Entry point for loading and running a JSON-based content graph.
    /// Attach this to a GameObject in your scene.
    /// </summary>
    public class TeachingContentSystemRuntime : MonoBehaviour
    {
        [Tooltip("Pfad zur Gliederung innerhalb von StreamingAssets, mit Schraegstrichen. Beispiel: TCS/ExampleCourse/course.json")]
        public string contentFolderPath = "TCS/ExampleCourse/course.json";

        private ContentManager _contentManager;

        void Awake()
        {
            _contentManager = GetComponent<ContentManager>();
            if (_contentManager == null)
            {
                Debug.LogError("[TeachingContentSystemRuntime] Missing ContentManager component.");
                return;
            }
        }

        void OnEnable() => TcsLanguage.Changed += Reload;

        void OnDisable() => TcsLanguage.Changed -= Reload;

        void Start() => Reload();

        /// <summary>
        /// Laedt den Kurs. Wird auch nach einem Sprachwechsel aufgerufen —
        /// dann kommen dieselben Kapitel in der anderen Fassung.
        /// </summary>
        public void Reload()
        {
            if (string.IsNullOrEmpty(contentFolderPath))
            {
                Debug.LogError("[TeachingContentSystemRuntime] No content folder path assigned.");
                return;
            }

            if (_contentManager == null)
                _contentManager = GetComponent<ContentManager>();
            if (_contentManager == null)
                return;

            var graph = CourseLoader.LoadCourse(contentFolderPath);
            if (graph == null)
            {
                Debug.LogError("[TeachingContentSystemRuntime] Failed to load graph.");
                return;
            }

            _contentManager.LoadGraph(graph);
        }


    }
}
