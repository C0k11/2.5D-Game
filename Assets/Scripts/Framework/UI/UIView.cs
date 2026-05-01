using UnityEngine;

namespace Game.Framework.UI
{
    /// <summary>
    /// UI 视图基类。一个视图 = 一块带独立 Canvas 的 UI 面板（主菜单、暂停、存档、对话框、HUD…）。
    ///
    /// 需同 GameObject 上有 Canvas（Override Sorting 自动开启），UIManager 负责动态调整 sortingOrder。
    /// 视图默认在场景中 SetActive(false) 预置，由 UIManager.Push 激活。
    ///
    /// 子类重写 OnPushed / OnPopped / OnCovered / OnRevealed 响应生命周期。
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public abstract class UIView : MonoBehaviour
    {
        [Tooltip("被 Push 后是否暂停游戏（Time.timeScale=0）。HUD/Dialogue 通常为 false。")]
        public bool pausesGame = false;

        [Tooltip("按 ESC 时由栈顶视图消费并弹栈。若为 false，ESC 事件会穿透到下层。")]
        public bool consumesEscape = true;

        [Tooltip("基础 sortingOrder；Stack 深度会加在这上面分层。")]
        public int baseSortingOrder = 100;

        [Tooltip("视图 ID，便于 UIManager.Get 查询。留空则用 GameObject 名。")]
        public string viewId;

        private Canvas _canvas;

        public string Id => string.IsNullOrEmpty(viewId) ? name : viewId;
        public bool IsOnStack { get; internal set; }

        protected virtual void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.overrideSorting = true;
            gameObject.SetActive(false);
        }

        internal void ApplySortingOrder(int depth)
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            _canvas.sortingOrder = baseSortingOrder + depth * 10;
        }

        internal void InternalPushed()    { gameObject.SetActive(true);  OnPushed(); }
        internal void InternalPopped()    { OnPopped();  gameObject.SetActive(false); }
        internal void InternalCovered()   { OnCovered(); }
        internal void InternalRevealed()  { OnRevealed(); }

        protected virtual void OnPushed() { }
        protected virtual void OnPopped() { }
        /// <summary>有另一个视图被 Push 到自己上方。用于暂停 HUD 的自动更新等。</summary>
        protected virtual void OnCovered() { }
        /// <summary>上方视图弹栈后自己重新成为栈顶。</summary>
        protected virtual void OnRevealed() { }
    }
}
