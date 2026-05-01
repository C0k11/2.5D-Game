using System.Collections;
using UnityEngine;

namespace Game.Framework.Timing
{
    /// <summary>
    /// 全局顿帧。原理：短时间把 Time.timeScale 拉到近 0 造成"打击定格"，
    /// 再恢复到 1。使用 unscaledDeltaTime 累计持续时间，保证即便 timeScale=0 也能自己恢复。
    ///
    /// 不走 EventBus —— 调用方 PlayerAttackModule / 敌人命中脚本都直接 HitStop.Pulse()，
    /// 这样可以为每次命中精细调参（不同段数不同顿帧长度）。
    /// </summary>
    public static class HitStop
    {
        private static HitStopDriver _driver;

        private static HitStopDriver GetDriver()
        {
            if (_driver != null) return _driver;
            var go = new GameObject("~HitStopDriver");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _driver = go.AddComponent<HitStopDriver>();
            return _driver;
        }

        /// <summary>顿帧 duration 秒（unscaled），期间全局 timeScale = freezeScale。</summary>
        public static void Pulse(float duration, float freezeScale = 0.01f)
        {
            if (duration <= 0f) return;
            GetDriver().Trigger(duration, freezeScale);
        }
    }

    internal class HitStopDriver : MonoBehaviour
    {
        private Coroutine _co;

        public void Trigger(float duration, float freezeScale)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(Run(duration, freezeScale));
        }

        private IEnumerator Run(float duration, float freezeScale)
        {
            float originalScale = UnityEngine.Time.timeScale;
            UnityEngine.Time.timeScale = freezeScale;
            float t = 0f;
            while (t < duration)
            {
                t += UnityEngine.Time.unscaledDeltaTime;
                yield return null;
            }
            // 保守恢复到 1，而不是 originalScale —— 如果顿帧期间 UI 暂停把 timeScale 设成 0，
            // 恢复回 originalScale 还是 0 反而对，交给 UIManager 自己管；这里只管自己改过的东西
            UnityEngine.Time.timeScale = Mathf.Approximately(originalScale, freezeScale) ? 1f : originalScale;
            _co = null;
        }
    }
}
