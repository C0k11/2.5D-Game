using System;
using System.Collections.Generic;

namespace Game.Framework
{
    /// <summary>
    /// 动作标签。使用 [Flags] 支持位运算组合，一次性锁多个动作。
    /// 新增动作时按 2 的幂继续往下加即可。
    /// </summary>
    [Flags]
    public enum ActionTag
    {
        None    = 0,
        Move    = 1 << 0,
        Jump    = 1 << 1,
        Dash    = 1 << 2,
        Attack  = 1 << 3,
        Damage  = 1 << 4,
        All     = ~0,
    }

    /// <summary>
    /// 动作闸门。替代参考项目的 ActionIgnoreTag 系统。
    /// 使用：Gate.Block(ActionTag.Dash, 0.3f) 锁定冲刺 0.3 秒；
    ///       模块在执行前 Gate.IsBlocked(ActionTag.Dash) 检查。
    /// </summary>
    public class ActionGate
    {
        private struct Lock
        {
            public ActionTag Tag;
            public float Remaining;
        }

        private readonly List<Lock> _locks = new List<Lock>(8);

        public void Block(ActionTag tag, float duration)
        {
            if (duration <= 0f || tag == ActionTag.None) return;
            _locks.Add(new Lock { Tag = tag, Remaining = duration });
        }

        public bool IsBlocked(ActionTag tag)
        {
            for (int i = 0; i < _locks.Count; i++)
                if ((_locks[i].Tag & tag) != 0) return true;
            return false;
        }

        /// <summary>由 Actor 每 Update 以已缩放的 dt 推进。</summary>
        public void Tick(float dt)
        {
            for (int i = _locks.Count - 1; i >= 0; i--)
            {
                var l = _locks[i];
                l.Remaining -= dt;
                if (l.Remaining <= 0f) _locks.RemoveAt(i);
                else _locks[i] = l;
            }
        }

        public void Clear() => _locks.Clear();
    }
}
