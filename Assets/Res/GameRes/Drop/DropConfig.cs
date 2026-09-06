using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掉落物类型。新增掉落物：加枚举值 → GameAssetsSystem 的 prefab 名映射加一行 →
/// DropConfig.asset 加一个条目（权重），无需改动任何系统逻辑。
/// </summary>
public enum DropType
{
    Nothing = 0,  // 什么都不掉（RollDrop 抽中后直接跳过生成，不掉落率本身也可作为可调权重）
    Gold,         // 金币（跨局货币）
    Gem,          // 经验宝石
    HealthPack,   // 回血道具：拾取后回满血
    Magnet,       // 磁铁：拾取后收集场上全部金币/宝石
}

/// <summary>
/// 敌人掉落配置（ScriptableObject，Inspector 可调，策划零代码改概率）。
/// 由 DropSystem 在架构初始化时异步加载（AB 名 data），敌人死亡时按权重随机掉落。
/// </summary>
[CreateAssetMenu(fileName = "DropConfig", menuName = "Game/掉落配置")]
public class DropConfig : ScriptableObject
{
    [Serializable]
    public class DropEntry
    {
        public DropType Type = DropType.Gold;
        [Tooltip("权重（相对值，无需加和为 100）")]
        public float Weight = 1f;
    }

    [Tooltip("掉落条目：每次敌人死亡按权重抽取其中一项")]
    public List<DropEntry> Entries = new List<DropEntry>();
}
