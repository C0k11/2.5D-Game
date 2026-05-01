using UnityEngine;

namespace Game.Framework.Sensors
{
    /// <summary>
    /// 接地检测模块（3D）。Order = -100，先于所有逻辑模块。
    /// 用 Physics.CheckSphere：probe 位置 + 半径，检查是否有碰撞体（除自身）。
    /// </summary>
    public class GroundSensor : ActorModule
    {
        public Transform probe;
        public float radius = 0.2f;
        public LayerMask groundLayer = ~0;
        [Tooltip("忽略与自身的 Trigger 碰撞。")]
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        public override int Order => -100;

        public override void FixedTick(float dt)
        {
            if (probe == null)
            {
                State.IsGrounded = true; // 兜底：未配置时视为在地，方便调试
                return;
            }
            State.IsGrounded = Physics.CheckSphere(probe.position, radius, groundLayer, triggerInteraction);
        }

        private void OnDrawGizmosSelected()
        {
            if (probe == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(probe.position, radius);
        }
    }
}
