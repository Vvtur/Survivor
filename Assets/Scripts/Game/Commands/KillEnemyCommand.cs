using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 敌人被击杀：修改敌人数数据并发出变更事件（携带死亡位置，供 DropSystem 掉落）。
    /// </summary>
    public class KillEnemyCommand : AbstractCommand
    {
        private readonly Vector3 mPosition; // 敌人死亡位置

        public KillEnemyCommand(Vector3 position)
        {
            mPosition = position;
        }

        protected override void OnExecute()
        {
            var model = this.GetModel<GameModel>();
            model.AliveEnemies.Value--;
            model.KillCount.Value++;   // 本局击杀数（HUD 显示用）
            this.SendEvent(new EnemyKilledEvent { Position = mPosition }); // 数据变更通知
        }
    }
}
