using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Game.Core;

namespace Game.Framework.Audio
{
    /// <summary>
    /// 全局音频管理器。单例 + AudioSource 池。
    ///
    /// 功能：
    ///   - PlaySfx(cue, worldPos?)：从池中取一个源播放 SFX，自动回收
    ///   - PlayMusic(track, fadeTime?)：双音乐轨交叉淡入淡出，无缝切歌
    ///   - ApplyFilter(preset) / ClearFilter()：运行时切换低通/高通/失真 + music duck
    ///     （不需要 AudioMixer，直接装在 BGM 源上）
    ///   - PauseAll / ResumeAll：场景切换/过场时静默
    ///
    /// 构造：场景里放一个挂 AudioManager 的 GameObject，DontDestroyOnLoad。
    /// 池大小按需改 sfxPoolSize。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Pool")]
        [SerializeField] private int sfxPoolSize = 16;

        [Header("Volumes")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 1f;

        [Header("Mixer (optional)")]
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup musicGroup;

        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private readonly Dictionary<AudioCue, int> _concurrentCount = new Dictionary<AudioCue, int>();

        // 两个 BGM 源做交叉淡入淡出
        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool _activeIsA = true;
        private AudioSource ActiveMusic => _activeIsA ? _musicA : _musicB;
        private AudioSource IdleMusic   => _activeIsA ? _musicB : _musicA;

        // 滤波器（挂在激活 BGM 源上）
        private AudioLowPassFilter _lp;
        private AudioHighPassFilter _hp;
        private AudioDistortionFilter _dist;
        private AudioFilterPreset _currentPreset;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < sfxPoolSize; i++) _sfxPool.Add(CreateSfxSource());
            _musicA = CreateMusicSource("Music A");
            _musicB = CreateMusicSource("Music B");

            _lp = gameObject.AddComponent<AudioLowPassFilter>();   _lp.enabled = false;
            _hp = gameObject.AddComponent<AudioHighPassFilter>();  _hp.enabled = false;
            _dist = gameObject.AddComponent<AudioDistortionFilter>(); _dist.enabled = false;

            // 事件 Facade：业务层通过 AudioEvents.* 发布，不必持本实例引用
            EventBus.Subscribe<PlaySfxEvent>(OnPlaySfx);
            EventBus.Subscribe<PlayMusicEvent>(OnPlayMusic);
            EventBus.Subscribe<StopMusicEvent>(OnStopMusic);
            EventBus.Subscribe<ApplyAudioFilterEvent>(OnApplyFilter);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlaySfxEvent>(OnPlaySfx);
            EventBus.Unsubscribe<PlayMusicEvent>(OnPlayMusic);
            EventBus.Unsubscribe<StopMusicEvent>(OnStopMusic);
            EventBus.Unsubscribe<ApplyAudioFilterEvent>(OnApplyFilter);
            if (Instance == this) Instance = null;
        }

        private void OnPlaySfx(PlaySfxEvent e) => PlaySfx(e.Cue, e.WorldPos);
        private void OnPlayMusic(PlayMusicEvent e) => PlayMusic(e.Track, e.Fade);
        private void OnStopMusic(StopMusicEvent e) => StopMusic(e.Fade);
        private void OnApplyFilter(ApplyAudioFilterEvent e)
        {
            if (e.Preset == null) ClearFilter(); else ApplyFilter(e.Preset);
        }

        private AudioSource CreateSfxSource()
        {
            var go = new GameObject("Sfx");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = sfxGroup;
            return src;
        }

        private AudioSource CreateMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = musicGroup;
            return src;
        }

        // ---------- SFX ----------
        public void PlaySfx(AudioCue cue, Vector3? worldPos = null)
        {
            if (cue == null) return;
            var clip = cue.PickClip();
            if (clip == null) return;

            if (cue.maxConcurrent > 0)
            {
                _concurrentCount.TryGetValue(cue, out int n);
                if (n >= cue.maxConcurrent) return;
                _concurrentCount[cue] = n + 1;
            }

            var src = GetFreeSfx();
            if (src == null) return;

            src.clip = clip;
            src.volume = cue.PickVolume() * sfxVolume * masterVolume;
            src.pitch = cue.PickPitch();
            src.spatialBlend = cue.spatialBlend;
            src.priority = cue.priority;
            if (worldPos.HasValue) src.transform.position = worldPos.Value;

            src.Play();
            StartCoroutine(ReleaseSfx(src, clip.length / Mathf.Max(0.01f, Mathf.Abs(src.pitch)), cue));
        }

        private AudioSource GetFreeSfx()
        {
            for (int i = 0; i < _sfxPool.Count; i++)
                if (!_sfxPool[i].isPlaying) return _sfxPool[i];
            // 全忙：抢占第一个（最旧的）
            return _sfxPool.Count > 0 ? _sfxPool[0] : null;
        }

        private IEnumerator ReleaseSfx(AudioSource src, float wait, AudioCue cue)
        {
            yield return new WaitForSecondsRealtime(wait);
            if (cue != null && _concurrentCount.ContainsKey(cue))
            {
                _concurrentCount[cue] = Mathf.Max(0, _concurrentCount[cue] - 1);
            }
        }

        // ---------- Music ----------
        public void PlayMusic(MusicTrack track, float? fadeTimeOverride = null)
        {
            if (track == null || track.clip == null) { StopMusic(fadeTimeOverride); return; }

            float fadeIn = fadeTimeOverride ?? track.defaultFadeIn;
            float fadeOut = fadeTimeOverride ?? track.defaultFadeOut;

            var oldSrc = ActiveMusic;
            _activeIsA = !_activeIsA;
            var newSrc = ActiveMusic;

            newSrc.clip = track.clip;
            newSrc.loop = track.loop;
            newSrc.time = track.loopStartSeconds;
            newSrc.volume = 0f;
            newSrc.Play();

            StartCoroutine(FadeSource(newSrc, MusicTarget(track), fadeIn));
            if (oldSrc.isPlaying) StartCoroutine(FadeAndStop(oldSrc, fadeOut));
        }

        public void StopMusic(float? fadeOutTime = null)
        {
            float t = fadeOutTime ?? 1f;
            if (_musicA.isPlaying) StartCoroutine(FadeAndStop(_musicA, t));
            if (_musicB.isPlaying) StartCoroutine(FadeAndStop(_musicB, t));
        }

        private float MusicTarget(MusicTrack track)
        {
            float filterScale = _currentPreset != null ? _currentPreset.musicVolumeScale : 1f;
            return track.targetVolume * musicVolume * masterVolume * filterScale;
        }

        private IEnumerator FadeSource(AudioSource src, float target, float duration)
        {
            if (duration <= 0f) { src.volume = target; yield break; }
            float start = src.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            src.volume = target;
        }

        private IEnumerator FadeAndStop(AudioSource src, float duration)
        {
            yield return FadeSource(src, 0f, duration);
            src.Stop();
        }

        // ---------- Filter ----------
        public void ApplyFilter(AudioFilterPreset preset)
        {
            if (preset == null) preset = AudioFilterDefaults.Passthrough;
            _currentPreset = preset;
            StartCoroutine(TransitionFilter(preset, preset.transitionTime));
        }

        public void ClearFilter() => ApplyFilter(AudioFilterDefaults.Passthrough);

        private IEnumerator TransitionFilter(AudioFilterPreset preset, float duration)
        {
            float startLp = _lp.cutoffFrequency;
            float startHp = _hp.cutoffFrequency;
            float startDist = _dist.distortionLevel;
            float startMusic = ActiveMusic.isPlaying ? ActiveMusic.volume : 0f;
            float targetMusic = startMusic * preset.musicVolumeScale;

            float t = 0f;
            _lp.enabled = preset.enableLowPass;
            _hp.enabled = preset.enableHighPass;
            _dist.enabled = preset.enableDistortion;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.001f, duration));
                _lp.cutoffFrequency = Mathf.Lerp(startLp, preset.lowPassCutoff, k);
                _lp.lowpassResonanceQ = preset.lowPassResonance;
                _hp.cutoffFrequency = Mathf.Lerp(startHp, preset.highPassCutoff, k);
                _dist.distortionLevel = Mathf.Lerp(startDist, preset.distortionLevel, k);
                if (ActiveMusic.isPlaying)
                    ActiveMusic.volume = Mathf.Lerp(startMusic, targetMusic, k);
                yield return null;
            }
        }

        // 注：LowPass/HighPass/Distortion 挂在 AudioManager 自身，作用于所有子 AudioSource（BGM+SFX）。
        // 如需仅作用于 BGM，用 AudioMixer snapshots 替代。

        // ---------- Utility ----------
        public void PauseAll()  { AudioListener.pause = true; }
        public void ResumeAll() { AudioListener.pause = false; }
    }
}
