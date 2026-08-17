using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 玩家拾取经验宝石：增加经验
    /// </summary>
    public class PlayerGetGemCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = this.GetModel<GameModel>();
            model.PlayerGem.Value++;
            model.Exp.Value++; // 每颗宝石 +1 经验（触发 LevelUpSystem 升级判断）
        }
    }
}
