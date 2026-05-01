using UnityEngine;

namespace Game.Framework.AI
{
    /// <summary>
    /// 敌人 Actor。目前只是 Actor 的子类 + 便捷访问，后续可以接入阵营 / 奖励 / AI 聚合管理。
    /// 挂载建议（Prefab 模板）：
    ///   - Rigidbody（useGravity 通常关掉，由 JumpModule 自管 Y）
    ///   - Collider（主碰撞）
    ///   - EnemyActor (本脚本)
    ///   - GroundSensor（探子 Transform 放脚下）
    ///   - EnvironmentProbe
    ///   - EnemySenses
    ///   - EnemyLocomotion
    ///   - Health
    ///   - EnemyBrain
    ///   - DepthLock(targetZ=0)   2.5D 下保持 Gameplay 平面
    /// </summary>
    public class EnemyActor : Actor
    {
        [Header("Enemy")]
        [Tooltip("敌人阵营 ID。未来做友伤 / 仇恨时用。")]
        public string faction = "Enemy";
    }
}
