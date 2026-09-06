using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 生成一个敌人：预制体由分层（EnemyTier）决定（null = 基础怪 Enemy），
    /// 并按分层倍率（血量/速度/体型/染色）+ 强度系数（随时间递增）调整数值。
    /// （敌人出生登记由 Enemy.Start 里的 EnemySpawnCommand 完成）
    /// </summary>
    public class SpawnEnemyCommand : AbstractCommand
    {
        private readonly UnityEngine.Vector3 mSpawnPosition;
        private readonly float mPower;       // 强度系数（≥1，随时间递增）
        private readonly EnemyTier mTier;    // 分层（null = 基础怪）

        public SpawnEnemyCommand(UnityEngine.Vector3 spawnPosition, float power = 1f, EnemyTier tier = null)
        {
            mSpawnPosition = spawnPosition;
            mPower = power;
            mTier = tier;
        }

        protected override void OnExecute()
        {
            var enemy = this.GetSystem<IGameAssetsSystem>().SpawnEnemy(mSpawnPosition, mTier);
            if (enemy == null) return;

            var enemyComp = enemy.GetComponent<Enemy>();
            if (enemyComp == null) return;

            // 分层倍率：同预制体也能长出不同怪（数值/体型/颜色）
            if (mTier != null)
            {
                enemyComp.HP = Mathf.Max(1, Mathf.RoundToInt(enemyComp.HP * mTier.HpMultiplier));
                enemyComp.Speed *= mTier.SpeedMultiplier;
                if (!Mathf.Approximately(mTier.ScaleMultiplier, 1f))
                {
                    enemy.transform.localScale *= mTier.ScaleMultiplier;
                }
                if (mTier.Tint != Color.white)
                {
                    enemyComp.SetTint(mTier.Tint);
                }
            }

            // 按强度系数提升敌人血量（随时间越刷越强）
            if (mPower > 1f)
            {
                enemyComp.HP = Mathf.Max(1, Mathf.RoundToInt(enemyComp.HP * mPower));
            }
        }
    }
}
