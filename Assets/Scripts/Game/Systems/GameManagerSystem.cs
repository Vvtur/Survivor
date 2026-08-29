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
    ///
    /// 一次经验增加可能跨越多级（磁铁吸多颗宝石：每颗 +1 经验，但回调只在最后一次 +N 时跑一次），
    /// 用 while 循环连续升级；只要 PendingLevelUps 从 0 变到 >0，才发一次 LevelUpEvent，
    /// 避免表现层在同一帧内开 N 个升级面板。
    ///
    /// 注意：升级时写 model.Exp.Value -= need 会同步重入本回调（BindableProperty 同步 Trigger），
    /// 所以用 mInUpgrading 守门防止重入；while 循环内部走 SetValueWithoutEvent 避免链式回调。
    /// </summary>
    public class LevelUpSystem : AbstractSystem, ILevelUpSystem
    {
        // 防重入：升级过程中改 Exp 会被 BindableProperty 同步触发回调，
        // 用本地循环而非依赖回调重入，避免同一帧内同一回调被多次进入造成计数错乱。
        private bool mInUpgrading;

        protected override void OnInit()
        {
            var model = this.GetModel<GameModel>();
            // 监听经验变化，够阈值就升级并广播
            model.Exp.Register(newExp =>
            {
                if (mInUpgrading) return;   // 重入兜底：while 循环已手动处理，不希望回调再走一遍

                bool wasPending = model.PendingLevelUps.Value > 0;
                mInUpgrading = true;
                try
                {
                    // 升级所需经验会随 Level 变化（公式：Level * 2 + 1），所以用 while
                    // 循环一次性把所有可达升级都判定完；公式单调递增，newExp 不断减少、
                    // 阈值不断增大，自然退出。
                    while (newExp >= model.ExpToNextLevel)
                    {
                        var need = model.ExpToNextLevel;
                        // 走 SetValueWithoutEvent：避免每次减 Exp 都同步触发本回调；
                        // while 内部已经显式递减 newExp，逻辑与原 if 版完全等价。
                        model.Exp.SetValueWithoutEvent(newExp - need);
                        newExp -= need;
                        model.Level.Value++;
                        model.PendingLevelUps.Value++;
                    }
                }
                finally
                {
                    mInUpgrading = false;
                }
                // 仅在"从无到有"的转换时发一次事件：表现层只需知道"该弹一次升级面板了"，
                // 剩余未选次数由 Model.PendingLevelUps 跟踪，GameLevelUpPanel 关闭时自取。
                if (!wasPending && model.PendingLevelUps.Value > 0)
                {
                    this.SendEvent(new LevelUpEvent()); // 通知表现层：打开升级面板
                }
            });
        }
    }
}
