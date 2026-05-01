using UnityEngine;

namespace Game.Framework.Interaction
{
    /// <summary>
    /// 可交互物体的抽象基类。任何需要被玩家"按 E 触发"的东西继承它：
    /// DialogueTrigger、宝箱、存档点、门、传送点……
    ///
    /// 为什么不用纯 interface：
    ///   - MonoBehaviour 上的 interface 字段无法在 Inspector 序列化
    ///   - 需要统一的 prompt 文本字段、叙事条件字段，抽象类直接暴露更干净
    ///   - 可以提供默认的 Gizmo 绘制
    ///
    /// 前置要求：同 GameObject 必须有一个 Collider（任意形状，是否 trigger 皆可）。
    /// InteractorModule 通过 OverlapSphere 命中这些 Collider 找到它们。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("交互提示文案。UI 层订阅 InteractCandidateChanged 事件获取。")]
        public string prompt = "按 E 调查";

        [Tooltip("若非空，必须已达成此 Milestone 才能交互（填 NarrativeState 里的 ID）。")]
        public string requireMilestone;

        [Tooltip("若非空，已达成此 Milestone 后反而不能再交互（用于一次性事件）。")]
        public string consumeOnMilestone;

        public virtual bool CanInteract(InteractorModule who)
        {
            var narrative = NarrativeRuntime.State;
            if (narrative != null)
            {
                if (!string.IsNullOrEmpty(requireMilestone) && !narrative.HasMilestone(requireMilestone))
                    return false;
                if (!string.IsNullOrEmpty(consumeOnMilestone) && narrative.HasMilestone(consumeOnMilestone))
                    return false;
            }
            return true;
        }

        public virtual string GetPrompt(InteractorModule who) => prompt;

        /// <summary>子类实现具体行为：打开对话、打开宝箱、传送……</summary>
        public abstract void Interact(InteractorModule who);

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
