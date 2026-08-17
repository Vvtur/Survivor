using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class Enemy : ViewController, IController
	{
		public IArchitecture GetArchitecture() => GameArchitecture.Interface; // 满足 IController
		public float Speed = 2f; // 敌人移动速度
		public float HP = 5;

		void Start()
		{
			// 动态生成的敌人无法在 Inspector 拖引用，运行时按 Tag 自动查找玩家
			if (Player == null)
			{
				Player = GameObject.FindWithTag("Player");
			}
			this.SendCommand<EnemySpawnCommand>(); // 登记：场上敌人 +1
		}

		void Update()
		{
			if (Player == null) return; // 玩家不存在（未找到/已销毁）时静止
			var dir = (Player.transform.position - transform.position).normalized;
			SelfRigidbody2D.linearVelocity = dir * Speed;
		}

		public void TakeDamage(float damage)
		{
			FlashRed();

			HP -= damage;
			if (HP <= 0)
			{
				DropGem(); // 死亡掉落经验宝石
				this.SendCommand<KillEnemyCommand>(); // 通知系统：敌人被击杀
				Destroy(this.gameObject);
			}
		}

		/// <summary>
		/// 在敌人位置掉落一个经验宝石（预制体由 GameAssetsSystem 长期持有，避免重复加载）
		/// </summary>
		private void DropGem()
		{
			this.GetSystem<IGameAssetsSystem>().SpawnGem(transform.position);
		}

		/// <summary>
		/// 受击闪红：变红 0.1 秒后恢复原色（ActionKit 链式）
		/// </summary>
		private void FlashRed()
		{
			var originalColor = SelfSpriteRenderer.color;
			ActionKit.Sequence()
				.Callback(() => SelfSpriteRenderer.color = Color.red)
				.Delay(0.3f)
				.Callback(() => SelfSpriteRenderer.color = originalColor)
				.Start(this)
				.IgnoreTimeScale(); // 不受 timeScale=0 影响
		}
    }
}
