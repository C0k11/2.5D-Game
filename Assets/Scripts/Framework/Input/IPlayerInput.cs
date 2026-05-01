namespace Game.Framework.Input
{
    /// <summary>
    /// 玩家输入抽象。模块只依赖此接口，不直接依赖 Unity 的 Input System。
    /// 这样 meta 剧情里可以把玩家输入"替换"成 AI / 录像 / 系统操控，而不动模块代码。
    ///
    /// 按键采用"时间戳 + Consume"模型：
    ///   - 按下瞬间：PressedAt 被更新为 Time.time
    ///   - 模块在 buffer 窗口内检查并调用 Consume*() 清掉
    /// 这样既支持按键预输入（jump buffer），又避免多个模块重复消耗同一次按键。
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>-1 / 0 / 1。</summary>
        float Horizontal { get; }
        /// <summary>-1 / 0 / 1。俯视模式下 W/S 驱动的方向轴；横版模式下通常忽略。</summary>
        float Vertical { get; }

        bool JumpHeld { get; }
        float JumpPressedAt { get; }
        float DashPressedAt { get; }
        float AttackPressedAt { get; }
        float InteractPressedAt { get; }

        void ConsumeJump();
        void ConsumeDash();
        void ConsumeAttack();
        void ConsumeInteract();
    }
}
