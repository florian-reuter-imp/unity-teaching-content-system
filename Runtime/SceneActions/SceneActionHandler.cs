using UnityEngine;

namespace TeachingContentSystem.Runtime.SceneActions
{
    /// <summary>
    /// Die Szenenseite einer Kurshandlung. Ein Modul leitet davon ab.
    ///
    /// EINRICHTEN
    /// ----------
    /// Komponente an das Objekt haengen, das die Sache ausfuehrt, und bei
    /// <see cref="ActionName"/> denselben Namen eintragen, den das Kapitel
    /// unter <c>action</c> nennt. Die Anmeldung passiert von selbst.
    ///
    /// Der Name steht absichtlich als Feld im Inspektor und nicht als
    /// Attribut im Code. Zwei Gruende: In der Szene ist dadurch sichtbar, was
    /// dort angeboten wird, und die Kurspruefung kann die Namen einsammeln,
    /// ohne das Projekt zu uebersetzen oder zu starten. Deshalb fallen
    /// Tippfehler im Editor auf und nicht erst in der Brille.
    ///
    /// WER WANN FERTIG IST
    /// -------------------
    /// <see cref="Run"/> setzt <see cref="IsComplete"/> zurueck und ruft
    /// <see cref="Begin"/>. Danach entscheidet die Umsetzung selbst, wann sie
    /// <see cref="Complete"/> aufruft. Tut sie es nie, bleibt der Baustein
    /// stehen, bis der Nutzer weiterblaettert — ein vergessenes
    /// <c>Complete()</c> haelt also niemanden fest.
    /// </summary>
    public abstract class SceneActionHandler : MonoBehaviour
    {
        [Tooltip("Name, unter dem das Kapitel diese Handlung anspricht. "
                 + "Muss zum Feld action im Kurs-JSON passen.")]
        public string ActionName;

        private string angemeldet;

        /// <summary>True, sobald die Handlung erledigt ist.</summary>
        public bool IsComplete { get; private set; }

        protected virtual void OnEnable()
        {
            angemeldet = ActionName;
            SceneActionRegistry.Register(angemeldet, this);
        }

        protected virtual void OnDisable()
        {
            // Mit dem Namen abmelden, unter dem angemeldet wurde: Wird
            // ActionName zur Laufzeit geaendert, bliebe sonst ein Eintrag
            // stehen, den niemand mehr loswird.
            SceneActionRegistry.Unregister(angemeldet, this);
        }

        /// <summary>Vom Baustein aufgerufen, wenn das Kapitel hier ankommt.</summary>
        /// <param name="parametersJson">Rohe Parameter aus dem Kurs-JSON, oder null.</param>
        public void Run(string parametersJson)
        {
            IsComplete = false;
            Begin(parametersJson);
        }

        /// <summary>Hier macht das Modul seine Arbeit.</summary>
        protected abstract void Begin(string parametersJson);

        /// <summary>Aufgeraeumt wird, wenn das Kapitel weiterblaettert.</summary>
        public virtual void End() { }

        /// <summary>Meldet, dass die Handlung erledigt ist.</summary>
        protected void Complete() => IsComplete = true;
    }
}
