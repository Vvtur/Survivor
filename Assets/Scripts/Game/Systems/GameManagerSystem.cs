using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 升级系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface ILevelUpSystem : ISystem
    {
    }

    /// <summary>
    /// 升级系统：监听经验变化，够阈值就升级并广播 LevelUpEvent。
    /// 表现层（PlayerController）收到事件后打开升级面板。
    /// </summary>
    public class LevelUpSystem : AbstractSystem, ILevelUpSystem
    {
        protected override void OnInit()
        {
            var model = this.GetModel<GameModel>();
            // 监听经验变化，够阈值就升级并广播
            model.Exp.Register(newExp =>
            {
                var need = model.Level.Value * 2 + 1;   // 升级所需经验公式
                if (newExp >= need)
                {
                    model.Exp.Value -= need;
                    model.Level.Value++;
                    this.SendEvent(new LevelUpEvent()); // 通知表现层：打开升级面板
                }
            });
        }
    }
}
