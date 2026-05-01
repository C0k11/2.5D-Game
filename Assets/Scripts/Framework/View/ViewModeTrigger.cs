using UnityEngine;

namespace Game.Framework.View
{
    /// <summary>
    /// 进入触发器 → 切 ViewMode（3D）；可选离开时反切。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ViewModeTrigger : MonoBehaviour
    {
        public ViewMode onEnterMode = ViewMode.TopDown;
        public bool revertOnExit = true;
        public ViewMode onExitMode = ViewMode.Side;

        public string requiredTag = "Player";
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
            ViewModeController.Instance?.SetMode(onEnterMode);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!revertOnExit) return;
            if (!Matches(other)) return;
            ViewModeController.Instance?.SetMode(onExitMode);
        }

        private bool Matches(Collider other)
        {
            if (string.IsNullOrEmpty(requiredTag)) return true;
            return other.CompareTag(requiredTag);
        }
    }
}
