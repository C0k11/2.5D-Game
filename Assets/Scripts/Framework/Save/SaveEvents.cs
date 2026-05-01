namespace Game.Framework.Save
{
    /// <summary>
    /// 存档事件（EventBus payload）。
    ///
    /// Capture/Restore 是"收集/回灌"阶段：业务系统（玩家位置、背包、任务系统）
    /// 订阅这两个事件，向 data.blobs 写入/读取自己的 JSON 片段。
    /// 这样 SaveManager 不需要知道任何具体业务。
    /// </summary>

    /// <summary>即将保存槽位，请各系统把自己的数据写入 Data。</summary>
    public readonly struct SaveCaptureRequested
    {
        public readonly SlotSaveData Data;
        public SaveCaptureRequested(SlotSaveData data) { Data = data; }
    }

    /// <summary>已读档，请各系统从 Data 还原自己的状态。</summary>
    public readonly struct SaveRestoreRequested
    {
        public readonly SlotSaveData Data;
        public SaveRestoreRequested(SlotSaveData data) { Data = data; }
    }

    public readonly struct SlotSaved
    {
        public readonly int SlotIndex;
        public SlotSaved(int slotIndex) { SlotIndex = slotIndex; }
    }

    public readonly struct SlotLoaded
    {
        public readonly SlotSaveData Data;
        public SlotLoaded(SlotSaveData data) { Data = data; }
    }

    /// <summary>Meta 存档已读入（Boot 时派发一次），叙事层可据此回灌跨周目状态。</summary>
    public readonly struct MetaLoaded
    {
        public readonly MetaSaveData Data;
        public MetaLoaded(MetaSaveData data) { Data = data; }
    }

    public readonly struct MetaSaved
    {
        public readonly MetaSaveData Data;
        public MetaSaved(MetaSaveData data) { Data = data; }
    }

    /// <summary>玩家主动删档（WipeSlotsOnly）完成。Meta 未动，wipeCount 已累加。
    /// 叙事层可据此触发"你以为你能逃走？"这类 meta 桥段。</summary>
    public readonly struct SlotsWiped
    {
        public readonly int WipeCount;
        public SlotsWiped(int count) { WipeCount = count; }
    }
}
