using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏配置资产：玩家初始属性、刷怪参数等平衡数值，Inspector 直接编辑。
/// GameModel 在 OnInit 时读取初始值。
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/游戏配置")]
public class GameConfig : ScriptableObject
{
    [Header("玩家初始属性")]
    public int MaxHp = 3;
    public float AttackDamage = 1f;
    public float MoveSpeed = 5f;
    public float AttackInterval = 1f;
    public float AttackRadius = 5f;
    public int WeaponCount = 1;       // 同时挥出的剑数（升级可加）

    [Header("无限刷怪配置")]
    public float SpawnInterval = 3f;    // 生成间隔（秒）
    public int MaxAliveEnemies = 10;    // 场上敌人上限（0 表示不限）
    public float EnemyPowerPerSecond = 0.02f;  // 敌人强度每秒增长系数（血量随时间变强）
    public float SurviveTimeToWin = 120f;      // 存活到该时间（秒）即胜利（0 表示不设胜利，只败不胜）

    [Header("分层刷怪（随时间解锁新怪，按解锁时间升序填）")]
    [Tooltip("每局从 0 秒重新解锁；已解锁的怪按权重随机刷出。PrefabName 填 \"Enemy\" 或留空 = 复用基础怪预制体（只变数值/体型/颜色）；填其他名字 = 使用对应 AB 预制体（首次出场异步加载，未就绪前先刷基础怪顶替）")]
    public List<EnemyTier> EnemyTiers = new List<EnemyTier>
    {
        new EnemyTier { TierName = "史莱姆", UnlockTime = 0f,   SpawnWeight = 10f },
        new EnemyTier { TierName = "疾风怪", UnlockTime = 30f,  SpawnWeight = 6f, HpMultiplier = 0.7f, SpeedMultiplier = 1.8f, ScaleMultiplier = 0.85f, Tint = new Color(0.45f, 0.90f, 1.00f) },
        new EnemyTier { TierName = "重装怪", UnlockTime = 60f,  SpawnWeight = 4f, HpMultiplier = 3.0f, SpeedMultiplier = 0.7f, ScaleMultiplier = 1.35f, Tint = new Color(1.00f, 0.55f, 0.20f) },
        new EnemyTier { TierName = "精英怪", UnlockTime = 120f, SpawnWeight = 2f, HpMultiplier = 8.0f, SpeedMultiplier = 0.9f, ScaleMultiplier = 1.70f, Tint = new Color(0.80f, 0.40f, 1.00f) },
    };
}

/// <summary>
/// 敌人分层：随局内时间解锁的新怪种。
/// 数值全部是乘区（基于 Enemy 预制体的基础值），同预制体也能长出不同怪。
/// </summary>
[System.Serializable]
public class EnemyTier
{
    [Header("解锁")]
    public string TierName = "新怪物";   // 显示名（解锁播报/调试日志用）
    public float UnlockTime = 0f;        // 局内多少秒后开始出现
    [Range(0f, 100f)]
    public float SpawnWeight = 5f;       // 与其他已解锁怪之间的刷出权重（0 = 不再刷）

    [Header("预制体（AB 资源名；\"Enemy\" = 基础怪）")]
    public string PrefabName = "Enemy";

    [Header("数值/外观乘区")]
    public float HpMultiplier = 1f;
    public float SpeedMultiplier = 1f;
    public float ScaleMultiplier = 1f;
    public Color Tint = Color.white;     // 出生染色（白 = 不染色）
}
