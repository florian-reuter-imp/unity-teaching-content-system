using System.Collections.Generic;
using UnityEngine;

namespace TeachingContentSystem.Runtime.SceneActions
{
    /// <summary>
    /// Vermittelt zwischen Kursinhalt und Szene.
    ///
    /// DAS PROBLEM
    /// -----------
    /// Ein Kapitel will etwas in der Szene ausloesen — „jetzt vergleiche die
    /// Steifigkeit in zwei Stellungen". Das Lehrinhaltssystem darf das Modul
    /// dafuer nicht kennen, sonst ist nichts mehr einzeln uebernehmbar. Und
    /// das Modul darf das Kapitel nicht kennen, sonst laeuft es nicht ohne.
    ///
    /// Also treffen sich beide bei einem Namen. Das Kapitel schreibt
    /// <c>"action": "stiffness_comparison"</c>, das Modul meldet unter
    /// demselben Namen einen <see cref="SceneActionHandler"/> an. Keiner
    /// kennt den anderen.
    ///
    /// WAS BEI EINEM TIPPFEHLER PASSIERT
    /// ---------------------------------
    /// Nichts Schlimmes, und das ist Absicht: Der Baustein zeigt einen
    /// Hinweis, und das Kapitel bleibt lesbar. Ein Kapitel, dessen Modul
    /// fehlt, soll nicht abbrechen — genau diese Zweistufigkeit macht
    /// Inhalte einzeln uebernehmbar. Gefunden werden Tippfehler vorher, von
    /// der Kurspruefung im Editor.
    /// </summary>
    public static class SceneActionRegistry
    {
        private static readonly Dictionary<string, SceneActionHandler> handler =
            new Dictionary<string, SceneActionHandler>();

        /// <summary>Alle angemeldeten Namen. Fuer Pruefung und Fehlermeldungen.</summary>
        public static IEnumerable<string> Names => handler.Keys;

        public static void Register(string name, SceneActionHandler h)
        {
            if (string.IsNullOrWhiteSpace(name) || h == null) return;

            if (handler.TryGetValue(name, out var vorhanden) && vorhanden != null && vorhanden != h)
            {
                Debug.LogWarning(
                    $"[SceneActionRegistry] Der Name {name} ist schon von {vorhanden.name} "
                    + $"belegt; {h.name} uebernimmt. Zwei Handler mit demselben Namen sind "
                    + "fast immer ein Versehen — meist liegt das Modul zweimal in der Szene.", h);
            }
            handler[name] = h;
        }

        public static void Unregister(string name, SceneActionHandler h)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (handler.TryGetValue(name, out var vorhanden) && vorhanden == h)
                handler.Remove(name);
        }

        /// <summary>Handler zu einem Namen, oder null.</summary>
        public static SceneActionHandler Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            handler.TryGetValue(name, out var h);
            // Ein zerstoertes Objekt hinterlaesst einen Eintrag, der wie ein
            // Handler aussieht, aber keiner mehr ist. Der Vergleich gegen null
            // faengt das ab, weil Unity ihn fuer zerstoerte Objekte wahr macht.
            return h != null ? h : null;
        }

        /// <summary>Lesbare Aufzaehlung der angemeldeten Namen.</summary>
        public static string KnownNames()
        {
            var liste = new List<string>(handler.Keys);
            liste.Sort();
            return liste.Count > 0 ? string.Join(", ", liste) : "nichts";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            // Fuer den Fall, dass „Enter Play Mode Options" die Domaene nicht
            // neu laedt: sonst bleiben Handler der vorigen Sitzung stehen.
            handler.Clear();
        }
    }
}
