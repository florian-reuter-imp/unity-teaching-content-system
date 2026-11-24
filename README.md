# Teaching Content System

Lehrinhalte als JSON-Dateien schreiben und zur Laufzeit in Unity abspielen:
Folien, Quizfragen, Interaktionen und Szenenaktionen, mehrsprachig.

Das Paket setzt weder Robotik noch XR voraus und hat außer Unity selbst keine
fremde Abhängigkeit.

## Der Grundgedanke: Dateien, nicht Assets

Ein Kurs ist ein Ordner mit JSON-Dateien. Nichts wird in einem Unity-Asset
abgelegt, es gibt keine Datenbank und kein Autorenwerkzeug, auf das man
angewiesen wäre. Das hat drei Folgen, und sie sind der Grund für diesen
Zuschnitt:

- **Versionierbar.** Änderungen an Inhalten sind lesbare Diffs.
- **Übersetzbar.** Übersetzt wird je Datei, nicht je Textfeld.
- **Umziehbar.** Ein Kapitel ist eine Datei; wer eines davon in sein eigenes
  Projekt holen will, kopiert es.

Früher gab es zusätzlich einen Graph-Editor auf Basis von xNode. Er ist
entfernt worden: Es entstand nie ein Kurs darüber, alle Inhalte wurden als
JSON geschrieben, und zwei Quellen der Wahrheit sind schlechter als eine.
Übrig bleibt aus dem Editor eine Prüfung der Dateien, siehe unten.

## Installation

Im Package Manager über **Add package from git URL**:

```
https://github.com/florian-reuter-imp/unity-teaching-content-system.git#v0.2.0
```

Oder als Eintrag in `Packages/manifest.json`:

```json
"com.github.hka-imp.tcs": "https://github.com/florian-reuter-imp/unity-teaching-content-system.git#v0.2.0"
```

Das Versionsschild am Ende gehört dazu. Ohne es zieht jeder Klon einen
beliebigen Stand, und das Projekt ist nicht mehr reproduzierbar.

### Beispielkurs

Im Package Manager unter **Samples** liegt **Example Course**: zwei Kapitel
auf Englisch und Deutsch, eines mit schlichten Folien, eines mit je einem
Baustein der Typen `Slide`, `Quiz`, `Interaction` und `SceneAction`. Nach dem
Import den Ordner `TCS` aus dem Beispiel nach `Assets/StreamingAssets/`
verschieben, sodass die Gliederung unter
`Assets/StreamingAssets/TCS/ExampleCourse/course.json` liegt. Die Texte
stehen unter CC BY 4.0, siehe `README.md` im Beispiel.

## Aufbau eines Kurses

Unter `StreamingAssets` liegt je Kurs ein Ordner:

```
TCS/ExampleCourse/
  course.json
  chapters/
    introduction.json
    introduction.de.json
    block_types.json
    block_types.de.json
```

`course.json` zählt die Kapitel in ihrer Reihenfolge auf:

```json
{
  "course": "Example Course",
  "chapters": [
    "chapters/introduction.json",
    "chapters/block_types.json"
  ]
}
```

Ein Kapitel ist eine geordnete Liste von Bausteinen. Jeder Baustein hat eine
Kennung, einen Typ und seine Daten:

```json
{
  "chapter": "Introduction",
  "blocks": [
    {
      "id": "slide_intro",
      "type": "Slide",
      "data": { "title": "Welcome", "text": "Erste Folie." }
    }
  ]
}
```

Der Fortschritt läuft linear durch die Bausteine; vor und zurück springen
geht. Verzweigungen gibt es bewusst nicht: Sie waren nie gebraucht und
hätten das Format verdoppelt.

### Die Bausteintypen

Fünf Typen, und sie sind nicht gleich weit. Das steht hier so offen, weil
sich der Unterschied sonst erst im Betrieb zeigt:

| `type` | Stand | Was er tut |
|---|---|---|
| `SceneAction` | **fertig** | Stößt eine Handlung in der Szene an und schaltet selbst weiter, wenn sie erledigt ist. |
| `Interaction` | **fertig** | Dasselbe, wartet aber auf den Nutzer statt selbst weiterzublättern. |
| `Slide` | **fertig, Anzeige ist deine Sache** | Hält Text und Bildnamen. Blättert über `duration` weiter, ohne Angabe wartet er. |
| `Quiz` | **fertig, Anzeige ist deine Sache** | Hält Fragen, Antworten und die richtige Wahl, beantwortet `IsCorrect`. Wartet auf `GoToNext()`. |
| `Composite` | **wirkungslos** | Soll mehrere Bausteine gleichzeitig laufen lassen, kann seine Kinder aber nicht anlegen. Meldet das beim Anlegen. |

Zu „Anzeige ist deine Sache": `Slide` und `Quiz` zeichnen absichtlich nichts.
Das Paket kennt den Inhalt, die Anwendung kennt ihre Darstellung : eine Folie
an einer Weltfläche in VR sieht anders aus als eine am Schreibtisch, und
beides gehört nicht in eine Inhaltsbibliothek. Gelesen wird über `Text`,
`ImageName`, `Questions`, `Answers` und `IsCorrect`; weitergeschaltet über
`ContentManager.GoToNext()`.

Zu `Composite`: Im JSON stehen unter `children` die Kennungen anderer
Bausteine, und um daraus Bausteine zu machen, bräuchte die `BlockFactory` das
ganze Kapitel statt nur des einen Bausteins. Das zu ändern greift in
öffentliche Schnittstellen ein. Bis dahin stehen die Kennungen in `ChildIds`
und lassen sich von der Anwendung selbst auflösen.

Welche Felder jeder Typ unter `data` erwartet, zeigt der Beispielkurs mit je
einem Baustein.

## Abspielen

`TeachingContentSystemRuntime` an ein GameObject hängen, dazu einen
`ContentManager`, und den Pfad zur Gliederung innerhalb von `StreamingAssets`
eintragen, etwa `TCS/ExampleCourse/course.json`.

## Wo die Kurse liegen

Ohne weitere Angabe liest das System aus `StreamingAssets`, wie bisher. Im
Code lässt sich eine geordnete Liste von Orten festlegen, auf Wunsch je
Plattform:

```csharp
using TeachingContentSystem.Runtime.Core;
using UnityEngine;

static class KursOrte
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Festlegen()
    {
        // Überall: erst der Ordner auf dem Gerät, dann die ausgelieferte Fassung.
        TcsContentLocations.Configure(
            FolderSource.PersistentData(),
            new StreamingAssetsSource());

        // Am Schreibtisch zusätzlich ein Arbeitsordner, der vorgeht.
        TcsContentLocations.ConfigureFor(RuntimePlatform.WindowsEditor,
            new FolderSource(@"D:\Kurse"),
            new StreamingAssetsSource());

        // Beim ersten Start auf dem Gerät eine bearbeitbare Kopie anlegen.
        // Vorhandene Dateien bleiben unangetastet.
        ContentSeeder.SeedCourse("TCS/ExampleCourse/course.json",
                                 Application.persistentDataPath);
    }
}
```

Die Regeln:

- **Je Datei, nicht je Kurs.** Für die Gliederung und für jedes Kapitel
  einzeln gewinnt der erste Ort, der die Datei hat. Ein Ordner auf dem Gerät
  kann so ein einzelnes Kapitel ersetzen; alles andere kommt weiter aus
  `StreamingAssets`.
- **Sprache je Ort.** Innerhalb eines Ortes gilt dieselbe Regel wie bisher:
  `kapitel.de.json`, wenn es sie gibt, sonst `kapitel.json`. Erst wenn ein
  Ort keine der beiden hat, kommt der nächste dran. Wer ein Kapitel auf dem
  Gerät ersetzt, ersetzt es also für alle Sprachen; eine ältere Übersetzung
  aus `StreamingAssets` schlägt eine neuere Grundform nicht. Sollen beide
  Sprachen ersetzt werden, gehören beide Dateien in den Ordner.
- **Pfade** sind relativ zum Ort und haben Schrägstriche, etwa
  `TCS/ExampleCourse/course.json`. Ein absoluter Pfad an `LoadCourse` wird
  wie früher direkt gelesen.
- Eine Liste für eine Plattform (`ConfigureFor`) geht vor die allgemeine
  (`Configure`). `TcsContentLocations.Reset()` stellt die Vorgabe wieder her.

Wichtig: **Ein Build kann Dateien nur nach `StreamingAssets` legen.** Die
erste Fassung jedes Kurses muss deshalb dort liegen. Alle anderen Orte sind
Überschreibungen auf dem Gerät, etwa um auf einer Brille einen Text zu
korrigieren, ohne neu zu bauen.

Die Orte im Einzelnen:

| Quelle | Wofür |
|---|---|
| `StreamingAssetsSource(unterordner)` | Die ausgelieferte Fassung. Liest auf Android und Quest über `UnityWebRequest` aus der APK, sonst über das Dateisystem. |
| `FolderSource(pfad)`, `FolderSource.PersistentData(unterordner)` | Ein beliebiger Ordner, auf Geräten meist `Application.persistentDataPath`. |
| `MemorySource` | Dateien im Arbeitsspeicher: für Tests, selbst erzeugte Inhalte oder vorab asynchron geholte Dateien. |
| eigenes `ICourseSource` | Eine Methode, `TryReadText(relativerPfad, out text)`. |

Wer an der globalen Einstellung vorbei mit eigener Liste laden will:
`CourseLoader.LoadCourse(pfad, new ContentResolver(quelle1, quelle2))`.

Gelesen wird synchron, wie bisher. Auf WebGL geht das für `StreamingAssets`
nicht; dort die Dateien vorab mit `UnityWebRequest` holen und über eine
`MemorySource` anbieten.

## Kursende

Am letzten Baustein ist „weiter" kein Fehler mehr, sondern das Ende des
Kurses:

- `ContentManager.CanGoNext` und `CanGoBack` sagen, ob die Knöpfe etwas tun.
  Dazu `IsAtEnd` und `IsAtStart`.
- `GoToNext()` am Ende meldet einmal je Besuch des letzten Bausteins
  `NavigationEvents.OnCourseEndReached` und das Ereignis
  `ContentManager.CourseEndReached`, jeweils mit der Kennung des Knotens.
- `TcsSettings.WrapAtEnd` (Vorgabe `false`) legt fest, ob es danach wieder von
  vorn losgeht; dann führt auch „zurück" am ersten Baustein zum letzten.

```csharp
TcsSettings.WrapAtEnd = true;
NavigationEvents.OnCourseEndReached += knoten => Debug.Log("Kurs fertig");
```

`RuntimeContentUI` graut „weiter" am Ende und „zurück" am Anfang aus, solange
kein Umlauf eingestellt ist. Die Knöpfe lassen sich in den Feldern
`nextButton` und `backButton` zuweisen; bleiben sie leer, sucht die
Oberfläche in ihrer Canvas nach Knöpfen, deren OnClick
`ContentManager.GoToNext` oder `GoBack` aufruft. Vorhandene Prefabs brauchen
also keine Änderung.

## Sprachen

Übersetzt wird je Datei: Neben `kapitel.json` liegt `kapitel.de.json`. Gibt
es die Datei in der gewählten Sprache, wird sie geladen, sonst die Grundform.

```csharp
TcsLanguage.Set("de");   // lädt den laufenden Kurs neu
TcsLanguage.Next();      // zur nächsten verfügbaren Sprache
TcsLanguage.Register("fr");
```

Die Wahl merkt sich das System je Gerät; ohne gemerkte Wahl entscheidet die
Systemsprache. `TeachingContentSystemRuntime` lädt bei einem Wechsel von
selbst neu, die Szene bleibt dabei stehen.

Vorteil gegenüber Textfeldern je Sprache: Bestehende Kurse bleiben unverändert
gültig, Übersetzer arbeiten an ganzen Dateien, und eine fehlende Übersetzung
fällt sauber auf die Grundform zurück.

## Prüfung der Kursdateien

**Tools › Teaching Content System › Kursdateien pruefen**

Weil die Inhalte Dateien sind, fällt ein Tippfehler im Dateinamen sonst erst
beim Abspielen auf. Die Prüfung geht alle `course.json` unter
`StreamingAssets/TCS` durch und meldet: fehlende Kapiteldateien, Kapitel ohne
Bausteine, doppelte Kennungen, unbekannte Bausteintypen, fehlende
Übersetzungen und, am wichtigsten, Übersetzungen, deren Bausteinkennungen
von der Grundform abweichen. Zuletzt lädt sie jeden Kurs einmal über die
Laufzeit, damit auch dieser Weg geprüft ist.

## Eigene Bausteine

Ein Baustein ist eine Laufzeitklasse, die `IContentBlock` umsetzt, plus ein
Eintrag in `BlockFactory`, der den Typnamen aus dem JSON darauf abbildet.
Mehr ist nicht nötig: Seit dem Wegfall des Graph-Editors gibt es kein
zweites, editorseitiges Gegenstück mehr zu pflegen.

## Lesbare Tafeln in VR

In einer Brille gibt es nur Weltflächen: Canvas im Modus **World Space**.
Bildschirm-Canvases (Screen Space) sieht dort niemand. Ob ein Text lesbar
ist, hängt am Sehwinkel, nicht an der Schriftgröße im Inspector.

### Die Faustregel

Für heutige Brillen mit rund 20 bis 25 Bildpunkten je Grad:

- **Versalhöhe mindestens 7 mm je Meter Abstand** (7 mrad, etwa 0,4 Grad).
  Darunter wird ein Großbuchstabe mit weniger als zehn Bildpunkten
  gezeichnet und flimmert bei jeder Kopfbewegung.
- **Fließtext bequem ab 10 mm je Meter** (10 mrad, etwa 0,6 Grad).
- Bei TextMeshPro ist die Versalhöhe etwa 0,7 der Schriftgröße.

Beispiel: Eine Tafel mit Maßstab 0,001 (eine Canvas-Einheit ist ein
Millimeter) in 1 m Abstand braucht mindestens Schriftgröße 10, für
Fließtext 14. In 2 m das Doppelte. Ein häufiger Fehler ist eine Tafel, die
im Editor aus der Nähe gut aussieht und in 1,5 m mit Schriftgröße 8 hängt.

### Der Helfer im Paket

`ReadableWorldPanel` an die Canvas hängen und den Leseabstand eintragen. Beim
Aktivieren (im Editor über das Kontextmenü **Apply now**):

- hebt es zu kleine Schriften auf die Mindestgröße an, bei TextMeshPro mit
  Autosize auch `fontSizeMin`; verkleinert wird nie,
- setzt `CanvasScaler.dynamicPixelsPerUnit` auf mindestens 3, damit
  uGUI-`Text` nicht grob gerastert wird (TextMeshPro braucht das nicht),
- gibt TextMeshPro im Spiel ein geteiltes Material mit etwas *Face Dilate*
  (Vorgabe 0,1). Etwas kräftigere Glyphen flimmern weniger als dünne.
- Die Rechnung steht auch für eigenen Code bereit:
  `ReadableWorldPanel.MinimumFontSize(maßstab, abstand)`.

### Was der Helfer nicht ersetzen kann

- **Schrift:** keine sehr dünnen Schnitte (Light, Thin). Regular oder Medium.
  Im Font Asset ein Atlas mit ausreichendem Abstand (Padding etwa 10 % der
  Sampling-Größe), sonst schneidet Dilate die Ränder ab.
- **Hintergründe:** keine 1-px-Linien, Haarlinien-Rahmen oder feinen Muster.
  Sie kippen zwischen zwei Bildpunkten hin und her. Flächen und Ränder ab
  etwa 2 mm je Meter Abstand. Texturen für Weltflächen mit Mipmaps
  importieren; das Hintergrundbild dieses Pakets tut das ab 0.2.0.
- **Kontrast:** heller Text auf dunkler, nicht ganz schwarzer Fläche ist in
  den meisten Brillen ruhiger als umgekehrt.

### Einstellungen im Projekt, außerhalb des Pakets

- **MSAA 4x** im Render-Pipeline-Asset (URP: *Quality › Anti Aliasing*).
  Glättet die Kanten von Flächen und Knöpfen; Schrift glättet TextMeshPro
  selbst.
- **Render Scale 1.0**, nicht darunter. Eine kleinere Skala spart Leistung
  und kostet genau die Bildpunkte, die Schrift braucht.
- Auf Quest: Eye-Buffer-Auflösung nicht senken, *Foveated Rendering*
  höchstens niedrig, weil Tafeln am Rand des Blickfelds sonst unscharf
  werden.
- Tafeln nicht dauernd minimal bewegen (etwa durch Nachführen am Kopf mit
  Zittern); ruhende Flächen flimmern am wenigsten.

**Fortgeschritten:** OpenXR *Composition Layers* (Paket
`com.unity.xr.compositionlayers`) zeichnen eine Tafel als eigene Ebene, die
der Compositor direkt in Bildschirmauflösung abtastet, ohne den Umweg über
das Augenbild. Text wird damit sichtbar schärfer. Das ist eine eigene
Abhängigkeit und lohnt sich für Tafeln mit viel Text; dieses Paket setzt sie
nicht voraus.

## Abhängigkeiten

- **Newtonsoft.Json** (`com.unity.nuget.newtonsoft-json`): Einlesen
- **uGUI / TextMeshPro** (`com.unity.ugui`): Laufzeitoberfläche
- **UnityWebRequest** (`com.unity.modules.unitywebrequest`, eingebautes
  Modul): `StreamingAssets` auf Android lesen

Alle kommen von Unity. Fremdsoftware enthält das Paket nicht.

## Lizenz

Code: [Apache-2.0](LICENSE.md). Copyright 2026 Hochschule Karlsruhe (HKA),
Institute of Materials and Processes (IMP).

Kursinhalte sind getrennt lizenziert. Logos liefert dieses Paket **keine**
mit: Marken gehören nicht in eine Bibliothek, die andere weiterverwenden
sollen. `RuntimeContentUI` nimmt ein eigenes Logo entgegen, und leere Felder
sind der Normalfall. `LICENSE.md` sagt beides ausführlich.

## Herkunft

Entstanden im Rahmen des Projekts SMART-XR und daraus herausgelöst, weil sich
das System auch unabhängig davon einsetzen lässt.

SMART-XR wurde gefördert durch **Open Call 2** des MASTER-Projekts
([master-xr.eu](https://master-xr.eu/)), das seine Mittel aus dem Programm
Horizont Europa der Europäischen Union unter der Fördervereinbarung
Nr. 101093079 erhielt. Das Hauptprojekt liegt unter
[florian-reuter-imp/SMART-XR](https://github.com/florian-reuter-imp/SMART-XR).

Geprüft gegen Unity **6000.0** und **6000.3**.
