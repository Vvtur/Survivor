using System.Collections.Generic;
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
        /// <summary>生成一个敌人：tier 决定预制体与分层（null = 基础怪 Enemy）</summary>
        GameObject SpawnEnemy(Vector3 position, EnemyTier tier = null);
        GameObject SpawnDrop(DropType type, Vector3 position);
        /// <summary>向指定方向发射一把穿透剑（伤害/穿透数按能力分支卡从 AbilitySystem 聚合）</summary>
        PierceSword SpawnPierceSword(Vector3 position, Vector2 direction);
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
        // 敌人预制体表：key = AB 资源名（"Enemy" = 基础怪；分层怪按需懒加载）
        private readonly Dictionary<string, GameObject> mEnemyPrefabs = new Dictionary<string, GameObject>();
        private readonly HashSet<string> mLoadingEnemyPrefabs = new HashSet<string>(); // 防重复发起加载
        private GameObject mWeaponPrefab;
        private GameObject mHealthPackPrefab;
        private GameObject mMagnetPrefab;
        private GameObject mPierceSwordPrefab;
        private PierceSwordConfig mPierceSwordConfig; // 穿透剑物理参数配置（AB data，长期持有）
        private IAbilitySystem mAbilitySystem;        // 能力数值聚合（分支升级卡的加成出口，惰性解析规避注册顺序）

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
                if (b) mEnemyPrefabs["Enemy"] = res.Asset.As<GameObject>();
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

        /// <summary>
        /// 在指定位置生成一个敌人：预制体按分层解析（基础怪 / 分层怪专用预制体）。
        /// </summary>
        public GameObject SpawnEnemy(Vector3 position, EnemyTier tier = null)
        {
            var prefab = ResolveEnemyPrefab(tier);
            return prefab != null ? Object.Instantiate(prefab, position, Quaternion.identity) : null;
        }

        /// <summary>
        /// 按分层取敌人预制体：未配置分层、名字为空或 "Enemy" = 基础怪；
        /// 分层怪预制体未加载时发起异步加载（首次只加载一次），本次先用基础怪顶替。
        /// </summary>
        private GameObject ResolveEnemyPrefab(EnemyTier tier)
        {
            mEnemyPrefabs.TryGetValue("Enemy", out var basePrefab);

            if (tier == null || string.IsNullOrWhiteSpace(tier.PrefabName)) return basePrefab;
            var prefabName = tier.PrefabName.Trim();
            if (prefabName == "Enemy") return basePrefab;

            if (mEnemyPrefabs.TryGetValue(prefabName, out var prefab) && prefab != null)
            {
                return prefab;
            }

            TryLoadEnemyPrefabAsync(prefabName); // 未就绪：本次回落基础怪，加载完成后自动切换
            return basePrefab;
        }

        /// <summary>懒加载分层怪预制体（同一名字只发起一次；失败回落基础怪并报错一次）</summary>
        private void TryLoadEnemyPrefabAsync(string prefabName)
        {
            if (!mLoadingEnemyPrefabs.Add(prefabName)) return;

            LogKit.I($"[GameAssetsSystem] 异步加载分层怪预制体：{prefabName}");
            mResLoader.Add2Load<GameObject>(prefabName, (b, res) =>
            {
                if (b)
                {
                    mEnemyPrefabs[prefabName] = res.Asset.As<GameObject>();
                    LogKit.I($"[GameAssetsSystem] 分层怪预制体加载完成：{prefabName}");
                }
                else
                {
                    LogKit.E($"[GameAssetsSystem] 加载分层怪预制体 {prefabName} 失败！请确认已标记 AB，该怪将回落基础怪 Enemy");
                }
            });
            mResLoader.LoadAsync();
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
        /// 发射一把穿透剑：数值全部由能力等级驱动（分支卡加成由 AbilitySystem 聚合）：
        /// 伤害 = 基础伤害 + Σ(伤害强化卡贡献)；穿透数 = 基础穿透 + Σ(穿透强化卡贡献)；
        /// 命中半径/飞行速度/存活时间同理可被对应分支卡强化。解锁状态由能力等级表决定，本方法不感知等级。
        /// </summary>
        public PierceSword SpawnPierceSword(Vector3 position, Vector2 direction)
        {
            if (mPierceSwordPrefab == null || mPierceSwordConfig == null)
            {
                LogKit.E("[GameAssetsSystem] PierceSword 预制体/配置未加载，无法发射穿透剑");
                return null;
            }

            mAbilitySystem ??= this.GetSystem<IAbilitySystem>();
            var damage = mPierceSwordConfig.Damage
                + mAbilitySystem.GetWeaponStatTotal(AbilitySystem.PierceSwordId, WeaponStat.Damage);
            var maxHits = mPierceSwordConfig.BasePierce
                + Mathf.RoundToInt(mAbilitySystem.GetWeaponStatTotal(AbilitySystem.PierceSwordId, WeaponStat.PierceCount));
            var hitRadius = mPierceSwordConfig.HitRadius
                + mAbilitySystem.GetWeaponStatTotal(AbilitySystem.PierceSwordId, WeaponStat.Radius);
            var speed = mPierceSwordConfig.Speed
                + mAbilitySystem.GetWeaponStatTotal(AbilitySystem.PierceSwordId, WeaponStat.Speed);
            var lifetime = mPierceSwordConfig.Lifetime
                + mAbilitySystem.GetWeaponStatTotal(AbilitySystem.PierceSwordId, WeaponStat.Lifetime);

            var sword = PierceSword.Allocate();
            sword.Init(position, direction, damage, maxHits,
                speed, lifetime, hitRadius, mPierceSwordConfig.SpriteRotationOffset);
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
            mEnemyPrefabs.Clear();
            mLoadingEnemyPrefabs.Clear();
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
