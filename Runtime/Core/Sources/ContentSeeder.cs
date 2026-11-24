using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Fuellt einen beschreibbaren Ordner beim ersten Start mit den Dateien
    /// aus StreamingAssets, damit man sie auf dem Geraet bearbeiten kann.
    ///
    /// Es wird nur kopiert, was im Zielordner fehlt. Eine vorhandene Datei
    /// wird nie ueberschrieben, auch nicht von einer neueren Fassung im
    /// Build: Was auf dem Geraet liegt, hat jemand mit Absicht dort
    /// abgelegt oder geaendert.
    ///
    /// StreamingAssets laesst sich auf Android nicht auflisten. Deshalb wird
    /// ein Kurs ueber seine <c>course.json</c> kopiert: die Gliederung, jedes
    /// darin genannte Kapitel und deren Sprachfassungen fuer alle
    /// Sprachen in <see cref="TcsLanguage.Available"/>.
    /// </summary>
    public static class ContentSeeder
    {
        /// <summary>
        /// Kopiert einen Kurs aus StreamingAssets nach
        /// <paramref name="targetRoot"/>, mit demselben relativen Pfad.
        /// Gibt die Zahl der neu angelegten Dateien zurueck.
        /// </summary>
        public static int SeedCourse(string courseJsonPath, string targetRoot)
            => SeedCourse(courseJsonPath, new StreamingAssetsSource(), targetRoot);

        /// <summary>Wie oben, aus einer beliebigen Quelle.</summary>
        public static int SeedCourse(string courseJsonPath, ICourseSource from, string targetRoot)
        {
            string course = ContentResolver.Normalize(courseJsonPath);
            var files = new List<string> { course };

            if (from.TryReadText(course, out string json))
            {
                string directory = ContentResolver.DirectoryOf(course);
                try
                {
                    JToken chapters = JObject.Parse(json)["chapters"];
                    if (chapters is JArray list)
                        foreach (JToken chapter in list)
                        {
                            string name = (string)chapter;
                            if (!string.IsNullOrEmpty(name))
                                files.Add(ContentResolver.Combine(directory, name));
                        }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[TCS] Seeding: '{course}' from {from.Name} is not readable: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[TCS] Seeding: '{course}' not found in {from.Name}.");
                return 0;
            }

            var withVariants = new List<string>();
            foreach (string file in files)
            {
                withVariants.Add(file);
                foreach (string language in TcsLanguage.Available)
                    if (language != TcsLanguage.Fallback)
                        withVariants.Add(ContentResolver.Variant(file, language));
            }

            return SeedFiles(withVariants, from, targetRoot);
        }

        /// <summary>
        /// Kopiert einzelne Dateien, die es in der Quelle gibt und im Ziel
        /// noch nicht. Fehlt eine Datei in der Quelle, wird sie
        /// uebergangen (etwa eine Sprachfassung, die es nicht gibt).
        /// </summary>
        public static int SeedFiles(IEnumerable<string> relativePaths, ICourseSource from, string targetRoot)
        {
            int copied = 0;
            var target = new FolderSource(targetRoot);
            foreach (string relative in relativePaths)
            {
                string path = target.FullPath(relative);
                if (File.Exists(path)) continue;
                if (!from.TryReadText(relative, out string text)) continue;

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(path, text);
                copied++;
            }
            return copied;
        }
    }
}
