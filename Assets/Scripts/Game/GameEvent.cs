namespace QFramework.Gameplay
{
    /// <summary>敌人数变更事件（由 KillEnemyCommand 发出）</summary>
    public struct EnemyKilledEvent { }
    /// <summary>游戏胜利事件（由 GameManagerSystem 发出）</summary>
    public struct GameWinEvent { }
    /// <summary>主角升级事件（由 LevelUpSystem 发出，表现层监听后打开升级面板）</summary>
    public struct LevelUpEvent { }
}
