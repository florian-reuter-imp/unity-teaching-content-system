using System.Collections.Generic;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Wo das System nach Kursdateien sucht, im Code festgelegt.
    ///
    /// Ohne Angabe gilt: nur StreamingAssets, wie bisher. Eine Anwendung
    /// stellt das einmal beim Start um, am besten in einer Methode mit
    /// <c>[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]</c>,
    /// damit es vor dem ersten Laden gilt:
    ///
    /// <code>
    /// TcsContentLocations.Configure(
    ///     FolderSource.PersistentData(),        // Ueberschreibungen auf dem Geraet
    ///     new StreamingAssetsSource());         // die ausgelieferte Fassung
    ///
    /// TcsContentLocations.ConfigureFor(RuntimePlatform.WindowsEditor,
    ///     new FolderSource(@"D:\kurse"),
    ///     new StreamingAssetsSource());
    /// </code>
    ///
    /// Eine Liste je Plattform geht vor die allgemeine Liste. Die Reihenfolge
    /// ist die Suchreihenfolge, je Datei (siehe <see cref="ContentResolver"/>).
    ///
    /// Ein Build kann Dateien nur nach StreamingAssets legen. Die erste
    /// Fassung jedes Kurses gehoert also dorthin; alle anderen Orte sind
    /// Ueberschreibungen auf dem Geraet. Um einen beschreibbaren Ordner beim
    /// ersten Start zu fuellen, gibt es <see cref="ContentSeeder"/>.
    /// </summary>
    public static class TcsContentLocations
    {
        private static List<ICourseSource> general;
        private static readonly Dictionary<RuntimePlatform, List<ICourseSource>> perPlatform
            = new Dictionary<RuntimePlatform, List<ICourseSource>>();

        /// <summary>
        /// Legt die Quellen fuer alle Plattformen fest, die keine eigene
        /// Liste haben. Ohne Argumente: zurueck auf StreamingAssets allein.
        /// </summary>
        public static void Configure(params ICourseSource[] sources)
        {
            general = sources == null || sources.Length == 0
                ? null
                : new List<ICourseSource>(sources);
        }

        /// <summary>
        /// Legt die Quellen fuer eine Plattform fest. Ohne Argumente wird die
        /// eigene Liste der Plattform entfernt, es gilt wieder die allgemeine.
        /// </summary>
        public static void ConfigureFor(RuntimePlatform platform, params ICourseSource[] sources)
        {
            if (sources == null || sources.Length == 0)
                perPlatform.Remove(platform);
            else
                perPlatform[platform] = new List<ICourseSource>(sources);
        }

        /// <summary>Die Quellen fuer die laufende Plattform.</summary>
        public static IReadOnlyList<ICourseSource> Current => For(Application.platform);

        /// <summary>Die Quellen, die auf einer Plattform gelten wuerden.</summary>
        public static IReadOnlyList<ICourseSource> For(RuntimePlatform platform)
        {
            if (perPlatform.TryGetValue(platform, out List<ICourseSource> own)) return own;
            if (general != null) return general;
            return Default;
        }

        /// <summary>Ein Sucher ueber die Quellen der laufenden Plattform.</summary>
        public static ContentResolver Resolver => new ContentResolver(Current);

        /// <summary>Die Vorgabe: StreamingAssets allein.</summary>
        public static IReadOnlyList<ICourseSource> Default { get; } =
            new List<ICourseSource> { new StreamingAssetsSource() };

        /// <summary>Alles zurueck auf die Vorgabe. Fuer Tests und Neustarts.</summary>
        public static void Reset()
        {
            general = null;
            perPlatform.Clear();
        }

#if UNITY_EDITOR
        // Ohne Domain Reload bliebe eine Einstellung aus der letzten
        // Spielsitzung stehen. Das waere eine schwer zu findende Ursache.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Reset();
#endif
    }
}
