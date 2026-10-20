using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TeachingContentSystem.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace TeachingContentSystem.EditorTools
{
    /// <summary>
    /// Prueft die Kursdateien unter StreamingAssets, ohne das Spiel zu starten.
    ///
    /// Die Kurse liegen als JSON im Dateisystem; das ist die einzige Quelle.
    /// Dadurch sind sie versionierbar, uebersetzbar und einzeln in ein anderes
    /// Projekt umziehbar — aber ein Tippfehler im Dateinamen faellt erst beim
    /// Abspielen auf. Genau das faengt diese Pruefung ab.
    ///
    /// Geprueft wird:
    ///   - laesst sich jede Gliederung lesen und nennt sie einen Kursnamen
    ///   - existiert jede genannte Kapiteldatei
    ///   - hat jedes Kapitel Bausteine, eindeutige Kennungen, bekannte Typen
    ///   - liegt zu jedem Kapitel die Uebersetzung vor, und zwar mit denselben
    ///     Bausteinkennungen (sonst laeuft die Uebersetzung auseinander)
    ///   - laedt die Laufzeit den Kurs tatsaechlich
    /// </summary>
    public static class Kurspruefung
    {
        private const string Unterordner = "TCS";

        // Muss zu BlockFactory passen. Weicht ein Typ ab, erzeugt die Laufzeit
        // einen Fehlerbaustein statt Inhalt.
        private static readonly string[] BekannteTypen =
        {
            "Slide", "Interaction", "SceneAction", "Quiz", "Composite", "Assistant"
        };

        // Handlungsname -> wo er genannt wird. Gesammelt beim Lesen der
        // Kapitel, geprueft am Ende gegen die Szenen.
        private static readonly Dictionary<string, string> GenannteHandlungen =
            new Dictionary<string, string>();

        [MenuItem("Tools/Teaching Content System/Kursdateien pruefen")]
        public static void Pruefen()
        {
            GenannteHandlungen.Clear();

            string wurzel = Path.Combine(Application.streamingAssetsPath, Unterordner);
            if (!Directory.Exists(wurzel))
            {
                Debug.LogWarning($"Kurspruefung: {wurzel} gibt es nicht.");
                return;
            }

            var gliederungen = Directory.GetFiles(wurzel, "course.json",
                                                  SearchOption.AllDirectories)
                                        .OrderBy(p => p)
                                        .ToList();
            if (gliederungen.Count == 0)
            {
                Debug.LogWarning($"Kurspruefung: keine course.json unter {wurzel}.");
                return;
            }

            int fehler = 0;
            int kapitelGesamt = 0;
            int bausteineGesamt = 0;
            var bericht = new List<string>();

            foreach (string pfad in gliederungen)
            {
                JObject gliederung;
                try
                {
                    gliederung = JObject.Parse(File.ReadAllText(pfad));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Kurspruefung: {Kurz(pfad)} ist kein gueltiges JSON — {e.Message}");
                    fehler++;
                    continue;
                }

                string name = gliederung.Value<string>("course");
                if (string.IsNullOrWhiteSpace(name))
                {
                    Debug.LogError($"Kurspruefung: {Kurz(pfad)} nennt kein Feld \"course\".");
                    fehler++;
                }

                // Zwei Arten von Gliederung: ein Kurs mit Kapiteln, oder ein
                // Pfad ueber mehrere Szenen, der auf weitere Gliederungen zeigt.
                var szenen = gliederung["scenes"] as JArray;
                if (szenen != null)
                {
                    foreach (var eintrag in szenen)
                    {
                        string ziel = Path.Combine(Path.GetDirectoryName(pfad), (string)eintrag);
                        if (!File.Exists(ziel))
                        {
                            Debug.LogError($"Kurspruefung: {Kurz(pfad)} verweist auf "
                                           + $"{eintrag}, das es nicht gibt.");
                            fehler++;
                        }
                    }
                    bericht.Add($"{Kurz(pfad)}: Pfad ueber {szenen.Count} Szenen");
                    continue;
                }

                var kapitel = gliederung["chapters"] as JArray;
                if (kapitel == null || kapitel.Count == 0)
                {
                    Debug.LogError($"Kurspruefung: {Kurz(pfad)} nennt keine Kapitel.");
                    fehler++;
                    continue;
                }

                int bausteine = 0;
                foreach (var eintrag in kapitel)
                {
                    string kapitelPfad = Path.Combine(Path.GetDirectoryName(pfad), (string)eintrag);
                    fehler += PruefeKapitel(kapitelPfad, (string)eintrag, pfad, out int anzahl);
                    bausteine += anzahl;
                }

                kapitelGesamt += kapitel.Count;
                bausteineGesamt += bausteine;
                bericht.Add($"{Kurz(pfad)}: {kapitel.Count} Kapitel, {bausteine} Bausteine");

                // Und zuletzt der Weg, den die Laufzeit nimmt.
                string relativ = pfad.Substring(Application.streamingAssetsPath.Length + 1)
                                     .Replace('\\', '/');
                try
                {
                    var graph = CourseLoader.LoadCourse(relativ);
                    if (graph.Nodes.Count != bausteine)
                    {
                        Debug.LogWarning($"Kurspruefung: {Kurz(pfad)} ergibt "
                                         + $"{graph.Nodes.Count} Knoten, erwartet waren "
                                         + $"{bausteine}. Das passiert, wenn in der "
                                         + "gewaehlten Sprache andere Bausteine stehen.");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Kurspruefung: die Laufzeit kann {Kurz(pfad)} nicht "
                                   + $"laden — {e.Message}");
                    fehler++;
                }
            }

            int ohneHandler = PruefeHandlungen();

            foreach (string zeile in bericht)
                Debug.Log("Kurspruefung: " + zeile);

            string sprache = TcsLanguage.Current;

            // Fehlende Handler getrennt ausweisen: Sie sind etwas anderes als
            // ein kaputtes Kapitel. Meist ist nur das Modul noch nicht in die
            // Szene gezogen, und dann ist der Kurs in Ordnung -- die Folie
            // zeigt dann zur Laufzeit einen Hinweis und bleibt lesbar.
            if (ohneHandler > 0)
                Debug.LogWarning(
                    $"Kurspruefung: {ohneHandler} Szenenhandlung(en) ohne Handler. "
                    + "Das ist kein Fehler im Kurs: Entweder liegt das zugehoerige "
                    + "Modul noch in keiner Szene, oder der Name ist verschrieben. "
                    + "Zur Laufzeit zeigt die Folie einen Hinweis und bleibt lesbar.");

            if (fehler == 0)
                Debug.Log($"Kurspruefung: in Ordnung — {gliederungen.Count} Gliederungen, "
                          + $"{kapitelGesamt} Kapitel, {bausteineGesamt} Bausteine. "
                          + $"Die Laufzeitprobe lief in Sprache \"{sprache}\".");
            else
                Debug.LogError($"Kurspruefung: {fehler} Beanstandung(en). "
                               + $"Die Laufzeitprobe lief in Sprache \"{sprache}\".");
        }

        /// <summary>
        /// Prueft, ob es zu jeder im Kurs genannten Szenenhandlung auch einen
        /// Handler gibt.
        ///
        /// WARUM VOR DER LAUFZEIT
        /// ----------------------
        /// Zur Laufzeit faellt ein Tippfehler zwar auf — der Baustein zeigt
        /// einen Hinweis —, aber erst, wenn jemand bis zu dieser Folie
        /// geblaettert ist, womoeglich mit der Brille auf. Hier faellt er beim
        /// Pruefen auf, zusammen mit der Stelle im Kapitel.
        ///
        /// WIE GESUCHT WIRD
        /// ----------------
        /// Nicht ueber geladene Szenen, sondern ueber den Text der Szenen- und
        /// Prefab-Dateien: <c>SceneActionHandler</c> serialisiert sein Feld
        /// <c>ActionName</c>, und das steht dort als Zeile. Das ist schnell,
        /// braucht kein Laden und findet auch Handler in Szenen, die gerade
        /// nicht offen sind.
        ///
        /// Die Grenze dieser Methode: Ein fremdes Skript mit einem Feld
        /// gleichen Namens wird mitgezaehlt. Dann gilt ein Name als vorhanden,
        /// der es nicht ist — eine zu milde Pruefung also, keine zu strenge.
        /// Fuer eine Pruefung, die auf Tippfehler zielt, ist das die richtige
        /// Richtung.
        /// </summary>
        private static int PruefeHandlungen()
        {
            if (GenannteHandlungen.Count == 0) return 0;

            var angemeldet = new HashSet<string>();
            string[] dateien = Directory.GetFiles(Application.dataPath, "*.prefab",
                                                  SearchOption.AllDirectories)
                                        .Concat(Directory.GetFiles(Application.dataPath, "*.unity",
                                                  SearchOption.AllDirectories))
                                        .ToArray();

            foreach (string datei in dateien)
            {
                foreach (string zeile in File.ReadLines(datei))
                {
                    int k = zeile.IndexOf("ActionName:");
                    if (k < 0) continue;
                    string wert = zeile.Substring(k + "ActionName:".Length).Trim();
                    if (wert.Length > 0) angemeldet.Add(wert);
                }
            }

            int fehler = 0;
            foreach (var paar in GenannteHandlungen.OrderBy(x => x.Key))
            {
                if (angemeldet.Contains(paar.Key)) continue;
                Debug.LogWarning(
                    $"Kurspruefung: {paar.Value} ruft die Handlung \"{paar.Key}\" auf, "
                    + "aber in keiner Szene und keinem Prefab ist ein SceneActionHandler "
                    + "mit diesem Namen zu finden. Entweder fehlt das Modul, oder der Name "
                    + $"ist verschrieben. Gefunden wurden: {Liste(angemeldet.OrderBy(x => x).ToList())}.");
                fehler++;
            }

            if (fehler == 0)
                Debug.Log($"Kurspruefung: alle {GenannteHandlungen.Count} Szenenhandlung(en) "
                          + "haben einen Handler.");
            return fehler;
        }

        /// <summary>
        /// Prueft eine Kapiteldatei und ihre Uebersetzungen. Gibt die Anzahl
        /// der Beanstandungen zurueck, die Bausteinzahl ueber <paramref name="anzahl"/>.
        /// </summary>
        private static int PruefeKapitel(string kapitelPfad, string eintrag,
                                         string gliederung, out int anzahl)
        {
            anzahl = 0;
            if (!File.Exists(kapitelPfad))
            {
                Debug.LogError($"Kurspruefung: {Kurz(gliederung)} nennt {eintrag}, "
                               + "aber die Datei fehlt.");
                return 1;
            }

            int fehler = 0;
            var kennungen = Lies(kapitelPfad, ref fehler, out anzahl);

            // Uebersetzungen: jede Datei der Form name.<code>.json daneben.
            string ordner = Path.GetDirectoryName(kapitelPfad);
            string stamm = Path.GetFileNameWithoutExtension(kapitelPfad);
            var varianten = Directory.GetFiles(ordner, stamm + ".*.json")
                                     .Where(p => !p.EndsWith(".meta"))
                                     .ToList();
            if (varianten.Count == 0)
            {
                Debug.LogWarning($"Kurspruefung: zu {Kurz(kapitelPfad)} gibt es keine "
                                 + "Uebersetzung.");
            }

            foreach (string variante in varianten)
            {
                var andere = Lies(variante, ref fehler, out int _);
                var fehlt = kennungen.Except(andere).ToList();
                var zuviel = andere.Except(kennungen).ToList();
                if (fehlt.Count > 0 || zuviel.Count > 0)
                {
                    Debug.LogError($"Kurspruefung: {Kurz(variante)} weicht ab. "
                                   + $"Fehlt: {Liste(fehlt)}. Zusaetzlich: {Liste(zuviel)}. "
                                   + "Beide Sprachen brauchen dieselben Bausteinkennungen.");
                    fehler++;
                }
            }

            return fehler;
        }

        /// <summary>Liest eine Kapiteldatei und prueft sie in sich.</summary>
        private static HashSet<string> Lies(string pfad, ref int fehler, out int anzahl)
        {
            var kennungen = new HashSet<string>();
            anzahl = 0;
            JObject kapitel;
            try
            {
                kapitel = JObject.Parse(File.ReadAllText(pfad));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Kurspruefung: {Kurz(pfad)} ist kein gueltiges JSON — {e.Message}");
                fehler++;
                return kennungen;
            }

            if (string.IsNullOrWhiteSpace(kapitel.Value<string>("chapter")))
            {
                Debug.LogError($"Kurspruefung: {Kurz(pfad)} nennt kein Feld \"chapter\".");
                fehler++;
            }

            var bausteine = kapitel["blocks"] as JArray;
            if (bausteine == null || bausteine.Count == 0)
            {
                Debug.LogError($"Kurspruefung: {Kurz(pfad)} hat keine Bausteine.");
                fehler++;
                return kennungen;
            }

            anzahl = bausteine.Count;
            foreach (var baustein in bausteine)
            {
                string id = baustein.Value<string>("id");
                string typ = baustein.Value<string>("type");

                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogError($"Kurspruefung: {Kurz(pfad)} hat einen Baustein ohne Kennung.");
                    fehler++;
                }
                else if (!kennungen.Add(id))
                {
                    Debug.LogError($"Kurspruefung: {Kurz(pfad)} nennt die Kennung \"{id}\" "
                                   + "mehrfach.");
                    fehler++;
                }

                // Szenenhandlungen nennen ein Modul. Den Namen merken, damit
                // am Ende geprueft werden kann, ob es ihn ueberhaupt gibt.
                if (typ == "SceneAction" || typ == "Interaction")
                {
                    string handlung = baustein.Value<string>("action")
                                      ?? baustein.Value<string>("interactionType")
                                      ?? baustein["data"]?.Value<string>("action")
                                      ?? baustein["data"]?.Value<string>("interactionType");
                    if (string.IsNullOrWhiteSpace(handlung))
                    {
                        Debug.LogError($"Kurspruefung: {Kurz(pfad)}, Baustein \"{id}\" ist "
                                       + "eine Szenenhandlung, nennt aber kein Feld "
                                       + "\"action\". So kann sie nichts ausloesen.");
                        fehler++;
                    }
                    else if (!GenannteHandlungen.ContainsKey(handlung))
                    {
                        GenannteHandlungen[handlung] = $"{Kurz(pfad)}, Baustein {id}";
                    }
                }

                if (!BekannteTypen.Contains(typ))
                {
                    Debug.LogError($"Kurspruefung: {Kurz(pfad)}, Baustein \"{id}\" hat den "
                                   + $"unbekannten Typ \"{typ}\". Bekannt sind: "
                                   + string.Join(", ", BekannteTypen));
                    fehler++;
                }
            }

            return kennungen;
        }

        private static string Kurz(string pfad)
        {
            int schnitt = pfad.IndexOf(Unterordner + Path.DirectorySeparatorChar);
            return schnitt >= 0 ? pfad.Substring(schnitt).Replace('\\', '/') : pfad;
        }

        private static string Liste(List<string> werte)
        {
            return werte.Count == 0 ? "nichts" : string.Join(", ", werte);
        }
    }
}
