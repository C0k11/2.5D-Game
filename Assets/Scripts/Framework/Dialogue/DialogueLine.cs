using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework.Dialogue
{
    /// <summary>
    /// 自研对话节点。一个 SO = 一段对话的入口 or 单行，通过 next 指向下一段形成链。
    /// 有 choices 时视作分支点，next 被忽略。
    ///
    /// 设计故意保持轻量：能跑通线性对话 + 简单分支 + 里程碑标记，
    /// 不实现变量替换、条件表达式等复杂功能。那些留给 Ink 做。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/Dialogue/Dialogue Line")]
    public class DialogueLine : ScriptableObject
    {
        public string speaker;
        [TextArea(2, 6)] public string text;

        [Tooltip("显示本行时标记到 NarrativeState 的里程碑 ID（留空则不标记）。")]
        public string markMilestoneOnShow;

        [Tooltip("无分支：直接衔接下一行；为空 = 对话结束。")]
        public DialogueLine next;

        [Tooltip("有分支时填写；每个 Choice 指向一条后续 DialogueLine。")]
        public List<Choice> choices = new List<Choice>();

        [Serializable]
        public class Choice
        {
            public string text;
            public DialogueLine target;
            [Tooltip("可选：只有当 NarrativeState 已有此 milestone 时选项才出现。")]
            public string requireMilestone;
        }

        public bool HasChoices => choices != null && choices.Count > 0;
    }
}
