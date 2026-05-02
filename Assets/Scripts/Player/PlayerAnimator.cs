using UnityEngine;
using Game.Core;
using Game.Framework;
using Game.Player.Modules;

namespace Game.Player
{
    /// <summary>
    /// 玩家动画路由（通用 Animator，Humanoid / Sprite 都适用）。
    ///
    /// Animator 期望参数（默认名，可改）：
    ///   Speed       (float)   水平速度大小（XZ magnitude，用于 Idle/Walk/Run blend）
    ///   VelocityY   (float)   Y 速度，区分 JumpUp / Fall
    ///   IsGrounded  (bool)
    ///   Jump        (trigger) 起跳瞬间
    ///   Land        (trigger) 落地瞬间
    ///   Dash        (trigger) 冲刺
    ///   Attack      (trigger) 攻击
    ///   AttackStep  (int)     当前连招段
    ///
    /// 缺参数 / 缺 Animator 不报错（HasParam 缓存检查）。
    /// Order = 200：所有逻辑模块之后。
    /// </summary>
    public class PlayerAnimator : ActorModule
    {
        [Header("Refs")]
        [Tooltip("Animator。不填则 GetComponentInChildren。")]
        public Animator animator;

        [Header("Animator Parameter Names")]
        public string speedParam     = "Speed";
        public string velocityYParam = "VelocityY";
        public string groundedParam  = "IsGrounded";
        public string jumpTrigger    = "Jump";
        public string landTrigger    = "Land";
        public string dashTrigger    = "Dash";
        public string attackTrigger  = "Attack";
        public string attackStepParam = "AttackStep";

        [Header("Tuning")]
        [Tooltip("Speed 阈值低于此视为 Idle，避免微小滑动触发 Walk。")]
        public float idleSpeedThreshold = 0.1f;
        [Tooltip("起跳触发的最低向上 Y 速度。")]
        public float jumpVelocityThreshold = 0.5f;

        public override int Order => 200;

        private int _hSpeed, _hVelY, _hGrounded, _hJump, _hLand, _hDash, _hAttack, _hAttackStep;
        private bool _has_Speed, _has_VelY, _has_Grounded, _has_Jump, _has_Land, _has_Dash, _has_Attack, _has_AttackStep;
        private bool _wasGrounded;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[PlayerAnimator] 找不到 Animator，动画路由禁用。", this);
                return;
            }

            _hSpeed = Animator.StringToHash(speedParam);
            _hVelY  = Animator.StringToHash(velocityYParam);
            _hGrounded = Animator.StringToHash(groundedParam);
            _hJump  = Animator.StringToHash(jumpTrigger);
            _hLand  = Animator.StringToHash(landTrigger);
            _hDash  = Animator.StringToHash(dashTrigger);
            _hAttack = Animator.StringToHash(attackTrigger);
            _hAttackStep = Animator.StringToHash(attackStepParam);

            for (int i = 0; i < animator.parameterCount; i++)
            {
                int h = animator.GetParameter(i).nameHash;
                if (h == _hSpeed) _has_Speed = true;
                else if (h == _hVelY) _has_VelY = true;
                else if (h == _hGrounded) _has_Grounded = true;
                else if (h == _hJump) _has_Jump = true;
                else if (h == _hLand) _has_Land = true;
                else if (h == _hDash) _has_Dash = true;
                else if (h == _hAttack) _has_Attack = true;
                else if (h == _hAttackStep) _has_AttackStep = true;
            }

            _wasGrounded = State.IsGrounded;
            EventBus.Subscribe<DashStarted>(OnDashStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DashStarted>(OnDashStarted);
        }

        public override void Tick(float dt)
        {
            if (animator == null) return;

            Vector3 v = State.Velocity;
            // 水平速度（XZ 平面）
            float horiz = new Vector2(v.x, v.z).magnitude;
            float vy = v.y;
            bool grounded = State.IsGrounded;

            if (_has_Speed)    animator.SetFloat(_hSpeed, horiz > idleSpeedThreshold ? horiz : 0f);
            if (_has_VelY)     animator.SetFloat(_hVelY, vy);
            if (_has_Grounded) animator.SetBool(_hGrounded, grounded);

            // 起跳
            if (_wasGrounded && !grounded && vy > jumpVelocityThreshold)
                if (_has_Jump) animator.SetTrigger(_hJump);

            // 落地
            if (!_wasGrounded && grounded)
                if (_has_Land) animator.SetTrigger(_hLand);

            _wasGrounded = grounded;
        }

        private void OnDashStarted(DashStarted _)
        {
            if (animator != null && _has_Dash) animator.SetTrigger(_hDash);
        }

        public void TriggerAttack(int step = 0)
        {
            if (animator == null) return;
            if (_has_AttackStep) animator.SetInteger(_hAttackStep, step);
            if (_has_Attack)     animator.SetTrigger(_hAttack);
        }
    }
}
