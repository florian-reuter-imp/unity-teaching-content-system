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
