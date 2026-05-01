namespace Game.Framework.Dialogue
{
    /// <summary>
    /// 对话运行器抽象。
    ///
    /// 为什么要抽象：今天用自研 SimpleDialogueRunner（SO 节点链，零依赖），
    /// 明天你若要接 Ink / Yarn Spinner，只需写一个新 IDialogueRunner 实现，
    /// 所有业务代码（DialogueTrigger、UI、NarrativeState）零改动。
    ///
    /// 事件经由 EventBus 广播（DialogueStarted / DialogueLineShown / DialogueEnded），
    /// UI 和叙事系统订阅即可，运行器本身不持有它们的引用。
    /// </summary>
    public interface IDialogueRunner
    {
        bool IsRunning { get; }
        /// <summary>startKey：自研 runner 传 DialogueLine 资产；Ink 实现可传 knot 名。</summary>
        void StartDialogue(object startKey);
        /// <summary>推进到下一行（或结束）。</summary>
        void Advance();
        /// <summary>在有分支时选择第 i 个选项。</summary>
        void Choose(int choiceIndex);
        /// <summary>强制结束（被打断时）。</summary>
        void Stop();
    }

    // === 对话事件 ===
    public readonly struct DialogueStarted
    {
        public readonly string Context;
        public DialogueStarted(string context) { Context = context; }
    }

    public readonly struct DialogueLineShown
    {
        public readonly string Speaker;
        public readonly string Text;
        public readonly string[] Choices; // null 表示无分支，Advance() 推进
        public DialogueLineShown(string speaker, string text, string[] choices)
        { Speaker = speaker; Text = text; Choices = choices; }
    }

    public readonly struct DialogueEnded
    {
        public readonly string Context;
        public DialogueEnded(string context) { Context = context; }
    }
}
