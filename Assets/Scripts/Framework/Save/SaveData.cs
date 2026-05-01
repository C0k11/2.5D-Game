using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework.Save
{
    /// <summary>
    /// 槽位存档。对应玩家"第一章存档/第二章存档"。
    /// 记录本次周目的剧情进度、场景、角色位置、以及各业务系统通过 Blob 写入的自定义数据。
    /// </summary>
    [Serializable]
    public class SlotSaveData
    {
        public int schemaVersion = 1;
        public int slotIndex;
        public string timestampIso;
        public float playTimeSeconds;
        public string sceneName;
        public string savePointId; // 存档点标识，读档时决定玩家初始位置

        // --- 叙事（本周目） ---
        public int loopIndex;
        public List<string> reachedMilestones = new List<string>();

        // --- 业务系统自定义数据 ---
        public List<BlobEntry> blobs = new List<BlobEntry>();

        [Serializable]
        public class BlobEntry
        {
            public string id;
            public string json;
        }

        public string GetBlob(string id)
        {
            for (int i = 0; i < blobs.Count; i++)
                if (blobs[i].id == id) return blobs[i].json;
            return null;
        }

        public void SetBlob(string id, string json)
        {
            for (int i = 0; i < blobs.Count; i++)
            {
                if (blobs[i].id == id) { blobs[i].json = json; return; }
            }
            blobs.Add(new BlobEntry { id = id, json = json });
        }
    }

    /// <summary>
    /// 全局 Meta 存档。跨所有槽位、跨所有周目永久累积。
    /// 这是"打破第四面墙"的数据基石 —— 它记得玩家到底玩过几次、死过几次、通过几个结局。
    /// </summary>
    [Serializable]
    public class MetaSaveData
    {
        public int schemaVersion = 1;
        public int totalBoots;
        public int totalDeaths;
        public int slotWipeCount;        // 玩家主动清空 slot 的次数 —— meta 叙事核心"系统记得你删过档"
        public string lastSlotWipeIso;   // 最近一次删档时间
        public bool glitchDiscovered;
        public List<string> endingsReached = new List<string>();
        public List<string> persistentFlags = new List<string>(); // 跨周目永久的标记
        public string firstBootIso;
        public string lastBootIso;

        public bool HasFlag(string id) => persistentFlags.Contains(id);
        public void SetFlag(string id)
        {
            if (!string.IsNullOrEmpty(id) && !persistentFlags.Contains(id))
                persistentFlags.Add(id);
        }
    }

    /// <summary>
    /// 槽位概要（给存档选择菜单用）。不需要读完整数据就能显示"第 3 槽 · 工业区 · 5 小时 12 分"。
    /// </summary>
    public struct SlotHeader
    {
        public int SlotIndex;
        public bool Exists;
        public string SceneName;
        public int LoopIndex;
        public float PlayTimeSeconds;
        public string TimestampIso;
    }
}
