using System.Collections;
using System.Collections.Generic;
using EasternFantasy.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Dungeon
{
    public sealed partial class DungeonRunController
    {
        private bool hasFailed;
        private bool resultAudioPlayed;
        private readonly Dictionary<AudioSource, float> resultMusicVolumes = new Dictionary<AudioSource, float>();

        // Called when the death screen is visible, after the death animation.
        public void NotifyFailureDisplayed()
        {
            if (IsCleared || leaving || hasFailed || !ContainsScene(SceneManager.GetActiveScene().name)) return;
            hasFailed = true;
            ui.HidePrompt();
            PlayResultAudio(false);
        }

        private IEnumerator ShowTimeoutFailure()
        {
            NotifyFailureDisplayed();
            SystemNoticeUI.Show("던전 실패: 제한 시간이 초과되었습니다.", this);
            // Let the result cue finish before loading mutes scene audio.
            yield return new WaitForSecondsRealtime(2f);
            ExitToVillage();
        }

        private void PlayResultAudio(bool cleared)
        {
            if (resultAudioPlayed) return;
            resultAudioPlayed = true;
            var clip = Resources.Load<AudioClip>(cleared ? "Audio/DungeonClear" : "Audio/DungeonFailure");
            if (clip != null)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.priority = 32;
                source.volume = cleared ? .8f : .65f;
                source.clip = clip;
                source.Play();
            }
            StartCoroutine(FadeResultMusic());
        }

        private IEnumerator FadeResultMusic()
        {
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                // Scene music is a looping 2D source named "Giant Farm BGM".
                // Keep combat and ambient loops at their existing volumes.
                if (!source.loop || source.spatialBlend > 0f) continue;
                string label = source.gameObject.name.ToLowerInvariant();
                if (!label.Contains("bgm") && !label.Contains("music")) continue;
                resultMusicVolumes[source] = source.volume;
            }
            float elapsed = 0f;
            while (elapsed < .5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float factor = Mathf.Lerp(1f, .25f, Mathf.Clamp01(elapsed / .5f));
                foreach (var pair in resultMusicVolumes)
                    if (pair.Key != null) pair.Key.volume = pair.Value * factor;
                yield return null;
            }
        }

        private void RestoreResultMusic()
        {
            foreach (var pair in resultMusicVolumes)
                if (pair.Key != null) pair.Key.volume = pair.Value;
            resultMusicVolumes.Clear();
        }
    }
}
