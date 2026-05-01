using UnityEngine;
using Game.Framework.Interaction;

namespace Game.Framework.Scene
{
    /// <summary>
    /// 关卡门。玩家交互后切到目标场景的目标 SpawnPoint。
    /// 继承自 Interactable，所以"按 E 进入"的提示与门控 milestone 都免费获得。
    /// </summary>
    public class LevelPortal : Interactable
    {
        public string targetScene;
        public string targetSpawnId;

        public override void Interact(InteractorModule who)
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning($"[LevelPortal] {name} has no target scene.");
                return;
            }
            if (LevelLoader.Instance == null)
            {
                Debug.LogWarning("[LevelPortal] No LevelLoader in scene.");
                return;
            }
            LevelLoader.Instance.LoadLevel(targetScene, targetSpawnId);
        }
    }
}
