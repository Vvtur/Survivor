using UnityEngine;
using QFramework;

namespace QFramework.Gameplay
{
	public partial class Gem : ViewController, IController
	{
		void Start()
		{
			// Code Here
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			if (collision.CompareTag("Player"))
			{
				this.SendCommand<PlayerGetGemCommand>(); // 经验 +1（写 Model）
				Destroy(gameObject);                     // 宝石消失
			}
		}
	}
}
