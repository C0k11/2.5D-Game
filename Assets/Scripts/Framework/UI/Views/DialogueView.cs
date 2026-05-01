using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Framework.Dialogue;

namespace Game.Framework.UI.Views
{
    /// <summary>
    /// 对话 UI。订阅 DialogueStarted / DialogueLineShown / DialogueEnded，
    /// 自动 Push/Pop 自身进 UIManager 栈；ESC 可中断对话。
    /// </summary>
    public class DialogueView : UIView
    {
        public Text speakerLabel;
        public Text bodyLabel;

        [Tooltip("选项按钮数组，按 1..N 索引对应 Choice。")]
        public Button[] choiceButtons;
        public Text[] choiceLabels;

        protected override void Awake()
        {
            base.Awake();
            pausesGame = false;      // 对话不暂停世界（角色由 DialoguePlayerFreeze 接管）
            consumesEscape = true;
            baseSortingOrder = 500;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DialogueStarted>(OnStarted);
            EventBus.Subscribe<DialogueLineShown>(OnLine);
            EventBus.Subscribe<DialogueEnded>(OnEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DialogueStarted>(OnStarted);
            EventBus.Unsubscribe<DialogueLineShown>(OnLine);
            EventBus.Unsubscribe<DialogueEnded>(OnEnded);
        }

        private void OnStarted(DialogueStarted _)
        {
            if (UIManager.Instance != null && !IsOnStack)
                UIManager.Instance.Push(this);
        }

        private void OnLine(DialogueLineShown evt)
        {
            if (speakerLabel != null) speakerLabel.text = evt.Speaker ?? "";
            if (bodyLabel != null) bodyLabel.text = evt.Text ?? "";

            int n = choiceButtons != null ? choiceButtons.Length : 0;
            for (int i = 0; i < n; i++)
            {
                bool has = evt.Choices != null && i < evt.Choices.Length;
                if (choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(has);
                if (has && choiceLabels != null && i < choiceLabels.Length && choiceLabels[i] != null)
                    choiceLabels[i].text = evt.Choices[i];
            }
        }

        private void OnEnded(DialogueEnded _)
        {
            if (UIManager.Instance != null && IsOnStack)
                UIManager.Instance.Pop();
        }

        // 供按钮 OnClick 绑定
        public void OnChoiceClicked(int index)
        {
            SimpleDialogueRunner.Instance?.Choose(index);
        }

        protected override void OnPopped()
        {
            // 被 ESC 中断时强制结束对话
            if (SimpleDialogueRunner.Instance != null && SimpleDialogueRunner.Instance.IsRunning)
                SimpleDialogueRunner.Instance.Stop();
        }
    }
}
