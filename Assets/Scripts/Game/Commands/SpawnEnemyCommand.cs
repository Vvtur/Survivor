using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 生成一个敌人：由 GameAssetsSystem 长期持有的 Enemy 预制体实例化
    /// （敌人出生登记由 Enemy.Start 里的 EnemySpawnCommand 完成）
    /// </summary>
    public class SpawnEnemyCommand : AbstractCommand
    {
        private readonly UnityEngine.Vector3 mSpawnPosition;

        public SpawnEnemyCommand(UnityEngine.Vector3 spawnPosition)
        {
            mSpawnPosition = spawnPosition;
        }

        protected override void OnExecute()
        {
            this.GetSystem<IGameAssetsSystem>().SpawnEnemy(mSpawnPosition);
        }
    }
}
