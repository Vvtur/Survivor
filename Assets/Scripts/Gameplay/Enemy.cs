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

		// 玩家对象缓存（静态）：所有敌人共用一次 FindWithTag 结果，避免每个敌人出生都做一次全局查找。
		// 玩家被销毁（死亡重开）后 Unity 的 == 重载判定为 null，下一只敌人会自动重新查找，无跨局残留。
		static GameObject sPlayer;

		// 受击闪红：原色只缓存一次 + 计时恢复。
		// 旧写法每次受击都取"当前颜色"当原色，连续受击时会把红色记成原色 → 敌人永久变红。
		Color mOriginalColor;
		bool mColorCached;
		float mFlashTimer;

		void Start()
		{
			// 动态生成的敌人无法在 Inspector 拖引用，运行时按 Tag 自动查找玩家
			if (Player == null)
			{
				if (sPlayer == null) sPlayer = GameObject.FindWithTag("Player");
				Player = sPlayer;
			}
			CacheOriginalColor();  // 尽早固定原色（闪红恢复的基准）
			this.SendCommand<EnemySpawnCommand>(); // 登记：场上敌人 +1
		}

		void Update()
		{
			if (mIsDying) return; // 死亡中：速度已归零，不再移动，也不再改色（让渐隐动画独占颜色）

			// 闪红计时：用 unscaledDeltaTime，暂停（升级面板 timeScale=0）时也能正常恢复原色
			if (mFlashTimer > 0f)
			{
				mFlashTimer -= Time.unscaledDeltaTime;
				if (mFlashTimer <= 0f) SelfSpriteRenderer.color = mOriginalColor;
			}

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
		/// 受击闪红：变红 0.3 秒后恢复原色。
		/// 用计时器而非 ActionKit.Sequence：连续受击只会刷新计时（不会叠加多条动画把红色写成原色），
		/// 且每次受击零分配。
		/// </summary>
		private void FlashRed()
		{
			CacheOriginalColor();  // 兜底：极端情况下 Start 之前就被打到
			SelfSpriteRenderer.color = Color.red;
			mFlashTimer = 0.3f;
		}

		/// <summary>缓存原始颜色（只缓存一次，作为闪红恢复的基准）</summary>
		private void CacheOriginalColor()
		{
			if (mColorCached) return;
			if (SelfSpriteRenderer == null) return;
			mOriginalColor = SelfSpriteRenderer.color;
			mColorCached = true;
		}

		/// <summary>
		/// 出生染色（分层怪用，SpawnEnemyCommand 在出生同帧调用）：把染色记为"原色"，
		/// 闪红恢复时以染色为基准（Start 尚未跑也能正确缓存）。
		/// </summary>
		public void SetTint(Color tint)
		{
			if (SelfSpriteRenderer == null) return;
			SelfSpriteRenderer.color = tint;
			mOriginalColor = tint;
			mColorCached = true;
		}
    }
}
