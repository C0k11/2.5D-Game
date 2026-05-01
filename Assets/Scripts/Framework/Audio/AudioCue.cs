using UnityEngine;

namespace Game.Framework.Audio
{
    /// <summary>
    /// SFX 数据资产。Clip + 参数随机化 + 通道优先级。
    /// 业务代码用 AudioManager.PlaySfx(cue) 即可，不需要关心 AudioSource。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSfx", menuName = "Game/Audio/SFX Cue")]
    public class AudioCue : ScriptableObject
    {
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 volumeJitter = new Vector2(0f, 0f);

        public float pitch = 1f;
        public Vector2 pitchJitter = new Vector2(-0.05f, 0.05f);

        [Tooltip("空间化程度。0 = 纯 2D，1 = 完全 3D 定位衰减。")]
        [Range(0f, 1f)] public float spatialBlend = 0f;

        [Tooltip("优先级：同 cue 连续触发时，若超过 maxConcurrent 会抢占。值越大越优先保留。")]
        public int priority = 128;

        [Tooltip("同一 Cue 的最大并发播放数。0=无限制。")]
        public int maxConcurrent = 4;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float PickVolume() => Mathf.Clamp01(volume + Random.Range(volumeJitter.x, volumeJitter.y));
        public float PickPitch() => pitch + Random.Range(pitchJitter.x, pitchJitter.y);
    }
}
