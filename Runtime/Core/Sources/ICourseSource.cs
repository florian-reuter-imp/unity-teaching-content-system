namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Ein Ort, an dem Kursdateien liegen koennen: StreamingAssets, ein
    /// beschreibbarer Ordner auf dem Geraet, ein Speicher im Arbeitsspeicher.
    ///
    /// Eine Quelle kennt nur relative Pfade mit Schraegstrichen, etwa
    /// <c>TCS/ExampleCourse/chapters/introduction.json</c>. Welche Quelle
    /// gewinnt und welche Sprachfassung genommen wird, entscheidet
    /// <see cref="ContentResolver"/>, nicht die Quelle selbst.
    ///
    /// Gelesen wird synchron, weil das bisherige Laden synchron war und
    /// bleiben soll. Kursdateien sind klein.
    /// </summary>
    public interface ICourseSource
    {
        /// <summary>Kurzer Name fuer Meldungen, etwa "StreamingAssets".</summary>
        string Name { get; }

        /// <summary>
        /// Liest eine Datei. Gibt false zurueck, wenn es sie in dieser Quelle
        /// nicht gibt; das ist kein Fehler, sondern der Normalfall einer
        /// Quelle, die nur einzelne Dateien ueberschreibt.
        /// </summary>
        bool TryReadText(string relativePath, out string text);
    }
}
