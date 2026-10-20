using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Sprache der Kursinhalte.
    ///
    /// Übersetzt wird je Datei, nicht je Textfeld: Neben
    /// <c>kinematics.json</c> liegt <c>kinematics.de.json</c>. Gibt es die
    /// Datei in der gewählten Sprache, wird sie geladen, sonst die Grundform.
    /// Das hat drei Vorteile: Bestehende Kurse bleiben unverändert gültig,
    /// Übersetzer arbeiten an ganzen Dateien statt an verstreuten Schnipseln,
    /// und eine fehlende Übersetzung macht nichts kaputt — sie fällt nur auf
    /// die Grundsprache zurück.
    ///
    /// Die Wahl merkt sich das System je Gerät.
    /// </summary>
    public static class TcsLanguage
    {
        /// <summary>Sprache der Grundform, also der Dateien ohne Kürzel.</summary>
        public const string Fallback = "en";

        private const string PrefKey = "TCS.Language";

        private static readonly List<string> available = new List<string> { "en", "de" };

        private static string current;

        /// <summary>Was zur Auswahl steht. Reihenfolge wie in der Oberfläche.</summary>
        public static IReadOnlyList<string> Available => available;

        /// <summary>Wird nach jedem Wechsel ausgelöst.</summary>
        public static event Action Changed;

        /// <summary>
        /// Aktuelle Sprache. Beim ersten Zugriff aus der gemerkten Wahl, sonst
        /// aus der Systemsprache, sonst <see cref="Fallback"/>.
        /// </summary>
        public static string Current
        {
            get
            {
                if (string.IsNullOrEmpty(current))
                    current = PlayerPrefs.GetString(PrefKey, FromSystem());
                return current;
            }
        }

        public static void Set(string code)
        {
            if (string.IsNullOrEmpty(code) || code == Current) return;

            current = code;
            PlayerPrefs.SetString(PrefKey, code);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Zur nächsten verfügbaren Sprache wechseln.</summary>
        public static void Next()
        {
            int index = available.IndexOf(Current);
            Set(available[(index + 1) % available.Count]);
        }

        /// <summary>
        /// Eine weitere Sprache anmelden, etwa aus einem Erweiterungspaket.
        /// </summary>
        public static void Register(string code)
        {
            if (!string.IsNullOrEmpty(code) && !available.Contains(code))
                available.Add(code);
        }

        /// <summary>
        /// Der Pfad zur Fassung in der gewählten Sprache, falls es sie gibt —
        /// sonst der übergebene Pfad.
        /// </summary>
        public static string Localize(string path)
        {
            if (string.IsNullOrEmpty(path) || Current == Fallback) return path;

            string translated = Variant(path, Current);
            return File.Exists(translated) ? translated : path;
        }

        /// <summary>
        /// Aus <c>…/kapitel.json</c> wird <c>…/kapitel.de.json</c>. Ohne
        /// Dateisystemzugriff, damit es sich auch prüfen lässt.
        /// </summary>
        public static string Variant(string path, string code)
        {
            string directory = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string file = $"{name}.{code}{extension}";

            return string.IsNullOrEmpty(directory) ? file : Path.Combine(directory, file);
        }

        private static string FromSystem()
            => Application.systemLanguage == SystemLanguage.German ? "de" : Fallback;

        /// <summary>Nur für Tests: zurück auf den Ausgangszustand.</summary>
        public static void ResetForTests(string code = Fallback)
        {
            current = code;
            Changed = null;
        }
    }
}
