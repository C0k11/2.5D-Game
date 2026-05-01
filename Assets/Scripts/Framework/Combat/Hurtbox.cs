using UnityEngine;
using Game.Framework.AI;

namespace Game.Framework.Combat
{
    /// <summary>
    /// 受击盒（3D）。挂在能挨打的对象身上。
    /// 自身不参与伤害判定，只作为 HitboxQuery.OverlapBox 搜寻的目标标签。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour
    {
        [Tooltip("阵营。HitboxQuery 据此过滤敌我。")]
        public Faction faction = Faction.Enemy;

        [Tooltip("伤害接收者。留空则 Awake 时 GetComponentInParent<IDamageable>()。")]
        [SerializeField] private MonoBehaviour damageableRef;

        private IDamageable _damageable;

        public IDamageable Damageable => _damageable;

        private void Awake()
        {
            _damageable = damageableRef as IDamageable;
            if (_damageable == null)
                _damageable = GetComponentInParent<IDamageable>();
            if (_damageable == null)
                Debug.LogWarning($"[Hurtbox] {name} 找不到 IDamageable，挨打不会掉血。", this);
        }
    }
}
