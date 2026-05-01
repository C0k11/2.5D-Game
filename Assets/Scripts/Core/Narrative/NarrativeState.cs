using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Narrative
{
    /// <summary>
    /// 全局叙事状态。以 ScriptableObject 作为运行时唯一事实来源，
    /// 独立于场景存在，便于多周目、存档、meta 事件判断。
    ///
    /// 持久化数据分两层：
    ///   1. 本次运行的运行时状态（loopIndex、已触发里程碑 等）
    ///   2. 跨运行的 meta 数据（播放次数、是否发现系统漏洞 等）
    ///
    /// 该 SO 不负责序列化到磁盘，仅作为内存中心数据。存档系统单独
    /// 读取/写入本对象字段，保持关注点分离。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Narrative/Narrative State", fileName = "NarrativeState")]
    public class NarrativeState : ScriptableObject
    {
        [Header("Loop / Meta")]
        [Tooltip("当前是第几周目。从 0 起。")]
        public int loopIndex;
        [Tooltip("跨运行累计启动次数。")]
        public int totalBoots;
        [Tooltip("玩家是否发现过可被 meta 剧情引用的系统异常。")]
        public bool glitchDiscovered;

        [Header("Milestones")]
        [Tooltip("已触发的剧情里程碑 ID 集合。")]
        public List<string> reachedMilestones = new List<string>();

        public bool HasMilestone(string id) => reachedMilestones.Contains(id);

        public void MarkMilestone(string id)
        {
            if (string.IsNullOrEmpty(id) || reachedMilestones.Contains(id)) return;
            reachedMilestones.Add(id);
            EventBus.Publish(new NarrativeMilestoneReached(id, loopIndex));
        }

        public void BeginNewLoop()
        {
            loopIndex++;
            reachedMilestones.Clear();
            EventBus.Publish(new LoopStarted(loopIndex));
        }
    }

    // === 全局叙事事件（EventBus payload）===
    public readonly struct NarrativeMilestoneReached
    {
        public readonly string Id;
        public readonly int LoopIndex;
        public NarrativeMilestoneReached(string id, int loopIndex) { Id = id; LoopIndex = loopIndex; }
    }

    public readonly struct LoopStarted
    {
        public readonly int LoopIndex;
        public LoopStarted(int loopIndex) { LoopIndex = loopIndex; }
    }
}
