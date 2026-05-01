using System;
using System.Collections.Generic;

namespace Game.Framework.AI
{
    /// <summary>
    /// 极简泛型有限状态机。TState 通常为 enum。
    /// 用法：
    ///   var fsm = new StateMachine&lt;EnemyState&gt;(EnemyState.Patrol);
    ///   fsm.Configure(EnemyState.Patrol)
    ///      .OnEnter(() =&gt; ...)
    ///      .OnTick(dt =&gt; ...)
    ///      .OnExit(() =&gt; ...);
    ///   每帧：fsm.Tick(dt);
    ///   切换：fsm.ChangeState(EnemyState.Chase);
    /// </summary>
    public class StateMachine<TState>
    {
        // internal 级别就够了 —— 出了 Assembly-CSharp 外部看不见；
        // 但类型名不再 private，嵌套类字段/参数引用它不会触发 CS0051/CS0052。
        internal class Handler
        {
            public Action OnEnter;
            public Action<float> OnTick;
            public Action<float> OnFixedTick;
            public Action OnExit;
        }

        public class StateBuilder
        {
            // H 的类型 Handler 是外层私有嵌套类，所以字段本身必须 private，
            // 否则 public StateBuilder 暴露一个更私密类型会触发 CS0052。
            // 外层 StateMachine<T> 作为嵌套类的父类，有权访问此私有字段去赋值。
            private Handler _h;
            internal StateBuilder(Handler h) { _h = h; }
            public StateBuilder OnEnter(Action a)              { _h.OnEnter     = a; return this; }
            public StateBuilder OnTick(Action<float> a)        { _h.OnTick      = a; return this; }
            public StateBuilder OnFixedTick(Action<float> a)   { _h.OnFixedTick = a; return this; }
            public StateBuilder OnExit(Action a)               { _h.OnExit      = a; return this; }
        }

        private readonly Dictionary<TState, Handler> _handlers = new Dictionary<TState, Handler>();
        public TState Current { get; private set; }
        public TState Previous { get; private set; }
        public float TimeInState { get; private set; }

        public event Action<TState, TState> OnTransition; // (from, to)

        public StateMachine(TState initial)
        {
            Current = initial;
            Previous = initial;
        }

        public StateBuilder Configure(TState state)
        {
            if (!_handlers.TryGetValue(state, out var h))
                _handlers[state] = h = new Handler();
            return new StateBuilder(h);
        }

        public void Start()
        {
            TimeInState = 0f;
            if (_handlers.TryGetValue(Current, out var h)) h.OnEnter?.Invoke();
        }

        public void ChangeState(TState next)
        {
            if (EqualityComparer<TState>.Default.Equals(Current, next)) return;
            if (_handlers.TryGetValue(Current, out var old)) old.OnExit?.Invoke();
            Previous = Current;
            Current = next;
            TimeInState = 0f;
            if (_handlers.TryGetValue(Current, out var nw)) nw.OnEnter?.Invoke();
            OnTransition?.Invoke(Previous, Current);
        }

        /// <summary>强制重入当前状态：走一遍 OnExit→OnEnter，TimeInState 归零。
        /// 用于 ChangeState(Current) 被 equal 检查吞掉的场景（例如攻击连招）。</summary>
        public void ReenterState()
        {
            if (_handlers.TryGetValue(Current, out var h))
            {
                h.OnExit?.Invoke();
                TimeInState = 0f;
                h.OnEnter?.Invoke();
            }
        }

        public void Tick(float dt)
        {
            TimeInState += dt;
            if (_handlers.TryGetValue(Current, out var h)) h.OnTick?.Invoke(dt);
        }

        public void FixedTick(float dt)
        {
            if (_handlers.TryGetValue(Current, out var h)) h.OnFixedTick?.Invoke(dt);
        }
    }
}
