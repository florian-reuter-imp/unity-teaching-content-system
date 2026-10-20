using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TeachingContentSystem.Runtime.Core;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Blocks
{
    /// <summary>
    /// Haelt die Fragen, Antwortmoeglichkeiten und richtigen Antworten einer
    /// Uebungsaufgabe und sagt, ob eine Wahl richtig war.
    ///
    /// DIE OBERFLAECHE GEHOERT NICHT HIERHER
    /// -------------------------------------
    /// Dieser Baustein zeigt nichts an. Das ist Absicht und dieselbe
    /// Aufteilung wie bei <see cref="SlideBlockRuntime"/>: Das Paket kennt
    /// den Inhalt, die Anwendung kennt ihre Darstellung. Eine Quizanzeige in
    /// VR sieht anders aus als eine am Schreibtisch, und beide gehoeren nicht
    /// in eine Inhaltsbibliothek.
    ///
    /// Eine Anwendung liest also <see cref="Questions"/>,
    /// <see cref="Answers"/> und fragt <see cref="IsCorrect"/>, baut daraus
    /// ihre Anzeige und ruft <c>ContentManager.GoToNext()</c>, wenn der
    /// Nutzer weiter will. <see cref="Duration"/> ist 0, also wartet das
    /// Kapitel auf genau diesen Anruf.
    ///
    /// WAS HIER AM 20.10. KORRIGIERT WURDE
    /// -----------------------------------
    /// Vorher stand hier <c>_data.Data["questions"] as List&lt;string&gt;</c>.
    /// Beides daran war falsch:
    ///
    /// 1. <c>Data</c> ist ein <c>Dictionary&lt;string, object&gt;</c>, das
    ///    Newtonsoft fuellt. Eine JSON-Liste kommt darin als
    ///    <see cref="JArray"/> an, nicht als <c>List&lt;string&gt;</c>. Die
    ///    <c>as</c>-Umwandlung lieferte deshalb immer null, und das Quiz
    ///    hatte nie Inhalt, ohne dass es sich beschwerte.
    /// 2. Der Indexer <c>Data["questions"]</c> wirft, wenn der Schluessel
    ///    fehlt. Ein Quizblock ohne Fragen riss damit den Konstruktor mit,
    ///    statt eine lesbare Meldung zu geben.
    ///
    /// Gelesen wird jetzt wie im <c>ChapterLoader</c>, der es richtig
    /// vormachte, und ein unvollstaendiger Block meldet sich mit Kennung.
    /// </summary>
    public class QuizBlockRuntime : IContentBlock
    {
        private readonly RuntimeContentBlock _data;

        private readonly List<string> _questions = new List<string>();
        private readonly List<List<string>> _answers = new List<List<string>>();
        private readonly List<int> _correct = new List<int>();

        public string Id => _data.Id;

        /// <summary>0: Das Kapitel wartet, bis die Anwendung weiterschaltet.</summary>
        public float Duration => 0f;

        /// <summary>Die Fragen in der Reihenfolge des JSON.</summary>
        public IReadOnlyList<string> Questions => _questions;

        /// <summary>Je Frage die Antwortmoeglichkeiten.</summary>
        public IReadOnlyList<IReadOnlyList<string>> Answers => _answers;

        /// <summary>Je Frage der Index der richtigen Antwort.</summary>
        public IReadOnlyList<int> CorrectIndices => _correct;

        public QuizBlockRuntime(RuntimeContentBlock data)
        {
            _data = data;

            LeseListe(data, "questions", _questions);
            LeseAntworten(data, "answers", _answers);
            LeseIndizes(data, "correct", _correct);

            Pruefe();
        }

        /// <summary>
        /// War die Wahl richtig?
        ///
        /// Liefert false bei einem Frageindex, den es nicht gibt: Eine
        /// Anzeige soll daran nicht abstuerzen, sondern ein Nein bekommen.
        ///
        /// Liefert ebenso false, wenn die gewaehlte Antwort ausserhalb der
        /// Antworten dieser Frage liegt, und wenn <c>correct</c> selbst
        /// danebenzeigt. Der zweite Fall ist ein Fehler in der Kursdatei, auf
        /// den der Konstruktor schon hingewiesen hat: Eine Frage, deren
        /// richtige Antwort es nicht gibt, laesst sich nicht richtig
        /// beantworten, und dann ist keine Wahl richtig. Alles andere waere
        /// ein stilles Ja auf eine Zahl, die nichts bedeutet.
        /// </summary>
        public bool IsCorrect(int question, int chosenAnswer)
        {
            if (question < 0 || question >= _correct.Count) return false;

            if (question < _answers.Count)
            {
                int anzahl = _answers[question].Count;
                if (chosenAnswer < 0 || chosenAnswer >= anzahl) return false;
                if (_correct[question] < 0 || _correct[question] >= anzahl) return false;
            }

            return _correct[question] == chosenAnswer;
        }

        public void Enter() { }

        public void Update(float deltaTime) { }

        public void Exit() { }

        // --- Lesen ---------------------------------------------------------
        //
        // Newtonsoft kann denselben Wert als JArray oder, wenn das JSON im
        // Code zusammengesetzt wurde, schon als fertige Liste liefern. Beide
        // Faelle werden genommen, statt sich auf einen zu verlassen und bei
        // dem anderen still leer zu bleiben.

        private static void LeseListe(RuntimeContentBlock data, string schluessel, List<string> ziel)
        {
            if (!data.Data.TryGetValue(schluessel, out object rohwert) || rohwert == null) return;

            if (rohwert is JArray feld)
            {
                foreach (JToken t in feld) ziel.Add(t?.ToString() ?? string.Empty);
                return;
            }

            if (rohwert is IEnumerable<object> folge)
            {
                foreach (object o in folge) ziel.Add(o?.ToString() ?? string.Empty);
                return;
            }

            ziel.Add(rohwert.ToString());
        }

        private static void LeseAntworten(RuntimeContentBlock data, string schluessel, List<List<string>> ziel)
        {
            if (!data.Data.TryGetValue(schluessel, out object rohwert) || rohwert == null) return;

            if (rohwert is JArray aussen)
            {
                foreach (JToken innen in aussen)
                {
                    var gruppe = new List<string>();
                    if (innen is JArray feld)
                        foreach (JToken t in feld) gruppe.Add(t?.ToString() ?? string.Empty);
                    else if (innen != null)
                        gruppe.Add(innen.ToString());
                    ziel.Add(gruppe);
                }
                return;
            }

            if (rohwert is IEnumerable<object> folge)
            {
                foreach (object innen in folge)
                {
                    var gruppe = new List<string>();
                    if (innen is IEnumerable<object> unterfolge)
                        foreach (object o in unterfolge) gruppe.Add(o?.ToString() ?? string.Empty);
                    else if (innen != null)
                        gruppe.Add(innen.ToString());
                    ziel.Add(gruppe);
                }
            }
        }

        private static void LeseIndizes(RuntimeContentBlock data, string schluessel, List<int> ziel)
        {
            var texte = new List<string>();
            LeseListe(data, schluessel, texte);
            foreach (string t in texte)
                ziel.Add(int.TryParse(t, out int i) ? i : -1);
        }

        /// <summary>
        /// Meldet, was nicht zusammenpasst. Ein Quiz, dessen Fragen und
        /// Antworten unterschiedlich lang sind, ist ein Fehler in der
        /// Kursdatei und keiner zur Laufzeit: darum eine Warnung mit der
        /// Kennung des Bausteins und kein Abbruch.
        /// </summary>
        private void Pruefe()
        {
            if (_questions.Count == 0)
            {
                Debug.LogWarning(
                    $"[TCS] Quizbaustein '{Id}' hat keine Fragen. Erwartet wird im "
                    + "JSON ein Feld \"questions\" mit einer Liste von Texten.");
                return;
            }

            if (_answers.Count != _questions.Count)
                Debug.LogWarning(
                    $"[TCS] Quizbaustein '{Id}': {_questions.Count} Fragen, aber "
                    + $"{_answers.Count} Antwortgruppen.");

            if (_correct.Count != _questions.Count)
                Debug.LogWarning(
                    $"[TCS] Quizbaustein '{Id}': {_questions.Count} Fragen, aber "
                    + $"{_correct.Count} Angaben in \"correct\".");

            for (int i = 0; i < _correct.Count && i < _answers.Count; i++)
                if (_correct[i] < 0 || _correct[i] >= _answers[i].Count)
                    Debug.LogWarning(
                        $"[TCS] Quizbaustein '{Id}', Frage {i + 1}: \"correct\" ist "
                        + $"{_correct[i]}, es gibt aber {_answers[i].Count} Antworten.");
        }
    }
}
