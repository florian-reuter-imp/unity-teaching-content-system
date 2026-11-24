using System;
using System.Collections.Generic;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Sucht eine Kursdatei in einer geordneten Liste von Quellen.
    ///
    /// DIE REGEL
    /// ---------
    /// Je Datei, nicht je Kurs: Die erste Quelle, die die Datei hat, gewinnt.
    /// Ein Ordner auf dem Geraet kann so einzelne Kapitel ersetzen, und alles
    /// andere kommt weiter aus StreamingAssets.
    ///
    /// Innerhalb einer Quelle gilt dasselbe wie bei
    /// <see cref="TcsLanguage.Localize"/>: Gibt es die Fassung in der
    /// gewaehlten Sprache (<c>kapitel.de.json</c>), wird sie genommen, sonst
    /// die Grundform (<c>kapitel.json</c>). Erst wenn eine Quelle keines von
    /// beiden hat, kommt die naechste dran.
    ///
    /// Folge: Wer ein Kapitel auf dem Geraet ersetzt, ersetzt es fuer alle
    /// Sprachen. Liegt dort nur die englische Grundform, sieht auch ein
    /// deutscher Nutzer sie, und nicht mehr die alte deutsche Fassung aus
    /// StreamingAssets. Das ist gewollt: Eine Ueberschreibung ist neuer als
    /// das, was sie ueberschreibt, und eine veraltete Uebersetzung waere
    /// still falsch. Wer beide Sprachen ersetzen will, legt beide Dateien ab.
    /// </summary>
    public class ContentResolver
    {
        private readonly List<ICourseSource> sources;

        public ContentResolver(IEnumerable<ICourseSource> sources)
        {
            this.sources = new List<ICourseSource>();
            if (sources == null) return;
            foreach (ICourseSource source in sources)
                if (source != null) this.sources.Add(source);
        }

        public ContentResolver(params ICourseSource[] sources)
            : this((IEnumerable<ICourseSource>)sources)
        {
        }

        /// <summary>Die Quellen in der Reihenfolge, in der gesucht wird.</summary>
        public IReadOnlyList<ICourseSource> Sources => sources;

        /// <summary>
        /// Liest eine Datei in der aktuellen Sprache (<see cref="TcsLanguage.Current"/>).
        /// </summary>
        public bool TryRead(string relativePath, out string text)
            => TryRead(relativePath, TcsLanguage.Current, out text, out _, out _);

        /// <summary>
        /// Liest eine Datei in einer Sprache. <paramref name="source"/> nennt
        /// die Quelle, aus der sie kam, <paramref name="resolvedPath"/> den
        /// relativen Pfad der tatsaechlich gelesenen Fassung.
        /// </summary>
        public bool TryRead(string relativePath, string language, out string text,
                            out ICourseSource source, out string resolvedPath)
        {
            text = null;
            source = null;
            resolvedPath = null;
            if (string.IsNullOrEmpty(relativePath)) return false;

            string basis = Normalize(relativePath);
            string variant = string.IsNullOrEmpty(language) || language == TcsLanguage.Fallback
                ? null
                : Variant(basis, language);

            foreach (ICourseSource candidate in sources)
            {
                if (variant != null && candidate.TryReadText(variant, out text))
                {
                    source = candidate;
                    resolvedPath = variant;
                    return true;
                }

                if (candidate.TryReadText(basis, out text))
                {
                    source = candidate;
                    resolvedPath = basis;
                    return true;
                }
            }

            text = null;
            return false;
        }

        /// <summary>
        /// Wie <see cref="TryRead(string, out string)"/>, wirft aber eine
        /// lesbare Ausnahme, wenn keine Quelle die Datei hat.
        /// </summary>
        public string Read(string relativePath)
        {
            if (TryRead(relativePath, TcsLanguage.Current, out string text, out _, out _))
                return text;
            throw new System.IO.FileNotFoundException(
                $"[TCS] '{relativePath}' was not found in any content location ({Describe()}).", relativePath);
        }

        /// <summary>Die Quellen als Text fuer Meldungen.</summary>
        public string Describe()
        {
            var names = new List<string>();
            foreach (ICourseSource s in sources) names.Add(s.Name);
            return names.Count == 0 ? "none" : string.Join(", ", names);
        }

        // --- Pfade ------------------------------------------------------------

        /// <summary>
        /// Relativer Pfad mit Schraegstrichen, ohne fuehrenden Schraegstrich
        /// und ohne <c>./</c>. Rueckstriche werden umgesetzt, damit unter
        /// Windows gebaute Pfade als Schluessel und in Adressen taugen.
        /// </summary>
        public static string Normalize(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return "";
            string p = relativePath.Replace('\\', '/');
            while (p.StartsWith("./", StringComparison.Ordinal)) p = p.Substring(2);
            return p.TrimStart('/');
        }

        /// <summary>
        /// <c>a/b/kapitel.json</c> wird <c>a/b/kapitel.de.json</c>, mit
        /// Schraegstrichen auf jeder Plattform (anders als
        /// <see cref="TcsLanguage.Variant"/>, das Path.Combine benutzt).
        /// </summary>
        public static string Variant(string relativePath, string language)
        {
            string p = Normalize(relativePath);
            int slash = p.LastIndexOf('/');
            string directory = slash >= 0 ? p.Substring(0, slash + 1) : "";
            string file = slash >= 0 ? p.Substring(slash + 1) : p;
            int dot = file.LastIndexOf('.');
            string name = dot > 0 ? file.Substring(0, dot) : file;
            string extension = dot > 0 ? file.Substring(dot) : "";
            return $"{directory}{name}.{language}{extension}";
        }

        /// <summary>
        /// Der Ordner eines relativen Pfades mit abschliessendem
        /// Schraegstrich, oder leer.
        /// </summary>
        public static string DirectoryOf(string relativePath)
        {
            string p = Normalize(relativePath);
            int slash = p.LastIndexOf('/');
            return slash >= 0 ? p.Substring(0, slash + 1) : "";
        }

        /// <summary>
        /// Setzt einen Pfad relativ zu einem Ordner zusammen und loest
        /// <c>..</c> auf, damit Kapitel auch neben dem Kursordner liegen
        /// duerfen.
        /// </summary>
        public static string Combine(string directory, string relativePath)
        {
            string joined = Normalize(directory);
            if (joined.Length > 0 && !joined.EndsWith("/", StringComparison.Ordinal)) joined += "/";
            joined += Normalize(relativePath);

            var parts = new List<string>();
            foreach (string part in joined.Split('/'))
            {
                if (part.Length == 0 || part == ".") continue;
                if (part == ".." && parts.Count > 0 && parts[parts.Count - 1] != "..")
                    parts.RemoveAt(parts.Count - 1);
                else
                    parts.Add(part);
            }
            return string.Join("/", parts);
        }
    }
}
