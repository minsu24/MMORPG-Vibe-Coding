using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SpiderCaveDripSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/TutorialMap/SpiderCave.unity";
    private const string ClipPath = "Assets/_Project/Audio/Ambience/freesound_community-droplets-in-a-cave-6785.mp3";

    [MenuItem("Eastern Fantasy/Spider Cave/Sync Water Drops To Existing Audio")]
    private static void SetupFromMenu() => Debug.Log(Apply());

    public static string Apply()
    {
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (EditorApplication.isPlaying || (!opened && scene.isDirty))
            throw new InvalidOperationException("Save SpiderCave and exit Play Mode before setup.");
        var times = AnalyzeRecording();
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var ambience = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<IntermittentAmbientAudio>(true))
                .First(a => a.GetComponent<AudioSource>().clip != null && AssetDatabase.GetAssetPath(a.GetComponent<AudioSource>().clip) == ClipPath);
            var drips = ambience.GetComponent<SpiderCaveDrips>();
            if (drips == null) drips = ambience.gameObject.AddComponent<SpiderCaveDrips>();
            drips.ambience = ambience;
            drips.impactTimes = times;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            System.IO.Directory.CreateDirectory("Artifacts/SpiderCaveDrips");
            System.IO.File.WriteAllText("Artifacts/SpiderCaveDrips/DetectedImpacts.txt", string.Join("\n", times.Select(t => t.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))));
            return "Connected water effects to " + times.Length + " detected drip attacks in the existing recording. Audio settings preserved.";
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static float[] AnalyzeRecording()
    {
        var importer = (AudioImporter)AssetImporter.GetAtPath(ClipPath);
        var original = importer.defaultSampleSettings;
        try
        {
            var settings = original;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            clip.LoadAudioData();
            var samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0)) throw new InvalidOperationException("Cannot analyze drip recording.");
            int hop = Mathf.Max(1, clip.frequency / 100);
            var energy = new float[clip.samples / hop];
            float maximum = 0;
            for (int i = 0; i < energy.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < hop; j++)
                for (int channel = 0; channel < clip.channels; channel++)
                {
                    float sample = samples[(i * hop + j) * clip.channels + channel];
                    sum += sample * sample;
                }
                energy[i] = Mathf.Sqrt((float)(sum / (hop * clip.channels)));
                maximum = Mathf.Max(maximum, energy[i]);
            }
            var times = new List<float>();
            float lastPeak = -1;
            for (int i = 10; i < energy.Length - 10; i++)
            {
                if (energy[i] < maximum * 0.18f) continue;
                bool peak = true;
                for (int j = i - 10; j <= i + 10; j++) if (energy[j] > energy[i]) { peak = false; break; }
                float peakTime = i * hop / (float)clip.frequency;
                if (!peak || peakTime - lastPeak < 0.32f) continue;
                // Align to the attack, rather than the slightly later loudest echo.
                int onset = i;
                while (onset > i - 8 && energy[onset - 1] > energy[i] * 0.25f) onset--;
                times.Add(onset * hop / (float)clip.frequency);
                lastPeak = peakTime;
            }
            if (times.Count < 8) throw new InvalidOperationException("Too few distinct drip attacks detected.");
            return times.ToArray();
        }
        finally
        {
            importer.defaultSampleSettings = original;
            importer.SaveAndReimport();
        }
    }
}
