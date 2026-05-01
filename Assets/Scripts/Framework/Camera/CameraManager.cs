using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Framework.Cameras
{
    /// <summary>
    /// 相机总控。负责：
    ///   1. 多虚拟相机优先级切换（State-Driven 式手动接口）。
    ///   2. 镜头参数（正交尺寸 / Follow Offset）的平滑插值。
    ///   3. 相机锁定（禁用跟随，常用于演出/Meta 剧情）。
    ///   4. 通过 ImpulseSource 触发震屏。
    /// 依赖：com.unity.cinemachine 3.x（命名空间 Unity.Cinemachine，组件为 CinemachineCamera）。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [System.Serializable]
        public struct NamedCamera
        {
            public string key;
            public CinemachineCamera cam;
        }

        [Header("Cameras")]
        [Tooltip("主跟随相机（默认激活，通常 Priority=10）。")]
        [SerializeField] private CinemachineCamera _mainVcam;

        [Tooltip("场景里可切换的具名相机列表。Priority 非激活时为 0，激活时为 activePriority。")]
        [SerializeField] private List<NamedCamera> _cameras = new();

        [Header("Priority")]
        [SerializeField] private int _mainPriority = 10;
        [SerializeField] private int _activePriority = 20;

        [Header("Impulse (可选)")]
        [Tooltip("默认震屏源；不填会在需要时动态添加到本对象上。")]
        [SerializeField] private CinemachineImpulseSource _defaultImpulseSource;

        private readonly Dictionary<string, CinemachineCamera> _lookup = new();
        private string _currentKey;
        private Coroutine _lensTween;
        private Coroutine _offsetTween;

        public CinemachineCamera Main => _mainVcam;
        public string CurrentKey => _currentKey;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var nc in _cameras)
            {
                if (nc.cam == null || string.IsNullOrEmpty(nc.key)) continue;
                _lookup[nc.key] = nc.cam;
                nc.cam.Priority = 0;
            }
            if (_mainVcam != null) _mainVcam.Priority = _mainPriority;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- 切换 ----------

        /// <summary>切到具名相机；传入 null/空字符串 则回到主相机。</summary>
        public void SwitchTo(string key)
        {
            foreach (var kv in _lookup) kv.Value.Priority = 0;

            if (string.IsNullOrEmpty(key))
            {
                _currentKey = null;
                if (_mainVcam != null) _mainVcam.Priority = _mainPriority;
                return;
            }

            if (!_lookup.TryGetValue(key, out var target))
            {
                Debug.LogWarning($"[CameraManager] 未找到相机 key={key}，回退到 Main。");
                _currentKey = null;
                if (_mainVcam != null) _mainVcam.Priority = _mainPriority;
                return;
            }

            _currentKey = key;
            target.Priority = _activePriority;
            if (_mainVcam != null) _mainVcam.Priority = _mainPriority; // 保底优先级
        }

        public void SetPriority(CinemachineCamera cam, int priority)
        {
            if (cam != null) cam.Priority = priority;
        }

        // ---------- 锁定 ----------

        /// <summary>锁定相机：禁用主相机跟随（相机将保持在当前位置）。</summary>
        public void LockCameraPosition(bool isLocked)
        {
            if (_mainVcam != null) _mainVcam.enabled = !isLocked;
        }

        // ---------- 镜头参数平滑 ----------

        /// <summary>平滑修改正交尺寸（2D 常用）。</summary>
        public void SetOrthoSize(float size, float duration = 0.5f, CinemachineCamera target = null)
        {
            var cam = target != null ? target : _mainVcam;
            if (cam == null) return;
            if (_lensTween != null) StopCoroutine(_lensTween);
            _lensTween = StartCoroutine(TweenOrtho(cam, size, duration));
        }

        private IEnumerator TweenOrtho(CinemachineCamera cam, float target, float duration)
        {
            var lens = cam.Lens;
            float start = lens.OrthographicSize;
            if (duration <= 0f)
            {
                lens.OrthographicSize = target;
                cam.Lens = lens;
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                lens.OrthographicSize = Mathf.Lerp(start, target, k);
                cam.Lens = lens;
                yield return null;
            }
            lens.OrthographicSize = target;
            cam.Lens = lens;
        }

        /// <summary>
        /// 平滑修改 Follow 偏移（适用于挂 CinemachineFollow 的 3D 跟随；
        /// 2D 场景如果用 CinemachinePositionComposer，请改用它自身的 TargetOffset）。
        /// </summary>
        public void SetFollowOffset(Vector3 offset, float duration = 0.5f, CinemachineCamera target = null)
        {
            var cam = target != null ? target : _mainVcam;
            if (cam == null) return;
            var follow = cam.GetComponent<CinemachineFollow>();
            if (follow == null)
            {
                Debug.LogWarning("[CameraManager] 目标相机没有 CinemachineFollow 组件。");
                return;
            }
            if (_offsetTween != null) StopCoroutine(_offsetTween);
            _offsetTween = StartCoroutine(TweenFollowOffset(follow, offset, duration));
        }

        private IEnumerator TweenFollowOffset(CinemachineFollow follow, Vector3 target, float duration)
        {
            Vector3 start = follow.FollowOffset;
            if (duration <= 0f) { follow.FollowOffset = target; yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                follow.FollowOffset = Vector3.Lerp(start, target, k);
                yield return null;
            }
            follow.FollowOffset = target;
        }

        // ---------- 震屏 ----------

        /// <summary>通过默认 ImpulseSource 触发震屏。velocity 为冲量方向与强度。</summary>
        public void Shake(Vector3 velocity)
        {
            if (_defaultImpulseSource == null)
            {
                _defaultImpulseSource = GetComponent<CinemachineImpulseSource>();
                if (_defaultImpulseSource == null)
                    _defaultImpulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            }
            _defaultImpulseSource.GenerateImpulseWithVelocity(velocity);
        }

        /// <summary>简单的水平/竖直快速抖动。</summary>
        public void ShakeQuick(float strength = 1f)
        {
            Shake(new Vector3(strength, strength * 0.5f, 0f));
        }
    }
}
