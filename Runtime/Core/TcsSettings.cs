using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Verhalten des Systems, im Code festgelegt, wie
    /// <see cref="TcsContentLocations"/>. Einmal beim Start setzen:
    ///
    /// <code>
    /// TcsSettings.WrapAtEnd = true;   // nach dem letzten Baustein wieder von vorn
    /// </code>
    /// </summary>
    public static class TcsSettings
    {
        /// <summary>
        /// Was "weiter" am letzten Baustein tut. Vorgabe: nichts, der Kurs
        /// ist zu Ende, die Oberflaeche graut "weiter" aus und meldet das
        /// Ende (<see cref="Navigation.NavigationEvents.OnCourseEndReached"/>).
        /// Mit true geht es zurueck zum ersten Baustein, und "zurueck" am
        /// ersten Baustein fuehrt zum letzten.
        /// </summary>
        public static bool WrapAtEnd { get; set; }

        /// <summary>Alles zurueck auf die Vorgabe. Fuer Tests und Neustarts.</summary>
        public static void Reset()
        {
            WrapAtEnd = false;
        }

#if UNITY_EDITOR
        // Ohne Domain Reload bliebe eine Einstellung aus der letzten
        // Spielsitzung stehen.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Reset();
#endif
    }
}
