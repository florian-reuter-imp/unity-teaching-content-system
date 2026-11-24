# Änderungen

Dieses Projekt folgt [Semantic Versioning](https://semver.org/lang/de/).

## [0.2.0]

### Neu: Wo die Kurse liegen, im Code festgelegt
- `ICourseSource` mit `StreamingAssetsSource`, `FolderSource` und
  `MemorySource`; `TcsContentLocations.Configure(...)` und
  `ConfigureFor(platform, ...)` legen eine geordnete Liste fest, auf Wunsch
  je Plattform. Vorgabe bleibt StreamingAssets allein.
- Aufgeloest wird je Datei: Die erste Quelle, die die Datei hat, gewinnt.
  Ein Ordner auf dem Geraet kann so einzelne Kapitel ersetzen. Die
  Sprachfassung (`*.de.json`) wird je Quelle gesucht, wie bei
  `TcsLanguage.Localize`.
- `StreamingAssetsSource` liest auf Android und Quest ueber
  `UnityWebRequest`. Vorher las `CourseLoader` mit `File.ReadAllText`, und
  das findet in einer APK nichts.
- `ContentSeeder.SeedCourse` fuellt einen beschreibbaren Ordner beim ersten
  Start aus StreamingAssets und ueberschreibt nie eine vorhandene Datei.
- `CourseLoader.LoadCourse(pfad)` und `ChapterLoader.LoadChapter(pfad)`
  bleiben, wie sie waren, und laufen jetzt ueber die Quellen. Ein absoluter
  Pfad wird weiter direkt gelesen. Neu sind Ueberladungen mit einem
  `ContentResolver` und `ChapterLoader.FromJson`.
- Die Kurspruefung im Editor laedt ausdruecklich aus StreamingAssets, damit
  sie die ausgelieferten Dateien prueft und nicht eine Ueberschreibung.

### Kursende
- `ContentManager.GoToNext()` am letzten Baustein schreibt keinen Fehler mehr
  ins Log. Vorher stand dort `has no NextIds`, bei einer Folie mit Dauer am
  Kursende sogar in jedem Bild.
- Neu: `CanGoNext`, `CanGoBack`, `IsAtEnd`, `IsAtStart`, das Ereignis
  `CourseEndReached` und `NavigationEvents.OnCourseEndReached`, einmal je
  Besuch des letzten Bausteins.
- `TcsSettings.WrapAtEnd` (Vorgabe `false`): nach dem Ende wieder von vorn,
  und "zurueck" am Anfang fuehrt zum Ende.
- `GoBack()` ohne Verlauf warnt nicht mehr, es tut nichts.
- `RuntimeContentUI` graut "weiter" am Ende und "zurueck" am Anfang aus.
  Neue optionale Felder `nextButton`, `backButton`, `contentManager`; leer
  findet die Oberflaeche die Knoepfe ueber deren OnClick selbst.

### Lesbare Tafeln in VR
- `ReadableWorldPanel` fuer Weltflaechen: Mindestschrift aus Leseabstand und
  Canvas-Massstab, `dynamicPixelsPerUnit`, etwas Dilate fuer TextMeshPro.
  Die README nennt die Faustregel (Versalhoehe mindestens 7 mm je Meter
  Abstand) und die Einstellungen ausserhalb des Pakets.
- Das Hintergrundbild `UI_Background.png` hat Mipmaps, damit es an einer
  entfernten Tafel nicht flimmert.

### Beispielkurs als Sample
- `Samples~/ExampleCourse` mit `samples`-Eintrag in der `package.json`: zwei
  Kapitel auf Englisch und Deutsch, Texte unter CC BY 4.0. Der Baustein vom
  Typ `Assistant` ist darin entfallen; den Typ gibt es seit 0.1.0 nicht mehr,
  er haette beim Abspielen "Unknown block type" gemeldet.

### Abhaengigkeiten
- `com.unity.modules.unitywebrequest` (eingebautes Modul) ist jetzt
  angegeben, weil `StreamingAssetsSource` es benutzt.

## [0.1.0] — 20.10.2026

### Vor der Veroeffentlichung durchgesehen

Das Paket wurde Zeile fuer Zeile daraufhin gelesen, was oeffentlich stehen
darf und was ein Fremder davon versteht. Fuenf Dinge kamen dabei heraus.

**Der Assistant-Baustein ist entfallen.** Die Datei war vollstaendig
auskommentiert und hing an einer Komponente aus einem fremden Projekt, die
hier nie vorlag. In `BlockFactory` stand ein Fall
`"Assistant"`, dessen einzige Zeile ebenfalls auskommentiert war: Ein solcher
Baustein fiel durch zum Standardfall und meldete „Unknown block type:
Assistant", obwohl der Typname in der Fabrik stand. Beides ist weg, die
Meldung sagt jetzt die Wahrheit.

**Der Quizbaustein hatte zwei echte Fehler**, die im Projekt nie auffielen:

- `Data["questions"] as List<string>` lieferte **immer null**. `Data` ist ein
  `Dictionary<string, object>`, das Newtonsoft fuellt, und eine JSON-Liste
  kommt darin als `JArray` an. Das Quiz hatte also nie Inhalt, ohne sich zu
  beschweren. `ChapterLoader` machte es daneben richtig vor.
- Der Indexer `Data["questions"]` **wirft**, wenn der Schluessel fehlt. Ein
  Quizblock ohne Fragen riss damit den Konstruktor mit.

Gelesen wird jetzt beides robust, ein unvollstaendiger Block meldet sich mit
seiner Kennung statt abzustuerzen, und Fragen, Antworten und `IsCorrect` sind
oeffentlich: Vorher waren sie privat und wurden nirgends benutzt, der
Baustein war also auch bei heilen Daten nicht verwendbar.

**`Slide` und `Quiz` sagen jetzt, dass die Anzeige nicht ihre Sache ist.**
Vorher stand dort `// TODO: Show quiz UI`, was wie ein vergessener Rest
aussieht. Es ist aber Absicht: Das Paket kennt den Inhalt, die Anwendung
kennt ihre Darstellung. `Text`, `ImageName`, `Questions` und `Answers` sind
oeffentlich, weitergeschaltet wird ueber `ContentManager.GoToNext()`.

**`Composite` ist wirkungslos und sagt das.** Er kann seine Kinder nicht
anlegen, weil `BlockFactory.Create` nur den einzelnen Baustein bekommt und
nicht das Kapitel, in dem die Kennungen stehen. Statt einer Warnung je Kind
ohne Zusammenhang gibt es jetzt eine Meldung mit Kennung, die sagt woran es
fehlt; die Kennungen stehen in `ChildIds` und lassen sich von aussen
aufloesen. Die README fuehrt alle fuenf Bausteintypen mit ihrem Stand auf.

**Die Beispielszene wird vorerst nicht mitgeliefert.** `Samples~/ExampleScenes`
enthaelt 88 Ueberschreibungen an realvirtuals eigenem Prefab und war damit
ohne dieses kostenpflichtige Paket nicht lauffaehig: ausgerechnet in einem
Paket, dessen Zusage „keine fremde Abhaengigkeit" lautet. Dazu fehlten allen
drei Dateien die `.meta`, sodass ihre Verweise untereinander beim Import
gebrochen waeren. Der `samples`-Eintrag in der `package.json` ist entfernt;
die Szene kommt wieder, wenn sie ohne Fremdbestandteile neu gebaut ist.

### Die Logos sind raus
Das Paket enthielt in `Branding~` das Logo der Hochschule und das des
Instituts. Sie sind entfallen.

Der Grund ist nicht, dass etwas daran falsch waere, sondern dass Marken nicht
in eine Bibliothek gehoeren, die andere weiterverwenden sollen: Apache-2.0
erteilt in Abschnitt 6 keine Markenrechte, aber ein mitgeliefertes Logo in
einem Apache-Repo liest trotzdem jeder als mitlizenziert. Was eine
Markenrichtlinie erlaubt, entscheidet ausserdem nicht ein Paket.

Es kostet nichts: Der Ordner trug nur die Vektorquelle und wurde von Unity
ohnehin nie importiert (Tilde im Namen). Das tatsaechlich angezeigte Bild lag
immer schon in der Anwendung. `RuntimeContentUI` nimmt ueber `SetBranding`
oder die beiden Bildfelder jedes beliebige Logo entgegen, und leere Felder
sind der Normalfall.

### Unity
`"unity"` steht auf `6000.0` statt `2022.3`. Die alte Angabe war nie
geprueft.


Erste Fassung als eigenständiges Paket. Zuvor lag der Code im
SMART-XR-Projekt; herausgelöst, damit er unabhängig davon nutzbar ist.

### Enthalten
- Kurse als JSON-Dateien: eine Gliederung je Kurs, eine Datei je Kapitel,
  darin eine geordnete Liste von Bausteinen
- Bausteine für Folien, Quizfragen, Interaktionen, Szenenaktionen,
  zusammengesetzte Blöcke und einen Assistenten
- Mehrsprachigkeit über Dateivarianten (`kapitel.de.json` neben
  `kapitel.json`), Sprachwechsel im laufenden Kurs
- Laufzeit zum Laden und Abspielen eines Kurses
- Editor-Prüfung der Kursdateien unter *Tools › Teaching Content System*
- Erfassung von Kennzahlen zum Lernfortschritt

### Entfernt gegenüber dem Stand im SMART-XR-Projekt
- **Graph-Editor und xNode.** Es ist nie ein Kurs über den Graphen entstanden;
  alle Inhalte wurden als JSON geschrieben. Der Graph war damit eine zweite
  Quelle der Wahrheit ohne Nutzen — 5396 Zeilen fremder und 2828 Zeilen
  eigener Editor-Code um eine Laufzeit von 1561 Zeilen. Geblieben ist eine
  Prüfung der Dateien. Das Paket hat seitdem keine fremde Abhängigkeit mehr.
- **`JsonLoader`** samt dem alten Format `graph.json`, das niemand mehr lud.
- **`Documentation~`**: leere Dateien, Entwurfsmitschriften und ein obsoletes
  Aufbauskript. Was gilt, steht im README.
