using UnityEngine;
using UnityEngine.Events;

namespace TeachingContentSystem.Runtime.SceneActions
{
    /// <summary>
    /// Ein <see cref="SceneActionHandler"/> ohne eigenen Code.
    ///
    /// Fuer alles, was sich in der Szene verdrahten laesst: Objekt
    /// einschalten, Animation starten, Kamera setzen. Wer kein Skript
    /// schreiben will, nimmt diese Komponente, traegt den Namen ein und
    /// haengt die Aufrufe in die Ereignisse.
    ///
    /// Das Fertigmelden muss die Szene selbst erledigen; dafuer ist
    /// <see cref="CompleteNow"/> eine oeffentliche Methode ohne Parameter —
    /// nur so taucht sie in der Auswahlliste eines UnityEvent auf, etwa am
    /// Ende einer Animation oder an einem Schalter.
    /// </summary>
    public class SceneActionEvents : SceneActionHandler
    {
        [Tooltip("Wird aufgerufen, wenn das Kapitel hier ankommt. Der Text ist "
                 + "das rohe parameters-Feld aus dem Kurs-JSON, oft leer.")]
        public UnityEvent<string> OnBegin;

        [Tooltip("Wird aufgerufen, wenn das Kapitel weiterblaettert.")]
        public UnityEvent OnEnd;

        [Tooltip("Sofort als erledigt melden, ohne auf die Szene zu warten. "
                 + "Fuer Handlungen, bei denen es nichts abzuwarten gibt.")]
        public bool CompleteImmediately;

        protected override void Begin(string parametersJson)
        {
            OnBegin?.Invoke(parametersJson);
            if (CompleteImmediately) CompleteNow();
        }

        public override void End() => OnEnd?.Invoke();

        /// <summary>Aus der Szene aufrufen, wenn die Handlung fertig ist.</summary>
        public void CompleteNow() => Complete();
    }
}
