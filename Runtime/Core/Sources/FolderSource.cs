using System;
using System.IO;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Ein gewoehnlicher Ordner im Dateisystem, etwa
    /// <c>Application.persistentDataPath</c> oder ein beliebiger absoluter
    /// Pfad. Gedacht fuer Dateien, die auf dem Geraet ersetzt werden sollen,
    /// ohne neu zu bauen.
    /// </summary>
    public class FolderSource : ICourseSource
    {
        /// <summary>Der Wurzelordner, absolut.</summary>
        public string Root { get; }

        public string Name { get; }

        public FolderSource(string root, string name = null)
        {
            if (string.IsNullOrEmpty(root))
                throw new ArgumentException("A folder source needs a root folder.", nameof(root));
            Root = root;
            Name = name ?? root;
        }

        /// <summary>
        /// Ein Ordner unter <c>Application.persistentDataPath</c>; ohne
        /// Angabe der Ordner selbst. Auf Android und Quest ist das der Ort,
        /// den eine Anwendung beschreiben darf.
        /// </summary>
        public static FolderSource PersistentData(string subfolder = null)
        {
            string root = string.IsNullOrEmpty(subfolder)
                ? Application.persistentDataPath
                : Path.Combine(Application.persistentDataPath, subfolder);
            return new FolderSource(root, "persistentDataPath" + (string.IsNullOrEmpty(subfolder) ? "" : "/" + subfolder));
        }

        /// <summary>Der volle Pfad zu einer relativen Datei in diesem Ordner.</summary>
        public string FullPath(string relativePath)
            => Path.Combine(Root, ContentResolver.Normalize(relativePath));

        public bool Exists(string relativePath)
            => File.Exists(FullPath(relativePath));

        public bool TryReadText(string relativePath, out string text)
        {
            text = null;
            string path = FullPath(relativePath);
            if (!File.Exists(path)) return false;
            text = File.ReadAllText(path);
            return true;
        }

        public override string ToString() => $"Folder({Root})";
    }
}
