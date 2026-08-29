using System;
using UnityEngine;

/// <summary>
/// 玩家属性位（被动能力 StatEffects 的作用目标，对应 GameModel 同名字段）。
/// 被动属性全部走"派生重算"（AbilitySystem.RecalcStats），基础值来自 GameConfig。
/// </summary>
public enum PlayerStat
{
    AttackDamage,   // 局内攻击力（最终伤害 = 商店永久攻击 Attack + 局内 AttackDamage）
    MoveSpeed,      // 移动速度
    MaxHp,          // 最大生命
    AttackInterval, // 攻击间隔（Multiply 模式：0.9 = 每级间隔 ×0.9）
    AttackRadius,   // 攻击范围半径
    WeaponCount,    // 同时挥出的剑数
}

/// <summary>数值作用方式</summary>
public enum ModifyMode
{
    Add,        // 加法：每级累加卡片数值
    Multiply,   // 乘法：每级相乘卡片数值（0.9 = 每级 ×0.9）
}

/// <summary>
/// 武器属性位（分支升级卡 WeaponStatModifiers 的作用目标）。
/// 武器生成时由 AbilitySystem.GetWeaponStatTotal 聚合所有分支卡贡献。
/// </summary>
public enum WeaponStat
{
    Damage,       // 伤害
    PierceCount,  // 穿透数
    Radius,       // 命中判定半径（攻击范围）
    Speed,        // 飞行速度
    Lifetime,     // 最长存活时间
    Count,        // 一次齐发的投射物数量
}

/// <summary>能力类别</summary>
public enum AbilityCategory
{
    /// <summary>被动属性：StatEffects 直接加成玩家属性（派生重算）</summary>
    PassiveStat = 0,
    /// <summary>武器：解锁即生效（等级仅作解锁标记，数值成长走 WeaponUpgrade 分支卡）</summary>
    Weapon = 1,
    /// <summary>武器分支升级：TargetAbilityId 指向要强化的武器，RequiresAbilityId 必须同指该武器</summary>
    WeaponUpgrade = 2,
}

/// <summary>被动效果条目：本卡升到哪级就把每级数值作用到哪些玩家属性（同卡内各属性共用每级数值）</summary>
[Serializable]
public class StatModifier
{
    [Tooltip("作用到哪个玩家属性")]
    public PlayerStat Stat;
    [Tooltip("Add=每级加卡片数值；Multiply=每级乘卡片数值（0.9=间隔×0.9）")]
    public ModifyMode Mode;
}

/// <summary>武器分支效果条目：本卡升到哪级就把每级数值加到武器的哪些属性上</summary>
[Serializable]
public class WeaponStatModifier
{
    [Tooltip("强化武器的哪个属性")]
    public WeaponStat Stat;
}

/// <summary>
/// 能力配置资产：一张"升级卡" = 可解锁（等级 0→1）+ 可升级（1→N）的完整数据。
/// 新增能力 = 建一个本资产 + 挂进 AbilityDatabase，零代码；
/// 武器新分支升级 = 建一张 WeaponUpgrade 卡（RequiresAbilityId/TargetAbilityId 指向武器卡）。
/// </summary>
[CreateAssetMenu(fileName = "Ability", menuName = "Game/能力")]
public class AbilityConfig : ScriptableObject
{
    [Header("显示")]
    [Tooltip("唯一 ID（等级表/前置引用/武器强化引用都用它，必填且不可重复）")]
    public string AbilityId;
    public string AbilityName;        // 显示名："攻击强化"
    [TextArea] public string Description;   // 描述："攻击 +1"
    public Sprite Icon;

    [Header("解锁与升级")]
    public AbilityCategory Category = AbilityCategory.PassiveStat;
    [Tooltip("最大等级。0 = 无限升级；>0 = 升到该级后不再被抽到（首次选中 = 1 级 = 解锁）")]
    public int MaxLevel = 5;
    [Tooltip("抽取权重（权重轮盘，0 = 永不出现）")]
    public float RollWeight = 1f;
    [Tooltip("前置能力 ID：该能力至少 1 级（已解锁）时本卡才会被抽到；留空 = 无前置")]
    public string RequiresAbilityId;

    [Header("每级数值")]
    [Tooltip("每级增量表：第 N 级带来的增量。表用尽后按 ValuePerLevel 继续线性成长")]
    public float[] LevelValues;
    [Tooltip("表外/未填表时的每级增量（不填 LevelValues 即恒定增量，天然支持无限升级）")]
    public float ValuePerLevel = 1f;

    [Header("效果（PassiveStat 用）")]
    [Tooltip("作用到哪些玩家属性（同卡内各属性共用每级数值；想不同数值就拆成多张卡）")]
    public StatModifier[] StatEffects;

    [Header("效果（WeaponUpgrade 用）")]
    [Tooltip("要强化的武器能力 ID（对应武器卡的 AbilityId）")]
    public string TargetAbilityId;
    [Tooltip("强化武器的哪些属性（同卡内共用每级数值，按加法成长）")]
    public WeaponStatModifier[] WeaponStatModifiers;

    /// <summary>
    /// 第 level 级带来的增量（level 从 1 开始）。
    /// LevelValues 表内取表，表外（含未填表）按 ValuePerLevel 线性继续——无限升级卡的核心取值口。
    /// </summary>
    public float GetLevelValue(int level)
    {
        if (LevelValues != null && LevelValues.Length > 0 && level <= LevelValues.Length)
        {
            return LevelValues[level - 1];
        }
        return ValuePerLevel;
    }
}
