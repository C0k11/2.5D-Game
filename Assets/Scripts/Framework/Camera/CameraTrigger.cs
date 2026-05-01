using UnityEngine;

namespace Framework.Cameras
{
    /// <summary>
    /// 走进触发器时切相机；可选离开时恢复（3D）。
    /// 挂在带 Collider (Is Trigger) 的物体上。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CameraTrigger : MonoBehaviour
    {
        [Tooltip("进入时要切换到的 CameraManager 里注册的 key；留空 = 回主相机。")]
        public string cameraKey;

        [Tooltip("离开时是否回到主相机（仅在 cameraKey 非空时生效）。")]
        public bool revertOnExit = true;

        [Tooltip("只响应带此 Tag 的物体；留空则响应所有。")]
        public string requiredTag = "Player";

        [Tooltip("只允许触发一次。")]
        public bool oneShot = false;

        private bool _fired;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Matches(other)) return;
            if (oneShot && _fired) return;
            _fired = true;

            if (CameraManager.Instance == null)
            {
                Debug.LogWarning("[CameraTrigger] 场景里没有 CameraManager。");
                return;
            }
            CameraManager.Instance.SwitchTo(string.IsNullOrEmpty(cameraKey) ? null : cameraKey);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!revertOnExit || string.IsNullOrEmpty(cameraKey)) return;
            if (!Matches(other)) return;
            if (CameraManager.Instance != null)
                CameraManager.Instance.SwitchTo(null);
        }

        private bool Matches(Collider other)
        {
            if (string.IsNullOrEmpty(requiredTag)) return true;
            return other.CompareTag(requiredTag);
        }
    }
}
