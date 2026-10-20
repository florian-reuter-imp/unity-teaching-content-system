using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TeachingContentSystem.Runtime.Core;
using TeachingContentSystem.Runtime.Core.Navigation;
using TeachingContentSystem.Runtime.Blocks;

namespace TeachingContentSystem.Runtime.UI
{
    public class RuntimeContentUI : MonoBehaviour
    {
        public static RuntimeContentUI Instance { get; private set; }

        [Header("UI References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI bodyText;
        public Image image;
        public CanvasGroup canvasGroup;

        [Header("Branding")]
        // Gewoehnliche uGUI-Bilder, keine SVG-Bilder.
        //
        // Hier lag vorher Unity.VectorGraphics.SVGImage, und das Paket
        // com.unity.vectorgraphics hing allein an diesen zwei Feldern -- ein
        // Vorschaupaket, das die Unity-Untergrenze des ganzen Projekts auf
        // 6000.3.0a5 hob. Fuer zwei Logos ist das zu teuer, zumal eine
        // Weiterverwendung ohnehin andere Logos zeigen will.
        //
        // Zum Benutzen: eigenes Logo als Sprite ins Projekt legen und hier
        // zuweisen, oder zur Laufzeit ueber SetBranding setzen. Bleibt ein Feld
        // leer, wird einfach kein Logo gezeigt -- das ist der Normalfall und
        // kein Mangel.
        //
        // Dieses Paket liefert selbst keine Logos mit. Marken gehoeren nicht in
        // eine Apache-lizenzierte Bibliothek: Apache erteilt in Abschnitt 6
        // keine Markenrechte, und ein mitgeliefertes Logo liest trotzdem jeder
        // als mitlizenziert.
        public Image hkaLogo;
        public Image impLogo;

        [Header("Scroll")]
        public ScrollRect scrollView;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            NavigationEvents.OnBlockStarted += HandleBlockStarted;
            NavigationEvents.OnBlockNotice += HandleBlockNotice;
        }

        void OnDisable()
        {
            NavigationEvents.OnBlockStarted -= HandleBlockStarted;
            NavigationEvents.OnBlockNotice -= HandleBlockNotice;
        }

        public void SetBranding(Sprite hka, Sprite imp)
        {
            if (hkaLogo != null) hkaLogo.sprite = hka;
            if (impLogo != null) impLogo.sprite = imp;
        }

        private void HandleBlockStarted(string blockId)
        {
            var manager = FindAnyObjectByType<ContentManager>();
            if (manager == null || manager.Graph == null)
                return;

            RuntimeContentBlock blockData = manager.Graph.Blocks.Find(b => b.Id == blockId);

            if (blockData == null)
            {
                ShowSlide("Error", $"Missing block: {blockId}", null);
                return;
            }

            switch (blockData.Type)
            {
                case "Slide": ShowSlideBlock(blockData); break;
                case "Quiz": ShowQuizBlock(blockData); break;
                // Ein Baustein fuer beide Typnamen, siehe BlockFactory.
                case "Interaction": ShowSceneActionBlock(blockData); break;
                case "SceneAction": ShowSceneActionBlock(blockData); break;

                default:
                    ShowSlide($"Block: {blockData.Type}",
                              "This block type has no UI implementation yet.",
                              null);
                    break;
            }
        }

        private void ShowSlideBlock(RuntimeContentBlock block)
        {
            string text = block.Data.TryGetValue("text", out var t) ? t?.ToString() : "";
            string title = block.Data.TryGetValue("title", out var ti) ? ti?.ToString() : "";
            string imageName = block.Data.TryGetValue("image", out var img) ? img?.ToString() : null;

            Sprite sprite = null;
            if (!string.IsNullOrEmpty(imageName))
                sprite = Resources.Load<Sprite>(imageName);

            ShowSlide(title, text, sprite);
        }

        private void ShowQuizBlock(RuntimeContentBlock block) { }

        /// <summary>
        /// Zeigt den Begleittext einer Szenenhandlung.
        ///
        /// Vorher war das ein leerer Rumpf, also blieb die Anzeige beim
        /// vorigen Baustein stehen und niemand sah, dass etwas passieren
        /// sollte. Titel und Text sind freiwillig; fehlen sie, steht
        /// wenigstens da, dass die Szene jetzt gefragt ist.
        /// </summary>
        private void ShowSceneActionBlock(RuntimeContentBlock block)
        {
            string title = block.Data.TryGetValue("title", out var ti) ? ti?.ToString() : null;
            string text = block.Data.TryGetValue("text", out var t) ? t?.ToString() : null;

            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text))
                text = "Weiter geht es in der Szene.";

            ShowSlide(title ?? "", text ?? "", null);
        }

        /// <summary>
        /// Zeigt, warum ein Baustein nicht tun konnte, was im Kapitel steht.
        ///
        /// Der Fall, um den es geht: Das Kapitel spricht ein Modul an, das in
        /// dieser Szene nicht liegt. Der Kurs laeuft weiter, aber ohne diese
        /// Anzeige stuende die Begruendung nur in einer Konsole, die in der
        /// Brille niemand sieht.
        /// </summary>
        private void HandleBlockNotice(string blockId, string text)
        {
            if (bodyText == null) return;
            string vorher = bodyText.text;
            string absatz = System.Environment.NewLine + System.Environment.NewLine;
            bodyText.text = string.IsNullOrWhiteSpace(vorher)
                ? text
                : vorher + absatz + text;
            ShowCanvas();
        }


        public void ShowSlide(string title, string body, Sprite sprite)
        {
            if (titleText != null)
                titleText.text = title;

            if (bodyText != null)
                bodyText.text = body;

            if (image != null)
            {
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.gameObject.SetActive(true);
                }
                else
                {
                    image.gameObject.SetActive(false);
                }
            }

            if (scrollView != null)
                scrollView.verticalNormalizedPosition = 1f;

            ShowCanvas();
        }

        public void HideCanvas()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void ShowCanvas()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
