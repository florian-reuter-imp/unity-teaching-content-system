using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TeachingContentSystem.Runtime.Core;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Blocks
{
    /// <summary>
    /// Laesst mehrere Bausteine gleichzeitig laufen.
    ///
    /// NICHT FERTIG, UND ZWAR AN EINER BESTIMMTEN STELLE
    /// ------------------------------------------------
    /// Im JSON stehen unter <c>"children"</c> die Kennungen anderer
    /// Bausteine. Um daraus Bausteine zu machen, braucht es die Zuordnung
    /// Kennung nach Definition des ganzen Kapitels.
    /// <see cref="BlockFactory.Create"/> bekommt aber nur den einen Baustein
    /// uebergeben, nicht das Kapitel. Dieser Baustein kann seine Kinder
    /// deshalb nicht selbst bauen.
    ///
    /// Das zu aendern heisst, der Fabrik einen Aufloeser mitzugeben und ihn
    /// durch den ContentManager zu fuehren: ein Eingriff in oeffentliche
    /// Schnittstellen, der sich nicht nebenbei machen laesst.
    ///
    /// Bis dahin ist dieser Baustein <b>wirkungslos</b> und meldet das beim
    /// Anlegen, einmal und mit seiner Kennung. Vorher stand hier eine
    /// Warnung je Kind, ohne die Kennung des Bausteins und ohne zu sagen,
    /// woran es fehlt.
    ///
    /// Wer parallele Inhalte braucht, legt sie vorerst als eigene Bausteine
    /// nebeneinander, oder benutzt einen <c>SceneAction</c>-Baustein, der
    /// mehrere Dinge in einem Handler anstoesst.
    /// </summary>
    public class CompositeBlockRuntime : IContentBlock
    {
        private readonly RuntimeContentBlock _data;
        private readonly List<IContentBlock> _children = new List<IContentBlock>();
        private readonly List<string> _childIds = new List<string>();

        public string Id => _data.Id;

        /// <summary>0: Das Kapitel wartet, bis die Anwendung weiterschaltet.</summary>
        public float Duration => 0f;

        /// <summary>
        /// Die Kennungen, die im JSON stehen. Oeffentlich, damit eine
        /// Anwendung sie selbst aufloesen kann, solange dieser Baustein es
        /// nicht kann.
        /// </summary>
        public IReadOnlyList<string> ChildIds => _childIds;

        public CompositeBlockRuntime(RuntimeContentBlock data)
        {
            _data = data;

            if (data.Data.TryGetValue("children", out object rohwert) && rohwert != null)
            {
                if (rohwert is JArray feld)
                    foreach (JToken t in feld) _childIds.Add(t?.ToString() ?? string.Empty);
                else if (rohwert is IEnumerable<object> folge)
                    foreach (object o in folge) _childIds.Add(o?.ToString() ?? string.Empty);
            }

            if (_childIds.Count > 0)
                Debug.LogWarning(
                    $"[TCS] Composite-Baustein '{Id}' nennt {_childIds.Count} Kinder, "
                    + "kann sie aber nicht anlegen: Die Fabrik kennt nur den einzelnen "
                    + "Baustein, nicht das Kapitel, in dem die Kennungen stehen. Der "
                    + "Baustein bleibt wirkungslos. Die Kennungen stehen in ChildIds "
                    + "und lassen sich von der Anwendung selbst aufloesen.");
        }

        // Die drei Durchreichen bleiben, damit dieser Baustein sofort
        // arbeitet, sobald Kinder von aussen eingehaengt werden koennen.

        public void Enter()
        {
            foreach (IContentBlock child in _children) child.Enter();
        }

        public void Update(float deltaTime)
        {
            foreach (IContentBlock child in _children) child.Update(deltaTime);
        }

        public void Exit()
        {
            foreach (IContentBlock child in _children) child.Exit();
        }
    }
}
