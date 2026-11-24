using System.Collections.Generic;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Dateien im Arbeitsspeicher, nach relativem Pfad. Fuer Tests, fuer
    /// Inhalte, die eine Anwendung selbst erzeugt, und fuer Dateien, die
    /// vorab asynchron geholt wurden (etwa ueber UnityWebRequest auf einer
    /// Plattform, die nicht synchron lesen kann).
    /// </summary>
    public class MemorySource : ICourseSource
    {
        private readonly Dictionary<string, string> files = new Dictionary<string, string>();

        public string Name { get; }

        public MemorySource(string name = "Memory")
        {
            Name = name;
        }

        /// <summary>Legt eine Datei ab oder ersetzt sie.</summary>
        public MemorySource Set(string relativePath, string text)
        {
            files[ContentResolver.Normalize(relativePath)] = text;
            return this;
        }

        public bool Remove(string relativePath) => files.Remove(ContentResolver.Normalize(relativePath));

        public void Clear() => files.Clear();

        public int Count => files.Count;

        public bool TryReadText(string relativePath, out string text)
            => files.TryGetValue(ContentResolver.Normalize(relativePath), out text);

        public override string ToString() => $"Memory({Name}, {files.Count} files)";
    }
}
