using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Framework
{
    /// <summary>
    /// Actor 基类。3D 版。
    ///
    /// 职责：
    ///   1. 持有 ActorState / ActionGate 两块中心数据。
    ///   2. 收集挂在同对象上的所有 IActorModule，按 Order 排序。
    ///   3. 统一驱动模块的 Tick / FixedTick（以 LocalTimeScale 缩放后的 dt）。
    ///   4. 广播 Actor 级事件到 EventBus。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Actor : MonoBehaviour
    {
        public ActorState State { get; } = new ActorState();
        public ActionGate Gate { get; } = new ActionGate();

        [Tooltip("是否用 transform 旋转表现朝向。关闭则由动画系统自行处理。")]
        public bool rotateByForward = true;

        [Tooltip("旋转平滑速度（每秒最大转角度数）。0 = 瞬切。")]
        public float rotateSpeedDegPerSec = 720f;

        [Tooltip("Actor ID，用于事件广播中识别身份。留空则使用 GameObject 名。")]
        public string actorId;

        private readonly List<IActorModule> _modules = new List<IActorModule>();

        public string Id => string.IsNullOrEmpty(actorId) ? name : actorId;

        public bool IsPaused { get; set; }

        protected virtual void Awake()
        {
            State.Rb = GetComponent<Rigidbody>();

            var raw = GetComponents<IActorModule>();
            _modules.Clear();
            _modules.AddRange(raw);
            _modules.Sort((a, b) => a.Order.CompareTo(b.Order));

            for (int i = 0; i < _modules.Count; i++)
                _modules[i].OnAttach(this);

            EventBus.Publish(new ActorSpawned(Id));
        }

        protected virtual void OnDestroy()
        {
            EventBus.Publish(new ActorDespawned(Id));
        }

        protected virtual void Update()
        {
            if (IsPaused) return;
            float dt = Time.deltaTime * State.LocalTimeScale;
            Gate.Tick(dt);
            for (int i = 0; i < _modules.Count; i++)
                _modules[i].Tick(dt);

            // 朝向平滑（在视觉子物体上做更好；这里默认旋转根 transform）
            if (rotateByForward && State.Forward.sqrMagnitude > 0.0001f)
            {
                Vector3 flat = new Vector3(State.Forward.x, 0f, State.Forward.z);
                if (flat.sqrMagnitude > 0.0001f)
                {
                    var target = Quaternion.LookRotation(flat.normalized, Vector3.up);
                    transform.rotation = rotateSpeedDegPerSec <= 0f
                        ? target
                        : Quaternion.RotateTowards(transform.rotation, target, rotateSpeedDegPerSec * dt);
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (IsPaused) return;
            float dt = Time.fixedDeltaTime * State.LocalTimeScale;
            for (int i = 0; i < _modules.Count; i++)
                _modules[i].FixedTick(dt);
        }

        /// <summary>3D 版：设置朝向（XZ 平面，自动归一化）。</summary>
        public void SetDirection(Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return;
            State.Forward = forward.normalized;
        }

        /// <summary>2D-style 兼容：1=右、-1=左，映射成 (±1,0,0)。</summary>
        public void SetDirection(int dir)
        {
            if (dir == 0) return;
            SetDirection(dir > 0 ? Vector3.right : Vector3.left);
        }

        public T GetModule<T>() where T : class, IActorModule
        {
            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i] is T t) return t;
            return null;
        }
    }

    public readonly struct ActorSpawned { public readonly string Id; public ActorSpawned(string id) { Id = id; } }
    public readonly struct ActorDespawned { public readonly string Id; public ActorDespawned(string id) { Id = id; } }
}
