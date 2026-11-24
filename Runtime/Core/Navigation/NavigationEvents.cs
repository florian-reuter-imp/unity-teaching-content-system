using System;

namespace TeachingContentSystem.Runtime.Core.Navigation
{
    /// <summary>
    /// Events emitted by the ContentManager during navigation.
    /// </summary>
    public static class NavigationEvents
    {
        public static Action<string> OnNodeEntered;
        public static Action<string> OnNodeExited;
        public static Action<string> OnBlockStarted;
        public static Action<string> OnBlockFinished;

        /// <summary>
        /// "Weiter" wurde am letzten Baustein verlangt: Der Kurs ist zu Ende.
        /// Der Wert ist die Kennung des letzten Knotens. Kommt einmal je
        /// Besuch des letzten Knotens, auch wenn eine Dauer oder ein fertiger
        /// Baustein immer wieder weiterschalten wollte. Mit
        /// <see cref="TcsSettings.WrapAtEnd"/> kommt es ebenfalls, bevor es
        /// wieder von vorn losgeht.
        /// </summary>
        public static Action<string> OnCourseEndReached;

        /// <summary>
        /// Ein Baustein konnte nicht tun, was im Kapitel steht, und sagt
        /// warum. Erster Wert ist die Bausteinkennung, zweiter der Text.
        ///
        /// Gedacht fuer den Fall, dass ein Kapitel ein Modul anspricht, das
        /// in dieser Szene nicht liegt. Der Kurs laeuft weiter; die
        /// Oberflaeche zeigt den Text, damit es nicht nur in einer Konsole
        /// steht, die in der Brille niemand sieht.
        /// </summary>
        public static Action<string, string> OnBlockNotice;
    }
}
