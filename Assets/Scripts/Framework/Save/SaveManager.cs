using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;

namespace Game.Framework.Save
{
    /// <summary>
    /// 存档管理器。双层结构：
    ///   - Slot 存档：每槽一个 JSON，记录当前周目进度
    ///   - Meta 存档：唯一一份，跨所有运行累积 —— 多周目 meta 叙事的基石
    ///
    /// 写入策略：原子写入（写 .tmp 再替换），避免中途崩溃导致存档损坏。
    /// 读写：Unity JsonUtility，零第三方依赖。
    /// 扩展：其他系统通过订阅 SaveCaptureRequested / SaveRestoreRequested 往
    ///       data.blobs 里写/读自己的 JSON，SaveManager 本身对业务无感知。
    ///
    /// 场景里放一个 GameObject 挂此组件即可；DontDestroyOnLoad 跨场景存在。
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Tooltip("槽位数量上限（UI 存档选择界面据此展示）。")]
        public int maxSlots = 6;

        [Tooltip("写入时是否使用格式化 JSON（调试用；发行时可关）。")]
        public bool prettyPrint = true;

        private MetaSaveData _meta;
        public MetaSaveData Meta => _meta;

        private string SavesDir => Path.Combine(Application.persistentDataPath, "Saves");
        private string SlotPath(int i) => Path.Combine(SavesDir, $"slot_{i}.json");
        private string MetaPath => Path.Combine(SavesDir, "meta.json");

        // ---------- 生命周期 ----------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            try { Directory.CreateDirectory(SavesDir); }
            catch (Exception ex) { Debug.LogError($"[Save] create dir failed: {ex.Message}"); }

            _meta = ReadJson<MetaSaveData>(MetaPath) ?? new MetaSaveData();
            _meta.totalBoots++;
            string now = DateTime.UtcNow.ToString("o");
            if (string.IsNullOrEmpty(_meta.firstBootIso)) _meta.firstBootIso = now;
            _meta.lastBootIso = now;
            WriteJson(MetaPath, _meta);

            // 让叙事层（或任何订阅者）有机会根据 meta 回灌状态
            EventBus.Publish(new MetaLoaded(_meta));
        }

        // ---------- Slot API ----------
        public bool SaveSlot(int slotIndex, string savePointId = null)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots)
            {
                Debug.LogWarning($"[Save] slotIndex {slotIndex} out of range.");
                return false;
            }

            var data = new SlotSaveData
            {
                slotIndex = slotIndex,
                timestampIso = DateTime.UtcNow.ToString("o"),
                sceneName = SceneManager.GetActiveScene().name,
                savePointId = savePointId ?? string.Empty,
            };

            // 叙事数据直接由 SaveManager 收集（硬绑定，本来就是核心）
            var state = NarrativeRuntime.State;
            if (state != null)
            {
                data.loopIndex = state.loopIndex;
                data.reachedMilestones = new List<string>(state.reachedMilestones);
            }

            // 业务数据：各系统通过事件回填 blobs
            EventBus.Publish(new SaveCaptureRequested(data));

            if (!WriteJson(SlotPath(slotIndex), data)) return false;

            // 槽位存档常意味着一次叙事节点；顺手把 Meta 也落盘
            SaveMeta();
            EventBus.Publish(new SlotSaved(slotIndex));
            return true;
        }

        public SlotSaveData LoadSlot(int slotIndex)
        {
            var data = ReadJson<SlotSaveData>(SlotPath(slotIndex));
            if (data == null) return null;

            var state = NarrativeRuntime.State;
            if (state != null)
            {
                state.loopIndex = data.loopIndex;
                state.reachedMilestones = new List<string>(data.reachedMilestones ?? new List<string>());
            }

            // 业务数据回灌（注意：消费者可能依赖场景已加载完成，
            // 所以调用方通常先 LoadScene 再调本方法，或订阅方自行等 scene ready）
            EventBus.Publish(new SaveRestoreRequested(data));
            EventBus.Publish(new SlotLoaded(data));
            return data;
        }

        public bool DeleteSlot(int slotIndex)
        {
            string p = SlotPath(slotIndex);
            if (!File.Exists(p)) return false;
            try { File.Delete(p); return true; }
            catch (Exception ex) { Debug.LogError($"[Save] delete failed: {ex.Message}"); return false; }
        }

        public SlotHeader GetSlotHeader(int slotIndex)
        {
            var h = new SlotHeader { SlotIndex = slotIndex, Exists = false };
            var data = ReadJson<SlotSaveData>(SlotPath(slotIndex));
            if (data == null) return h;
            h.Exists = true;
            h.SceneName = data.sceneName;
            h.LoopIndex = data.loopIndex;
            h.PlayTimeSeconds = data.playTimeSeconds;
            h.TimestampIso = data.timestampIso;
            return h;
        }

        public SlotHeader[] ListSlots()
        {
            var arr = new SlotHeader[maxSlots];
            for (int i = 0; i < maxSlots; i++) arr[i] = GetSlotHeader(i);
            return arr;
        }

        // ---------- Meta API ----------
        public void SaveMeta()
        {
            var state = NarrativeRuntime.State;
            if (state != null)
            {
                _meta.glitchDiscovered = state.glitchDiscovered;
            }
            WriteJson(MetaPath, _meta);
            EventBus.Publish(new MetaSaved(_meta));
        }

        public void RecordEnding(string endingId)
        {
            if (!string.IsNullOrEmpty(endingId) && !_meta.endingsReached.Contains(endingId))
            {
                _meta.endingsReached.Add(endingId);
                SaveMeta();
            }
        }

        public void RecordDeath()
        {
            _meta.totalDeaths++;
            // 不立即落盘，避免频繁 IO；下次 SaveSlot / 退出时会保存
        }

        /// <summary>
        /// 玩家向：只删 slot 存档，Meta 完整保留。
        /// 这是"系统记得你删过档"机制的入口 —— Meta 会记录本次删档并累加 slotWipeCount。
        /// 游戏内"删除存档"按钮应该调这个，而不是 WipeAll。
        /// </summary>
        public void WipeSlotsOnly()
        {
            if (!Directory.Exists(SavesDir)) Directory.CreateDirectory(SavesDir);
            try
            {
                var files = Directory.GetFiles(SavesDir, "slot_*.json");
                for (int i = 0; i < files.Length; i++) File.Delete(files[i]);
                // 顺便清 .tmp 残留，避免下次启动误读
                var tmps = Directory.GetFiles(SavesDir, "slot_*.json.tmp");
                for (int i = 0; i < tmps.Length; i++) File.Delete(tmps[i]);
            }
            catch (Exception ex) { Debug.LogError($"[Save] wipe slots failed: {ex.Message}"); }

            _meta.slotWipeCount++;
            _meta.lastSlotWipeIso = DateTime.UtcNow.ToString("o");
            SaveMeta();
            EventBus.Publish(new SlotsWiped(_meta.slotWipeCount));
        }

        /// <summary>开发者向：清掉一切，包括 Meta。一般只给 debug 菜单/QA 用。</summary>
        public void WipeAll()
        {
            if (Directory.Exists(SavesDir))
                try { Directory.Delete(SavesDir, true); }
                catch (Exception ex) { Debug.LogError($"[Save] wipe failed: {ex.Message}"); }
            Directory.CreateDirectory(SavesDir);
            // totalBoots 不重置：它记录的是"这台机器启动过多少次游戏"，与存档无关
            _meta = new MetaSaveData { totalBoots = _meta.totalBoots };
            WriteJson(MetaPath, _meta);
        }

        // ---------- JSON I/O（原子写入）----------
        private bool WriteJson(string path, object obj)
        {
            try
            {
                string json = JsonUtility.ToJson(obj, prettyPrint);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] write {path} failed: {ex.Message}");
                return false;
            }
        }

        private T ReadJson<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] read {path} failed: {ex.Message}");
                return null;
            }
        }

        // ---------- Blob 小工具 ----------
        public static void WriteBlob<T>(SlotSaveData data, string id, T payload) where T : class
        {
            if (data == null || string.IsNullOrEmpty(id)) return;
            data.SetBlob(id, JsonUtility.ToJson(payload));
        }

        public static T ReadBlob<T>(SlotSaveData data, string id) where T : class, new()
        {
            if (data == null || string.IsNullOrEmpty(id)) return null;
            string json = data.GetBlob(id);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] blob '{id}' parse failed: {ex.Message}");
                return null;
            }
        }
    }
}
