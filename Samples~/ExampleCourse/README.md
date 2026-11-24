# Example Course

A small course in two chapters, in English and German. It shows the file
layout, one chapter with plain slides and one with a block of each type
(`Slide`, `Quiz`, `Interaction`, `SceneAction`).

## Where to put it

Course files are read from `StreamingAssets`, which is the only folder a
build can carry as plain files. After importing the sample:

1. Move the folder `TCS` from this sample into `Assets/StreamingAssets/`, so
   that the outline ends up at
   `Assets/StreamingAssets/TCS/ExampleCourse/course.json`.
   If `Assets/StreamingAssets/TCS` already exists, move only `ExampleCourse`
   into it.
2. Add `TeachingContentSystemRuntime` and `ContentManager` to a GameObject
   and set the path to `TCS/ExampleCourse/course.json`.
3. Optional: run **Tools > Teaching Content System > Kursdateien pruefen**
   to check the files.

German is picked from `chapters/*.de.json` when `TcsLanguage.Current` is
`de`; every other language falls back to the English files.

The `SceneAction` block asks the scene for an action called `SpawnObject`.
Without a matching `SceneActionHandler` in your scene it shows a notice and
the course continues. That is expected in this sample.

## Licence

The course texts are licensed under
[Creative Commons Attribution 4.0 International (CC BY 4.0)](https://creativecommons.org/licenses/by/4.0/),
not under the Apache licence of the package code. Attribution:

    Teaching Content System example course, Institute of Materials and
    Processes (IMP), Hochschule Karlsruhe (HKA), CC BY 4.0

The sample contains only these texts: no images, fonts, models or other
third-party material.
