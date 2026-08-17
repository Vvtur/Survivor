using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 敌人被击杀：修改敌人数数据并发出变更事件
    /// </summary>
    public class KillEnemyCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            this.GetModel<GameModel>().AliveEnemies.Value--;
            this.SendEvent(new EnemyKilledEvent()); // 数据变更通知
        }
    }
}
