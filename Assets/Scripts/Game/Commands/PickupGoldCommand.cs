using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 拾取金币：Money +1（Money.Register 自动存档，跨局保留）
    /// </summary>
    public class PickupGoldCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            this.GetModel<GameModel>().Money.Value++;
        }
    }
}
