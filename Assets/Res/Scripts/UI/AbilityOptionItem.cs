using QFramework;
using QFramework.Gameplay;
using UnityEngine;

namespace QFramework.UI
{
	public partial class AbilityOptionItem : ViewController
	{
		// 由面板调用，填充单个能力的数据（currentLevel = 当前等级，0 = 未解锁将首次解锁）
		public void SetData(AbilityConfig ability, int currentLevel)
		{
			Img_Icon.sprite = ability.Icon;
			// 角标：首次选中 = 解锁；已解锁显示等级推进（MaxLevel<=0 = 无限升级）
			var levelTag = currentLevel <= 0
				? "解锁"
				: $"Lv{currentLevel}→{(ability.MaxLevel <= 0 ? "∞" : (currentLevel + 1).ToString())}";
			Txt_Des.text = $"{ability.AbilityName}（{levelTag}）\n{ability.Description}";
		}
	}
}
