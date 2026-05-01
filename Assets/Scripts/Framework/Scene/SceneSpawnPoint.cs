using UnityEngine;

namespace Game.Framework.Scene
{
    /// <summary>
    /// 场景内生成点。玩家从其他关卡进来时，LevelLoader 根据 spawnId 匹配对应的 SpawnPoint
    /// 来决定出生位置与朝向。
    ///
    /// 用法：
    ///   - 每个关卡里放若干空物体挂此组件，起 ID（如 "from_forest"、"from_north_door"）
    ///   - 另一侧的 LevelTrigger 切换场景时指定目标 spawnId
    /// </summary>
    public class SceneSpawnPoint : MonoBehaviour
    {
        [Tooltip("场景内唯一 ID。LevelLoader 据此匹配目标出生点。")]
        public string spawnId;

        [Tooltip("进入时玩家的朝向（1=右，-1=左）。")]
        public int facing = 1;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Vector3 dir = transform.right * Mathf.Sign(facing) * 0.8f;
            Gizmos.DrawLine(transform.position, transform.position + dir);
        }
    }
}
