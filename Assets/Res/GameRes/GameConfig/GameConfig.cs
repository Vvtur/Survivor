using UnityEngine;

/// <summary>
/// 游戏配置资产：玩家初始属性、波次参数等平衡数值，Inspector 直接编辑。
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

    [Header("波次配置")]
    public int TotalWaves = 3;
    public int FirstWaveEnemies = 7;
    public int WaveEnemyIncrement = 2;  // 每波敌人数量递增
    public float SpawnInterval = 3f;    // 波内生成间隔（秒）
    public int MaxAliveEnemies = 10;    // 场上敌人上限（0 表示不限）
}
