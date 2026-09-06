using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 敌人数变更事件（由 KillEnemyCommand 发出）。
    /// Position = 敌人死亡位置，DropSystem 据此在原地生成掉落物。
    /// </summary>
    public struct EnemyKilledEvent
    {
        public Vector3 Position;
    }
    /// <summary>游戏胜利事件（由 WaveSystem 发出，存活时间到触发）</summary>
    public struct GameWinEvent { }
    /// <summary>主角升级事件（由 LevelUpSystem 发出，表现层监听后打开升级面板）</summary>
    public struct LevelUpEvent { }
    /// <summary>
    /// 能力等级变化事件（由 ChooseAbilityCommand 发出）。
    /// AbilityId = 能力卡 ID；NewLevel = 升完后的等级（1 = 首次解锁）。
    /// 订阅点：需要"选中某能力后生效"的系统规则卡（如未来掉率强化卡，DropSystem 按 Id 调 AddWeightMultiplier）。
    /// </summary>
    public struct AbilityLeveledEvent
    {
        public string AbilityId;
        public int NewLevel;
    }
    /// <summary>
    /// 敌人生成请求（由 WaveSystem 发出，WaveDriver 接收后通过 SpawnEnemyCommand 执行）。
    /// 规范：System 不能 SendCommand，需通过事件通知 IController 层来执行状态变更。
    /// </summary>
    public struct SpawnEnemyRequestEvent
    {
        public UnityEngine.Vector3 SpawnPosition;
        public float Power;   // 强度系数（≥1，随时间递增）
        public EnemyTier Tier; // 分层（null = 基础怪）；决定预制体与数值/外观乘区
    }
}
