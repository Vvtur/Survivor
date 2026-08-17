using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 能力池系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IAbilityPoolSystem : ISystem
    {
        AbilityConfig[] RollOptions(int count);
    }

    /// <summary>
    /// 能力池：从能力数据库加载全部能力，随机抽取不重复的 N 个作为升级选项
    /// </summary>
    public class AbilityPoolSystem : AbstractSystem, IAbilityPoolSystem
    {
        private AbilityConfig[] mAllAbilities;   // 能力池（全部能力）

        // 持有 ResLoader 不立即回收，避免 ResKit 在 Recycle2Cache 时卸载已加载的 SO
        private ResLoader mResLoader;

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
                    LogKit.E("[AbilityPoolSystem] 加载 AbilityDatabase 失败！请确认 Data 文件夹已标记 AB（ab 名 data）");
                    mAllAbilities = new AbilityConfig[0];
                }
            });
            mResLoader.LoadAsync();
        }

        protected override void OnDeinit()
        {
            mResLoader?.Recycle2Cache();
            mResLoader = null;
            mAllAbilities = null;
            base.OnDeinit();
        }

        /// <summary>
        /// 随机抽取 count 个不重复的能力
        /// </summary>
        public AbilityConfig[] RollOptions(int count)
        {
            if (mAllAbilities == null || mAllAbilities.Length == 0)
            {
                return new AbilityConfig[0];
            }

            // 洗牌取前 count 个，保证不重复
            var pool = new List<AbilityConfig>(mAllAbilities);
            var result = new List<AbilityConfig>();

            for (int i = pool.Count - 1; i >= 0 && result.Count < count; i--)
            {
                int index = Random.Range(0, i + 1);
                result.Add(pool[index]);
                pool[index] = pool[i];
            }

            return result.ToArray();
        }
    }
}