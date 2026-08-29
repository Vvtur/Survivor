using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
	/// <summary>
	/// 磁铁：拾取后收集场上全部"可磁吸"掉落物（金币/经验宝石）。
	/// 本身不写任何 Model 数据——收集到的每个掉落物各自走 OnPickup 发 Command，
	/// 数据链路与玩家手动拾取完全一致（QF 规范：写数据必须经过 Command）。
	/// 拾取流程由基类 PickupItem 统一处理。
	/// </summary>
	public class Magnet : PickupItem
	{
		protected override void OnPickup()
		{
			// 磁铁拾取是低频操作，FindObjectsOfType 的开销可接受；
			// 若未来掉落物数量膨胀，可改为 GameAssetsSystem 维护生成登记表（O(1) 遍历）。
			var items = FindObjectsOfType<PickupItem>();
			foreach (var item in items)
			{
				if (item == this) continue;      // 不收集自己
				item.CollectByMagnet();          // 可磁吸的掉落物静默生效并消失
			}
		}

		protected override string PickupSound => AudioNames.Skill; // 磁铁用技能音效区分"扫场"
	}
}
