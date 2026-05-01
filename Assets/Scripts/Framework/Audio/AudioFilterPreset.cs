using UnityEngine;

namespace Game.Framework.Audio
{
    /// <summary>
    /// 音频滤镜预设。用于 meta 场景的音效失真，例如"进入黑客空间"、"受伤回响"、"水下"。
    /// AudioManager.ApplyFilter(preset) 平滑过渡到目标参数，ClearFilter() 恢复。
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioFilter", menuName = "Game/Audio/Filter Preset")]
    public class AudioFilterPreset : ScriptableObject
    {
        public bool enableLowPass = true;
        [Range(10f, 22000f)] public float lowPassCutoff = 1200f;
        [Range(1f, 10f)] public float lowPassResonance = 1f;

        public bool enableHighPass = false;
        [Range(10f, 22000f)] public float highPassCutoff = 100f;

        public bool enableDistortion = false;
        [Range(0f, 1f)] public float distortionLevel = 0.3f;

        [Tooltip("BGM 整体音量倍率（0=静音，1=原音量）。duck 效果可用 0.3。")]
        [Range(0f, 1f)] public float musicVolumeScale = 1f;

        public float transitionTime = 0.5f;
    }

    /// <summary>默认 preset（全部关闭、音量 1），供 ClearFilter 内部使用。</summary>
    public static class AudioFilterDefaults
    {
        public static readonly AudioFilterPreset Passthrough = CreateDefault();

        private static AudioFilterPreset CreateDefault()
        {
            var p = ScriptableObject.CreateInstance<AudioFilterPreset>();
            p.enableLowPass = false;
            p.enableHighPass = false;
            p.enableDistortion = false;
            p.musicVolumeScale = 1f;
            p.transitionTime = 0.5f;
            return p;
        }
    }
}
