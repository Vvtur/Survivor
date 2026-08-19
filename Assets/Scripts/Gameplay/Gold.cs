using UnityEngine;
using QFramework;

namespace QFramework.Gameplay
{
	public partial class Gold : ViewController
	{
		private void OnTriggerEnter2D(Collider2D collision)
		{
			if (collision.CompareTag("Player"))
			{
				// 拾取金币：Money +1（自动存档，跨局保留）
				GameArchitecture.Interface.SendCommand(new PickupGoldCommand());
				Destroy(gameObject);
			}
		}
	}
}
