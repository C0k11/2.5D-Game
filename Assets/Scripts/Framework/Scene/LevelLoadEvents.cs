namespace Game.Framework.Scene
{
    /// <summary>切场景请求被接受（淡出开始前）。</summary>
    public readonly struct LevelLoadRequested
    {
        public readonly string TargetScene;
        public readonly string SpawnId;
        public LevelLoadRequested(string scene, string spawn) { TargetScene = scene; SpawnId = spawn; }
    }

    /// <summary>场景已激活，玩家已 Teleport 到 SpawnPoint（淡入开始前）。</summary>
    public readonly struct LevelActivated
    {
        public readonly string SceneName;
        public readonly string SpawnId;
        public LevelActivated(string scene, string spawn) { SceneName = scene; SpawnId = spawn; }
    }

    /// <summary>切场景全流程完成（淡入结束）。</summary>
    public readonly struct LevelLoadCompleted
    {
        public readonly string SceneName;
        public LevelLoadCompleted(string scene) { SceneName = scene; }
    }
}
