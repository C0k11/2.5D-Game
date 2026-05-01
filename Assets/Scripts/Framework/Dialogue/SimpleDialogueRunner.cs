using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Framework.Dialogue
{
    /// <summary>
    /// 零依赖对话运行器。消费 DialogueLine SO 节点链，按 EventBus 广播线路。
    /// 放在场景里一个 GameObject 上（只留一份 —— 由 Instance 静态入口提供）。
    ///
    /// 集成点：
    ///   - NarrativeRuntime.State.MarkMilestone：每行 Show 时自动标记
    ///   - EventBus.DialogueStarted/LineShown/Ended：UI/Freeze 订阅
    /// </summary>
    public class SimpleDialogueRunner : MonoBehaviour, IDialogueRunner
    {
        public static SimpleDialogueRunner Instance { get; private set; }

        [Tooltip("将本行 speaker/text 同步打印到控制台，便于无 UI 时测试。")]
        public bool logToConsole = true;

        private DialogueLine _current;

        public bool IsRunning => _current != null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartDialogue(object startKey)
        {
            var line = startKey as DialogueLine;
            if (line == null)
            {
                Debug.LogWarning($"[Dialogue] StartDialogue expects a DialogueLine, got {startKey}");
                return;
            }
            EventBus.Publish(new DialogueStarted(line.name));
            Show(line);
        }

        public void Advance()
        {
            if (_current == null) return;
            if (_current.HasChoices)
            {
                Debug.LogWarning("[Dialogue] Advance() on a choice line; call Choose(i) instead.");
                return;
            }
            if (_current.next != null) Show(_current.next);
            else End();
        }

        public void Choose(int choiceIndex)
        {
            if (_current == null || !_current.HasChoices) return;
            var visible = GetVisibleChoices(_current);
            if (choiceIndex < 0 || choiceIndex >= visible.Count) return;
            var target = visible[choiceIndex].target;
            if (target != null) Show(target); else End();
        }

        public void Stop()
        {
            if (_current != null) End();
        }

        // --- internal ---
        private void Show(DialogueLine line)
        {
            _current = line;
            if (!string.IsNullOrEmpty(line.markMilestoneOnShow))
                NarrativeRuntime.State?.MarkMilestone(line.markMilestoneOnShow);

            string[] choiceTexts = null;
            if (line.HasChoices)
            {
                var visible = GetVisibleChoices(line);
                choiceTexts = new string[visible.Count];
                for (int i = 0; i < visible.Count; i++) choiceTexts[i] = visible[i].text;
            }

            if (logToConsole)
            {
                string prefix = string.IsNullOrEmpty(line.speaker) ? "" : $"[{line.speaker}] ";
                Debug.Log($"{prefix}{line.text}");
                if (choiceTexts != null)
                    for (int i = 0; i < choiceTexts.Length; i++)
                        Debug.Log($"  {i + 1}. {choiceTexts[i]}");
            }

            EventBus.Publish(new DialogueLineShown(line.speaker, line.text, choiceTexts));
        }

        private void End()
        {
            string ctx = _current != null ? _current.name : "";
            _current = null;
            EventBus.Publish(new DialogueEnded(ctx));
        }

        private static List<DialogueLine.Choice> GetVisibleChoices(DialogueLine line)
        {
            var list = new List<DialogueLine.Choice>(line.choices.Count);
            var state = NarrativeRuntime.State;
            foreach (var c in line.choices)
            {
                if (!string.IsNullOrEmpty(c.requireMilestone))
                    if (state == null || !state.HasMilestone(c.requireMilestone)) continue;
                list.Add(c);
            }
            return list;
        }
    }
}
