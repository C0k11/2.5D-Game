using UnityEngine;
using Game.Core;
using Game.Framework.Input;

namespace Game.Framework.Interaction
{
    /// <summary>
    /// 玩家交互模块。挂在 PlayerActor 上，每 FixedTick 扫描半径内的 Interactable，
    /// 选取最近的作为当前候选；吃 InteractPressedAt 触发其 Interact()。
    ///
    /// 候选变更时广播 InteractCandidateChanged，UI 层直接订阅显示/隐藏"按 E 调查"提示，
    /// 与本模块零耦合。
    /// </summary>
    public class InteractorModule : ActorModule
    {
        [Tooltip("扫描半径（米）。")]
        public float searchRadius = 1.2f;
        [Tooltip("只检测指定层；默认全部。")]
        public LayerMask interactLayers = ~0;
        [Tooltip("按键预输入时间。")]
        public float inputBufferTime = 0.2f;

        public override int Order => 50; // 在移动/跳跃之后

        private IPlayerInput _input;
        private Interactable _current;
        private readonly Collider[] _hitBuffer = new Collider[16];

        public Interactable Current => _current;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);
            _input = actor.GetComponent<IPlayerInput>();
            if (_input == null)
                Debug.LogWarning($"[InteractorModule] {actor.name} has no IPlayerInput component.");
        }

        public override void FixedTick(float dt)
        {
            RefreshCandidate();

            if (_current == null || _input == null) return;
            bool buffered = Time.time - _input.InteractPressedAt <= inputBufferTime;
            if (!buffered) return;
            if (!_current.CanInteract(this)) return;
            if (Gate.IsBlocked(ActionTag.Move)) return; // 冲刺/击退中屏蔽交互

            _input.ConsumeInteract();
            var target = _current;
            EventBus.Publish(new InteractPerformed(target));
            target.Interact(this);
        }

        private void RefreshCandidate()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, _hitBuffer, interactLayers, QueryTriggerInteraction.Collide);
            Interactable best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var c = _hitBuffer[i];
                if (c == null) continue;
                var it = c.GetComponent<Interactable>();
                if (it == null) continue;
                if (!it.CanInteract(this)) continue;

                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = it; }
            }

            if (best != _current)
            {
                _current = best;
                EventBus.Publish(new InteractCandidateChanged(_current));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
    }

    // === 事件 ===
    public readonly struct InteractCandidateChanged
    {
        public readonly Interactable Target; // null 表示离开所有候选
        public InteractCandidateChanged(Interactable target) { Target = target; }
    }
    public readonly struct InteractPerformed
    {
        public readonly Interactable Target;
        public InteractPerformed(Interactable target) { Target = target; }
    }
}
