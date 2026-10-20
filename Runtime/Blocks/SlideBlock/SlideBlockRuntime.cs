using System;
using TeachingContentSystem.Runtime.Core;

namespace TeachingContentSystem.Runtime.Blocks
{
    /// <summary>
    /// Haelt den Text und optional den Namen eines Bildes fuer eine Folie.
    ///
    /// DIE ANZEIGE GEHOERT NICHT HIERHER
    /// ---------------------------------
    /// Dieser Baustein zeigt nichts an, und das ist Absicht: Das Paket kennt
    /// den Inhalt, die Anwendung kennt ihre Darstellung. Eine Folie an einer
    /// Weltflaeche in VR sieht anders aus als eine am Schreibtisch.
    ///
    /// Eine Anwendung liest <see cref="Text"/> und <see cref="ImageName"/>
    /// und baut daraus ihre Anzeige. Weitergeschaltet wird ueber
    /// <see cref="Duration"/>: Steht im JSON eine Dauer, blaettert das
    /// Kapitel von selbst weiter; steht keine da, ist sie 0 und das Kapitel
    /// wartet auf <c>ContentManager.GoToNext()</c>.
    /// </summary>
    public class SlideBlockRuntime : IContentBlock
    {
        private readonly RuntimeContentBlock _data;

        public string Id => _data.Id;
        public float Duration => _data.Data.ContainsKey("duration")
            ? Convert.ToSingle(_data.Data["duration"])
            : 0f;

        private readonly string _text;
        private readonly string _imageName;

        /// <summary>Der Folientext aus dem JSON, nie null.</summary>
        public string Text => _text;

        /// <summary>Der Bildname aus dem JSON, oder null wenn keiner angegeben ist.</summary>
        public string ImageName => _imageName;

        public SlideBlockRuntime(RuntimeContentBlock data)
        {
            _data = data;
            _text = data.Data.ContainsKey("text") ? data.Data["text"]?.ToString() : "";
            _imageName = data.Data.ContainsKey("image") ? data.Data["image"]?.ToString() : null;
        }

        // Enter, Update und Exit sind hier leer, weil es nichts zu tun gibt:
        // Der Inhalt steht schon im Konstruktor fest und aendert sich nicht.
        // Die Anzeige haengt sich an den ContentManager, nicht hierher.

        public void Enter() { }

        public void Update(float deltaTime) { }

        public void Exit() { }
    }
}
