using UnityEngine;
using Game.Core;
using Game.Core.Narrative;
using Game.Framework.Save;

namespace Game.Framework
{
    /// <summary>
    /// 全局叙事状态的运行时入口。把 NarrativeState SO 指针存在这里，
    /// 任何代码都可通过 NarrativeRuntime.State 拿到；避免到处 FindObject。
    ///
    /// 启动期订阅 MetaLoaded：SaveManager 从磁盘读到 Meta 后派发此事件，
    /// 我们在这里把跨周目数据（glitchDiscovered / endings）回灌到 NarrativeState。
    /// 槽位数据则在 SaveManager.LoadSlot 时直接写入 State，无需走事件。
    ///
    /// 用法：场景里放一个 GameObject 挂此组件，把 NarrativeState SO 拖进 stateAsset。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class NarrativeRuntime : MonoBehaviour
    {
        public static NarrativeState State { get; private set; }
        private static NarrativeRuntime _instance;

        [SerializeField] private NarrativeState stateAsset;
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Tooltip("进入 Play 时是否把 State 里的本周目字段（loopIndex、milestones）重置。"
               + "避免编辑器下 SO 残留上次 Play 的数据。")]
        [SerializeField] private bool resetSlotFieldsOnAwake = true;

        private void Awake()
        {
            // 严格单例：任何重复实例直接销毁，避免重复 Subscribe 造成事件二次触发
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            State = stateAsset;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            if (resetSlotFieldsOnAwake && State != null)
            {
                State.loopIndex = 0;
                State.reachedMilestones.Clear();
            }

            EventBus.Subscribe<MetaLoaded>(OnMetaLoaded);
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            EventBus.Unsubscribe<MetaLoaded>(OnMetaLoaded);
            if (State == stateAsset) State = null;
            _instance = null;
        }

        private void OnMetaLoaded(MetaLoaded evt)
        {
            if (State == null || evt.Data == null) return;
            State.totalBoots = evt.Data.totalBoots;
            State.glitchDiscovered = evt.Data.glitchDiscovered;
            // endings / persistentFlags 目前 NarrativeState 里没字段存；
            // 需要时由订阅 MetaLoaded 的业务层自行取 evt.Data.endingsReached 使用
        }
    }
}
