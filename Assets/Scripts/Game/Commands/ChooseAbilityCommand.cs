using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 玩家选择能力卡：等级 +1（0→1 即解锁），被动属性随后统一派生重算。
    /// 所有能力的数值成长都来自 AbilityConfig（LevelValues/ValuePerLevel + StatEffects/WeaponStatModifiers），
    /// 本命令不再 switch 各效果——新增能力/分支升级卡只需建 SO 资产，零代码。
    /// 武器卡（Weapon 类）只改等级，实际数值由 GameAssetsSystem 生成投射物时按 GetWeaponStatTotal 聚合。
    /// </summary>
    public class ChooseAbilityCommand : AbstractCommand
    {
        private readonly AbilityConfig mAbility;

        public ChooseAbilityCommand(AbilityConfig ability)
        {
            mAbility = ability;
        }

        protected override void OnExecute()
        {
            if (mAbility == null) return;

            // 配置错误防线：AbilityId 是等级表的 key，漏填则等级永远记不上（选了等于白选）
            if (string.IsNullOrEmpty(mAbility.AbilityId))
            {
                LogKit.E($"[ChooseAbilityCommand] 能力卡 {mAbility.name} 缺少 AbilityId，无法记录等级！请在资产上补填唯一 ID");
                return;
            }

            var model = this.GetModel<GameModel>();
            var abilitySystem = this.GetSystem<IAbilitySystem>();

            // 满级兜底：正常流程 RollOptions 已过滤满级卡，这里防异常路径（连点/重复发命令）。
            // MaxLevel <= 0 = 无限升级，永不拦截。
            var level = model.GetAbilityLevel(mAbility.AbilityId);
            if (mAbility.MaxLevel > 0 && level >= mAbility.MaxLevel) return;

            model.SetAbilityLevel(mAbility.AbilityId, level + 1);

            // 被动属性 = 基础值 + 能力贡献 的派生值，任何能力选择后统一重算
            if (mAbility.Category == AbilityCategory.PassiveStat)
            {
                abilitySystem.RecalcStats();
            }

            // 广播等级变化。订阅点：需要"选中某能力后生效"的系统规则卡
            //（如未来"掉率强化"卡，DropSystem 订阅此事件按 Id 调 AddWeightMultiplier）
            this.SendEvent(new AbilityLeveledEvent
            {
                AbilityId = mAbility.AbilityId,
                NewLevel = level + 1,
            });
        }
    }
}
