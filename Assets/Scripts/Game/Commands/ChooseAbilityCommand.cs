using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 玩家选择能力：按能力效果修改 Model 中对应的属性值
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

            var model = this.GetModel<GameModel>();
            switch (mAbility.Effect)
            {
                case AbilityEffect.AttackUp:
                    model.AttackDamage.Value += mAbility.Value;
                    break;

                case AbilityEffect.SpeedUp:
                    model.MoveSpeed.Value += mAbility.Value;
                    break;

                case AbilityEffect.MaxHpUp:
                    model.MaxHp.Value += (int)mAbility.Value;
                    model.HP.Value += (int)mAbility.Value; // 当前血量同步增加，立即生效
                    break;

                case AbilityEffect.AttackSpeedUp:
                    // 攻击速度提升 = 攻击间隔缩短（下限保护，防止变成无限攻击）
                    var interval = model.AttackInterval.Value * mAbility.Value;
                    model.AttackInterval.Value = interval < 0.05f ? 0.05f : interval;
                    break;

                case AbilityEffect.AttackRangeUp:
                    // 攻击范围扩大（加法累加，如每级 +1）
                    model.AttackRadius.Value += mAbility.Value;
                    break;

                default:
                    LogKit.E($"[ChooseAbilityCommand] 未处理的能力效果: {mAbility.Effect}");
                    break;
            }
        }
    }
}
