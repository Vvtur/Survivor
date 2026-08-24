using UnityEngine;
using QFramework;

namespace QFramework.Gameplay
{
	/// <summary>
	/// 近战武器：从 QFramework SafeObjectPool 取出，播放挥砍动画；
	/// 动画事件（Attack 帧）触发 Attack()，对目标敌人造成伤害；
	/// 动画播放完毕自动回收入池（SetActive(false)）复用。
	/// 目标敌人由玩家在生成武器时注入（按距离最近的 N 个敌人排序后传入）。
	/// </summary>
	[RequireComponent(typeof(Animator))]
	public partial class Weapon : ViewController, IPoolable, IPoolType
	{
		// 攻击动画状态名哈希（避免字符串查找）
		static readonly int AttackHash = Animator.StringToHash("Attack");

		// 本次攻击伤害与目标敌人（玩家注入）
		float mDamage;
		Enemy mTarget;
		// 相对目标敌人的偏移（生成时设定，动画期间跟随目标保持该偏移）
		Vector3 mOffset;

		// 无 GC 分配的碰撞检测缓冲（复用避免每帧分配）
		readonly Collider2D[] mHitBuffer = new Collider2D[16];
		// 本次攻击命中的敌人去重缓冲（复用，避免同一敌人被重复结算）
		readonly System.Collections.Generic.List<Enemy> mHitEnemies = new(16);
		// 单次挥砍最多命中的敌人数（AOE 上限）
		const int MaxHitCount = 5;
		ContactFilter2D mFilter;

		Animator mAnimator;

		// ---- QFramework PoolKit：SafeObjectPool 接口实现 ----

		/// <summary>是否已被回收（SafeObjectPool 约束）</summary>
		public bool IsRecycled { get; set; }

		/// <summary>从对象池取出一个武器</summary>
		public static Weapon Allocate()
		{
			return SafeObjectPool<Weapon>.Instance.Allocate();
		}

		/// <summary>回收到对象池</summary>
		public void Recycle2Cache()
		{
			SafeObjectPool<Weapon>.Instance.Recycle(this);
		}

		/// <summary>被回收入池时回调：清状态 + 隐藏武器</summary>
		public void OnRecycled()
		{
			mTarget = null;
			mDamage = 0f;
			gameObject.SetActive(false);
		}

		void Awake()
		{
			mAnimator = GetComponent<Animator>();

			// 攻击检测过滤器：只检测 Enemy 层
			mFilter.useTriggers = true;
			mFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
		}

		/// <summary>
		/// 初始化武器（玩家攻击时调用）：
		/// 定位到目标敌人位置靠左一点、注入伤害与目标敌人、从头播攻击动画。
		/// </summary>
		public void Init(Vector3 targetEnemyPos, float damage, Enemy target)
		{
			mDamage = damage;
			mTarget = target;

			// 生成在敌人位置「靠左一点」，并叠加一点随机偏移（左右/上下微调），
			// 让每把剑的落点略有差异，视觉更自然；未来升级可扩展为扇形分布。
			const float OffsetX = -1f;
			const float Jitter = 0.5f; // 随机抖动幅度
			mOffset = new Vector3(
				OffsetX + Random.Range(-Jitter, Jitter),
				Random.Range(-Jitter, Jitter),
				0f);
			gameObject.SetActive(true);
			transform.position = targetEnemyPos + mOffset;

			if (mAnimator != null)
			{
				mAnimator.Play(AttackHash, 0, 0f);
			}
		}

		void Update()
		{
			if (IsRecycled) return;

			// 跟随目标敌人移动（怪物在走，剑保持相对偏移，避免挥空）
			if (mTarget != null)
			{
				transform.position = mTarget.transform.position + mOffset;
			}

			if (mAnimator == null) return;

			// 攻击动画（非循环）播放完毕 → 回收入池隐藏
			var state = mAnimator.GetCurrentAnimatorStateInfo(0);
			if (state.shortNameHash == AttackHash && state.normalizedTime >= 1f)
			{
				Recycle2Cache();
			}
		}

		/// <summary>
		/// 由攻击动画事件（Attack 帧）调用：
		/// AOE 攻击：以武器碰撞体（trigger）范围为判定区，命中范围内敌人，
		/// 但单次最多命中 MaxHitCount 个（优先目标敌人，再按距离取最近）。
		/// </summary>
		public void Attack()
		{
			// 以武器碰撞体 bounds 作为 AOE 判定区，检测范围内敌人
			var bounds = SelfBoxCollider2D.bounds;
			var hitCount = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, mFilter, mHitBuffer);

			mHitEnemies.Clear();
			bool targetHit = false;

			// 收集范围内所有敌人（去重）
			for (int i = 0; i < hitCount; i++)
			{
				if (mHitBuffer[i].TryGetComponent<Enemy>(out var enemy))
				{
					if (enemy == mTarget) targetHit = true;
					if (!mHitEnemies.Contains(enemy)) mHitEnemies.Add(enemy);
				}
			}

			// 目标敌人若因剑偏移而落在碰撞体外，补打一次，保证目标必被命中
			if (mTarget != null && !targetHit)
			{
				mHitEnemies.Add(mTarget);
			}

			// 若命中数超上限，按距武器中心的距离排序，只保留最近的 MaxHitCount 个
			// （目标敌人优先级最高，已保证在列表内）
			if (mHitEnemies.Count > MaxHitCount)
			{
				var center = bounds.center;
				mHitEnemies.Sort((a, b) =>
					(a.transform.position - center).sqrMagnitude
						.CompareTo((b.transform.position - center).sqrMagnitude));
				mHitEnemies.RemoveRange(MaxHitCount, mHitEnemies.Count - MaxHitCount);
			}

			// 对最终命中的敌人结算伤害
			for (int i = 0; i < mHitEnemies.Count; i++)
			{
				mHitEnemies[i].TakeDamage(mDamage);
			}

			// 命中音效（至少命中一个敌人才播，避免空挥也有声）
			if (mHitEnemies.Count > 0) AudioKit.PlaySound(AudioNames.Hit);
			}

		void OnDestroy()
		{
			// 场景卸载：清空静态对象池，避免跨局残留失效引用
			SafeObjectPool<Weapon>.Instance.Clear();
		}
	}
}