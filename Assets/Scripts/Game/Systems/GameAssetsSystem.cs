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
        GameObject SpawnEnemy(Vector3 position);
    }

    /// <summary>
    /// 游戏资源系统：统一加载并长期持有战斗中高频复用的预制体（Gem、Enemy 等）。
    /// WebGL 平台必须用异步加载（Add2Load + LoadAsync），加载完成后持有引用。
    /// </summary>
    public class GameAssetsSystem : AbstractSystem, IGameAssetsSystem
    {
        private ResLoader mResLoader;

        private GameObject mGemPrefab;
        private GameObject mEnemyPrefab;

        protected override void OnInit()
        {
            mResLoader = ResLoader.Allocate();

            // WebGL 只能异步加载，加载完成后填充字段
            mResLoader.Add2Load<GameObject>("Gem", (b, res) =>
            {
                if (b) mGemPrefab = res.Asset.As<GameObject>();
                else LogKit.E("[GameAssetsSystem] 加载 Gem 预制体失败！请确认已标记 AB（资源名 Gem）");
            });
            mResLoader.Add2Load<GameObject>("Enemy", (b, res) =>
            {
                if (b) mEnemyPrefab = res.Asset.As<GameObject>();
                else LogKit.E("[GameAssetsSystem] 加载 Enemy 预制体失败！请确认已标记 AB（资源名 Enemy）");
            });
            mResLoader.LoadAsync();
        }

        /// <summary>在指定位置生成一个经验宝石</summary>
        public GameObject SpawnGem(Vector3 position)
        {
            return mGemPrefab != null ? Object.Instantiate(mGemPrefab, position, Quaternion.identity) : null;
        }

        /// <summary>在指定位置生成一个敌人</summary>
        public GameObject SpawnEnemy(Vector3 position)
        {
            return mEnemyPrefab != null ? Object.Instantiate(mEnemyPrefab, position, Quaternion.identity) : null;
        }

        protected override void OnDeinit()
        {
            mResLoader?.Recycle2Cache(); // 系统卸载时才释放资源引用
            mResLoader = null;
            mGemPrefab = null;
            mEnemyPrefab = null;
            base.OnDeinit();
        }
    }
}
