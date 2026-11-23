using UnityEngine;
using UnityEngine.Video;

namespace EasternFantasy.Opening
{
    [CreateAssetMenu(menuName = "Eastern Fantasy/Opening Scene Data")]
    public sealed class OpeningSceneData : ScriptableObject
    {
        public enum Media { Image, Video }
        public enum Motion { Still, ZoomIn, ZoomOut, PanRight, PanLeft }
        public enum Effect { None, CloudsOpen, SealLight, SandWipe, BrokenSeal, CloudsClose, Embers }

        [Header("장면과 리소스")]
        public string sceneName;
        public Media media;
        [Tooltip("영상 재생이 끝나거나 실패했을 때도 이 스틸을 사용합니다.")]
        public Sprite artwork;
        public VideoClip video;
        [Min(0.1f)] public float duration = 8f;

        [Header("카메라와 그림 효과")]
        public Motion motion;
        [Min(1f)] public float zoom = 1.12f;
        [Min(0f)] public float motionStart;
        [Tooltip("그림에서 카메라가 가까이 다가갈 지점입니다.")]
        public Vector2 focus = new(0.5f, 0.5f);
        public Effect effect;
        [Min(0f)] public float effectStart;
        [Min(0f)] public float revealSeconds;
        [Min(0f)] public float exitSeconds;
        [Min(0f)] public float fadeIn = 0.45f;
        [Min(0f)] public float fadeOut = 0.7f;

        [Header("자막과 내레이션")]
        [TextArea(2, 4)] public string subtitle;
        [Min(0f)] public float subtitleStart = 0.6f;
        [Min(0f)] public float subtitleEnd = 7f;
        public AudioClip narration;
        [Min(0f)] public float narrationAt = 0.6f;
        [System.Serializable]
        public struct SubtitleCue
        {
            [TextArea(1, 3)] public string text;
            [Min(0f)] public float start;
            [Min(0f)] public float end;
        }
        [Tooltip("문장 호흡별 자막입니다. 비워 두면 위의 전체 자막을 사용합니다.")]
        public SubtitleCue[] subtitleCues;

        [Header("배경음과 효과음")]
        public bool keepBgm = true;
        public AudioClip newBgm;
        public bool enableSfx;
        public AudioClip sfx;
        [Min(0f)] public float sfxAt;
    }
}
