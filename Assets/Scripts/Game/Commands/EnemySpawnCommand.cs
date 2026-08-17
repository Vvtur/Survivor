using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 敌人出生登记：修改敌人数数据
    /// </summary>
    public class EnemySpawnCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            this.GetModel<GameModel>().AliveEnemies.Value++;
        }
    }
}
