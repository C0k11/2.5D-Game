using System.Collections;
using UnityEngine;

namespace Game.Framework.UI.Views
{
    /// <summary>
    /// 渐入渐出遮罩视图。场景切换时由 LevelLoader 调用。
    /// 自身不进 UIManager 栈（避免干扰暂停逻辑），直接控制 CanvasGroup.alpha。
    /// sortingOrder 调得最高，保证盖住一切。
    /// </summary>
    public class FadeView : UIView
    {
        public static FadeView Instance { get; private set; }

        [SerializeField] private CanvasGroup group;

        protected override void Awake()
        {
            base.Awake();
            baseSortingOrder = 10000;
            if (group == null) group = GetComponent<CanvasGroup>();
            if (Instance == null) Instance = this;
            gameObject.SetActive(true);      // FadeView 常驻，靠 alpha 控制
            if (group != null) group.alpha = 0f;
            ApplySortingOrder(0);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public IEnumerator FadeTo(float target, float duration)
        {
            if (group == null) yield break;
            if (duration <= 0f) { group.alpha = target; yield break; }

            float start = group.alpha;
            float t = 0f;
            group.blocksRaycasts = target > 0.01f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = target > 0.99f;
        }
    }
}
