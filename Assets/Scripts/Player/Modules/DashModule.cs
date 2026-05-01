using UnityEngine;
using Game.Framework;
using Game.Player.Data;

namespace Game.Player.Modules
{
    /// <summary>
    /// 冲刺（3D）：朝当前 Forward 方向做强制位移。
    /// 启动时锁 Move 防止水平输入干扰。无敌处理交给外部伤害模块订阅 DashStarted/DashEnded。
    /// </summary>
    public class DashModule : ActorModule
    {
        public Displacement dashDisplacement;
        public float cooldown = 0.3f;
        public float inputBufferTime = 0.2f;

        public override int Order => 30; // 在 Move / Jump 之后，可覆盖其结果

        private PlayerActor _player;
        private float _cooldownTimer;
        private float _dashElapsed;
        private bool _isDashing;
        private Vector3 _dashDir;

        public bool IsDashing => _isDashing;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);
            _player = actor as PlayerActor;
            if (_player == null) Debug.LogError($"[DashModule] {name} 必须挂在 PlayerActor 上", this);
        }

        public override void Tick(float dt)
        {
            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - dt);
        }

        public override void FixedTick(float dt)
        {
            if (_player == null) return;
            if (_isDashing)
            {
                StepDash(dt);
                return;
            }

            if (dashDisplacement == null) return;
            if (_cooldownTimer > 0f) return;
            if (Gate.IsBlocked(ActionTag.Dash)) return;

            bool buffered = Time.time - _player.Input.DashPressedAt <= inputBufferTime;
            if (buffered)
            {
                _player.Input.ConsumeDash();
                StartDash();
            }
        }

        private void StartDash()
        {
            _isDashing = true;
            _dashElapsed = 0f;
            // 朝当前 Forward 冲；若 forward 是零向量（极端），用 transform.forward 兜底
            Vector3 d = State.Forward;
            d.y = 0f;
            if (d.sqrMagnitude < 0.001f) d = transform.forward;
            _dashDir = d.normalized;

            Gate.Block(ActionTag.Move, dashDisplacement.length);
            Gate.Block(ActionTag.Dash, dashDisplacement.length + cooldown);
            Core.EventBus.Publish(new DashStarted());
        }

        private void StepDash(float dt)
        {
            _dashElapsed += dt;
            float t = _dashElapsed / dashDisplacement.length;
            if (t >= 1f) { EndDash(); return; }

            float factor = dashDisplacement.speedCurve.Evaluate(t);
            float speed = dashDisplacement.maxSpeed * factor;
            var v = State.Velocity;
            v.x = _dashDir.x * speed;
            v.z = _dashDir.z * speed;
            if (dashDisplacement.zeroGravity) v.y = 0f;
            State.Velocity = v;
        }

        private void EndDash()
        {
            _isDashing = false;
            _cooldownTimer = cooldown;
            Core.EventBus.Publish(new DashEnded());
        }
    }

    public readonly struct DashStarted { }
    public readonly struct DashEnded { }
}
