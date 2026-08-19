using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 游戏管理接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IGameManagerSystem : ISystem
    {
    }

    /// <summary>
    /// 系统层：只管规则，不存数据（数据在 GameModel）。
    /// 胜利由 WaveSystem 按存活时间触发（无限刷怪模式），本系统不再监听波次。
    /// </summary>
    public class GameManagerSystem : AbstractSystem, IGameManagerSystem
    {
        protected override void OnInit()
        {
            // 无限刷怪模式下胜利判定已移至 WaveSystem（存活时间到即胜利），此处无需逻辑
        }
    }

    /// <summary>
    /// 升级系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface ILevelUpSystem : ISystem
    {
    }

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
