using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using TeachingContentSystem.Runtime.Blocks;
using TeachingContentSystem.Runtime.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TeachingContentSystem.Tests
{
    /// <summary>
    /// Die ersten Tests dieses Pakets, und sie entstanden aus einem Anlass:
    /// Beim Durchsehen vor der Veroeffentlichung hatte der Quizbaustein zwei
    /// Fehler, die niemand bemerkt hatte, weil nichts ihn prueft.
    ///
    /// WORAUF SIE ZIELEN
    /// -----------------
    /// Nicht auf die Rechnung, sondern auf die **Grenze**: Was passiert mit
    /// dem, was aus einer JSON-Datei hereinkommt. Genau dort lagen beide
    /// Fehler, und genau dort kann kein Compiler helfen, weil
    /// <c>RuntimeContentBlock.Data</c> ein
    /// <c>Dictionary&lt;string, object&gt;</c> ist: Jeder Lesefehler ist erst
    /// zur Laufzeit sichtbar.
    ///
    /// Die Bausteine werden deshalb aus **echtem JSON** gebaut, nicht aus
    /// von Hand gefuellten Dictionaries. Ein Test mit handgefuellten Listen
    /// haette beide Fehler durchgelassen, weil er genau den Typ geliefert
    /// haette, den der Code faelschlich erwartete.
    /// </summary>
    public class BlockDataTests
    {
        private static RuntimeContentBlock AusJson(string json)
        {
            return JsonConvert.DeserializeObject<RuntimeContentBlock>(json);
        }

        // --- Quiz: das, was vorher still fehlschlug ------------------------

        /// <summary>
        /// Der eigentliche Regressionstest. Vorher stand im Quizbaustein
        /// <c>Data["questions"] as List&lt;string&gt;</c>, und weil Newtonsoft
        /// eine JSON-Liste als <c>JArray</c> ablegt, war das Ergebnis immer
        /// null: Das Quiz hatte nie Inhalt und sagte nichts.
        /// </summary>
        [Test]
        public void QuizLiestFragenAusEchtemJson()
        {
            var block = AusJson(@"{
                ""id"": ""quiz1"",
                ""type"": ""Quiz"",
                ""data"": {
                    ""questions"": [""Wie viele Achsen?"", ""Welche Einheit?""],
                    ""answers"": [[""vier"", ""sechs""], [""Grad"", ""Meter""]],
                    ""correct"": [1, 0]
                }
            }");

            var quiz = new QuizBlockRuntime(block);

            Assert.AreEqual(2, quiz.Questions.Count, "Fragen");
            Assert.AreEqual("Wie viele Achsen?", quiz.Questions[0]);
            Assert.AreEqual(2, quiz.Answers.Count, "Antwortgruppen");
            Assert.AreEqual("sechs", quiz.Answers[0][1]);
            Assert.AreEqual(new[] { 1, 0 }, quiz.CorrectIndices);
        }

        /// <summary>
        /// Der zweite Fehler: Der Indexer <c>Data["questions"]</c> wirft bei
        /// fehlendem Schluessel. Ein unvollstaendiger Block riss damit den
        /// Konstruktor mit, statt eine lesbare Meldung zu geben.
        /// </summary>
        [Test]
        public void QuizOhneFragenWirftNichtSondernWarnt()
        {
            var block = AusJson(@"{ ""id"": ""leer"", ""type"": ""Quiz"", ""data"": {} }");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Quizbaustein 'leer' hat keine Fragen"));

            QuizBlockRuntime quiz = null;
            Assert.DoesNotThrow(() => quiz = new QuizBlockRuntime(block));
            Assert.AreEqual(0, quiz.Questions.Count);
        }

        /// <summary>
        /// Eine Kursdatei, in der Fragen und Antworten verschieden lang sind,
        /// ist ein Fehler im Inhalt. Er soll auffallen, aber nichts abbrechen:
        /// Der Rest des Kapitels bleibt brauchbar.
        /// </summary>
        [Test]
        public void QuizMeldetUngleicheLaengen()
        {
            var block = AusJson(@"{
                ""id"": ""schief"",
                ""type"": ""Quiz"",
                ""data"": {
                    ""questions"": [""A"", ""B""],
                    ""answers"": [[""x"", ""y""]],
                    ""correct"": [0]
                }
            }");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("2 Fragen, aber 1 Antwortgruppen"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"2 Fragen, aber 1 Angaben"));

            var quiz = new QuizBlockRuntime(block);
            Assert.AreEqual(2, quiz.Questions.Count);
        }

        /// <summary>
        /// Zeigt ein <c>correct</c> auf eine Antwort, die es nicht gibt, ist
        /// die Frage unbeantwortbar. Auch das soll sich melden.
        /// </summary>
        [Test]
        public void QuizMeldetEinenUnmoeglichenLoesungsindex()
        {
            var block = AusJson(@"{
                ""id"": ""daneben"",
                ""type"": ""Quiz"",
                ""data"": {
                    ""questions"": [""A""],
                    ""answers"": [[""x"", ""y""]],
                    ""correct"": [5]
                }
            }");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"Frage 1: ""correct"" ist 5"));

            var quiz = new QuizBlockRuntime(block);
            Assert.IsFalse(quiz.IsCorrect(0, 5), "Ein Index ausserhalb zaehlt nicht als richtig.");
        }

        [Test]
        public void IsCorrectAntwortetAufFragenDieEsNichtGibt()
        {
            var block = AusJson(@"{
                ""id"": ""q"", ""type"": ""Quiz"",
                ""data"": { ""questions"": [""A""], ""answers"": [[""x"",""y""]], ""correct"": [1] }
            }");
            var quiz = new QuizBlockRuntime(block);

            Assert.IsTrue(quiz.IsCorrect(0, 1));
            Assert.IsFalse(quiz.IsCorrect(0, 0));
            // Darf nicht werfen: Eine Anzeige soll an einem falschen Index
            // nicht abstuerzen, sondern "nein" bekommen.
            Assert.IsFalse(quiz.IsCorrect(7, 1));
            Assert.IsFalse(quiz.IsCorrect(-1, 1));
        }

        // --- Slide ---------------------------------------------------------

        [Test]
        public void SlideLiestTextBildUndDauer()
        {
            var block = AusJson(@"{
                ""id"": ""folie"", ""type"": ""Slide"",
                ""data"": { ""text"": ""Hallo"", ""image"": ""bild.png"", ""duration"": 2.5 }
            }");
            var slide = new SlideBlockRuntime(block);

            Assert.AreEqual("Hallo", slide.Text);
            Assert.AreEqual("bild.png", slide.ImageName);
            Assert.AreEqual(2.5f, slide.Duration, 1e-4f);
        }

        /// <summary>
        /// Ohne <c>duration</c> ist die Dauer 0, und 0 heisst laut
        /// <see cref="IContentBlock"/>: warte auf den Nutzer. Eine Folie ohne
        /// Zeitangabe darf also nicht sofort weiterblaettern.
        /// </summary>
        [Test]
        public void SlideOhneDauerWartet()
        {
            var block = AusJson(@"{ ""id"": ""f"", ""type"": ""Slide"", ""data"": { ""text"": ""x"" } }");
            var slide = new SlideBlockRuntime(block);

            Assert.AreEqual(0f, slide.Duration);
            Assert.IsNull(slide.ImageName, "Ohne Bildangabe soll null stehen, nicht ein leerer Text.");
        }

        // --- Composite -----------------------------------------------------

        /// <summary>
        /// Der Baustein kann seine Kinder nicht anlegen. Das ist bekannt und
        /// dokumentiert. Dieser Test haelt zweierlei fest: dass er es sagt,
        /// und dass die Kennungen trotzdem auslesbar sind, damit eine
        /// Anwendung sie selbst aufloesen kann.
        /// </summary>
        [Test]
        public void CompositeNenntSeineKinderUndMeldetDassErSieNichtBauenKann()
        {
            var block = AusJson(@"{
                ""id"": ""zusammen"", ""type"": ""Composite"",
                ""data"": { ""children"": [""a"", ""b"", ""c""] }
            }");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("'zusammen' nennt 3 Kinder"));

            var composite = new CompositeBlockRuntime(block);
            Assert.AreEqual(new[] { "a", "b", "c" }, composite.ChildIds);
        }

        [Test]
        public void CompositeOhneKinderSchweigt()
        {
            var block = AusJson(@"{ ""id"": ""leer"", ""type"": ""Composite"", ""data"": {} }");

            var composite = new CompositeBlockRuntime(block);
            Assert.AreEqual(0, composite.ChildIds.Count);
        }

        // --- Fabrik --------------------------------------------------------

        /// <summary>
        /// Vorher stand in der Fabrik ein Fall "Assistant", dessen einzige
        /// Zeile auskommentiert war: Ein solcher Baustein fiel durch zum
        /// Standardfall und meldete "Unknown block type", obwohl der Typname
        /// dastand. Der Fall ist entfallen, und dieser Test haelt fest, dass
        /// ein unbekannter Typ sauber im Fehlerbaustein landet.
        /// </summary>
        [Test]
        public void UnbekannterTypWirdZumFehlerbaustein()
        {
            var block = AusJson(@"{ ""id"": ""x"", ""type"": ""GibtsNicht"", ""data"": {} }");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Unknown block type: GibtsNicht"));

            IContentBlock erzeugt = BlockFactory.Create(block);
            Assert.IsInstanceOf<ErrorBlockRuntime>(erzeugt);
        }

        [Test]
        public void BekannteTypenErgebenIhrenBaustein()
        {
            var faelle = new Dictionary<string, System.Type>
            {
                { "Slide", typeof(SlideBlockRuntime) },
                { "Quiz",  typeof(QuizBlockRuntime)  },
            };

            foreach (KeyValuePair<string, System.Type> fall in faelle)
            {
                var block = AusJson($@"{{ ""id"": ""b"", ""type"": ""{fall.Key}"",
                    ""data"": {{ ""questions"": [""A""], ""answers"": [[""x""]], ""correct"": [0] }} }}");
                Assert.IsInstanceOf(fall.Value, BlockFactory.Create(block), fall.Key);
            }
        }
    }
}
