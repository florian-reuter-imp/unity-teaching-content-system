using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeachingContentSystem.Runtime.UI
{
    /// <summary>
    /// Macht eine Weltflaeche (Canvas im Modus World Space) in einer Brille
    /// lesbar: Mindestschriftgroesse aus Leseabstand und Canvas-Massstab,
    /// genug Rasterpunkte fuer uGUI-Text und eine etwas kraeftigere
    /// TextMeshPro-Schrift, die weniger flimmert.
    ///
    /// DIE REGEL
    /// ---------
    /// Lesbarkeit haengt am Sehwinkel, nicht an der Schriftgroesse im
    /// Inspector. Als Faustregel fuer heutige Brillen (rund 20 bis 25
    /// Bildpunkte je Grad):
    ///
    /// - Versalhoehe mindestens **7 mm je Meter Abstand** (7 mrad, etwa
    ///   0,4 Grad). Darunter wird ein Buchstabe von weniger als zehn
    ///   Bildpunkten gezeichnet und flimmert.
    /// - Fliesstext bequemer bei **10 mm je Meter** (10 mrad, etwa 0,6 Grad).
    ///
    /// Bei TextMeshPro ist die Versalhoehe etwa 0,7 der Schriftgroesse. Eine
    /// Tafel mit Massstab 0,001 (eine Canvas-Einheit ist ein Millimeter) in
    /// 1 m Abstand braucht also mindestens Schriftgroesse 10, fuer
    /// Fliesstext eher 14; in 2 m das Doppelte.
    ///
    /// Die Komponente hebt nur an, sie verkleinert nie. Sie aendert Werte
    /// beim Aktivieren; im Editor ueber das Kontextmenue "Apply now".
    /// Bildschirm-Canvases (Screen Space) bleiben unberuehrt: Die gibt es in
    /// einer Brille nicht.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class ReadableWorldPanel : MonoBehaviour
    {
        /// <summary>Mindestversalhoehe je Meter Abstand, in Metern (7 mrad).</summary>
        public const float MinimumCapHeightPerMeter = 0.007f;

        /// <summary>Empfohlene Versalhoehe fuer Fliesstext je Meter Abstand (10 mrad).</summary>
        public const float BodyCapHeightPerMeter = 0.010f;

        /// <summary>Versalhoehe im Verhaeltnis zur Schriftgroesse, typisch fuer serifenlose Schriften.</summary>
        public const float CapHeightRatio = 0.7f;

        [Tooltip("Typical distance from the eyes to this panel, in metres.")]
        [Min(0.1f)]
        public float viewingDistance = 1.0f;

        [Tooltip("Minimum cap height per metre of viewing distance, in metres. 0.007 = 7 mrad (absolute minimum), 0.010 = comfortable body text.")]
        [Range(0.004f, 0.02f)]
        public float capHeightPerMeter = MinimumCapHeightPerMeter;

        [Tooltip("CanvasScaler.dynamicPixelsPerUnit. Affects legacy uGUI Text and dynamic font rasterisation; TextMeshPro is resolution independent.")]
        [Min(1f)]
        public float dynamicPixelsPerUnit = 3f;

        [Tooltip("TextMeshPro face dilate. Slightly bolder glyphs shimmer less in a headset. 0 leaves materials untouched.")]
        [Range(0f, 0.4f)]
        public float faceDilate = 0.1f;

        [Tooltip("Apply when the component is enabled. Texts created later need another Apply().")]
        public bool applyOnEnable = true;

        private static readonly int FaceDilateId = Shader.PropertyToID("_FaceDilate");
        private static readonly Dictionary<string, Material> dilated = new Dictionary<string, Material>();
        private static readonly HashSet<Material> ownMaterials = new HashSet<Material>();

        void OnEnable()
        {
            if (applyOnEnable) Apply();
        }

        /// <summary>
        /// Kleinste Schriftgroesse in Canvas-Einheiten, bei der die
        /// Versalhoehe im gegebenen Abstand die Regel erfuellt.
        /// </summary>
        /// <param name="worldUnitsPerCanvasUnit">Massstab der Canvas, etwa 0.001 wenn eine Einheit ein Millimeter ist.</param>
        /// <param name="viewingDistance">Leseabstand in Metern.</param>
        /// <param name="capHeightPerMeter">Versalhoehe je Meter Abstand, in Metern.</param>
        public static float MinimumFontSize(float worldUnitsPerCanvasUnit, float viewingDistance,
                                            float capHeightPerMeter = MinimumCapHeightPerMeter)
        {
            if (worldUnitsPerCanvasUnit <= 0f) return 0f;
            float capHeightMeters = capHeightPerMeter * viewingDistance;
            return capHeightMeters / CapHeightRatio / worldUnitsPerCanvasUnit;
        }

        /// <summary>Versalhoehe in Metern fuer eine Schriftgroesse auf einer Canvas.</summary>
        public static float CapHeight(float fontSize, float worldUnitsPerCanvasUnit)
            => fontSize * CapHeightRatio * worldUnitsPerCanvasUnit;

        /// <summary>
        /// Wendet die Regeln auf alle Texte unter dieser Canvas an. Gibt die
        /// Zahl der Texte zurueck, deren Groesse angehoben wurde.
        /// </summary>
        [ContextMenu("Apply now")]
        public int Apply()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogWarning($"[TCS] ReadableWorldPanel on '{name}': canvas is not in World Space, nothing changed. "
                                 + "Screen-space canvases are not visible in a headset.", this);
                return 0;
            }

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null && scaler.dynamicPixelsPerUnit < dynamicPixelsPerUnit)
                scaler.dynamicPixelsPerUnit = dynamicPixelsPerUnit;

            float scale = Mathf.Abs(transform.lossyScale.y);
            float minimum = MinimumFontSize(scale, viewingDistance, capHeightPerMeter);
            int raised = 0;

            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.enableAutoSizing)
                {
                    if (text.fontSizeMin < minimum) { text.fontSizeMin = minimum; raised++; }
                    if (text.fontSizeMax < text.fontSizeMin) text.fontSizeMax = text.fontSizeMin;
                }
                else if (text.fontSize < minimum)
                {
                    text.fontSize = minimum;
                    raised++;
                }

                // Nur im Spiel: Ein im Editor erzeugtes Material waere kein
                // Asset und ginge beim Speichern der Szene verloren.
                if (faceDilate > 0f && Application.isPlaying) Dilate(text, faceDilate);
            }

            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                int size = Mathf.CeilToInt(minimum);
                if (text.fontSize < size) { text.fontSize = size; raised++; }
                if (text.resizeTextForBestFit && text.resizeTextMinSize < size) text.resizeTextMinSize = size;
            }

            return raised;
        }

        /// <summary>
        /// Gibt dem Text ein Material mit etwas Dilate. Je Ausgangsmaterial
        /// und Wert gibt es ein geteiltes Material, damit Texte mit derselben
        /// Schrift weiter zusammen gezeichnet werden.
        /// </summary>
        private static void Dilate(TMP_Text text, float amount)
        {
            Material source = text.fontSharedMaterial;
            if (source == null || ownMaterials.Contains(source) || !source.HasProperty(FaceDilateId)) return;
            if (source.GetFloat(FaceDilateId) >= amount) return;

            string key = source.GetInstanceID() + ":" + amount.ToString("F3");
            if (!dilated.TryGetValue(key, out Material material) || material == null)
            {
                material = new Material(source) { name = source.name + " (Readable)" };
                material.SetFloat(FaceDilateId, amount);
                dilated[key] = material;
                ownMaterials.Add(material);
            }
            text.fontSharedMaterial = material;
        }
    }
}
