using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 轻量级全局事件总线。按事件类型（struct/class）解耦广播。
    ///
    /// 设计初衷：为后期“打破第四面墙”、多周目 meta 叙事预留插入点。
    /// 任何系统（Player / UI / Dialogue / Save / 甚至 Editor 工具）都可以仅通过
    /// 事件类型进行通讯，彼此不持有引用。
    ///
    /// 用法：
    ///   EventBus.Subscribe&lt;NarrativeMilestoneReached&gt;(OnMilestone);
    ///   EventBus.Publish(new NarrativeMilestoneReached("FIRST_DEATH"));
    ///   EventBus.Unsubscribe&lt;NarrativeMilestoneReached&gt;(OnMilestone);
    ///
    /// 注意：
    ///   - 订阅者在销毁时必须 Unsubscribe，否则持有委托引用会泄漏。
    ///   - 当前实现为同步派发，主线程使用。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var existing))
                _handlers[t] = Delegate.Combine(existing, handler);
            else
                _handlers[t] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (!_handlers.TryGetValue(t, out var existing)) return;
            var result = Delegate.Remove(existing, handler);
            if (result == null) _handlers.Remove(t);
            else _handlers[t] = result;
        }

        public static void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var d) || !(d is Action<T> a)) return;
            // 逐个派发：单个订阅者抛异常不应阻断其他订阅者（单体游戏里很容易一处错误级联挂整条事件链）
            var list = a.GetInvocationList();
            for (int i = 0; i < list.Length; i++)
            {
                try { ((Action<T>)list[i]).Invoke(evt); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }

        // 域重载时清空，避免 Editor 下 Domain Reload Off 的悬挂订阅
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => _handlers.Clear();
    }
}
