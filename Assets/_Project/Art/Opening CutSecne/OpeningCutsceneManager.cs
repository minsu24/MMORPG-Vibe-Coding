using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace EasternFantasy.Opening
{
    [DisallowMultipleComponent]
    public sealed class OpeningCutsceneManager : MonoBehaviour
    {
        public const string SceneName = "OpeningCutscene";
        private const string SeenKey = "Opening.Seen.v1";
        private const string SubtitlesKey = "Opening.Subtitles";
        private static bool replayRequested;
        private static readonly int CameraData = Shader.PropertyToID("_CameraData");
        private static readonly int EffectData = Shader.PropertyToID("_EffectData");
        private static readonly int WipeData = Shader.PropertyToID("_WipeData");

        [SerializeField] private OpeningSceneData[] scenes;
        [SerializeField] private string firstDestination = "MainScreen";
        [SerializeField] private string replayDestination = "MainScreen";
        [SerializeField] private bool allowNextScene = true;
        [SerializeField] private RawImage artwork;
        [SerializeField] private RawImage videoView;
        [SerializeField] private Material illustrationMaterial;
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private CanvasGroup subtitlePanel;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text subtitleButtonText;
        [SerializeField] private Button nextHitArea;
        [SerializeField] private VideoPlayer video;
        [SerializeField] private AudioSource videoAudio;
        [SerializeField] private AudioSource bgm;
        [SerializeField] private AudioSource narration;
        [SerializeField] private AudioSource sfx;

        private Material illustration;
        private bool advance;
        private bool skipAll;
        private bool finishing;
        private bool showSubtitles;
        private bool videoFailed;
        private bool videoEnded;
        private bool firstFrameReady;
        private int activeSubtitle = -2;
        private Vector2 subtitleRestPosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlaybackRequest() => replayRequested = false;

        public static void ReplayFromTitle()
        {
            if (replayRequested) return;
            if (!Application.CanStreamedLevelBeLoaded(SceneName))
            {
                Debug.LogError("빌드 씬 목록에 OpeningCutscene을 추가하세요.");
                return;
            }

            replayRequested = true;
            SceneManager.LoadScene(SceneName);
        }

        private IEnumerator Start()
        {
            bool replay = replayRequested;
            replayRequested = false;
            bool seen = PlayerPrefs.GetInt(SeenKey, 0) == 1;
            // 에디터에서 전용 씬을 직접 열고 재생하면 언제든 연출을 확인할 수 있습니다.
            bool preview = Application.isEditor && !replay;
            bool play = replay || preview || !seen;
            string destination = replay || (seen && !preview) ? replayDestination : firstDestination;
            fadePanel.alpha = 1f;

            if (!Application.CanStreamedLevelBeLoaded(destination))
            {
                Debug.LogError($"오프닝의 이동 대상 씬을 빌드 목록에 추가하세요. ({destination})", this);
                yield break;
            }

            if (play)
            {
                showSubtitles = PlayerPrefs.GetInt(SubtitlesKey, 1) == 1;
                AudioListener.volume = PlayerPrefs.GetFloat("Settings.MasterVolume", 1f);
                nextHitArea.gameObject.SetActive(allowNextScene);
                RefreshSubtitleButton();
                subtitleRestPosition = subtitleText.rectTransform.anchoredPosition;
                illustration = new Material(illustrationMaterial);
                artwork.material = illustration;
                video.frameReady += OnVideoFrame;
                video.loopPointReached += OnVideoEnd;
                video.errorReceived += OnVideoError;
                video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
                video.audioOutputMode = VideoAudioOutputMode.AudioSource;
                video.controlledAudioTrackCount = 1;
                video.SetTargetAudioSource(0, videoAudio);

                bool playedScene = false;
                foreach (OpeningSceneData scene in scenes)
                {
                    if (skipAll) break;
                    if (scene == null) continue;
                    advance = false;
                    yield return PlayScene(scene);
                    playedScene = true;
                }

                if (!replay && !preview && !seen && (playedScene || skipAll))
                {
                    PlayerPrefs.SetInt(SeenKey, 1);
                    PlayerPrefs.Save();
                }
            }

            finishing = true;
            bgm.Stop();
            yield return SceneManager.LoadSceneAsync(destination);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || finishing) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
                SkipAll();
            else if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame ||
                     keyboard.numpadEnterKey.wasPressedThisFrame)
                NextScene();
        }

        public void SkipAll() => skipAll = true;

        public void NextScene()
        {
            if (allowNextScene && !finishing) advance = true;
        }

        public void ToggleSubtitles()
        {
            showSubtitles = !showSubtitles;
            PlayerPrefs.SetInt(SubtitlesKey, showSubtitles ? 1 : 0);
            PlayerPrefs.Save();
            RefreshSubtitleButton();
        }

        private void RefreshSubtitleButton()
        {
            subtitleButtonText.text = showSubtitles ? "자막 켬" : "자막 끔";
        }

        private IEnumerator PlayScene(OpeningSceneData scene)
        {
            fadePanel.alpha = 1f;
            subtitlePanel.alpha = 0f;
            artwork.enabled = false;
            videoView.enabled = false;

            if (scene.media == OpeningSceneData.Media.Video && scene.video != null)
                yield return PrepareVideo(scene.video);
            if (advance || skipAll)
            {
                video.Stop();
                yield break;
            }

            bool playingVideo = scene.media == OpeningSceneData.Media.Video &&
                                firstFrameReady && !videoFailed && video.texture != null;
            float duration = scene.duration;
            float clipDuration = playingVideo ? (float)scene.video.length : 0f;
            if (playingVideo)
            {
                duration = Mathf.Max(duration, clipDuration);
                videoView.texture = video.texture;
                videoView.enabled = true;
            }
            else
            {
                video.Stop();
                ShowArtwork(scene);
            }

            if (!scene.keepBgm) bgm.Stop();
            if (scene.newBgm != null && (bgm.clip != scene.newBgm || !bgm.isPlaying))
            {
                bgm.clip = scene.newBgm;
                bgm.Play();
            }

            subtitleText.text = scene.subtitle;
            activeSubtitle = -2;
            bool voiceStarted = false;
            bool sfxStarted = false;
            double start = Time.unscaledTimeAsDouble;
            while (!advance && !skipAll)
            {
                float time = Mathf.Min(duration, (float)(Time.unscaledTimeAsDouble - start));
                fadePanel.alpha = 1f - Smooth(0f, scene.fadeIn, time);
                if (playingVideo && scene.artwork != null &&
                    (videoEnded || videoFailed || (time >= clipDuration && video.frame >= (long)video.frameCount - 1)))
                {
                    playingVideo = false;
                    videoView.enabled = false;
                    video.Stop();
                    ShowArtwork(scene);
                }
                if (artwork.enabled) DrawIllustration(scene, time, duration);

                DrawSubtitle(scene, time);
                if (!voiceStarted && scene.narration != null && time >= scene.narrationAt)
                {
                    voiceStarted = true;
                    narration.PlayOneShot(scene.narration);
                }
                if (!sfxStarted && scene.enableSfx && scene.sfx != null && time >= scene.sfxAt)
                {
                    sfxStarted = true;
                    sfx.PlayOneShot(scene.sfx);
                }

                if (time >= duration) break;
                yield return null;
            }

            // 재생 중 스킵하면 소리를 즉시 멈추고 화면은 짧게 검게 닫습니다.
            subtitlePanel.alpha = 0f;
            narration.Stop();
            sfx.Stop();
            if (advance || skipAll) video.Pause();
            float from = fadePanel.alpha;
            start = Time.unscaledTimeAsDouble;
            while (fadePanel.alpha < 1f)
            {
                float fadeDuration = advance || skipAll ? 0.18f : scene.fadeOut;
                float time = (float)(Time.unscaledTimeAsDouble - start);
                fadePanel.alpha = Mathf.Lerp(from, 1f, Smooth(0f, fadeDuration, time));
                yield return null;
            }
            video.Stop();
            videoView.texture = null;
            firstFrameReady = false;
        }

        private IEnumerator PrepareVideo(VideoClip clip)
        {
            videoFailed = videoEnded = firstFrameReady = false;
            video.clip = clip;
            if (clip.audioTrackCount > 0) video.EnableAudioTrack(0, true);
            video.sendFrameReadyEvents = true;
            video.Prepare();
            double deadline = Time.unscaledTimeAsDouble + 4.0;
            while (!advance && !skipAll && !videoFailed && !video.isPrepared &&
                   Time.unscaledTimeAsDouble < deadline)
                yield return null;

            if (!advance && !skipAll && !videoFailed && video.isPrepared)
            {
                video.Play();
                deadline = Time.unscaledTimeAsDouble + 2.0;
                while (!advance && !skipAll && !videoFailed && !firstFrameReady &&
                       Time.unscaledTimeAsDouble < deadline)
                    yield return null;
            }
            video.sendFrameReadyEvents = false;
        }

        private void ShowArtwork(OpeningSceneData scene)
        {
            artwork.texture = scene.artwork != null ? scene.artwork.texture : null;
            artwork.enabled = scene.artwork != null;
        }

        private void DrawSubtitle(OpeningSceneData scene, float time)
        {
            float start = scene.subtitleStart;
            float end = scene.subtitleEnd;
            string text = scene.subtitle;
            int selected = -1;
            if (scene.subtitleCues != null && scene.subtitleCues.Length > 0)
            {
                // 한 번에 한 구절만 보여 줍니다. 자막 사이 빈 구간은 그대로 둡니다.
                for (int i = 0; i < scene.subtitleCues.Length; i++)
                {
                    var cue = scene.subtitleCues[i];
                    if (time < cue.start || time >= cue.end) continue;
                    selected = i;
                    start = cue.start;
                    end = cue.end;
                    text = cue.text;
                    break;
                }
                if (selected < 0)
                {
                    subtitlePanel.alpha = 0f;
                    return;
                }
            }
            if (activeSubtitle != selected)
            {
                subtitleText.text = text;
                activeSubtitle = selected;
            }
            float appear = Smooth(start, start + 0.24f, time);
            subtitlePanel.alpha = showSubtitles
                ? appear * (1f - Smooth(end - 0.20f, end, time)) : 0f;
            // 아주 작은 상승 모션으로 한 구절씩 종이 위에 떠오르게 합니다.
            subtitleText.rectTransform.anchoredPosition = subtitleRestPosition + Vector2.down * (5f * (1f - appear));
        }

        private void DrawIllustration(OpeningSceneData scene, float time, float duration)
        {
            float t = Smooth(scene.motionStart, duration, time);
            float zoom = scene.motion switch
            {
                OpeningSceneData.Motion.ZoomIn => Mathf.Lerp(1f, scene.zoom, t),
                OpeningSceneData.Motion.ZoomOut => Mathf.Lerp(scene.zoom, 1f, t),
                OpeningSceneData.Motion.Still => 1f,
                _ => scene.zoom
            };

            // 확대할수록 지정한 초점으로 이동합니다. 패닝은 그림 안쪽 여백을 사용합니다.
            Vector2 focus = scene.focus;
            if (scene.motion == OpeningSceneData.Motion.PanRight) focus.x = t;
            if (scene.motion == OpeningSceneData.Motion.PanLeft) focus.x = 1f - t;
            float reveal = Smooth(0f, scene.revealSeconds, time);
            float hide = scene.exitSeconds > 0f ? Smooth(duration - scene.exitSeconds, duration, time) : 0f;
            float effectTime = Smooth(scene.effectStart, duration, time);
            illustration.SetVector(CameraData, new Vector4(zoom, focus.x, focus.y, 0f));
            illustration.SetVector(EffectData, new Vector4((float)scene.effect, effectTime, time, duration));
            illustration.SetVector(WipeData, new Vector4(reveal, hide, scene.revealSeconds, scene.exitSeconds));
        }

        private static float Smooth(float start, float end, float time)
        {
            return end <= start ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, time));
        }

        private void OnVideoFrame(VideoPlayer source, long frame) => firstFrameReady = true;
        private void OnVideoEnd(VideoPlayer source) => videoEnded = true;

        private void OnVideoError(VideoPlayer source, string message)
        {
            videoFailed = true;
            Debug.LogWarning($"오프닝 영상에 문제가 있어 스틸로 전환합니다. ({message})", this);
        }

        private void OnDestroy()
        {
            if (video != null)
            {
                video.frameReady -= OnVideoFrame;
                video.loopPointReached -= OnVideoEnd;
                video.errorReceived -= OnVideoError;
            }
            if (illustration != null) Destroy(illustration);
        }
    }
}
