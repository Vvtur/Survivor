using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
	/// <summary>
	/// 经验宝石：拾取后经验 +1（升级判定由 LevelUpSystem 完成）。
	/// 拾取流程（碰撞/音效/销毁/磁铁联动）由基类 PickupItem 统一处理。
	/// </summary>
	public partial class Gem : PickupItem
	{
		public override bool CollectibleByMagnet => true;

		protected override void OnPickup()
		{
			this.SendCommand<PlayerGetGemCommand>(); // 经验 +1（写 Model）
		}
	}
}
