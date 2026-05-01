using UnityEngine;

namespace Game.Framework.AI
{
    /// <summary>
    /// 地面敌人 3D 环境探测：
    ///   - 前方墙壁：沿 Forward 水平 Raycast；
    ///   - 前方悬崖：从 Forward 一小段 + 向下 Raycast；
    /// 排除自身 Rigidbody 防自击。
    /// Order = -90：GroundSensor(-100) 之后，逻辑之前。
    /// </summary>
    public class EnvironmentProbe : ActorModule
    {
        [Header("Wall Probe")]
        [Tooltip("沿 Forward 的水平探测距离。")]
        public float wallProbeDistance = 0.6f;
        [Tooltip("射线起点相对 Actor 的偏移（脚踝高度略上）。X 没用，Y 是高度。")]
        public Vector3 wallProbeOriginOffset = new Vector3(0f, 0.4f, 0f);
        public LayerMask wallLayer = ~0;

        [Header("Ledge Probe")]
        [Tooltip("从前方一小段位置向下的探测距离。")]
        public float ledgeProbeDistance = 1.2f;
        [Tooltip("起点相对 Actor 的偏移。Forward 方向上的领先距离 + 高度。")]
        public Vector3 ledgeProbeOriginOffset = new Vector3(0.6f, 0.2f, 0f);
        public LayerMask groundLayer = ~0;

        [Header("Debug")]
        public bool drawGizmos = true;

        public bool HasWallAhead { get; private set; }
        public bool HasGroundAhead { get; private set; }

        public bool CanAdvance => !HasWallAhead && HasGroundAhead;

        public override int Order => -90;

        private readonly RaycastHit[] _hitBuf = new RaycastHit[4];

        public override void FixedTick(float dt)
        {
            Vector3 forward = State.Forward.sqrMagnitude > 0.001f ? State.Forward.normalized : transform.forward;

            // Wall
            Vector3 wallOrigin = transform.position
                               + Vector3.up * wallProbeOriginOffset.y;
            HasWallAhead = FirstHitExcludingSelf(wallOrigin, forward, wallProbeDistance, wallLayer);

            // Ledge: 起点向前一段，向下检测
            Vector3 ledgeOrigin = transform.position
                                + forward * ledgeProbeOriginOffset.x
                                + Vector3.up * ledgeProbeOriginOffset.y;
            HasGroundAhead = FirstHitExcludingSelf(ledgeOrigin, Vector3.down, ledgeProbeDistance, groundLayer);
        }

        private bool FirstHitExcludingSelf(Vector3 origin, Vector3 dir, float dist, LayerMask mask)
        {
            int n = Physics.RaycastNonAlloc(origin, dir, _hitBuf, dist, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = _hitBuf[i].collider;
                if (c == null) continue;
                if (State != null && State.Rb != null && _hitBuf[i].rigidbody == State.Rb) continue;
                return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            Vector3 fwd = Application.isPlaying && State != null && State.Forward.sqrMagnitude > 0.001f
                ? State.Forward.normalized
                : transform.forward;

            Vector3 wallOrigin = transform.position + Vector3.up * wallProbeOriginOffset.y;
            Gizmos.color = HasWallAhead ? Color.red : Color.yellow;
            Gizmos.DrawLine(wallOrigin, wallOrigin + fwd * wallProbeDistance);

            Vector3 ledgeOrigin = transform.position + fwd * ledgeProbeOriginOffset.x + Vector3.up * ledgeProbeOriginOffset.y;
            Gizmos.color = HasGroundAhead ? Color.green : Color.red;
            Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector3.down * ledgeProbeDistance);
        }
    }
}
