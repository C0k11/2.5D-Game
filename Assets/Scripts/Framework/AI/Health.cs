using System;
using UnityEngine;
using Game.Core;

namespace Game.Framework.AI
{
    /// <summary>
    /// 任何能挨打的物体的契约。攻击方调用 TakeDamage 即可，
    /// 具体如何扣血 / 播受击动画 / 触发死亡由实现方决定。
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }

    /// <summary>
    /// 攻击方 → 受击方 的伤害描述。足够薄，以后可以扩充元素 / 击退曲线 / debuff。
    /// </summary>
    public struct DamageInfo
    {
        public float amount;
        public Vector3 hitPoint;
        public Vector3 knockback; // 3D 冲量，由受击方覆写 Rigidbody.linearVelocity
        public GameObject source; // 造成伤害的物体（可为空）
        public float stunDuration; // 硬直时间（秒），0 表示不进入硬直
    }

    // EventBus payload
    public readonly struct ActorDamaged { public readonly string Id; public readonly float Amount; public ActorDamaged(string id, float a) { Id = id; Amount = a; } }
    public readonly struct ActorDied    { public readonly string Id; public ActorDied(string id)    { Id = id; } }

    /// <summary>
    /// 通用血量模块。给 Enemy / Player / 可破坏物挂。
    /// </summary>
    public class Health : ActorModule, IDamageable
    {
        [Min(1f)] public float maxHp = 10f;
        [SerializeField] private float _hp;

        public float Hp => _hp;
        public bool IsAlive => _hp > 0f;

        public event Action<DamageInfo> OnDamaged;
        public event Action OnDied;

        public override int Order => -50;

        public override void OnAttach(Actor actor)
        {
            base.OnAttach(actor);
            _hp = maxHp;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;

            _hp = Mathf.Max(0f, _hp - info.amount);

            // 击退（物理层面的）
            if (info.knockback.sqrMagnitude > 0.0001f && State.Rb != null)
                State.Rb.linearVelocity = info.knockback;

            EventBus.Publish(new ActorDamaged(Actor.Id, info.amount));
            OnDamaged?.Invoke(info);

            if (_hp <= 0f)
            {
                EventBus.Publish(new ActorDied(Actor.Id));
                OnDied?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            _hp = Mathf.Min(maxHp, _hp + amount);
        }
    }
}
