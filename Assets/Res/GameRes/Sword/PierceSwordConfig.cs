using UnityEngine;

/// <summary>
/// 穿透剑配置资产：投射物的物理参数与解锁时基础数值，Inspector 直接编辑（AB 名 data）。
/// 由 GameAssetsSystem 异步加载并持有，生成穿透剑时把"分支升级卡加成"（AbilitySystem 聚合）
/// 叠加在本基础值上。伤害/穿透的成长走 AbilityDatabase 里的 WeaponUpgrade 分支卡
///（穿透剑·伤害强化 / 穿透剑·穿透强化），本配置只管基础值与物理手感。
/// </summary>
[CreateAssetMenu(fileName = "PierceSwordConfig", menuName = "Game/穿透剑配置")]
public class PierceSwordConfig : ScriptableObject
{
    [Header("基础值（解锁即生效）")]
    public float Damage = 1f;              // 基础伤害（成长走"穿透剑·伤害强化"分支卡）
    [Tooltip("基础穿透数（成长走\"穿透剑·穿透强化\"分支卡，每级 +1，可无限升）")]
    public int BasePierce = 1;

    [Header("飞行")]
    public float Speed = 12f;              // 飞行速度（固定方向，怪物可走位躲开）
    public float Lifetime = 2f;            // 最长存活时间（秒），超时自动回收
    public float HitRadius = 0.4f;         // 命中判定半径
    [Tooltip("贴图朝向修正角（度）：剑贴图默认朝 +X 时不偏转；朝上则填 90")]
    public float SpriteRotationOffset = 0f;
}
