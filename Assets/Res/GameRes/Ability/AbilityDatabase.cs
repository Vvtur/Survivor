using UnityEngine;

/// <summary>
/// 能力数据库：一个资产持有全部能力引用，供 AbilityPoolSystem 随机抽取
/// </summary>
[CreateAssetMenu(fileName = "AbilityDatabase", menuName = "Game/能力数据库")]
public class AbilityDatabase : ScriptableObject
{
    [Tooltip("能力池：把所有能力资产拖进来")]
    public AbilityConfig[] AllAbilities;
}
