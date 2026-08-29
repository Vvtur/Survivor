using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 玩家受到伤害：扣 1 点 HP（下限钳到 0）。
    /// QF 规范：改数据必须经过 Command（此前表现层在 OnTriggerStay2D 里直接
    /// mModel.HP.Value--，属于表现层越层直写 Model）。
    /// 下限保护写在 Command 内：所有扣血路径共用，HUD 不会出现负数血量。
    /// </summary>
    public class DamagePlayerCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = this.GetModel<GameModel>();
            model.HP.Value = Mathf.Max(0, model.HP.Value - 1);
        }
    }
}
