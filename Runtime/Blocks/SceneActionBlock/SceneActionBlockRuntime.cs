using System.Collections.Generic;
using UnityEngine;
using TeachingContentSystem.Runtime.Core;
using TeachingContentSystem.Runtime.Core.Navigation;
using TeachingContentSystem.Runtime.SceneActions;

namespace TeachingContentSystem.Runtime.Blocks
{
    /// <summary>
    /// Loest eine Handlung in der Szene aus und wartet, bis sie erledigt ist.
    ///
    /// EIN BAUSTEIN FUER ZWEI TYPEN
    /// ----------------------------
    /// Es gab hier zwei Bausteine, <c>SceneAction</c> und <c>Interaction</c>,
    /// und beide taten dasselbe: nichts. Ein Blick in die Umsetzungen zeigte
    /// auch keinen Unterschied, der einen zweiten Typ gerechtfertigt haette —
    /// beide nannten einen Namen und warteten. Jetzt gibt es einen Baustein
    /// und der alte Typname bleibt als Zweitname gueltig, damit vorhandene
    /// Kapitel weiterlaufen.
    ///
    /// Der Unterschied, den es wirklich gibt, steckt im Weiterblaettern und
    /// ist jetzt ein Feld statt eines Typs:
    ///
    ///   <c>SceneAction</c>   blaettert selbst weiter, wenn die Handlung
    ///                        fertig ist. Eine abgefahrene Bewegung hat
    ///                        nichts mehr zu zeigen.
    ///   <c>Interaction</c>   wartet auf den Nutzer. Wer eine Aufgabe geloest
    ///                        hat, will das Ergebnis noch ansehen.
    ///
    /// Beides laesst sich mit <c>"autoAdvance"</c> im JSON uebersteuern.
    ///
    /// WENN DAS MODUL FEHLT
    /// --------------------
    /// Dann wird nicht abgebrochen, sondern ein Hinweis angezeigt und auf den
    /// Nutzer gewartet. Ein Kapitel ohne sein Modul soll lesbar bleiben —
    /// sonst kann man Inhalte nicht einzeln uebernehmen. Tippfehler findet
    /// vorher die Kurspruefung im Editor.
    /// </summary>
    public class SceneActionBlockRuntime : IContentBlock, ICompletableBlock
    {
        private readonly RuntimeContentBlock _data;
        private readonly string _action;
        private readonly string _parameters;
        private readonly bool _autoAdvance;

        private SceneActionHandler _handler;

        public string Id => _data.Id;

        /// <summary>0: nie nach Zeit weiterblaettern, nur nach Erledigung.</summary>
        public float Duration => 0f;

        public bool AutoAdvance => _autoAdvance;

        /// <summary>
        /// Ohne Handler nie fertig: Der Hinweis soll stehen bleiben, bis
        /// jemand ihn gelesen und weitergeblaettert hat.
        /// </summary>
        public bool IsComplete => _handler != null && _handler.IsComplete;

        /// <param name="autoAdvanceDefault">Vorgabe je Typname, siehe BlockFactory.</param>
        public SceneActionBlockRuntime(RuntimeContentBlock data, bool autoAdvanceDefault = true)
        {
            _data = data;

            // "interactionType" ist das Feld des alten Interaction-Bausteins.
            _action = Text(data.Data, "action") ?? Text(data.Data, "interactionType");
            _parameters = Text(data.Data, "parameters");
            _autoAdvance = Flag(data.Data, "autoAdvance", autoAdvanceDefault);
        }

        public void Enter()
        {
            if (string.IsNullOrWhiteSpace(_action))
            {
                Melden($"Baustein {Id} nennt keine Handlung "
                        + "(Feld action fehlt oder ist leer).");
                return;
            }

            _handler = SceneActionRegistry.Find(_action);
            if (_handler == null)
            {
                Melden($"Fuer {_action} ist in dieser Szene nichts angemeldet. "
                        + $"Vorhanden: {SceneActionRegistry.KnownNames()}.");
                return;
            }

            _handler.Run(_parameters);
        }

        public void Update(float deltaTime)
        {
            // Nichts zu tun: Der ContentManager fragt IsComplete ab. Hier zu
            // pollen hiesse, dieselbe Pruefung zweimal zu haben.
        }

        public void Exit()
        {
            if (_handler == null) return;
            _handler.End();
            _handler = null;
        }

        /// <summary>
        /// Sagt es dem Protokoll und der Oberflaeche. Die Oberflaeche zeigt
        /// den Text auf der Folie — nur so sieht es jemand, der das Kapitel
        /// in der Brille liest und keine Konsole hat.
        /// </summary>
        private void Melden(string text)
        {
            Debug.LogWarning($"[SceneActionBlock] {text}");
            NavigationEvents.OnBlockNotice?.Invoke(Id, text);
        }

        // --- Lesen aus dem lose getippten Datenwoerterbuch -----------------
        // data.Data ist ein Dictionary<string, object> aus dem JSON. Vorher
        // stand hier data.Data["action"], was bei einem fehlenden Feld eine
        // KeyNotFoundException wirft — mitten im Laden eines Kapitels.

        private static string Text(Dictionary<string, object> d, string key)
        {
            if (d == null || !d.TryGetValue(key, out var v) || v == null) return null;
            string s = v.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private static bool Flag(Dictionary<string, object> d, string key, bool vorgabe)
        {
            string s = Text(d, key);
            return s != null && bool.TryParse(s, out bool b) ? b : vorgabe;
        }
    }
}
