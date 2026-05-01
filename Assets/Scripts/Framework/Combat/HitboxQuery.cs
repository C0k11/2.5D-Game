using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Framework.AI;

namespace Game.Framework.Combat
{
    /// <summary>
    /// Hitbox 判定（3D）。攻击方在 active 帧调，内部 Physics.OverlapBoxNonAlloc，
    /// 阵营过滤 + 跨帧去重，命中发 HitLanded。
    /// 再入安全：buffer 池 + 深度跟踪。
    /// </summary>
    public static class HitboxQuery
    {
        private const int BufferCapacity = 32;
        private const int PoolDepth = 4;
        private static readonly Collider[][] _bufPool = InitPool();
        private static int _depth;

        private static Collider[][] InitPool()
        {
            var arr = new Collider[PoolDepth][];
            for (int i = 0; i < PoolDepth; i++) arr[i] = new Collider[BufferCapacity];
            return arr;
        }

        private static Collider[] RentBuffer()
        {
            if (_depth < PoolDepth) return _bufPool[_depth];
            return new Collider[BufferCapacity];
        }

        /// <param name="halfExtents">box 的半边长（Vector3）。</param>
        /// <param name="orientation">box 旋转。通常 Quaternion.LookRotation(forward)。</param>
        /// <returns>本次造成有效伤害的目标数。</returns>
        public static int OverlapBox(
            Vector3 center, Vector3 halfExtents, Quaternion orientation,
            LayerMask mask,
            Faction attackerFaction,
            DamageInfo template,
            GameObject attacker,
            HashSet<Hurtbox> alreadyHit = null,
            QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide)
        {
            var buf = RentBuffer();
            _depth++;
            int count;
            try
            {
                count = Physics.OverlapBoxNonAlloc(center, halfExtents, buf, orientation, mask, triggerInteraction);
            }
            catch
            {
                _depth--;
                throw;
            }

            int hits = 0;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    var col = buf[i];
                    if (col == null) continue;

                    var hb = col.GetComponent<Hurtbox>();
                    if (hb == null) continue;

                    if (!attackerFaction.IsHostile(hb.faction)) continue;

                    if (alreadyHit != null && !alreadyHit.Add(hb)) continue;

                    var dmg = hb.Damageable;
                    if (dmg == null || !dmg.IsAlive) continue;

                    var info = template;
                    info.hitPoint = center;
                    info.source = attacker;
                    dmg.TakeDamage(info);

                    EventBus.Publish(new HitLanded(attacker, hb.gameObject, center, info.amount));
                    hits++;
                }
            }
            finally
            {
                _depth--;
            }
            return hits;
        }
    }

    public readonly struct HitLanded
    {
        public readonly GameObject Attacker;
        public readonly GameObject Target;
        public readonly Vector3 Point;
        public readonly float Amount;
        public HitLanded(GameObject a, GameObject t, Vector3 p, float amt)
        { Attacker = a; Target = t; Point = p; Amount = amt; }
    }
}
