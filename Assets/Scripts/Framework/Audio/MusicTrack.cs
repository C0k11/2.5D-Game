using UnityEngine;

namespace Game.Framework.Audio
{
    /// <summary>
    /// BGM 轨资产。支持循环、淡入淡出时长、进入时的音量目标。
    /// meta 剧情中切"黑客空间"时可搭配 AudioFilterPreset 产生滤波失真感。
    /// </summary>
    [CreateAssetMenu(fileName = "NewMusic", menuName = "Game/Audio/Music Track")]
    public class MusicTrack : ScriptableObject
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float targetVolume = 0.8f;
        public float defaultFadeIn = 1.5f;
        public float defaultFadeOut = 1.5f;
        public bool loop = true;

        [Tooltip("片段中循环起点（秒）。clip.length 为终点。用于有前奏的 BGM。")]
        public float loopStartSeconds = 0f;
    }
}
