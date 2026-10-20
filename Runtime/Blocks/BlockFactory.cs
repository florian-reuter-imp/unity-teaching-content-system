using UnityEngine;
using TeachingContentSystem.Runtime.Core;
using TeachingContentSystem.Runtime.Blocks;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Creates runtime block instances from JSON-loaded RuntimeContentBlock data.
    /// </summary>
    public static class BlockFactory
    {
        public static IContentBlock Create(RuntimeContentBlock data)
        {
            switch (data.Type)
            {
                case "Slide":
                    return new SlideBlockRuntime(data);

                // Ein Baustein, zwei Typnamen. Der Unterschied steckt nicht
                // im Verhalten, sondern allein im Weiterblaettern, und das
                // ist jetzt ein Vorgabewert statt eines eigenen Typs:
                // eine Szenenhandlung blaettert selbst weiter, eine Uebung
                // wartet auf den Nutzer. Im JSON uebersteuerbar.
                case "SceneAction":
                    return new SceneActionBlockRuntime(data, autoAdvanceDefault: true);

                case "Interaction":
                    return new SceneActionBlockRuntime(data, autoAdvanceDefault: false);

                case "Quiz":
                    return new QuizBlockRuntime(data);

                case "Composite":
                    return new CompositeBlockRuntime(data);

                // Hier stand ein Fall "Assistant", dessen einzige Zeile
                // auskommentiert war: Ein solcher Baustein fiel durch zum
                // Standardfall und meldete "Unknown block type: Assistant",
                // obwohl der Typname in dieser Fabrik stand. Die Umsetzung
                // hing an einer Komponente aus einem fremden Projekt, die
                // hier nicht vorliegt. Beides ist entfallen; die Meldung
                // sagt jetzt die Wahrheit.

                default:
                    Debug.LogError($"Unknown block type: {data.Type}");
                    return new ErrorBlockRuntime(data);
            }
        }
    }
}
