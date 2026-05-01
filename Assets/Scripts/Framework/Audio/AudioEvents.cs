using UnityEngine;
using Game.Core;

namespace Game.Framework.Audio
{
    /// <summary>
    /// Audio 事件 Facade。业务层不直接持有 AudioManager 引用，统一走 EventBus：
    ///   AudioEvents.PlaySfx(cue)            —— 不关心位置
    ///   AudioEvents.PlaySfxAt(cue, worldPos)—— 空间化 SFX
    ///   AudioEvents.PlayMusic(track, fade)
    ///   AudioEvents.ApplyFilter(preset) / ClearFilter()
    ///
    /// AudioManager 在自己的 Awake/OnDestroy 订阅这些事件。没有 AudioManager 场景时静默失败。
    /// 好处：单元模块 / 测试脚本 / Meta 剧情都能发"PlaySfx"而不强依赖音频系统存在。
    /// </summary>
    public static class AudioEvents
    {
        public static void PlaySfx(AudioCue cue) => EventBus.Publish(new PlaySfxEvent(cue, null));
        public static void PlaySfxAt(AudioCue cue, Vector3 worldPos) => EventBus.Publish(new PlaySfxEvent(cue, worldPos));
        public static void PlayMusic(MusicTrack track, float? fade = null) => EventBus.Publish(new PlayMusicEvent(track, fade));
        public static void StopMusic(float? fade = null) => EventBus.Publish(new StopMusicEvent(fade));
        public static void ApplyFilter(AudioFilterPreset preset) => EventBus.Publish(new ApplyAudioFilterEvent(preset));
        public static void ClearFilter() => EventBus.Publish(new ApplyAudioFilterEvent(null));
    }

    public readonly struct PlaySfxEvent
    {
        public readonly AudioCue Cue;
        public readonly Vector3? WorldPos;
        public PlaySfxEvent(AudioCue cue, Vector3? pos) { Cue = cue; WorldPos = pos; }
    }

    public readonly struct PlayMusicEvent
    {
        public readonly MusicTrack Track;
        public readonly float? Fade;
        public PlayMusicEvent(MusicTrack t, float? f) { Track = t; Fade = f; }
    }

    public readonly struct StopMusicEvent
    {
        public readonly float? Fade;
        public StopMusicEvent(float? f) { Fade = f; }
    }

    public readonly struct ApplyAudioFilterEvent
    {
        public readonly AudioFilterPreset Preset;
        public ApplyAudioFilterEvent(AudioFilterPreset p) { Preset = p; }
    }
}
