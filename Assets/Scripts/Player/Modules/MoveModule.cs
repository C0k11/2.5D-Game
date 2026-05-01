using UnityEngine;
using Game.Framework;
using Game.Framework.View;

namespace Game.Player.Modules
{
    /// <summary>
    /// 玩家移动（3D）。WASD → XZ 平面位移；Y 轴交给 JumpModule + 重力。
    ///   - Side 模式：相机锁向 X 轴时，只 Horizontal 驱动 X，Vertical 忽略
    ///   - TopDown 模式：Horizontal + Vertical 同时驱动 XZ，斜向自动归一化
    ///   - Free 模式（默认）：相对相机的前向 / 右向（Cinemachine standard）
    /// 加速 / 刹车分离，空中折损可选。Gate.Move 锁定时完全让出。
    /// </summary>
    public class MoveModule : ActorModule
    {
        public float moveSpeed = 6f;
        public float acceleration = 50f;
        public float brake = 60f;
        [Range(0f, 1f)] public float airAccelerationFactor = 0.4f;

        [Header("Camera Relative")]
        [Tooltip("是否按相机朝向作为输入参考系（标准第三人称做法）。")]
        public bool cameraRelative = true;
        [Tooltip("相机引用，留空时尝试 Camera.main。")]
        public Camera referenceCamera;

        public override int Order => 10;

        private PlayerActor _player;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);
            _player = actor as PlayerActor;
            if (_player == null) Debug.LogError($"[MoveModule] {name} 必须挂在 PlayerActor 上", this);
            if (referenceCamera == null) referenceCamera = Camera.main;
        }

        public override void FixedTick(float dt)
        {
            if (_player == null) return;
            if (Gate.IsBlocked(ActionTag.Move)) return;

            // 输入向量（local input space）
            float ix = _player.Input.Horizontal;
            float iy = _player.Input.Vertical;

            // 转 world 输入
            Vector3 inputWorld;
            if (cameraRelative && referenceCamera != null)
            {
                Vector3 camFwd = referenceCamera.transform.forward; camFwd.y = 0f; camFwd.Normalize();
                Vector3 camRight = referenceCamera.transform.right;  camRight.y = 0f; camRight.Normalize();
                inputWorld = camRight * ix + camFwd * iy;
            }
            else
            {
                // 世界坐标：X = horizontal, Z = vertical
                inputWorld = new Vector3(ix, 0f, iy);
            }

            // ViewMode 修正：横版只用 X，俯视用 XZ
            if (ViewModeController.Current == ViewMode.Side)
            {
                // 抠掉 Z 分量，只保留 X（侧视）
                inputWorld = new Vector3(inputWorld.x, 0f, 0f);
            }

            if (inputWorld.sqrMagnitude > 1f) inputWorld.Normalize();

            // 目标速度
            Vector3 target = inputWorld * moveSpeed;

            // 当前速度（保留 Y 不动）
            Vector3 v = State.Velocity;
            float vy = v.y;

            // X / Z 分别加 / 刹车
            float ax = (target.x * v.x > 0f) ? acceleration : brake;
            float az = (target.z * v.z > 0f) ? acceleration : brake;
            if (!State.IsGrounded) { ax *= airAccelerationFactor; az *= airAccelerationFactor; }

            v.x = Mathf.MoveTowards(v.x, target.x, ax * dt);
            v.z = Mathf.MoveTowards(v.z, target.z, az * dt);
            v.y = vy;

            State.Velocity = v;

            // 朝向：有输入就更新 forward
            Vector3 flat = new Vector3(inputWorld.x, 0f, inputWorld.z);
            if (flat.sqrMagnitude > 0.001f) Actor.SetDirection(flat);
        }
    }
}
