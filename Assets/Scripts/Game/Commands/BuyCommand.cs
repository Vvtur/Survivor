using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 商店购买攻击：扣金币、Attack+1。
    /// 字段命名为 mCost 以遵循 QFramework 命名规范（mXxx 前缀）。
    /// Command 是无状态的一次性任务，对象用完即丢。
    /// </summary>
    public class BuyCommand : AbstractCommand
    {
        private readonly int mCost;

        public BuyCommand(int cost)
        {
            mCost = cost;
        }

        protected override void OnExecute()
        {
            var model = this.GetModel<GameModel>();
            if (model.Money.Value < mCost) return;

            model.Money.Value -= mCost;
            model.Attack.Value += 1;
        }
    }
}
