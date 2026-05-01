using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// Actor 的运行时中心化状态。所有模块读写同一份数据。
    /// 3D 版：使用 Rigidbody，速度 / 朝向都是 Vector3。
    /// </summary>
    public class ActorState
    {
        public Rigidbody Rb;

        public Vector3 Velocity
        {
            get => Rb.linearVelocity;
            set => Rb.linearVelocity = value;
        }

        /// <summary>水平面（XZ）朝向。归一化向量；默认 (0,0,1)。</summary>
        public Vector3 Forward = Vector3.forward;

        /// <summary>由 GroundSensor 每 FixedUpdate 刷新。</summary>
        public bool IsGrounded;

        /// <summary>预留：贴墙判定。</summary>
        public bool IsTouchingWall;

        /// <summary>
        /// 个体时间缩放。Actor 以此乘 Time.deltaTime 后分发给模块，
        /// 实现单体时停 / 慢放，不影响全局 Time.timeScale。
        /// </summary>
        public float LocalTimeScale = 1f;
    }
}
