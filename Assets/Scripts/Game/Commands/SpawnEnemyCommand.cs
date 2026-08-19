using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 生成一个敌人：由 GameAssetsSystem 长期持有的 Enemy 预制体实例化，可指定强度系数
    /// （敌人出生登记由 Enemy.Start 里的 EnemySpawnCommand 完成）
    /// </summary>
    public class SpawnEnemyCommand : AbstractCommand
    {
        private readonly UnityEngine.Vector3 mSpawnPosition;
        private readonly float mPower; // 强度系数（≥1，随时间递增）

        public SpawnEnemyCommand(UnityEngine.Vector3 spawnPosition, float power = 1f)
        {
            mSpawnPosition = spawnPosition;
            mPower = power;
        }

        protected override void OnExecute()
        {
            var enemy = this.GetSystem<IGameAssetsSystem>().SpawnEnemy(mSpawnPosition);
            if (enemy != null)
            {
                // 按强度系数提升敌人血量（随时间越刷越强）
                var enemyComp = enemy.GetComponent<Enemy>();
                if (enemyComp != null && mPower > 1f)
                {
                    enemyComp.HP = Mathf.RoundToInt(enemyComp.HP * mPower);
                }
            }
        }
    }
}
