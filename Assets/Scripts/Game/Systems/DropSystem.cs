using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 掉落系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IDropSystem : ISystem
    {
        /// <summary>设置某掉落物的权重乘区（后续"掉率升级"选项的写入入口，1 = 不变）</summary>
        void SetWeightMultiplier(DropType type, float multiplier);

        /// <summary>在当前乘区上叠加（如 +20% 掉率 → 乘 1.2）</summary>
        void AddWeightMultiplier(DropType type, float delta);

        /// <summary>读取当前乘区（升级 UI 显示用）</summary>
        float GetWeightMultiplier(DropType type);

        /// <summary>重置全部乘区（每局开始调用，防止局内升级的掉率加成跨局残留）</summary>
        void ResetRun();
    }

    /// <summary>
    /// 掉落系统（规则层）：监听 EnemyKilledEvent，按 DropConfig 权重决定掉落物类型，
    /// 委托 IGameAssetsSystem 在敌人死亡位置生成掉落物。
    ///
    /// 设计要点：
    /// - EnemyKilledEvent 是架构预留的事件订阅点（此前无订阅者），掉落规则收敛在本系统，
    ///   表现层（Enemy）不再关心"掉什么"；
    /// - 掉落概率全部来自 DropConfig 资产（AB 名 data），改概率零代码；
    /// - Nothing 条目 = 什么都不掉，其权重同样可调（"不掉率"也是一种可升级选项）；
    /// - 运行时乘区 mWeightMultipliers 支撑后续"掉率升级选项"：
    ///   未来升级卡 Command 内调 this.GetSystem&lt;IDropSystem&gt;().AddWeightMultiplier(...)
    ///   （ICommand : ICanGetSystem，写数据链路 Command → System 合规）；
    /// - 加载范式与 AbilityPoolSystem 一致：异步加载 + ResLoader 长期持有，防止 SO 被卸载。
    /// </summary>
    public class DropSystem : AbstractSystem, IDropSystem
    {
        private DropConfig mConfig;   // 掉落配置（AB 异步加载，初始基准权重）
        private ResLoader mResLoader; // 持有 ResLoader 防止 SO 资源被回收
        private IGameAssetsSystem mAssetsSystem; // 生成掉落物（惰性解析，规避注册顺序问题）

        // 运行时权重乘区（每型独立，默认 1）：升级选项改这里，不回写配置资产
        private readonly Dictionary<DropType, float> mWeightMultipliers = new Dictionary<DropType, float>();

        protected override void OnInit()
        {
            // 加载掉落配置（WebGL 平台必须异步加载）
            mResLoader = ResLoader.Allocate();
            mResLoader.Add2Load<DropConfig>("data", "DropConfig", (b, res) =>
            {
                if (b)
                {
                    mConfig = res.Asset.As<DropConfig>();
                }
                else
                {
                    LogKit.E("[DropSystem] 加载 DropConfig 失败！请确认 DropConfig.asset 已标记 AB（ab 名 data），掉落将失效");
                    mConfig = null;
                }
            });
            mResLoader.LoadAsync();

            // 订阅击杀事件（System 随架构常驻，无需反注册）
            this.RegisterEvent<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnEnemyKilled(EnemyKilledEvent e)
        {
            var dropType = RollDrop();
            if (dropType == null) return; // 抽中 Nothing / 配置未加载 / 无有效条目：不掉落

            if (mAssetsSystem == null)
            {
                mAssetsSystem = this.GetSystem<IGameAssetsSystem>(); // 惰性缓存（事件触发时架构必然已初始化完成）
            }
            mAssetsSystem.SpawnDrop(dropType.Value, e.Position);
        }

        /// <summary>
        /// 按权重随机抽取掉落类型（基准权重 × 运行时乘区）。
        /// 返回 null = 什么都不掉（含抽中 Nothing 的情况）。
        /// </summary>
        private DropType? RollDrop()
        {
            if (mConfig == null || mConfig.Entries == null || mConfig.Entries.Count == 0) return null;

            // 求总权重（跳过非正权重条目）
            float total = 0f;
            foreach (var entry in mConfig.Entries)
            {
                if (entry != null && EffectiveWeight(entry) > 0f) total += EffectiveWeight(entry);
            }
            if (total <= 0f) return null;

            // 轮盘赌：随机点落在哪个区间就掉哪个
            var roll = Random.value * total;
            foreach (var entry in mConfig.Entries)
            {
                var weight = EffectiveWeight(entry);
                if (entry == null || weight <= 0f) continue;
                roll -= weight;
                if (roll <= 0f)
                {
                    // Nothing 表示"什么都不掉"：与配置未加载同义处理，不生成任何物体
                    return entry.Type == DropType.Nothing ? (DropType?)null : entry.Type;
                }
            }

            return null; // 浮点误差兜底
        }

        /// <summary>有效权重 = 配置基准权重 × 运行时乘区（未设过乘区 = 1；显式设 0 = 永不掉落）</summary>
        private float EffectiveWeight(DropConfig.DropEntry entry)
        {
            return mWeightMultipliers.TryGetValue(entry.Type, out var multiplier)
                ? entry.Weight * multiplier
                : entry.Weight;
        }

        /// <summary>设置权重乘区（升级选项写入入口）。传 1 恢复基准掉率，传 0 = 永不掉落。</summary>
        public void SetWeightMultiplier(DropType type, float multiplier)
        {
            mWeightMultipliers[type] = Mathf.Max(0f, multiplier);
        }

        /// <summary>在当前乘区上叠加（+20% 掉率 → delta 传 0.2）</summary>
        public void AddWeightMultiplier(DropType type, float delta)
        {
            var current = GetWeightMultiplier(type);
            mWeightMultipliers[type] = Mathf.Max(0f, current + delta);
        }

        public float GetWeightMultiplier(DropType type)
        {
            mWeightMultipliers.TryGetValue(type, out var current);
            return current == 0f ? 1f : current;
        }

        /// <summary>每局开始重置乘区：局内升级的掉率加成不跨局残留（防跨局污染，见 CODE_WIKI 坑位 8.3）</summary>
        public void ResetRun()
        {
            mWeightMultipliers.Clear();
        }

        protected override void OnDeinit()
        {
            mResLoader?.Recycle2Cache();
            mResLoader = null;
            mConfig = null;
            mWeightMultipliers.Clear();
            base.OnDeinit();
        }
    }
}
