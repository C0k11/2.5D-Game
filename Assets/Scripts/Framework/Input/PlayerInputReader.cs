using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Framework.Input
{
    /// <summary>
    /// IPlayerInput 的具体实现，基于 Unity 新 Input System 的 PlayerInput
    /// ("Send Messages" 行为) 回调。
    ///
    /// 挂载要求：同对象上有 PlayerInput 组件，Actions 指向 InputSystem_Actions，
    /// Default Map = Player。
    /// </summary>
    public class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        private const float NeverPressed = -999f;

        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpHeld { get; private set; }
        public float JumpPressedAt { get; private set; } = NeverPressed;
        public float DashPressedAt { get; private set; } = NeverPressed;
        public float AttackPressedAt { get; private set; } = NeverPressed;
        public float InteractPressedAt { get; private set; } = NeverPressed;

        public void ConsumeJump()     => JumpPressedAt     = NeverPressed;
        public void ConsumeDash()     => DashPressedAt     = NeverPressed;
        public void ConsumeAttack()   => AttackPressedAt   = NeverPressed;
        public void ConsumeInteract() => InteractPressedAt = NeverPressed;

        // --- PlayerInput "Send Messages" 回调 ---
        private void OnMove(InputValue v)
        {
            var raw = v.Get<Vector2>();
            Horizontal = Mathf.Abs(raw.x) < 0.01f ? 0f : Mathf.Sign(raw.x);
            Vertical   = Mathf.Abs(raw.y) < 0.01f ? 0f : Mathf.Sign(raw.y);
        }

        private void OnJump(InputValue v)
        {
            JumpHeld = v.isPressed;
            if (v.isPressed) JumpPressedAt = Time.time;
        }

        private void OnSprint(InputValue v)
        {
            if (v.isPressed) DashPressedAt = Time.time;
        }

        private void OnAttack(InputValue v)
        {
            if (v.isPressed) AttackPressedAt = Time.time;
        }

        private void OnInteract(InputValue v)
        {
            if (v.isPressed) InteractPressedAt = Time.time;
        }
    }
}
