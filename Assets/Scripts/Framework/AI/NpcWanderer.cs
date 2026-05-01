using UnityEngine;

namespace Game.Framework.AI
{
    /// <summary>
    /// 轻量 NPC 游荡模块（3D，XZ 平面）。Idle / Wander / Talk 三态。
    /// Talk 由外部调 SetTalking 切换；不做攻击。
    /// </summary>
    [RequireComponent(typeof(EnemyLocomotion))]
    public class NpcWanderer : ActorModule
    {
        public enum NpcState { Idle, Wander, Talk }

        [Header("Wander")]
        public float wanderRange = 2f;
        public float idleMinSeconds = 1f;
        public float idleMaxSeconds = 3f;
        public float wanderMinSeconds = 1.5f;
        public float wanderMaxSeconds = 4f;
        public float walkSpeed = 1.5f;

        [Header("Debug")]
        [SerializeField] private NpcState _debug;

        private StateMachine<NpcState> _fsm;
        private EnemyLocomotion _loco;
        private EnvironmentProbe _probe;

        private Vector3 _origin;
        private Vector3 _targetDir;
        private float _stateDuration;

        public override int Order => 50;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);
            _loco = GetComponent<EnemyLocomotion>();
            _probe = GetComponent<EnvironmentProbe>();
            _origin = transform.position;

            _fsm = new StateMachine<NpcState>(NpcState.Idle);
            _fsm.Configure(NpcState.Idle)
                .OnEnter(() => { _stateDuration = Random.Range(idleMinSeconds, idleMaxSeconds); _loco.Stop(); });
            _fsm.Configure(NpcState.Wander)
                .OnEnter(() =>
                {
                    _stateDuration = Random.Range(wanderMinSeconds, wanderMaxSeconds);
                    // 在 XZ 平面随机选一个朝向
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    _targetDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                });
            _fsm.Configure(NpcState.Talk)
                .OnEnter(() => _loco.Stop());
            _fsm.Start();
        }

        public override void Tick(float dt)
        {
            _debug = _fsm.Current;
            if (_fsm.Current == NpcState.Talk) return;

            if (_fsm.TimeInState >= _stateDuration)
            {
                _fsm.ChangeState(_fsm.Current == NpcState.Idle ? NpcState.Wander : NpcState.Idle);
                return;
            }

            if (_fsm.Current == NpcState.Wander)
            {
                Vector3 fromOrigin = transform.position - _origin;
                fromOrigin.y = 0f;
                if (fromOrigin.magnitude > wanderRange && Vector3.Dot(fromOrigin, _targetDir) > 0f)
                    _targetDir = -_targetDir;

                if (_probe != null && Vector3.Dot(_targetDir, State.Forward) > 0.5f && !_probe.CanAdvance)
                    _targetDir = -_targetDir;

                _loco.Request(_targetDir, walkSpeed);
            }
        }

        public void SetTalking(bool talking)
        {
            if (talking) _fsm.ChangeState(NpcState.Talk);
            else _fsm.ChangeState(NpcState.Idle);
        }
    }
}
