using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Scene routing already uses unique scene names. Keep persistent identities independent of folders.
public static class SceneStateIdentity
{
    public static string Key(Scene scene) => scene.name;

    public static string NormalizeEncounterKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        int separator = key.IndexOf(':');
        if (separator <= 0) return key;
        string scenePath = key.Substring(0, separator);
        if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) return key;
        int nameStart = scenePath.LastIndexOf('/') + 1;
        string sceneName = scenePath.Substring(nameStart, scenePath.Length - nameStart - ".unity".Length);
        return sceneName + key.Substring(separator);
    }

    public static IEnumerable<string> NormalizeEncounterKeys(IEnumerable<string> keys)
    {
        if (keys == null) yield break;
        foreach (string key in keys)
            if (!string.IsNullOrEmpty(key)) yield return NormalizeEncounterKey(key);
    }
}
