using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Die Dateien, die mit dem Build ausgeliefert werden:
    /// <c>Assets/StreamingAssets</c>, auf Wunsch ein Unterordner davon.
    ///
    /// Das ist der einzige Ort, an den ein Build Dateien legen kann. Die
    /// erste Fassung jedes Kurses gehoert deshalb hierher; andere Quellen
    /// ueberschreiben auf dem Geraet.
    ///
    /// WIE GELESEN WIRD
    /// ----------------
    /// Wo StreamingAssets ein gewoehnlicher Ordner ist (Editor, Windows,
    /// macOS, Linux, iOS), ueber das Dateisystem. Auf Android und Quest
    /// steckt der Ordner in der APK, <c>Application.streamingAssetsPath</c>
    /// ist dann eine <c>jar:file://</c>-Adresse, und <c>File.ReadAllText</c>
    /// sieht nichts. Dort wird ueber <see cref="UnityWebRequest"/> gelesen
    /// und auf das Ende gewartet; fuer Dateien aus der eigenen APK dauert
    /// das Millisekunden.
    ///
    /// Auf WebGL geht synchrones Warten nicht (ein einziger Thread, die
    /// Anfrage laeuft ueber den Browser). Dort die Dateien vorab
    /// asynchron holen und ueber eine <see cref="MemorySource"/> anbieten.
    /// </summary>
    public class StreamingAssetsSource : ICourseSource
    {
        /// <summary>Unterordner unter StreamingAssets, leer fuer den Ordner selbst.</summary>
        public string Subfolder { get; }

        public string Name => string.IsNullOrEmpty(Subfolder) ? "StreamingAssets" : "StreamingAssets/" + Subfolder;

        public StreamingAssetsSource(string subfolder = null)
        {
            Subfolder = string.IsNullOrEmpty(subfolder) ? "" : ContentResolver.Normalize(subfolder).TrimEnd('/');
        }

        /// <summary>
        /// Pfad oder Adresse einer Datei, mit Schraegstrichen. Auf Android
        /// eine <c>jar:file://</c>-Adresse, sonst ein Dateipfad.
        /// </summary>
        public string Location(string relativePath)
        {
            string root = Application.streamingAssetsPath;
            if (!string.IsNullOrEmpty(Subfolder)) root += "/" + Subfolder;
            return root + "/" + ContentResolver.Normalize(relativePath);
        }

        public bool TryReadText(string relativePath, out string text)
        {
            text = null;
            string location = Location(relativePath);

            if (!location.Contains("://"))
            {
                if (!File.Exists(location)) return false;
                text = File.ReadAllText(location);
                return true;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogError("[TCS] StreamingAssets cannot be read synchronously on WebGL. "
                           + "Fetch the files asynchronously and offer them through a MemorySource.");
            return false;
#else
            using (UnityWebRequest request = UnityWebRequest.Get(location))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    System.Threading.Thread.Sleep(1);

                if (request.result != UnityWebRequest.Result.Success)
                    return false;

                text = request.downloadHandler.text;
                return true;
            }
#endif
        }

        public override string ToString() => Name;
    }
}
