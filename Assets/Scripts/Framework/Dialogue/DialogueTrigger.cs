using UnityEngine;
using Game.Framework.Interaction;

namespace Game.Framework.Dialogue
{
    /// <summary>
    /// 对话触发器：一个 Interactable，Interact 时启动指定 DialogueLine。
    /// 作为参考实现；同样的模式可套用到宝箱、存档点等。
    /// </summary>
    public class DialogueTrigger : Interactable
    {
        [Tooltip("对话入口节点。")]
        public DialogueLine entry;

        public override void Interact(InteractorModule who)
        {
            if (entry == null)
            {
                Debug.LogWarning($"[DialogueTrigger] {name} has no entry.");
                return;
            }
            var runner = SimpleDialogueRunner.Instance;
            if (runner == null)
            {
                Debug.LogWarning("[DialogueTrigger] No SimpleDialogueRunner in scene.");
                return;
            }
            runner.StartDialogue(entry);
        }
    }
}
