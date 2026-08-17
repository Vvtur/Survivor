using UnityEngine;


[CreateAssetMenu(fileName = "Ability", menuName = "Game/能力")]
public class AbilityConfig : ScriptableObject
{
    public string AbilityName;        // 显示名："攻击强化"
    public string Description;        // 描述："攻击 +1"
    public Sprite Icon;
    public AbilityEffect Effect;      // 效果类型（见下）
    public float Value;               // 数值
}

public enum AbilityEffect
{
    AttackUp,      // 攻击力提升
    SpeedUp,       // 移动速度提升
    MaxHpUp,       // 最大血量提升
    AttackSpeedUp, // 攻击速度提升（缩短攻击间隔）
    AttackRangeUp, // 攻击范围扩大
}
