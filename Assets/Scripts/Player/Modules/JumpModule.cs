using UnityEngine;
using Game.Framework;
using Game.Framework.View;

namespace Game.Player.Modules
{
    /// <summary>
    /// 跳跃 + 自管 Y 轴重力（3D）。Rigidbody.useGravity 应设为 false，由本模块完全控制 Y。
    /// 分段重力（Up / Float / Fall），Coyote + Jump Buffer + 可变跳高。
    /// </summary>
    public class JumpModule : ActorModule
    {
        [Header("Jump")]
        public float jumpSpeed = 8f;
        public float jumpCutSpeed = 3f;

        [Header("Gravity 分段")]
        public float gravityUp = 25f;
        public float gravityFloat = 18f;
        public float gravityFall = 35f;
        public float floatThreshold = 1f;
        public float maxFallingSpeed = 25f;

        [Header("Feel")]
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.2f;

        public override int Order => 20;

        private PlayerActor _player;
        private float _coyote;
        private bool _wasGrounded;
        private float _peakFallSpeed;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);
            _player = actor as PlayerActor;
            if (_player == null) Debug.LogError($"[JumpModule] {name} 必须挂在 PlayerActor 上", this);
            _wasGrounded = true;
        }

        public override void Tick(float dt)
        {
            _coyote = State.IsGrounded ? coyoteTime : Mathf.Max(0f, _coyote - dt);

            // 落地：上一帧空中、当前帧着地（暂留 hook，后续可触发 land 动画）
            if (!_wasGrounded && State.IsGrounded)
            {
                _peakFallSpeed = 0f;
            }
            _wasGrounded = State.IsGrounded;
        }

        public override void FixedTick(float dt)
        {
            if (_player == null) return;
            if (ViewModeController.Current == ViewMode.TopDown) return; // 俯视模式下不跳

            Vector3 v = State.Velocity;

            // 起跳
            bool buffered = Time.time - _player.Input.JumpPressedAt <= jumpBufferTime;
            if (buffered && _coyote > 0f && !Gate.IsBlocked(ActionTag.Jump))
            {
                v.y = jumpSpeed;
                _coyote = 0f;
                _player.Input.ConsumeJump();
                Gate.Block(ActionTag.Jump, 0.1f);
            }
            else if (!_player.Input.JumpHeld && v.y > jumpCutSpeed)
            {
                v.y = jumpCutSpeed;
            }

            // 分段重力
            float g;
            if (v.y > floatThreshold) g = gravityUp;
            else if (v.y > -floatThreshold) g = gravityFloat;
            else g = gravityFall;

            v.y -= g * dt;
            if (v.y < -maxFallingSpeed) v.y = -maxFallingSpeed;

            if (!State.IsGrounded && v.y < _peakFallSpeed) _peakFallSpeed = v.y;

            State.Velocity = v;
        }
    }
}
