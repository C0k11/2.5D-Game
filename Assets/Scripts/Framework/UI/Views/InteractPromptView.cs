using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Framework.Interaction;

namespace Game.Framework.UI.Views
{
    /// <summary>
    /// 非模态 HUD：靠近可交互物时显示"按 E 调查"提示。
    /// 订阅 InteractCandidateChanged 自动开关。不走 UIManager 栈（常驻底层）。
    /// </summary>
    public class InteractPromptView : UIView
    {
        [Tooltip("显示提示文本的 UI.Text；可留空，则只做 SetActive 切换。")]
        public Text promptLabel;

        [Tooltip("提示面板根节点。无此引用则整个 UIView 在有候选时显示。")]
        public GameObject panel;

        protected override void Awake()
        {
            base.Awake();
            // InteractPromptView 启动时主动激活自身（它自己通过 panel 控制可见性）
            gameObject.SetActive(true);
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<InteractCandidateChanged>(OnChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<InteractCandidateChanged>(OnChanged);
        }

        private void OnChanged(InteractCandidateChanged evt)
        {
            var target = evt.Target;
            if (target == null)
            {
                if (panel != null) panel.SetActive(false);
                return;
            }
            if (promptLabel != null) promptLabel.text = target.GetPrompt(null);
            if (panel != null) panel.SetActive(true);
        }
    }
}
