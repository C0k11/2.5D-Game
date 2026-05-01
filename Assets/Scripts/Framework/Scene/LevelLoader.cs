using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;
using Game.Framework.UI.Views;
using Game.Framework.Save;

namespace Game.Framework.Scene
{
    /// <summary>
    /// 异步场景加载器。流程：
    ///   1. FadeView 淡入到全黑
    ///   2. （可选）自动保存到指定槽位 —— 记录你离开时的进度
    ///   3. SceneManager.LoadSceneAsync(target, Single)，allowSceneActivation=false 到准备好
    ///   4. 找到目标 SpawnPoint，把玩家 Teleport 过去
    ///   5. FadeView 淡出
    ///
    /// 不要裸调 SceneManager.LoadScene！那会阻塞主线程并破坏存档链。
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Instance { get; private set; }

        [Header("Fade")]
        public float fadeOutTime = 0.5f;
        public float fadeInTime = 0.5f;

        [Header("Auto Save")]
        [Tooltip("切场景前自动存到哪个槽位。-1 = 不自动存。")]
        public int autoSaveSlot = -1;

        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void LoadLevel(string sceneName, string spawnId = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[LevelLoader] already loading, ignored.");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName, spawnId));
        }

        private IEnumerator LoadRoutine(string sceneName, string spawnId)
        {
            IsLoading = true;
            EventBus.Publish(new LevelLoadRequested(sceneName, spawnId));

            // 1. 淡出
            if (FadeView.Instance != null)
                yield return FadeView.Instance.FadeTo(1f, fadeOutTime);

            // 2. 自动存档（在旧场景卸载前）
            if (autoSaveSlot >= 0 && SaveManager.Instance != null)
                SaveManager.Instance.SaveSlot(autoSaveSlot, spawnId);

            // 3. 异步加载，卡到 90% 等激活
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;
            while (op.progress < 0.9f) yield return null;
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            // 4. 等一帧让新场景 Awake/Start 完成
            yield return null;

            // 5. 把玩家放到 SpawnPoint
            TeleportPlayerToSpawn(spawnId);

            EventBus.Publish(new LevelActivated(sceneName, spawnId));

            // 6. 淡入
            if (FadeView.Instance != null)
                yield return FadeView.Instance.FadeTo(0f, fadeInTime);

            IsLoading = false;
            EventBus.Publish(new LevelLoadCompleted(sceneName));
        }

        private void TeleportPlayerToSpawn(string spawnId)
        {
            if (string.IsNullOrEmpty(spawnId)) return;

            var points = FindObjectsByType<SceneSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SceneSpawnPoint match = null;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].spawnId == spawnId) { match = points[i]; break; }
            }
            if (match == null)
            {
                Debug.LogWarning($"[LevelLoader] spawn '{spawnId}' not found in {SceneManager.GetActiveScene().name}");
                return;
            }

            var actor = FindFirstObjectByType<Game.Framework.Actor>();
            if (actor == null) return;
            actor.transform.position = match.transform.position;
            actor.SetDirection(match.facing);
            actor.State.Velocity = Vector2.zero;
        }
    }
}
