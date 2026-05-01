using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;

namespace Game.Framework.UI
{
    /// <summary>
    /// UI 栈管理器。单例。
    ///
    /// 设计：
    ///   - 视图以 Stack 组织，顶层消费输入
    ///   - 任一视图声明 pausesGame 时 Time.timeScale=0；弹栈后恢复
    ///   - ESC 键：有栈 → 弹栈；空栈 → 打开 openOnEscape（通常是暂停菜单）
    ///   - sortingOrder 自动按深度分配，避免视图互相遮挡
    ///
    /// 视图在场景中预置（挂 UIView 子类），UIManager 启动时自动注册。
    /// 业务代码通过 UIManager.Instance.Push(view) 或 .Push<T>() 打开。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Tooltip("空栈时按 ESC 默认打开的视图（通常挂你的 PauseMenu 进来）。")]
        [SerializeField] private UIView openOnEscape;

        [Tooltip("受控的视图根节点；启动时递归扫描所有 UIView 自动注册。留空则扫整个场景。")]
        [SerializeField] private Transform viewRoot;

        private readonly List<UIView> _stack = new List<UIView>(8);
        private readonly Dictionary<string, UIView> _registry = new Dictionary<string, UIView>();
        private bool _lastPauseState;
        private float _cachedTimeScale = 1f;

        public UIView Top => _stack.Count == 0 ? null : _stack[_stack.Count - 1];
        public int Depth => _stack.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Transform scanRoot = viewRoot != null ? viewRoot : null;
            UIView[] views = scanRoot != null
                ? scanRoot.GetComponentsInChildren<UIView>(true)
                : FindObjectsByType<UIView>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < views.Length; i++)
            {
                var v = views[i];
                if (_registry.ContainsKey(v.Id))
                {
                    Debug.LogWarning($"[UI] duplicate view id: {v.Id}");
                    continue;
                }
                _registry[v.Id] = v;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (Top != null && Top.consumesEscape) Pop();
                else if (Top == null && openOnEscape != null) Push(openOnEscape);
            }
        }

        // ---------- 查找 ----------
        public T Get<T>() where T : UIView
        {
            foreach (var v in _registry.Values)
                if (v is T t) return t;
            return null;
        }

        public UIView Get(string id)
        {
            _registry.TryGetValue(id, out var v);
            return v;
        }

        // ---------- 栈操作 ----------
        public void Push(UIView view)
        {
            if (view == null || view.IsOnStack) return;

            var prevTop = Top;
            _stack.Add(view);
            view.IsOnStack = true;
            view.ApplySortingOrder(_stack.Count - 1);

            prevTop?.InternalCovered();
            view.InternalPushed();

            RefreshPauseState();
            EventBus.Publish(new UIPushed(view));
        }

        public T Push<T>() where T : UIView
        {
            var v = Get<T>();
            if (v != null) Push(v);
            return v;
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            var top = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            top.IsOnStack = false;
            top.InternalPopped();

            Top?.InternalRevealed();
            RefreshPauseState();
            EventBus.Publish(new UIPopped(top));
        }

        public void PopUntil(UIView view)
        {
            while (_stack.Count > 0 && Top != view) Pop();
        }

        public void PopAll()
        {
            while (_stack.Count > 0) Pop();
        }

        // ---------- 暂停 ----------
        private void RefreshPauseState()
        {
            bool shouldPause = false;
            for (int i = 0; i < _stack.Count; i++)
                if (_stack[i].pausesGame) { shouldPause = true; break; }

            if (shouldPause == _lastPauseState) return;
            _lastPauseState = shouldPause;

            if (shouldPause)
            {
                _cachedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _cachedTimeScale;
            }
            EventBus.Publish(new UIPauseStateChanged(shouldPause));
        }
    }
}
