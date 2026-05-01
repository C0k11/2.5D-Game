using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Framework.Dialogue
{
    /// <summary>
    /// 临时的对话推进器（在还没有正式对话 UI 前使用）。
    /// 对话进行中：按 Interact/Submit/空格 推进；数字键 1-4 选择分支。
    /// 正式 UI 接入后删掉本组件即可。
    /// </summary>
    public class DialogueAdvancer : MonoBehaviour
    {
        private void Update()
        {
            var runner = SimpleDialogueRunner.Instance;
            if (runner == null || !runner.IsRunning) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                runner.Advance();

            if (kb.digit1Key.wasPressedThisFrame) runner.Choose(0);
            else if (kb.digit2Key.wasPressedThisFrame) runner.Choose(1);
            else if (kb.digit3Key.wasPressedThisFrame) runner.Choose(2);
            else if (kb.digit4Key.wasPressedThisFrame) runner.Choose(3);
        }
    }
}
