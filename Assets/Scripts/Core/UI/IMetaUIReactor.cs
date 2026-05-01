using UnityEngine;

namespace Game.Core.UI
{
    /// <summary>
    /// Meta UI 反应器。任何 UI 元素（血条、菜单、HUD）若想参与
    /// 第四面墙破坏效果（故障、被覆盖、被“系统”篡改），实现此接口。
    ///
    /// 具体视觉由实现类决定；框架只规定“存在一个可被外部命令干涉
    /// 的 UI 对象”这一契约。
    /// </summary>
    public interface IMetaUIReactor
    {
        /// <summary>应用一个 meta 效果，例如 "glitch"、"corrupt"、"hide"。</summary>
        void ApplyMetaEffect(string effectId, float intensity);
        /// <summary>恢复到正常显示。</summary>
        void ClearMetaEffect();
    }

    /// <summary>Meta UI 广播事件，供 UI 反应器订阅。</summary>
    public readonly struct MetaUIEffectRequest
    {
        public readonly string TargetTag;   // 空字符串 = 广播给所有反应器
        public readonly string EffectId;
        public readonly float Intensity;
        public MetaUIEffectRequest(string targetTag, string effectId, float intensity)
        { TargetTag = targetTag; EffectId = effectId; Intensity = intensity; }
    }
}
