using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 玩家完成一次升级选项：PendingLevelUps -1。
    /// 仅在 GameLevelUpPanel 按钮点击后调用，与 ChooseAbilityCommand 配对使用：
    /// 选能力改数据、扣计数推进升级队列。
    /// QF 规范：表现层改 Model 必须走 Command，禁止直写 BindableProperty。
    /// </summary>
    public class ConsumePendingLevelUpCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = this.GetModel<GameModel>();
            if (model.PendingLevelUps.Value > 0)
            {
                model.PendingLevelUps.Value--;
            }
            // 防御性兜底：异常路径下也不让计数跌成负数（视觉/逻辑都依赖 ≥0）。
        }
    }
}
