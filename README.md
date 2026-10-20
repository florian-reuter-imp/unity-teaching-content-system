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
https://github.com/florian-reuter-imp/unity-teaching-content-system.git#v0.1.0
```

Oder als Eintrag in `Packages/manifest.json`:

```json
"com.github.hka-imp.tcs": "https://github.com/florian-reuter-imp/unity-teaching-content-system.git#v0.1.0"
```

Das Versionsschild am Ende gehört dazu. Ohne es zieht jeder Klon einen
beliebigen Stand, und das Projekt ist nicht mehr reproduzierbar.

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

## Abhängigkeiten

- **Newtonsoft.Json** (`com.unity.nuget.newtonsoft-json`): Einlesen
- **uGUI / TextMeshPro** (`com.unity.ugui`): Laufzeitoberfläche

Beide kommen von Unity. Fremdsoftware enthält das Paket nicht.

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
