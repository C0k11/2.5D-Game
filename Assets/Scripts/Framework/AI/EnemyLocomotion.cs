using UnityEngine;

namespace Game.Framework.AI
{
    /// <summary>
    /// 敌人移动模块（3D 简化版）。当前只在 XZ 平面、按 Forward 方向走，没考虑寻路。
    /// 为之后接 NavMeshAgent 留口。
    /// Order = 0：Brain(+100) 之前，Senses(-80) / Probe(-90) 之后。
    /// </summary>
    public class EnemyLocomotion : ActorModule
    {
        [Header("Speed")]
        public float walkSpeed = 2f;
        public float chaseSpeed = 4f;

        [Header("Acceleration")]
        public float acceleration = 30f;
        public float brake = 60f;

        [Header("Behavior")]
        [Tooltip("遇墙 / 悬崖时停下（仅前向）。")]
        public bool respectEnvironment = true;

        private EnvironmentProbe _probe;
        private Vector3 _inputDir;
        private float _targetSpeed;

        public override int Order => 0;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);
            _probe = GetComponent<EnvironmentProbe>();
        }

        /// <summary>下达 3D 方向 + 速度。dir 内部归一化。</summary>
        public void Request(Vector3 dir, float speed)
        {
            dir.y = 0f;
            _inputDir = dir.sqrMagnitude < 0.001f ? Vector3.zero : dir.normalized;
            _targetSpeed = speed;
        }

        /// <summary>2D 兼容：1=右、-1=左。</summary>
        public void Request(float dirX, float speed) => Request(new Vector3(Mathf.Sign(dirX), 0f, 0f), speed);

        public void Stop() => Request(Vector3.zero, 0f);

        public override void FixedTick(float dt)
        {
            Vector3 effectiveDir = _inputDir;

            if (respectEnvironment && _probe != null && effectiveDir.sqrMagnitude > 0.001f)
            {
                Vector3 facing = State.Forward;
                if (Vector3.Dot(effectiveDir, facing) > 0.5f && !_probe.CanAdvance)
                    effectiveDir = Vector3.zero;
            }

            if (_inputDir.sqrMagnitude > 0.001f) Actor.SetDirection(_inputDir);

            Vector3 v = State.Velocity;
            float vy = v.y;
            Vector3 target = effectiveDir * _targetSpeed;

            // X / Z 分别加速 / 刹车
            float rateX = (target.x * v.x > 0f) ? acceleration : brake;
            float rateZ = (target.z * v.z > 0f) ? acceleration : brake;
            v.x = Mathf.MoveTowards(v.x, target.x, rateX * dt);
            v.z = Mathf.MoveTowards(v.z, target.z, rateZ * dt);
            v.y = vy;
            State.Velocity = v;
        }
    }
}
