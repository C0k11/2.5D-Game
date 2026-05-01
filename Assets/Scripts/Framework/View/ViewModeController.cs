using UnityEngine;
using Game.Core;

namespace Game.Framework.View
{
    /// <summary>
    /// 视角模式全局控制器。静态 Current 供任何模块 readonly 查询，
    /// 切换走 SetMode(...) 并广播 ViewModeChanged 到 EventBus。
    ///
    /// 场景挂载单例：一个空 GameObject + 本组件即可。没有挂时模式保持默认 Side。
    /// 可选：指定切 Side/TopDown 时对应的 CameraManager key，自动联动相机切换。
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class ViewModeController : MonoBehaviour
    {
        public static ViewModeController Instance { get; private set; }

        /// <summary>全局只读视角模式。没有 Controller 实例时默认 Side，模块可以无脑读。</summary>
        public static ViewMode Current { get; private set; } = ViewMode.Side;

        [Header("Initial")]
        [SerializeField] private ViewMode _initialMode = ViewMode.Side;

        [Header("相机联动 (可选)")]
        [Tooltip("切到 Side 模式时调用 CameraManager.SwitchTo(sideCameraKey)。留空不联动。")]
        public string sideCameraKey = "";
        [Tooltip("切到 TopDown 模式时调用 CameraManager.SwitchTo(topDownCameraKey)。留空不联动。")]
        public string topDownCameraKey = "";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SetMode(_initialMode, silentCamera: true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Current = ViewMode.Side;
            }
        }

        // 域重载关闭时，Play Mode 重启后 static 字段可能残留，强制归零
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; Current = ViewMode.Side; }

        public void SetMode(ViewMode mode) => SetMode(mode, silentCamera: false);

        public void SetMode(ViewMode mode, bool silentCamera)
        {
            var prev = Current;
            Current = mode;

            if (!silentCamera)
            {
                var cm = global::Framework.Cameras.CameraManager.Instance;
                if (cm != null)
                {
                    string key = mode == ViewMode.TopDown ? topDownCameraKey : sideCameraKey;
                    if (!string.IsNullOrEmpty(key)) cm.SwitchTo(key);
                }
            }

            if (prev != mode) EventBus.Publish(new ViewModeChanged(prev, mode));
        }
    }

    /// <summary>EventBus payload。模块按此事件响应视角切换（禁重力 / 开俯视输入等）。</summary>
    public readonly struct ViewModeChanged
    {
        public readonly ViewMode Previous;
        public readonly ViewMode Current;
        public ViewModeChanged(ViewMode prev, ViewMode cur) { Previous = prev; Current = cur; }
    }
}
