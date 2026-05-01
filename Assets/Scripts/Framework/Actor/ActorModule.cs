using UnityEngine;

namespace Game.Framework
{
    /// <summary>
    /// Actor 模块契约。模块不使用 Unity 自身的 Update / FixedUpdate，
    /// 由 Actor 按 Order 排序后统一调度，保证执行顺序确定。
    /// </summary>
    public interface IActorModule
    {
        /// <summary>执行顺序，数值小的先执行。Sensor 类负数，逻辑类正数。</summary>
        int Order { get; }
        void OnAttach(Actor actor);
        void Tick(float dt);
        void FixedTick(float dt);
    }

    /// <summary>
    /// 模块基类。继承它就能获得对 Actor / State / Gate 的访问，
    /// 并通过 RequireComponent 保证挂载时 Actor 存在。
    /// </summary>
    [RequireComponent(typeof(Actor))]
    public abstract class ActorModule : MonoBehaviour, IActorModule
    {
        protected Actor Actor { get; private set; }
        protected ActorState State => Actor.State;
        protected ActionGate Gate => Actor.Gate;

        public virtual int Order => 0;

        public virtual void OnAttach(Actor actor) { Actor = actor; }
        public virtual void Tick(float dt) { }
        public virtual void FixedTick(float dt) { }
    }
}
