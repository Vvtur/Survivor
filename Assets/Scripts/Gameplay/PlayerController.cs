using QFramework;
using QFramework.UI;
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class PlayerController : ViewController, IController
	{
		public IArchitecture GetArchitecture() => GameArchitecture.Interface;

		// 玩家属性统一从 GameModel 读取，能力系统通过 Command 修改后即时生效
		float Speed => this.GetModel<GameModel>().MoveSpeed.Value;
		float AttackDamage => this.GetModel<GameModel>().AttackDamage.Value;
		float AttackInterval => this.GetModel<GameModel>().AttackInterval.Value;
		float AttackRadius => this.GetModel<GameModel>().AttackRadius.Value;

		InputSystem_Actions input;
		float lastAttackTime = -99f;  // 上次攻击时间
		float lastHitTime = -99f;     // 上次主角受击时间

		void Awake()
		{
			input = new();
			this.RegisterEvent<GameWinEvent>(OnGameWin);   // 订阅胜利事件（架构事件系统）
			this.RegisterEvent<LevelUpEvent>(OnLevelUp);  // 订阅升级事件
		}

		void Start()
		{
			// 打开常驻玩家信息面板（显示 HP/EXP/LV，数据由 Model 驱动刷新）
			// WebGL 下 AB 只能异步加载，用 OpenPanelAsync（同步 OpenPanel 首次加载 uiprefab 包必失败）；
			// 协程挂自己身上：若面板打开前场景就被卸载（秒死回 GameStart），面板也无需再开
			StartCoroutine(UIKit.OpenPanelAsync<PlayerInfoPanel>());
		}

		// 升级：从能力池随机抽 3 个能力，打开选择面板
		private void OnLevelUp(LevelUpEvent e)
		{
			Time.timeScale = 0f; // 升级暂停游戏

			// 随机抽取 3 个不重复的能力
			var options = this.GetSystem<IAbilityPoolSystem>().RollOptions(3);

			// 类名与预制体名一致（GameLevelUpPanel），无需传 prefabName
			// WebGL 下 AB 只能异步加载，用 OpenPanelAsync（同步 OpenPanel 首次加载 uiprefab 包必失败）
			StartCoroutine(UIKit.OpenPanelAsync<GameLevelUpPanel>(
				uiData: new GameLevelUpPanelData
				{
					Options = options
				}));
		}

		void OnEnable()
		{
			input.Enable();

			// 配置攻击检测过滤器：只检测 Enemy 层，避免命中玩家自身/宝石等
			mAttackFilter.useTriggers = true;
			mAttackFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
		}

		void OnDisable()
		{
			input.Disable();
		}

		void OnDestroy()
		{
			// 注意：这里不能调 UIKit.ClosePanel 关面板——编辑器停止 Play 时，部分析构帧里
			// isPlaying 仍为 true（守卫不可靠），QF 惰性单例会在析构期重新 Instantiate UIRoot，
			// 触发 "Some objects were not cleaned up" 警告。
			// PlayerInfoPanel 的关闭改由 GameRoot.OnSceneLoaded(GameStart) 负责。
			this.UnRegisterEvent<GameWinEvent>(OnGameWin);   // 取消订阅
			this.UnRegisterEvent<LevelUpEvent>(OnLevelUp);  // 取消订阅
			input.Dispose();
		}

		// 游戏胜利：由 GameManagerSystem 发送 GameWinEvent 触发
		private void OnGameWin(GameWinEvent e)
		{
			GameOver();
		}

		void Update()
		{
			SelfRigidbody2D.linearVelocity = input.Player.Move.ReadValue<Vector2>() * Speed;
			TryAttackAllInRange();
		}

		// 无 GC 分配的碰撞检测结果缓冲（复用避免每帧分配）
		private readonly Collider2D[] mAttackHits = new Collider2D[16];
		private ContactFilter2D mAttackFilter;

		// 攻击范围内所有敌人（一次 CD 打全部，符合割草游戏群攻）
		private void TryAttackAllInRange()
		{
			// 攻击 CD
			if (Time.time - lastAttackTime < AttackInterval) return;

			// Unity 6 无 GC 版本：结果写入预分配的缓冲数组，返回实际命中数量
			// 圆心用玩家自身位置，半径用 AttackRadius 配置
			var hitCount = Physics2D.OverlapCircle(transform.position, AttackRadius, mAttackFilter, mAttackHits);

			bool hitAny = false;
			for (int i = 0; i < hitCount; i++)
			{
				var col = mAttackHits[i];
				if (!col.gameObject.CompareTag("Enemy")) continue;

				var enemy = col.gameObject.GetComponent<Enemy>();
				if (enemy != null)
				{
					enemy.TakeDamage(AttackDamage);
					hitAny = true;
				}
			}

			// 只要打到至少一个，就消耗本次攻击
			if (hitAny)
			{
				lastAttackTime = Time.time;
			}
		}

		// 玩家被敌人碰到 → 扣血（固定 1 秒受击间隔）
		private void OnTriggerStay2D(Collider2D collision)
		{
			if (!collision.gameObject.CompareTag("Enemy")) return;

			// 只有身体（方形）碰到敌人才扣血
			if (!SelfBoxCollider2D.IsTouching(collision)) return;
			if (Time.time - lastHitTime < 1f) return;
			lastHitTime = Time.time;

			var model = this.GetModel<GameModel>();
			model.HP.Value--;
			if (model.HP.Value <= 0)
			{
				GameOver();
			}
		}

		private void GameOver()
		{
			// WebGL 下 AB 只能异步加载，用 OpenPanelAsync；
			// 协程宿主用常驻 GameRoot（本对象马上 SetActive(false) 会杀掉自己身上的协程）
			GameRoot.Instance.StartCoroutine(UIKit.OpenPanelAsync<GameOverPanel>());
			gameObject.SetActive(false); // 主角消失
			Time.timeScale = 0f;         // 暂停
		}
	}
}
