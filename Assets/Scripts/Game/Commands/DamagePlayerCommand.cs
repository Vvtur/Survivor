using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 玩家受到伤害：扣 1 点 HP。
    /// QF 规范：改数据必须经过 Command（此前表现层在 OnTriggerStay2D 里直接
    /// mModel.HP.Value--，属于表现层越层直写 Model）。
    /// </summary>
    public class DamagePlayerCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            this.GetModel<GameModel>().HP.Value--;
        }
    }
}
