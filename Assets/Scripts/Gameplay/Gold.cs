using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
	/// <summary>
	/// 金币：拾取后 Money +1（Money.Register 自动存档，跨局保留）。
	/// 拾取流程（碰撞/音效/销毁/磁铁联动）由基类 PickupItem 统一处理。
	/// </summary>
	public partial class Gold : PickupItem
	{
		public override bool CollectibleByMagnet => true;

		protected override void OnPickup()
		{
			this.SendCommand(new PickupGoldCommand()); // 金币 +1（写 Model，自动落盘）
		}
	}
}
