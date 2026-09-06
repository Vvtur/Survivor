using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 回血道具生效：HP 直接回满（上限为当前 MaxHp）。
    /// 与 DamagePlayerCommand 对称：HP 的所有变更必须走 Command，表现层不得直写 Model。
    /// </summary>
    public class HealPlayerCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = this.GetModel<GameModel>();
            model.HP.Value = model.MaxHp.Value; // 回满血
        }
    }
}
