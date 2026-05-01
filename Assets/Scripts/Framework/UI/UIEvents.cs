namespace Game.Framework.UI
{
    /// <summary>UI 栈事件（EventBus payload），用于打开调试浮层、做音效触发等。</summary>
    public readonly struct UIPushed
    {
        public readonly UIView View;
        public UIPushed(UIView v) { View = v; }
    }

    public readonly struct UIPopped
    {
        public readonly UIView View;
        public UIPopped(UIView v) { View = v; }
    }

    public readonly struct UIPauseStateChanged
    {
        public readonly bool Paused;
        public UIPauseStateChanged(bool p) { Paused = p; }
    }
}
