using System;
using UnityEngine;

namespace EasternFantasy.Saving
{
    // GUIDs remain stable when assets are renamed or moved. References also keep them in builds.
    public sealed class SaveAssetCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry { public string id; public UnityEngine.Object asset; }
        public Entry[] entries = Array.Empty<Entry>();
        private static SaveAssetCatalog instance;
        private static SaveAssetCatalog Instance => instance != null ? instance : (instance = Resources.Load<SaveAssetCatalog>("SaveAssetCatalog"));
        public static string Id(UnityEngine.Object asset)
        {
            if (asset == null || Instance == null) return null;
            foreach (var entry in Instance.entries) if (entry.asset == asset) return entry.id;
            throw new InvalidOperationException("Save catalog has no entry for " + asset.name);
        }
        public static T Resolve<T>(string id) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(id) || Instance == null) return null;
            foreach (var entry in Instance.entries) if (entry.id == id) return entry.asset as T;
            return null;
        }
    }
}
