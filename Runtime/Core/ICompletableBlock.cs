namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Ein Baustein, der von sich aus fertig werden kann.
    ///
    /// WARUM EIN EIGENES INTERFACE
    /// ---------------------------
    /// <see cref="IContentBlock"/> kennt nur <c>Duration</c>: nach so vielen
    /// Sekunden wird weitergeschaltet, und 0 heisst „warte auf den Nutzer".
    /// Fuer eine Aufgabe in der Szene passt beides nicht — sie ist fertig,
    /// wenn sie geloest ist, und das dauert jedes Mal anders lang.
    ///
    /// Statt <see cref="IContentBlock"/> zu erweitern — was jede vorhandene
    /// Umsetzung brechen wuerde — steht das hier daneben. Der ContentManager
    /// fragt danach, wenn ein Baustein es anbietet, und sonst nicht.
    /// </summary>
    public interface ICompletableBlock
    {
        /// <summary>True, sobald die Sache erledigt ist.</summary>
        bool IsComplete { get; }

        /// <summary>
        /// Soll das Kapitel dann selbst weiterblaettern?
        ///
        /// Bei einer Szenenhandlung ja — sie ist vorbei, es gibt nichts mehr
        /// zu sehen. Bei einer Uebung eher nicht: Wer sie geloest hat, will
        /// das Ergebnis noch ansehen und selbst weiterklicken.
        /// </summary>
        bool AutoAdvance { get; }
    }
}
