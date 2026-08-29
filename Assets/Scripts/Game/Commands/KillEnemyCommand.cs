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
            var model = this.GetModel<GameModel>();
            model.AliveEnemies.Value--;
            model.KillCount.Value++;   // 本局击杀数（HUD 显示用）
            this.SendEvent(new EnemyKilledEvent()); // 数据变更通知
        }
    }
}
