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
}
