using System.IO;
using NUnit.Framework;
using TeachingContentSystem.Runtime.Core;
using TeachingContentSystem.Runtime.UI;
using UnityEngine;

namespace TeachingContentSystem.Tests
{
    /// <summary>
    /// Wo Kursdateien gesucht werden (<see cref="ContentResolver"/>,
    /// <see cref="TcsContentLocations"/>) und wie ein beschreibbarer Ordner
    /// gefuellt wird (<see cref="ContentSeeder"/>).
    ///
    /// Die Quellen sind hier <see cref="MemorySource"/>s und ein Ordner im
    /// Temp-Verzeichnis; StreamingAssets des Projekts bleibt unberuehrt.
    /// </summary>
    public class ContentLocationTests
    {
        private string previousLanguage;
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            previousLanguage = TcsLanguage.Current;
            TcsContentLocations.Reset();
            tempRoot = Path.Combine(Path.GetTempPath(), "tcs-tests-" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            TcsLanguage.ResetForTests(previousLanguage);
            TcsContentLocations.Reset();
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }

        private static string Chapter(string name, params string[] ids)
        {
            var blocks = new System.Text.StringBuilder();
            for (int i = 0; i < ids.Length; i++)
            {
                if (i > 0) blocks.Append(",");
                blocks.Append($"{{\"id\":\"{ids[i]}\",\"type\":\"Slide\",\"data\":{{\"title\":\"{ids[i]}\",\"text\":\"t\"}}}}");
            }
            return $"{{\"chapter\":\"{name}\",\"blocks\":[{blocks}]}}";
        }

        private const string Course =
            "{\"course\":\"C\",\"chapters\":[\"chapters/a.json\",\"chapters/b.json\"]}";

        // --- Reihenfolge und Rueckfall je Datei -------------------------------

        [Test]
        public void ErsteQuelleMitDerDateiGewinnt()
        {
            var a = new MemorySource("A").Set("x.json", "from A");
            var b = new MemorySource("B").Set("x.json", "from B").Set("y.json", "only B");

            Assert.IsTrue(new ContentResolver(a, b).TryRead("x.json", "en", out string text, out ICourseSource from, out _));
            Assert.AreEqual("from A", text);
            Assert.AreSame(a, from);

            Assert.IsTrue(new ContentResolver(b, a).TryRead("x.json", "en", out text, out from, out _));
            Assert.AreEqual("from B", text);

            Assert.IsTrue(new ContentResolver(a, b).TryRead("y.json", "en", out text, out from, out _));
            Assert.AreEqual("only B", text, "Fehlt die Datei in A, kommt sie aus B.");
            Assert.AreSame(b, from);

            Assert.IsFalse(new ContentResolver(a, b).TryRead("z.json", "en", out text, out _, out _));
            Assert.IsNull(text);
        }

        /// <summary>
        /// Der eigentliche Zweck: Ein Ordner auf dem Geraet ersetzt nur ein
        /// Kapitel, alles andere kommt weiter aus der ausgelieferten Fassung.
        /// </summary>
        [Test]
        public void GeraeteordnerErsetztNurEinzelneKapitel()
        {
            TcsLanguage.ResetForTests("en");
            var shipped = new MemorySource("shipped")
                .Set("TCS/C/course.json", Course)
                .Set("TCS/C/chapters/a.json", Chapter("A shipped", "a1", "a2"))
                .Set("TCS/C/chapters/b.json", Chapter("B shipped", "b1"));
            var device = new MemorySource("device")
                .Set("TCS/C/chapters/b.json", Chapter("B device", "b1", "b2", "b3"));

            RuntimeContentGraph graph = CourseLoader.LoadCourse("TCS/C/course.json", new ContentResolver(device, shipped));

            CollectionAssert.AreEqual(new[] { "A shipped", "B device" }, graph.ChapterOrder);
            Assert.AreEqual(5, graph.Nodes.Count);
        }

        [Test]
        public void KonfigurierteOrteGeltenFuerDenBisherigenAufruf()
        {
            TcsLanguage.ResetForTests("en");
            var shipped = new MemorySource("shipped")
                .Set("TCS/C/course.json", Course)
                .Set("TCS/C/chapters/a.json", Chapter("A", "a1"))
                .Set("TCS/C/chapters/b.json", Chapter("B", "b1"));
            TcsContentLocations.Configure(shipped);

            RuntimeContentGraph graph = CourseLoader.LoadCourse("TCS/C/course.json");
            Assert.AreEqual(2, graph.Nodes.Count);

            ChapterContent chapter = ChapterLoader.LoadChapter("TCS/C/chapters/b.json");
            Assert.AreEqual("B", chapter.ChapterName);
        }

        [Test]
        public void FehlendeDateiNenntDieOrte()
        {
            var ex = Assert.Throws<FileNotFoundException>(
                () => CourseLoader.LoadCourse("nope/course.json", new ContentResolver(new MemorySource("leer"))));
            StringAssert.Contains("leer", ex.Message);
        }

        // --- Sprachfassungen je Quelle ----------------------------------------

        [Test]
        public void SprachfassungWirdJeQuelleAufgeloest()
        {
            var a = new MemorySource("A").Set("k.json", "A en").Set("k.de.json", "A de");
            var b = new MemorySource("B").Set("k.json", "B en").Set("k.de.json", "B de");
            var resolver = new ContentResolver(a, b);

            resolver.TryRead("k.json", "de", out string text, out _, out string path);
            Assert.AreEqual("A de", text);
            Assert.AreEqual("k.de.json", path);

            resolver.TryRead("k.json", "en", out text, out _, out path);
            Assert.AreEqual("A en", text, "Die Grundsprache fragt nicht nach Varianten.");
            Assert.AreEqual("k.json", path);

            resolver.TryRead("k.json", "fr", out text, out _, out _);
            Assert.AreEqual("A en", text, "Ohne Fassung in der Sprache: Grundform derselben Quelle.");
        }

        /// <summary>
        /// Eine Ueberschreibung gilt fuer alle Sprachen: Hat die erste Quelle
        /// nur die Grundform, gewinnt sie auch gegen eine Uebersetzung in
        /// einer spaeteren Quelle, die zur alten Fassung gehoert.
        /// </summary>
        [Test]
        public void GrundformEinerFruehenQuelleSchlaegtUebersetzungEinerSpaeteren()
        {
            var device = new MemorySource("device").Set("k.json", "device en");
            var shipped = new MemorySource("shipped").Set("k.json", "shipped en").Set("k.de.json", "shipped de");

            new ContentResolver(device, shipped).TryRead("k.json", "de", out string text, out ICourseSource from, out _);
            Assert.AreEqual("device en", text);
            Assert.AreSame(device, from);

            // Nur die Uebersetzung auf dem Geraet: sie gewinnt fuer Deutsch,
            // Englisch kommt weiter aus der ausgelieferten Fassung.
            var deviceDe = new MemorySource("device").Set("k.de.json", "device de");
            var resolver = new ContentResolver(deviceDe, shipped);
            resolver.TryRead("k.json", "de", out text, out _, out _);
            Assert.AreEqual("device de", text);
            resolver.TryRead("k.json", "en", out text, out _, out _);
            Assert.AreEqual("shipped en", text);
        }

        [Test]
        public void KursLaedtKapitelInDerAktuellenSprache()
        {
            TcsLanguage.ResetForTests("de");
            var shipped = new MemorySource("shipped")
                .Set("TCS/C/course.json", Course)
                .Set("TCS/C/chapters/a.json", Chapter("A", "a1"))
                .Set("TCS/C/chapters/a.de.json", Chapter("A de", "a1"))
                .Set("TCS/C/chapters/b.json", Chapter("B", "b1"));

            RuntimeContentGraph graph = CourseLoader.LoadCourse("TCS/C/course.json", new ContentResolver(shipped));
            CollectionAssert.AreEqual(new[] { "A de", "B" }, graph.ChapterOrder);
        }

        // --- Pfade --------------------------------------------------------------

        [Test]
        public void PfadeHabenSchraegstricheAufJederPlattform()
        {
            Assert.AreEqual("a/b/k.de.json", ContentResolver.Variant("a\\b\\k.json", "de"));
            Assert.AreEqual("k.de.json", ContentResolver.Variant("k.json", "de"));
            Assert.AreEqual("TCS/C/", ContentResolver.DirectoryOf("/TCS/C/course.json"));
            Assert.AreEqual("TCS/shared/x.json", ContentResolver.Combine("TCS/C/", "../shared/x.json"));
            Assert.AreEqual("TCS/C/chapters/a.json", ContentResolver.Combine("TCS/C", "./chapters/a.json"));
        }

        [Test]
        public void OrdnerquelleLiestAusDemDateisystem()
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, "TCS", "C"));
            File.WriteAllText(Path.Combine(tempRoot, "TCS", "C", "x.json"), "disk");

            var folder = new FolderSource(tempRoot);
            Assert.IsTrue(folder.TryReadText("TCS/C/x.json", out string text));
            Assert.AreEqual("disk", text);
            Assert.IsFalse(folder.TryReadText("TCS/C/y.json", out _));
        }

        // --- Einstellung je Plattform ---------------------------------------------

        [Test]
        public void VorgabeIstStreamingAssetsAllein()
        {
            Assert.AreEqual(1, TcsContentLocations.Current.Count);
            Assert.IsInstanceOf<StreamingAssetsSource>(TcsContentLocations.Current[0]);
        }

        [Test]
        public void ListeJePlattformGehtVorDieAllgemeine()
        {
            var general = new MemorySource("general");
            var android = new MemorySource("android");

            TcsContentLocations.Configure(general);
            TcsContentLocations.ConfigureFor(RuntimePlatform.Android, android, general);

            Assert.AreSame(android, TcsContentLocations.For(RuntimePlatform.Android)[0]);
            Assert.AreEqual(2, TcsContentLocations.For(RuntimePlatform.Android).Count);
            Assert.AreSame(general, TcsContentLocations.For(RuntimePlatform.WindowsPlayer)[0]);

            TcsContentLocations.ConfigureFor(RuntimePlatform.Android);
            Assert.AreSame(general, TcsContentLocations.For(RuntimePlatform.Android)[0], "Entfernt: wieder die allgemeine Liste.");

            TcsContentLocations.Reset();
            Assert.IsInstanceOf<StreamingAssetsSource>(TcsContentLocations.For(RuntimePlatform.Android)[0]);
        }

        // --- Erster Start: Ordner fuellen -----------------------------------------

        [Test]
        public void FuellenUeberschreibtNie()
        {
            var shipped = new MemorySource("shipped")
                .Set("TCS/C/course.json", Course)
                .Set("TCS/C/chapters/a.json", "a")
                .Set("TCS/C/chapters/a.de.json", "a de")
                .Set("TCS/C/chapters/b.json", "b");

            string edited = Path.Combine(tempRoot, "TCS", "C", "chapters", "a.json");
            Directory.CreateDirectory(Path.GetDirectoryName(edited));
            File.WriteAllText(edited, "edited on device");

            int copied = ContentSeeder.SeedCourse("TCS/C/course.json", shipped, tempRoot);

            Assert.AreEqual(3, copied, "course.json, a.de.json und b.json; a.json lag schon da.");
            Assert.AreEqual("edited on device", File.ReadAllText(edited));
            Assert.AreEqual("a de", File.ReadAllText(Path.Combine(tempRoot, "TCS", "C", "chapters", "a.de.json")));
            Assert.AreEqual("b", File.ReadAllText(Path.Combine(tempRoot, "TCS", "C", "chapters", "b.json")));

            // Auch eine neuere Fassung im Build ersetzt nichts.
            shipped.Set("TCS/C/chapters/b.json", "b, newer build");
            Assert.AreEqual(0, ContentSeeder.SeedCourse("TCS/C/course.json", shipped, tempRoot));
            Assert.AreEqual("b", File.ReadAllText(Path.Combine(tempRoot, "TCS", "C", "chapters", "b.json")));
        }

        [Test]
        public void GefuellterOrdnerTaugtAlsQuelle()
        {
            TcsLanguage.ResetForTests("en");
            var shipped = new MemorySource("shipped")
                .Set("TCS/C/course.json", Course)
                .Set("TCS/C/chapters/a.json", Chapter("A", "a1"))
                .Set("TCS/C/chapters/b.json", Chapter("B", "b1"));

            ContentSeeder.SeedCourse("TCS/C/course.json", shipped, tempRoot);
            RuntimeContentGraph graph = CourseLoader.LoadCourse("TCS/C/course.json",
                new ContentResolver(new FolderSource(tempRoot)));
            Assert.AreEqual(2, graph.Nodes.Count);
        }

        // --- Lesbarkeit -------------------------------------------------------------

        [Test]
        public void MindestschriftFolgtAbstandUndMassstab()
        {
            // Massstab 0.001 (1 Einheit = 1 mm), 1 m Abstand, 7 mm Versalhoehe:
            // 7 mm / 0.7 = 10 mm Schriftgroesse = 10 Einheiten.
            Assert.AreEqual(10f, ReadableWorldPanel.MinimumFontSize(0.001f, 1f), 1e-3f);
            Assert.AreEqual(20f, ReadableWorldPanel.MinimumFontSize(0.001f, 2f), 1e-3f);
            Assert.AreEqual(0.007f, ReadableWorldPanel.CapHeight(10f, 0.001f), 1e-6f);
        }
    }
}
