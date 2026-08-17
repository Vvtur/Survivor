using QFramework;
using QFramework.Gameplay;
using UnityEngine;

namespace QFramework.UI
{
	public partial class AbilityOptionItem : ViewController
	{
		// 由面板调用，填充单个能力的数据
		public void SetData(AbilityConfig ability)
		{
			Img_Icon.sprite = ability.Icon;
			Txt_Des.text = $"{ability.AbilityName}\n{ability.Description}";
		}
	}
}
