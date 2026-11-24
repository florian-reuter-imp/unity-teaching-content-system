using NUnit.Framework;
using TeachingContentSystem.Runtime.Core;
using TeachingContentSystem.Runtime.Core.Navigation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TeachingContentSystem.Tests
{
    /// <summary>
    /// Das Kursende. Vorher meldete <c>GoToNext</c> am letzten Baustein
    /// einen Fehler ("has no NextIds"), bei einer Folie mit Dauer sogar in
    /// jedem Bild. Jetzt ist das Ende ein Zustand: abfragbar
    /// (<see cref="ContentManager.CanGoNext"/>), einmal gemeldet, und mit
    /// <see cref="TcsSettings.WrapAtEnd"/> wahlweise ein Umlauf.
    /// </summary>
    public class CourseEndTests
    {
        private GameObject host;
        private ContentManager manager;
        private int endEvents;
        private int instanceEndEvents;

        private void OnEnd(string nodeId) => endEvents++;
        private void OnInstanceEnd(string nodeId) => instanceEndEvents++;

        [SetUp]
        public void SetUp()
        {
            TcsSettings.Reset();
            endEvents = 0;
            instanceEndEvents = 0;
            NavigationEvents.OnCourseEndReached += OnEnd;

            var source = new MemorySource()
                .Set("C/course.json", "{\"course\":\"C\",\"chapters\":[\"one.json\",\"two.json\"]}")
                .Set("C/one.json", "{\"chapter\":\"One\",\"blocks\":["
                                   + "{\"id\":\"s1\",\"type\":\"Slide\",\"data\":{\"text\":\"1\"}},"
                                   + "{\"id\":\"s2\",\"type\":\"Slide\",\"data\":{\"text\":\"2\"}}]}")
                .Set("C/two.json", "{\"chapter\":\"Two\",\"blocks\":["
                                   + "{\"id\":\"s3\",\"type\":\"Slide\",\"data\":{\"text\":\"3\"}}]}");

            host = new GameObject("CourseEndTests");
            manager = host.AddComponent<ContentManager>();
            manager.CourseEndReached += OnInstanceEnd;
            manager.LoadGraph(CourseLoader.LoadCourse("C/course.json", new ContentResolver(source)));
        }

        [TearDown]
        public void TearDown()
        {
            NavigationEvents.OnCourseEndReached -= OnEnd;
            if (host != null) Object.DestroyImmediate(host);
            TcsSettings.Reset();
        }

        private string Current => manager.GetState().CurrentNodeId;

        [Test]
        public void AmStartGehtNurWeiter()
        {
            Assert.IsTrue(manager.IsAtStart);
            Assert.IsFalse(manager.IsAtEnd);
            Assert.IsTrue(manager.CanGoNext);
            Assert.IsFalse(manager.CanGoBack, "Am Start ohne Umlauf: zurueck ist ausgegraut.");
        }

        [Test]
        public void AmEndeKeinFehlerSondernEinmalDasEreignis()
        {
            manager.GoToNext();
            manager.GoToNext();
            Assert.AreEqual("C.Two.s3_node", Current);
            Assert.IsTrue(manager.IsAtEnd);
            Assert.IsFalse(manager.CanGoNext);
            Assert.IsTrue(manager.CanGoBack);
            Assert.AreEqual(0, endEvents);

            manager.GoToNext();
            manager.GoToNext();
            manager.GoToNext();

            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual("C.Two.s3_node", Current, "Ohne Umlauf bleibt der Kurs am Ende stehen.");
            Assert.AreEqual(1, endEvents, "Einmal je Besuch des letzten Knotens.");
            Assert.AreEqual(1, instanceEndEvents);

            // Wer zurueck und wieder ans Ende geht, beendet den Kurs erneut.
            manager.GoBack();
            manager.GoToNext();
            manager.GoToNext();
            Assert.AreEqual(2, endEvents);
        }

        [Test]
        public void MitUmlaufGehtEsVonVornLos()
        {
            TcsSettings.WrapAtEnd = true;
            manager.GoToNext();
            manager.GoToNext();
            Assert.IsTrue(manager.IsAtEnd);
            Assert.IsTrue(manager.CanGoNext, "Mit Umlauf bleibt weiter bedienbar.");

            manager.GoToNext();
            Assert.AreEqual("C.One.s1_node", Current);
            Assert.AreEqual(1, endEvents, "Das Ende wird auch beim Umlauf gemeldet.");

            // Zurueck im Verlauf fuehrt wieder ans Ende.
            manager.GoBack();
            Assert.AreEqual("C.Two.s3_node", Current);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MitUmlaufFuehrtZurueckAmStartZumLetzten()
        {
            TcsSettings.WrapAtEnd = true;
            Assert.IsTrue(manager.IsAtStart);
            Assert.IsTrue(manager.CanGoBack);

            manager.GoBack();
            Assert.AreEqual("C.Two.s3_node", Current);
        }

        [Test]
        public void OhneUmlaufTutZurueckAmStartNichts()
        {
            manager.GoBack();
            Assert.AreEqual("C.One.s1_node", Current);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
