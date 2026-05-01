namespace Game.Framework.Combat
{
    /// <summary>
    /// 战斗阵营枚举。替换字符串 faction 避免笔误：同阵营不互相伤害，敌对则相反。
    /// 如果将来需要多势力（NPC 派系），改 [Flags] 用位掩码。
    /// </summary>
    public enum Faction
    {
        None    = 0,
        Player  = 1,
        Enemy   = 2,
        Neutral = 3,
    }

    public static class FactionExt
    {
        /// <summary>是否敌对。None 总是不敌对（避免环境伤害误伤自己之类）。Neutral 同理。</summary>
        public static bool IsHostile(this Faction a, Faction b)
        {
            if (a == Faction.None || b == Faction.None) return false;
            if (a == Faction.Neutral || b == Faction.Neutral) return false;
            return a != b;
        }
    }
}
