using System.Collections.Generic;
using UnityEngine;
using Game.Framework.Combat;

namespace Game.Framework.AI
{
    /// <summary>
    /// 敌人大脑（3D 版）。Patrol / Chase / Attack / Stunned / Dead。
    /// 依赖同对象：EnemyLocomotion / EnemySenses / EnvironmentProbe / Health（后两个可选）。
    /// 巡逻：在 startPos 周围 patrolRange 半径的水平面（XZ）内随机选点。
    /// </summary>
    [RequireComponent(typeof(EnemyLocomotion))]
    [RequireComponent(typeof(EnemySenses))]
    public class EnemyBrain : ActorModule
    {
        public enum EnemyState { Patrol, Chase, Attack, Stunned, Dead }

        [Header("Patrol")]
        public float patrolRange = 3f;
        public float patrolPauseSeconds = 1.0f;

        [Header("Attack")]
        public float attackWindup = 0.3f;
        public float attackActive = 0.15f;
        public float attackRecovery = 0.6f;
        public float attackDamage = 1f;
        [Tooltip("hitbox 半边长。")]
        public Vector3 attackHitboxHalfExtents = new Vector3(0.9f, 0.5f, 0.9f);
        [Tooltip("hitbox 中心相对 Actor 的偏移。X 为 Forward 方向上的领先距离。")]
        public Vector3 attackHitboxOffset = new Vector3(0.8f, 0.3f, 0f);
        public LayerMask attackableLayer = ~0;
        public Vector3 attackKnockback = new Vector3(4f, 2f, 0f);

        [Header("Combat Integration")]
        public Faction attackerFaction = Faction.Enemy;

        [Header("Stun")]
        public float defaultStunSeconds = 0.4f;

        [Header("Debug")]
        [SerializeField] private EnemyState _debugState;

        public StateMachine<EnemyState> FSM { get; private set; }
        public EnemyState CurrentState => FSM != null ? FSM.Current : EnemyState.Patrol;

        private EnemyLocomotion _loco;
        private EnemySenses _senses;
        private EnvironmentProbe _probe;
        private Health _health;

        private Vector3 _startPos;
        private Vector3 _patrolTarget;
        private float _patrolWaitTimer;

        public override int Order => 100;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);
            _loco   = GetComponent<EnemyLocomotion>();
            _senses = GetComponent<EnemySenses>();
            _probe  = GetComponent<EnvironmentProbe>();
            _health = GetComponent<Health>();

            _startPos = transform.position;
            PickNewPatrolTarget();
            BuildFSM();

            if (_health != null)
            {
                _health.OnDamaged += OnDamaged;
                _health.OnDied    += OnDied;
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged -= OnDamaged;
                _health.OnDied    -= OnDied;
            }
        }

        private void BuildFSM()
        {
            FSM = new StateMachine<EnemyState>(EnemyState.Patrol);

            FSM.Configure(EnemyState.Patrol).OnTick(dt => TickPatrol(dt));

            FSM.Configure(EnemyState.Chase)
                .OnEnter(() => { })
                .OnTick(_ => TickChase());

            FSM.Configure(EnemyState.Attack)
                .OnEnter(() =>
                {
                    _loco.Stop();
                    _attackFired = false;
                    _hitThisSwing.Clear();
                    if (_senses.Target != null)
                    {
                        Vector3 d = _senses.Target.position - transform.position;
                        d.y = 0f;
                        if (d.sqrMagnitude > 0.001f) Actor.SetDirection(d);
                    }
                })
                .OnTick(dt => TickAttack(dt));

            FSM.Configure(EnemyState.Stunned)
                .OnEnter(() => _loco.Stop())
                .OnTick(_ => { if (FSM.TimeInState >= _stunSeconds) FSM.ChangeState(EnemyState.Patrol); });

            FSM.Configure(EnemyState.Dead)
                .OnEnter(() =>
                {
                    _loco.Stop();
                    if (State.Rb != null) State.Rb.isKinematic = true;
                });

            FSM.Start();
        }

        public override void Tick(float dt)
        {
            _debugState = FSM.Current;
            if (FSM.Current == EnemyState.Dead) return;

            if (FSM.Current != EnemyState.Stunned && FSM.Current != EnemyState.Attack)
            {
                if (_senses.InAttackRange)      FSM.ChangeState(EnemyState.Attack);
                else if (_senses.InDetection)   FSM.ChangeState(EnemyState.Chase);
                else if (FSM.Current != EnemyState.Patrol) FSM.ChangeState(EnemyState.Patrol);
            }

            FSM.Tick(dt);
        }

        public override void FixedTick(float dt) => FSM.FixedTick(dt);

        private void TickPatrol(float dt)
        {
            if (_patrolWaitTimer > 0f)
            {
                _patrolWaitTimer -= dt;
                _loco.Stop();
                return;
            }

            Vector3 toTarget = _patrolTarget - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.01f)
            {
                _patrolWaitTimer = patrolPauseSeconds;
                PickNewPatrolTarget();
                _loco.Stop();
                return;
            }

            Vector3 dir = toTarget.normalized;

            if (_probe != null && Vector3.Dot(dir, State.Forward) > 0.5f && !_probe.CanAdvance)
            {
                _patrolTarget = transform.position;
                _patrolWaitTimer = patrolPauseSeconds;
                _loco.Stop();
                return;
            }

            _loco.Request(dir, _loco.walkSpeed);
        }

        private void TickChase()
        {
            if (_senses.Target == null) { _loco.Stop(); return; }
            Vector3 toTarget = _senses.Target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0025f) { _loco.Stop(); return; }
            _loco.Request(toTarget.normalized, _loco.chaseSpeed);
        }

        private void TickAttack(float dt)
        {
            float t = FSM.TimeInState;
            if (t < attackWindup) { /* windup */ }
            else if (t < attackWindup + attackActive)
            {
                if (!_attackFired)
                {
                    PerformAttackHit();
                    _attackFired = true;
                }
            }
            else if (t < attackWindup + attackActive + attackRecovery) { /* recovery */ }
            else
            {
                if (_senses.InAttackRange) FSM.ReenterState();
                else if (_senses.InDetection) FSM.ChangeState(EnemyState.Chase);
                else FSM.ChangeState(EnemyState.Patrol);
            }
        }

        private bool _attackFired;
        private readonly HashSet<Hurtbox> _hitThisSwing = new HashSet<Hurtbox>();

        private void PerformAttackHit()
        {
            Vector3 fwd = State.Forward.sqrMagnitude > 0.001f ? State.Forward : transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 up = Vector3.up;

            Vector3 center = transform.position
                           + fwd * attackHitboxOffset.x
                           + up * attackHitboxOffset.y
                           + right * attackHitboxOffset.z;

            Quaternion orient = Quaternion.LookRotation(fwd, Vector3.up);

            // 击退方向：本地→世界（X 沿 forward，Y 上，Z 沿 right）
            Vector3 knock = fwd * attackKnockback.x + Vector3.up * attackKnockback.y + right * attackKnockback.z;

            var template = new DamageInfo
            {
                amount = attackDamage,
                knockback = knock,
                stunDuration = 0.2f,
            };

            HitboxQuery.OverlapBox(
                center, attackHitboxHalfExtents, orient,
                attackableLayer, attackerFaction, template, gameObject, _hitThisSwing);
        }

        private float _stunSeconds;

        private void OnDamaged(DamageInfo info)
        {
            if (FSM.Current == EnemyState.Dead) return;
            _stunSeconds = info.stunDuration > 0f ? info.stunDuration : defaultStunSeconds;
            FSM.ChangeState(EnemyState.Stunned);
        }

        private void OnDied() => FSM.ChangeState(EnemyState.Dead);

        private void PickNewPatrolTarget()
        {
            // XZ 平面随机点
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(0f, patrolRange);
            _patrolTarget = _startPos + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Vector3 origin = Application.isPlaying ? _startPos : transform.position;
            Gizmos.DrawWireSphere(origin, patrolRange);

            Vector3 fwd = Application.isPlaying && State != null && State.Forward.sqrMagnitude > 0.001f
                ? State.Forward : transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 atk = transform.position
                        + fwd * attackHitboxOffset.x
                        + Vector3.up * attackHitboxOffset.y
                        + right * attackHitboxOffset.z;
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.7f);
            Gizmos.matrix = Matrix4x4.TRS(atk, Quaternion.LookRotation(fwd, Vector3.up), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, attackHitboxHalfExtents * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
