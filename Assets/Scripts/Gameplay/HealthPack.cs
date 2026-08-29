using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
	/// <summary>
	/// 回血道具：拾取后 HP 直接回满（上限为当前 MaxHp）。
	/// 生效 = HealPlayerCommand（HP 变更走 Command，与 DamagePlayerCommand 对称）；
	/// 拾取流程由基类 PickupItem 统一处理。
	/// </summary>
	public class HealthPack : PickupItem
	{
		protected override void OnPickup()
		{
			this.SendCommand<HealPlayerCommand>(); // 回满血（写 Model，PlayerInfoPanel 自动刷新）
		}
	}
}
