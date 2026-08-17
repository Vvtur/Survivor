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
    /// 系统层：只管规则，不存数据（数据在 GameModel）
    /// </summary>
    public class GameManagerSystem : AbstractSystem, IGameManagerSystem
    {
        protected override void OnInit()
        {
            // 监听击杀事件，检查胜利条件：所有波次打完且场上没有敌人
            this.RegisterEvent<EnemyKilledEvent>(e =>
            {
                var model = this.GetModel<GameModel>();
                if (model.CurrentWave.Value > model.TotalWaves.Value && model.AliveEnemies.Value <= 0)
                {
                    this.SendEvent(new GameWinEvent()); // 通知表现层：胜利
                }
            });
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
