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
            SafeObjectPool<Weapon>.Instance.Clear();
            base.OnDeinit();
        }
    }
}
