using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 能力系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IAbilitySystem : ISystem
    {
        /// <summary>
        /// 抽 N 张不重复的升级卡。池内自动过滤：
        /// 已满级（MaxLevel &gt; 0 且等级已达上限）剔除；前置能力未解锁（RequiresAbilityId）剔除；
        /// 按 RollWeight 权重轮盘抽取。MaxLevel &lt;= 0 = 无限升级，永不满级。
        /// </summary>
        AbilityConfig[] RollOptions(int count);

        /// <summary>
        /// 重算被动属性（派生式：GameConfig 基础值 + Σ 各被动能力按当前等级的贡献）。
        /// 每次能力选择后调用：任何改动都从基础值推导，杜绝累加误差与每局重置残留。
        /// </summary>
        void RecalcStats();

        /// <summary>
        /// 聚合某武器的某项属性加成总量（所有 WeaponUpgrade 分支卡贡献之和）。
        /// 武器生成时查询（SpawnPierceSword 等），是"伤害强化/穿透强化/范围强化..."分支卡的数据出口。
        /// </summary>
        float GetWeaponStatTotal(string weaponId, WeaponStat stat);
    }

    /// <summary>
    /// 能力系统：能力池 + 数值成长中枢。
    /// - 池：从 AbilityDatabase 加载全部能力，升级时按状态过滤 + 权重抽取；
    /// - 成长：被动能力 → RecalcStats 派生重算写回 GameModel；武器能力 → 分支卡贡献按 GetWeaponStatTotal 聚合。
    /// </summary>
    public class AbilitySystem : AbstractSystem, IAbilitySystem
    {
        // 能力 ID 常量：跨 Command/System 引用，避免魔法字符串散落（与资产 AbilityId 一致）
        public const string PierceSwordId = "pierce_sword";

        // 空结果复用（不额外引入 System 命名空间：会与 UnityEngine.Random 冲突）
        private static readonly AbilityConfig[] sEmptyOptions = new AbilityConfig[0];

        private AbilityConfig[] mAllAbilities;   // 能力池（全部能力）

        // 持有 ResLoader 不立即回收，避免 ResKit 在 Recycle2Cache 时卸载已加载的 SO
        private ResLoader mResLoader;

        // 抽取缓冲（每次升级才调用，复用 List 减少分配）
        private readonly List<AbilityConfig> mCandidates = new List<AbilityConfig>(16);
        // 结果缓冲：RollOptions 返回 ToArray() 的拷贝，List 本身可以安全复用
        private readonly List<AbilityConfig> mResult = new List<AbilityConfig>(8);

        protected override void OnInit()
        {
            // 加载能力数据库（WebGL 平台必须异步加载）
            mResLoader = ResLoader.Allocate();
            mResLoader.Add2Load<AbilityDatabase>("data", "AbilityDatabase", (b, res) =>
            {
                if (b)
                {
                    var database = res.Asset.As<AbilityDatabase>();
                    // 把数组引用保存到字段。注意：SO 资产本身由 ResLoader 持有，
                    // 不要 Recycle2Cache，否则会卸载 SO，导致引用失效
                    mAllAbilities = database.AllAbilities ?? new AbilityConfig[0];
                }
                else
                {
                    LogKit.E("[AbilitySystem] 加载 AbilityDatabase 失败！请确认 Data 文件夹已标记 AB（ab 名 data）");
                    mAllAbilities = new AbilityConfig[0];
                }
            });
            mResLoader.LoadAsync();
        }

        public AbilityConfig[] RollOptions(int count)
        {
            var result = mResult;
            result.Clear();
            if (mAllAbilities == null || mAllAbilities.Length == 0) return sEmptyOptions;

            var model = this.GetModel<GameModel>();

            // 候选过滤：满级剔除（MaxLevel <= 0 无限级永不剔）、前置未解锁剔除、权重非法剔除
            mCandidates.Clear();
            foreach (var cfg in mAllAbilities)
            {
                if (cfg == null || cfg.RollWeight <= 0f) continue;

                var level = model.GetAbilityLevel(cfg.AbilityId);
                if (cfg.MaxLevel > 0 && level >= cfg.MaxLevel) continue;            // 已满级，不再出现
                if (!string.IsNullOrEmpty(cfg.RequiresAbilityId) &&
                    model.GetAbilityLevel(cfg.RequiresAbilityId) <= 0) continue;    // 前置未解锁（分支卡本体未解锁）

                mCandidates.Add(cfg);
            }

            // 权重轮盘 + 不放回抽取（与 DropSystem.RollDrop 同一套模式）
            while (result.Count < count && mCandidates.Count > 0)
            {
                float total = 0f;
                foreach (var cfg in mCandidates) total += cfg.RollWeight;

                var roll = Random.value * total;
                var picked = mCandidates[mCandidates.Count - 1]; // 浮点误差兜底：取最后一个
                for (int i = 0; i < mCandidates.Count; i++)
                {
                    roll -= mCandidates[i].RollWeight;
                    if (roll <= 0f)
                    {
                        picked = mCandidates[i];
                        break;
                    }
                }

                result.Add(picked);
                mCandidates.Remove(picked);
            }

            return result.ToArray();
        }

        public void RecalcStats()
        {
            var model = this.GetModel<GameModel>();

            // 从基础值出发（GameConfig 初始值缓存），叠加所有被动能力的等级贡献
            var attack = model.BaseAttackDamage;
            var moveSpeed = model.BaseMoveSpeed;
            var interval = model.BaseAttackInterval;
            var radius = model.BaseAttackRadius;
            var maxHp = (float)model.BaseMaxHp;
            var weaponCount = (float)model.BaseWeaponCount;

            if (mAllAbilities != null)
            {
                foreach (var cfg in mAllAbilities)
                {
                    if (cfg == null || cfg.Category != AbilityCategory.PassiveStat || cfg.StatEffects == null) continue;
                    var level = model.GetAbilityLevel(cfg.AbilityId);
                    if (level <= 0) continue;

                    foreach (var mod in cfg.StatEffects)
                    {
                        if (mod == null) continue;
                        switch (mod.Stat)
                        {
                            case PlayerStat.AttackDamage:   attack = ApplyContribution(attack, cfg, level, mod.Mode); break;
                            case PlayerStat.MoveSpeed:      moveSpeed = ApplyContribution(moveSpeed, cfg, level, mod.Mode); break;
                            case PlayerStat.AttackInterval: interval = ApplyContribution(interval, cfg, level, mod.Mode); break;
                            case PlayerStat.AttackRadius:   radius = ApplyContribution(radius, cfg, level, mod.Mode); break;
                            case PlayerStat.MaxHp:          maxHp = ApplyContribution(maxHp, cfg, level, mod.Mode); break;
                            case PlayerStat.WeaponCount:    weaponCount = ApplyContribution(weaponCount, cfg, level, mod.Mode); break;
                        }
                    }
                }
            }

            // 写回（下限保护沿用旧规则：攻击间隔 >= 0.05s 防无限攻速）
            model.AttackDamage.Value = attack;
            model.MoveSpeed.Value = Mathf.Max(0.5f, moveSpeed);
            model.AttackInterval.Value = Mathf.Max(0.05f, interval);
            model.AttackRadius.Value = Mathf.Max(0.5f, radius);

            // 最大生命变化：涨上限同步回血（与旧 MaxHpUp 行为一致），降上限裁剪当前血量
            var newMaxHp = Mathf.Max(1, Mathf.RoundToInt(maxHp));
            if (newMaxHp > model.MaxHp.Value) model.HP.Value += newMaxHp - model.MaxHp.Value;
            if (model.HP.Value > newMaxHp) model.HP.Value = newMaxHp;
            model.MaxHp.Value = newMaxHp;

            model.WeaponCount.Value = Mathf.Max(1, Mathf.RoundToInt(weaponCount));
        }

        public float GetWeaponStatTotal(string weaponId, WeaponStat stat)
        {
            if (mAllAbilities == null) return 0f;

            var model = this.GetModel<GameModel>();
            float total = 0f;

            foreach (var cfg in mAllAbilities)
            {
                if (cfg == null || cfg.Category != AbilityCategory.WeaponUpgrade) continue;
                if (cfg.TargetAbilityId != weaponId) continue;
                var level = model.GetAbilityLevel(cfg.AbilityId);
                if (level <= 0) continue;
                if (cfg.WeaponStatModifiers == null) continue;

                foreach (var mod in cfg.WeaponStatModifiers)
                {
                    if (mod != null && mod.Stat == stat)
                    {
                        // 分支卡统一按加法成长（伤害 +x、穿透 +1、范围 +x...）
                        total += SumContribution(cfg, level);
                    }
                }
            }

            return total;
        }

        protected override void OnDeinit()
        {
            mResLoader?.Recycle2Cache();
            mResLoader = null;
            mAllAbilities = null;
            mCandidates.Clear();
            mResult.Clear();
            base.OnDeinit();
        }

        // ---- 数值聚合 ----

        /// <summary>Σ 第 1..level 级增量（ModifyMode.Add 的等级贡献）</summary>
        static float SumContribution(AbilityConfig cfg, int level)
        {
            float sum = 0f;
            for (int i = 1; i <= level; i++) sum += cfg.GetLevelValue(i);
            return sum;
        }

        /// <summary>Π 第 1..level 级系数（ModifyMode.Multiply 的等级贡献）</summary>
        static float FactorContribution(AbilityConfig cfg, int level)
        {
            float factor = 1f;
            for (int i = 1; i <= level; i++) factor *= cfg.GetLevelValue(i);
            return factor;
        }

        /// <summary>把一张卡在当前等级的贡献按作用方式合并进属性值</summary>
        static float ApplyContribution(float value, AbilityConfig cfg, int level, ModifyMode mode)
        {
            return mode == ModifyMode.Multiply
                ? value * FactorContribution(cfg, level)
                : value + SumContribution(cfg, level);
        }
    }
}
