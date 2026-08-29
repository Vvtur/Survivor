using UnityEngine;

/// <summary>
/// 穿透剑配置资产：投射物的全部数值，Inspector 直接编辑（AB 名 data）。
/// 由 GameAssetsSystem 异步加载并持有，生成穿透剑时按等级换算实际属性。
/// </summary>
[CreateAssetMenu(fileName = "PierceSwordConfig", menuName = "Game/穿透剑配置")]
public class PierceSwordConfig : ScriptableObject
{
    [Header("伤害")]
    public float Damage = 1f;              // 基础伤害
    [Tooltip("每级额外伤害（升级成长，0 = 伤害不随等级成长）")]
    public float DamagePerLevel = 0f;

    [Header("穿透")]
    [Tooltip("基础穿透数（实际穿透 = 基础 + 等级，即每升一级多穿一个）")]
    public int BasePierce = 0;

    [Header("飞行")]
    public float Speed = 12f;              // 飞行速度（固定方向，怪物可走位躲开）
    public float Lifetime = 2f;            // 最长存活时间（秒），超时自动回收
    public float HitRadius = 0.4f;         // 命中判定半径
    [Tooltip("贴图朝向修正角（度）：剑贴图默认朝 +X 时不偏转；朝上则填 90")]
    public float SpriteRotationOffset = 0f;
}
