using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace QFramework.Gameplay
{
	/// <summary>
	/// 穿透剑投射物：解锁穿透剑能力后，玩家攻击时向最近的敌人发射。
	/// 方向在发射瞬间定死、飞行途中不转向（固定弹道，怪物可走位躲开）；
	/// 命中判定按等级携带穿透数，同一敌人只结算一次，穿透用尽或超时自动回池。
	/// 从 QFramework SafeObjectPool 取出/回收（与 Weapon 同一套池化模式，零 GC）。
	/// </summary>
	public class PierceSword : MonoBehaviour, IPoolable, IPoolType
	{
		// 飞行方向（发射瞬间定死，不再改变）
		Vector2 mDirection;
		// 运行参数（生成时由 GameAssetsSystem 按 PierceSwordConfig + 等级换算注入）
		float mSpeed;
		float mDamage;
		float mHitRadius;
		float mLifetime;
		int mMaxHits;

		float mTimer;                 // 已飞行时间
		int mHitCount;                // 已穿透敌人数
		bool mFired;                  // 是否在有效飞行中（防止刚实例化未 Init 就跑 Update）

		// 无 GC 碰撞检测缓冲（与 PlayerController / Weapon 同款预分配模式）
		readonly Collider2D[] mHitBuffer = new Collider2D[8];
		// 已命中敌人去重（同一敌人不重复结算）
		readonly List<Enemy> mHitEnemies = new(8);
		ContactFilter2D mFilter;

		// ---- QFramework PoolKit：SafeObjectPool 接口实现 ----

		/// <summary>是否已被回收（SafeObjectPool 约束）</summary>
		public bool IsRecycled { get; set; }

		/// <summary>从对象池取出一把穿透剑</summary>
		public static PierceSword Allocate()
		{
			return SafeObjectPool<PierceSword>.Instance.Allocate();
		}

		/// <summary>回收到对象池</summary>
		public void Recycle2Cache()
		{
			SafeObjectPool<PierceSword>.Instance.Recycle(this);
		}

		/// <summary>被回收入池时回调：清状态 + 隐藏</summary>
		public void OnRecycled()
		{
			mFired = false;
			mHitEnemies.Clear();
			gameObject.SetActive(false);
		}

		void Awake()
		{
			// 命中检测过滤器：只检测 Enemy 层
			mFilter.useTriggers = true;
			mFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
			gameObject.SetActive(false); // 池工厂刚实例化时先隐藏，Init 再显示
		}

		/// <summary>
		/// 初始化（玩家攻击时由 GameAssetsSystem.SpawnPierceSword 调用）：
		/// 定位、锁定弹道方向（朝向目标）、注入按等级换算后的属性。
		/// </summary>
		/// <param name="position">发射位置（玩家位置）</param>
		/// <param name="direction">弹道方向（指向最近敌人，归一化）</param>
		/// <param name="spriteAngle">贴图朝向修正角（贴图默认朝 +X 时不偏转）</param>
		public void Init(Vector3 position, Vector2 direction, float damage, int maxHits,
			float speed, float lifetime, float hitRadius, float spriteAngle)
		{
			gameObject.SetActive(true);
			transform.position = position;

			mDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
			mDamage = damage;
			mMaxHits = maxHits;
			mSpeed = speed;
			mLifetime = lifetime;
			mHitRadius = hitRadius;
			mTimer = 0f;
			mHitCount = 0;
			mHitEnemies.Clear();
			mFired = true;

			// 弹道朝向：剑头指向飞行方向（生成瞬间定死，飞行途中不追踪 → 怪可躲）
			var angle = Mathf.Atan2(mDirection.y, mDirection.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngle);
		}

		void Update()
		{
			if (IsRecycled || !mFired) return;

			// 固定方向匀速飞行
			transform.position += (Vector3)(mDirection * (mSpeed * Time.deltaTime));
			mTimer += Time.deltaTime;
			if (mTimer >= mLifetime)
			{
				Recycle2Cache();
				return;
			}

			// 无 GC 圆形检测：结算飞行路径上的敌人
			var hitCount = Physics2D.OverlapCircle(transform.position, mHitRadius, mFilter, mHitBuffer);
			var hitAny = false;
			for (int i = 0; i < hitCount; i++)
			{
				if (!mHitBuffer[i].TryGetComponent<Enemy>(out var enemy)) continue;
				if (mHitEnemies.Contains(enemy)) continue; // 穿透不重复结算同一敌人

				mHitEnemies.Add(enemy);
				enemy.TakeDamage(mDamage);
				mHitCount++;
				hitAny = true;

				// 穿透数用尽：就地消失
				if (mHitCount >= mMaxHits)
				{
					PlayHitSound(hitAny);
					Recycle2Cache();
					return;
				}
			}

			PlayHitSound(hitAny);
		}

		// 命中音效只播一次：一次挥砍穿透 N 个敌人时，避免同一帧叠 N 次音导致爆音
		private static void PlayHitSound(bool hitAny)
		{
			if (hitAny) AudioKit.PlaySound(AudioNames.Hit);
		}

		void OnDestroy()
		{
			// 场景卸载：清空对象池，避免跨局残留失效引用（与 Weapon 同款坑位处理）
			SafeObjectPool<PierceSword>.Instance.Clear();
		}
	}
}
