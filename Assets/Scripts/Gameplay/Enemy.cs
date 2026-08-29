using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class Enemy : ViewController, IController
	{
		public IArchitecture GetArchitecture() => GameArchitecture.Interface; // 满足 IController
		public float Speed = 2f; // 敌人移动速度
		public float HP = 5;

		bool mIsDying; // 死亡中（速度归零 + 渐隐，期间不再移动）

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
			if (mIsDying) return; // 死亡中：速度已归零，不再移动
			if (Player == null) return; // 玩家不存在（未找到/已销毁）时静止
			var dir = (Player.transform.position - transform.position).normalized;
			SelfRigidbody2D.linearVelocity = dir * Speed;
		}

		public void TakeDamage(float damage)
		{
			if (mIsDying) return; // 死亡中不重复触发
			FlashRed();
			ShowDamageNumber(damage);

			HP -= damage;
			if (HP <= 0)
			{
				Die();
			}
		}

		// 在敌人头顶弹出伤害数字（对象池复用，零 GC 分配）
		private void ShowDamageNumber(float damage)
		{
			// 略随机偏移，避免叠字
			var pos = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 1.2f, 0);
			DamageTextPool.Instance.Show(pos, damage, Color.red);
		}

		// 死亡：速度归零 → 1 秒渐隐 → 销毁
		private void Die()
		{
			mIsDying = true;
			AudioKit.PlaySound(AudioNames.EnemyDie); // 死亡音效
			SelfRigidbody2D.linearVelocity = Vector2.zero; // 速度归零
			SelfBoxCollider2D.enabled = false;

			// 通知系统：敌人被击杀（携带死亡位置，DropSystem 收到事件后决定掉落什么）
			this.SendCommand(new KillEnemyCommand(transform.position));

			// 1 秒内透明度渐变到 0，完成后销毁（ActionKit 链式）
			var originalColor = SelfSpriteRenderer.color;
			ActionKit.Sequence()
				.Lerp(originalColor.a, 0f, 1f, a =>
				{
					var c = SelfSpriteRenderer.color;
					SelfSpriteRenderer.color = new Color(c.r, c.g, c.b, a);
				})
				.Callback(() =>
				{
					if (SelfSpriteRenderer != null)
					{
						SelfSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
					}
					Destroy(gameObject);
				})
				.Start(this);
				// .IgnoreTimeScale(); // 不受 timeScale=0 影响
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
