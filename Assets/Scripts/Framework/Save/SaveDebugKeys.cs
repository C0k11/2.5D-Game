using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Framework.Save
{
    /// <summary>
    /// 存档调试按键：F5 存档，F9 读档，F7 玩家向删档（保留 Meta），F8 开发者向清全（含 Meta）。
    /// 仅开发期使用；正式 UI 接入后删掉本组件。
    /// </summary>
    public class SaveDebugKeys : MonoBehaviour
    {
        public int slot = 0;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || SaveManager.Instance == null) return;

            if (kb.f5Key.wasPressedThisFrame)
            {
                bool ok = SaveManager.Instance.SaveSlot(slot);
                Debug.Log(ok ? $"[SaveDebug] saved slot {slot}" : $"[SaveDebug] save slot {slot} failed");
            }
            else if (kb.f9Key.wasPressedThisFrame)
            {
                var data = SaveManager.Instance.LoadSlot(slot);
                Debug.Log(data != null ? $"[SaveDebug] loaded slot {slot} (scene={data.sceneName}, loop={data.loopIndex})"
                                       : $"[SaveDebug] slot {slot} empty");
            }
            else if (kb.f7Key.wasPressedThisFrame)
            {
                SaveManager.Instance.WipeSlotsOnly();
                Debug.Log($"[SaveDebug] wiped slots only (meta preserved, wipeCount={SaveManager.Instance.Meta.slotWipeCount})");
            }
            else if (kb.f8Key.wasPressedThisFrame)
            {
                SaveManager.Instance.WipeAll();
                Debug.Log("[SaveDebug] wiped all (including meta)");
            }
        }
    }
}
