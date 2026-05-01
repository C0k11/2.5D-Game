using UnityEngine;

namespace Game.Framework.AI
{
    /// <summary>
    /// 敌人感知：缓存玩家引用并计算距离 / 视野夹角（面朝方向正负 90°内可视）。
    /// 查玩家的方式：
    ///   - 优先按 Tag 查（默认 "Player"）
    ///   - 每 repeatSeconds 秒做一次防丢查找（玩家被销毁 / 场景重载会 null）
    ///
    /// Order = -80，保证在 Brain(+100) 之前。
    /// </summary>
    public class EnemySenses : ActorModule
    {
        [Header("Target")]
        public string playerTag = "Player";
        [Tooltip("未找到玩家时每隔多少秒重试一次。")]
        public float repeatSeconds = 0.5f;

        [Header("Detection")]
        [Min(0f)] public float detectionRadius = 6f;
        [Min(0f)] public float attackRadius = 1.3f;
        [Tooltip("失去目标的距离（大于 detection 时用这个更大的值来防抖，构成迟滞环）。")]
        [Min(0f)] public float loseRadius = 8f;

        [Header("Facing (可选)")]
        [Tooltip("只有玩家在前方时才会被检测到。关闭则 360° 无死角。")]
        public bool requireFacing = false;

        [Header("Sight Block (可选)")]
        [Tooltip("是否做视线遮挡检测。勾上后有墙挡住就看不见。")]
        public bool lineOfSight = false;
        public LayerMask obstacleLayer = 0;

        public Transform Target { get; private set; }
        public float Distance { get; private set; } = float.PositiveInfinity;
        public bool InAttackRange { get; private set; }
        public bool InDetection { get; private set; }

        private float _retryTimer;

        public override int Order => -80;

        public override void Tick(float dt)
        {
            if (Target == null)
            {
                _retryTimer -= dt;
                if (_retryTimer <= 0f)
                {
                    AcquireTarget();
                    _retryTimer = repeatSeconds;
                }
            }

            if (Target == null)
            {
                Distance = float.PositiveInfinity;
                InDetection = false;
                InAttackRange = false;
                return;
            }

            Vector3 self = transform.position;
            Vector3 tgt = Target.position;
            Vector3 delta = tgt - self;
            Distance = delta.magnitude;

            bool canDetect = true;
            if (requireFacing)
            {
                Vector3 fwd = State.Forward.sqrMagnitude > 0.001f ? State.Forward : transform.forward;
                Vector3 deltaFlat = new Vector3(delta.x, 0f, delta.z);
                if (deltaFlat.sqrMagnitude > 0.001f)
                    canDetect &= Vector3.Dot(deltaFlat.normalized, fwd) > 0f;
            }
            if (lineOfSight && canDetect && Distance < loseRadius)
            {
                if (Physics.Raycast(self, delta.normalized, out _, Distance, obstacleLayer))
                    canDetect = false;
            }

            // 迟滞环：没检测到时用 detectionRadius 触发，已经检测到后改用 loseRadius 才丢失
            float threshold = InDetection ? loseRadius : detectionRadius;
            InDetection = canDetect && Distance <= threshold;
            InAttackRange = InDetection && Distance <= attackRadius;
        }

        private void AcquireTarget()
        {
            if (string.IsNullOrEmpty(playerTag)) return;
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) Target = go.transform;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, attackRadius);
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, loseRadius);
        }
    }
}
