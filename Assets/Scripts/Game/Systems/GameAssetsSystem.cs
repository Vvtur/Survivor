using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 游戏资源系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IGameAssetsSystem : ISystem
    {
        GameObject SpawnGem(Vector3 position);
        GameObject SpawnGold(Vector3 position);
        GameObject SpawnEnemy(Vector3 position);
        GameObject SpawnDrop(DropType type, Vector3 position);
        /// <summary>向指定方向发射一把穿透剑（伤害/穿透数按等级从 PierceSwordConfig 换算）</summary>
        PierceSword SpawnPierceSword(Vector3 position, Vector2 direction, int level);
        Weapon SpawnWeapon(Vector3 targetEnemyPos, float damage, Enemy target);
    }

    /// <summary>
    /// 游戏资源系统：统一加载并长期持有战斗中高频复用的预制体（Gem、Gold、Enemy 等）。
    /// WebGL 平台必须用异步加载（Add2Load + LoadAsync），加载完成后持有引用。
    /// </summary>
    public class GameAssetsSystem : AbstractSystem, IGameAssetsSystem
    {
        private ResLoader mResLoader;

        private GameObject mGemPrefab;
        private GameObject mGoldPrefab;
        private GameObject mEnemyPrefab;
        private GameObject mWeaponPrefab;
        private GameObject mHealthPackPrefab;
        private GameObject mMagnetPrefab;
        private GameObject mPierceSwordPrefab;
        private PierceSwordConfig mPierceSwordConfig; // 穿透剑数值配置（AB data，长期持有）

        protected override void OnInit()
        {
            mResLoader = ResLoader.Allocate();

            // WebGL 只能异步加载，加载完成后填充字段
            mResLoader.Add2Load<GameObject>("Gem", (b, res) =>
            {
                if (b) mGemPrefab = res.Asset.As<GameObject>();
                else LogKit.E("[GameAssetsSystem] 加载 Gem 预制体失败！请确认已标记 AB（资源名 Gem）");
            });
            mResLoader.Add2Load<GameObject>("Gold", (b, res) =>
            {
                if (b) mGoldPrefab = res.Asset.As<GameObject>();
                else LogKit.E("[GameAssetsSystem] 加载 Gold 预制体失败！请确认已标记 AB（资源名 Gold）");
            });
            mResLoader.Add2Load<GameObject>("Enemy", (b, res) =>
            {
                if (b) mEnemyPrefab = res.Asset.As<GameObject>();
                else LogKit.E("[GameAssetsSystem] 加载 Enemy 预制体失败！请确认已标记 AB（资源名 Enemy）");
            });
            mResLoader.Add2Load<GameObject>("Weapon", (b, res) =>
            {
                if (b)
                {
                    mWeaponPrefab = res.Asset.As<GameObject>();
                    // 配置对象池工厂：MonoBehaviour 不能 new，从预制体实例化
                    SafeObjectPool<Weapon>.Instance.SetFactoryMethod(() =>
                        Object.Instantiate(mWeaponPrefab).GetComponent<Weapon>());
                }
                else
                {
                    LogKit.E("[GameAssetsSystem] 加载 Weapon 预制体失败！请确认已标记 AB（资源名 Weapon）");
                }
            });
            mResLoader.Add2Load<GameObject>("HealthPack", (b, res) =>
            {
                if (b) mHealthPackPrefab = res.Asset.As<GameObject>();
                else LogKit.E("[GameAssetsSystem] 加载 HealthPack 预制体失败！请确认已标记 AB（资源名 HealthPack）");
            });
            mResLoader.Add2Load<GameObject>("Magnet", (b, res) =>
            {
                if (b) mMagnetPrefab = res.Asset.As<GameObject>();
                else LogKit.E("[GameAssetsSystem] 加载 Magnet 预制体失败！请确认已标记 AB（资源名 Magnet）");
            });
            mResLoader.Add2Load<GameObject>("PierceSword", (b, res) =>
            {
                if (b)
                {
                    mPierceSwordPrefab = res.Asset.As<GameObject>();
                    // 配置对象池工厂：MonoBehaviour 不能 new，必须从预制体实例化。
                    // SafeObjectPool 默认工厂是 new T()，对 MonoBehaviour 会产出
                    // gameObject 为 null 的假对象（NRE：get_gameObject），Weapon 同款写法。
                    SafeObjectPool<PierceSword>.Instance.SetFactoryMethod(() =>
                        Object.Instantiate(mPierceSwordPrefab).GetComponent<PierceSword>());
                }
                else
                {
                    LogKit.E("[GameAssetsSystem] 加载 PierceSword 预制体失败！请确认已标记 AB（资源名 PierceSword）");
                }
            });
            mResLoader.Add2Load<PierceSwordConfig>("data", "PierceSwordConfig", (b, res) =>
            {
                if (b) mPierceSwordConfig = res.Asset.As<PierceSwordConfig>();
                else LogKit.E("[GameAssetsSystem] 加载 PierceSwordConfig 失败！请确认已标记 AB（ab 名 data）");
            });
            mResLoader.LoadAsync();
        }

        /// <summary>在指定位置生成一个经验宝石</summary>
        public GameObject SpawnGem(Vector3 position)
        {
            return mGemPrefab != null ? Object.Instantiate(mGemPrefab, position, Quaternion.identity) : null;
        }

        /// <summary>在指定位置生成一个金币</summary>
        public GameObject SpawnGold(Vector3 position)
        {
            return mGoldPrefab != null ? Object.Instantiate(mGoldPrefab, position, Quaternion.identity) : null;
        }

        /// <summary>在指定位置生成一个敌人</summary>
        public GameObject SpawnEnemy(Vector3 position)
        {
            return mEnemyPrefab != null ? Object.Instantiate(mEnemyPrefab, position, Quaternion.identity) : null;
        }

        /// <summary>
        /// 按掉落类型在指定位置生成掉落物（由 DropSystem 掷点后调用）。
        /// 新增掉落物：DropType 加枚举值 → 此处 switch 加一个 case + 预加载一个预制体。
        /// </summary>
        public GameObject SpawnDrop(DropType type, Vector3 position)
        {
            var prefab = type switch
            {
                DropType.Gold => mGoldPrefab,
                DropType.Gem => mGemPrefab,
                DropType.HealthPack => mHealthPackPrefab,
                DropType.Magnet => mMagnetPrefab,
                DropType.Nothing => null, // 什么都不掉：正常流程在 DropSystem.RollDrop 已拦截，此处兼防
                _ => null,
            };

            if (prefab == null)
            {
                LogKit.E($"[GameAssetsSystem] 掉落物 {type} 预制体未加载，无法生成");
                return null;
            }

            return Object.Instantiate(prefab, position, Quaternion.identity);
        }

        /// <summary>
        /// 发射一把穿透剑：伤害/穿透数按等级从 PierceSwordConfig 换算（数据驱动，零硬编码）：
        /// 伤害 = Damage + DamagePerLevel × (等级 - 1)；穿透数 = BasePierce + 等级。
        /// </summary>
        public PierceSword SpawnPierceSword(Vector3 position, Vector2 direction, int level)
        {
            if (mPierceSwordPrefab == null || mPierceSwordConfig == null)
            {
                LogKit.E("[GameAssetsSystem] PierceSword 预制体/配置未加载，无法发射穿透剑");
                return null;
            }
            if (level < 1) return null; // 未解锁

            var damage = mPierceSwordConfig.Damage + mPierceSwordConfig.DamagePerLevel * (level - 1);
            var maxHits = mPierceSwordConfig.BasePierce + level;

            var sword = PierceSword.Allocate();
            sword.Init(position, direction, damage, maxHits,
                mPierceSwordConfig.Speed, mPierceSwordConfig.Lifetime, mPierceSwordConfig.HitRadius,
                mPierceSwordConfig.SpriteRotationOffset);
            return sword;
        }

        /// <summary>
        /// 在目标敌人位置附近生成一个近战武器（对象池复用）：
        /// 武器播放挥砍动画，动画事件触发 Attack() 对目标敌人造成伤害。
        /// </summary>
        public Weapon SpawnWeapon(Vector3 targetEnemyPos, float damage, Enemy target)
        {
            if (mWeaponPrefab == null)
            {
                LogKit.E("[GameAssetsSystem] Weapon 预制体未加载，无法生成武器");
                return null;
            }

            var weapon = Weapon.Allocate();
            weapon.Init(targetEnemyPos, damage, target);
            return weapon;
        }

        protected override void OnDeinit()
        {
            mResLoader?.Recycle2Cache(); // 系统卸载时才释放资源引用
            mResLoader = null;
            mGemPrefab = null;
            mGoldPrefab = null;
            mEnemyPrefab = null;
            mWeaponPrefab = null;
            mHealthPackPrefab = null;
            mMagnetPrefab = null;
            mPierceSwordPrefab = null;
            mPierceSwordConfig = null;
            SafeObjectPool<Weapon>.Instance.Clear();
            SafeObjectPool<PierceSword>.Instance.Clear();
            base.OnDeinit();
        }
    }
}
