namespace Game.Framework.Pooling
{
    /// <summary>
    /// 池化对象可选契约。Pool 会在 Acquire/Release 时调用，允许对象重置状态。
    /// 非必需：没实现也能正常池化，但 Acquire 回来的对象可能保留上一次的残留（如 Rigidbody 速度）。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>从池取出、启用前调用。</summary>
        void OnAcquire();
        /// <summary>归还给池、禁用前调用。</summary>
        void OnRelease();
    }
}
