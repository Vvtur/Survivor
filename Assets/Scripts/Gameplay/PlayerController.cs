using QFramework;
using QFramework.UI;
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class PlayerController : ViewController, IController
	{
		/// <summary>
		/// 玩家动画状态（QF FSM）
		/// </summary>
		public enum PlayerAnimState
		{
			Idle,
			Walk,
		}

		public IArchitecture GetArchitecture() => GameArchitecture.Interface;

		// 缓存的 GameModel 引用（避免每次 GetModel 走字典查找）
		GameModel mModel;
		// 缓存的资源系统引用（生成武器等）
		IGameAssetsSystem mAssetsSystem;
		// 武器能力系统：攻击节奏后齐发已解锁的武器能力（穿透剑等）
		IWeaponAbilitySystem mWeaponSystem;

		// 玩家属性统一从缓存的 GameModel 读取，能力系统通过 Command 修改后即时生效
		float Speed => mModel.MoveSpeed.Value;
		// 实际攻击力 = 商店升级攻击（跨局）+ 局内升级攻击（每局）
		float AttackDamage => mModel.Attack.Value + mModel.AttackDamage.Value;
		float AttackInterval => mModel.AttackInterval.Value;
		float AttackRadius => mModel.AttackRadius.Value;

		InputSystem_Actions input;
		FSM<PlayerAnimState> mAnimFSM;   // QF 状态机：驱动待机/移动动画
		Camera mMainCam;                 // 跟随的摄像机
		Vector3 mCamVelocity;            // SmoothDamp 内部速度（消除抖动）
		float lastAttackTime = -99f;  // 上次攻击时间
		float lastHitTime = -99f;     // 上次主角受击时间

		bool isOver = false;

		void Awake()
		{
			mModel = this.GetModel<GameModel>();   // 缓存 Model，避免每帧 GetModel
			mAssetsSystem = this.GetSystem<IGameAssetsSystem>();   // 缓存资源系统，生成武器用
			mWeaponSystem = this.GetSystem<IWeaponAbilitySystem>();   // 缓存武器能力系统，攻击后齐发
			input = new();
			this.RegisterEvent<GameWinEvent>(OnGameWin).UnRegisterWhenGameObjectDestroyed(this);   // 订阅胜利事件（架构事件系统）
			this.RegisterEvent<LevelUpEvent>(OnLevelUp).UnRegisterWhenGameObjectDestroyed(this);  // 订阅升级事件

			InitAnimFSM();
		}

		// 动画名哈希（Animator.Play 用 hash 避免字符串查找）
		readonly int IdleHash = Animator.StringToHash("Idle");
		readonly int WalkHash = Animator.StringToHash("Walk");

		// 初始化动画状态机：Idle 播放 Idle，Walk 播放 Walk（无需 Animator Controller 连线）
		void InitAnimFSM()
		{
			mAnimFSM = new FSM<PlayerAnimState>();

			mAnimFSM.State(PlayerAnimState.Idle)
				.OnEnter(() => SelfAnimator.Play(IdleHash));

			mAnimFSM.State(PlayerAnimState.Walk)
				.OnEnter(() => SelfAnimator.Play(WalkHash));

			mAnimFSM.StartState(PlayerAnimState.Idle);
		}

		void Start()
		{
			// 每局开局重置局内数据（死亡重开也生效）：HP/Exp/Level/局内攻击力归零
			mModel.ResetRunData();
			// 刷怪系统局内状态归零（System 只初始化一次，不随场景重载重跑：
			// 不重置则上一局的累计时间会带到下一局——敌人强度沿用、胜利判定提前）
			this.GetSystem<IWaveSystem>().ResetRun();
			// 掉落乘区归位重置（局内掉率升级不跨局残留，与 WaveSystem.ResetRun 同理）
			this.GetSystem<IDropSystem>().ResetRun();

			// 战斗 BGM（AudioKit 经 ResKit 从 AB 异步加载，进入战斗循环播放）
			AudioKit.PlayMusic(AudioNames.BgmBattle);

			// 缓存主摄像机引用（带 MainCamera 标签）
			mMainCam = Camera.main;

			// 打开常驻玩家信息面板（显示 HP/EXP/LV，数据由 Model 驱动刷新）
			// WebGL 下 AB 只能异步加载，用 OpenPanelAsync（同步 OpenPanel 首次加载 uiprefab 包必失败）；
			// 协程挂自己身上：若面板打开前场景就被卸载（秒死回 GameStart），面板也无需再开
			// StartCoroutine(UIKit.OpenPanelAsync<PlayerInfoPanel>());
		}

		// 摄像机跟随：LateUpdate 在物理/动画更新后、渲染前执行。
		// 用 SmoothDamp（平滑阻尼）替代 Lerp——帧率无关，消除一卡一卡的抖动。
		void LateUpdate()
		{
			if (mMainCam == null) return;

			// 只跟随 X/Y，保持 Z（2D 摄像机在 -10）
			var targetPos = new Vector3(
				transform.position.x,
				transform.position.y,
				mMainCam.transform.position.z
			);
			mMainCam.transform.position = Vector3.SmoothDamp(
				mMainCam.transform.position, targetPos, ref mCamVelocity, 0.2f);
		}

		// 升级：从能力池抽 3 张升级卡（池内已过滤满级/前置未解锁），打开选择面板
		private void OnLevelUp(LevelUpEvent e)
		{
			// 随机抽取 3 个不重复的能力
			var options = this.GetSystem<IAbilitySystem>().RollOptions(3);
			if (options.Length == 0)
			{
				// 全部能力已满级/无候选：直接消费掉本次升级，不暂停不开面板
				//（防 PendingLevelUps 残留 + timeScale=0 卡死）
				while (mModel.PendingLevelUps.Value > 0)
					this.SendCommand(new ConsumePendingLevelUpCommand());
				return;
			}

			AudioKit.PlaySound(AudioNames.LevelUp); // 升级音效
			Time.timeScale = 0f; // 升级暂停游戏

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
			mAnimFSM?.Clear();   // 清理状态机
			input.Dispose();
		}

		// 游戏胜利：由 WaveSystem 发送 GameWinEvent 触发
		private void OnGameWin(GameWinEvent e)
		{
			AudioKit.PlaySound(AudioNames.Victory); // 胜利音效
			GameOver();
		}

		void Update()
		{
			var move = input.Player.Move.ReadValue<Vector2>();
			SelfRigidbody2D.linearVelocity = move * Speed;

			// 角色翻转：朝左翻转（scale.x 为负），朝右恢复
			if (move.x < -0.01f)
			{
				var s = transform.localScale;
				if (s.x > 0) transform.localScale = new Vector3(-s.x, s.y, s.z);
			}
			else if (move.x > 0.01f)
			{
				var s = transform.localScale;
				if (s.x < 0) transform.localScale = new Vector3(-s.x, s.y, s.z);
			}

			// 根据是否有移动输入切换动画状态（QF FSM）
			mAnimFSM.ChangeState(move.magnitude > 0.01f ? PlayerAnimState.Walk : PlayerAnimState.Idle);

			TryAttackAllInRange();
		}

		// 无 GC 分配的碰撞检测结果缓冲（复用避免每帧分配）
		private readonly Collider2D[] mAttackHits = new Collider2D[16];
		private ContactFilter2D mAttackFilter;
		// 攻击目标临时缓冲：按距玩家距离排序后，取前 WeaponCount 个生成剑
		private readonly System.Collections.Generic.List<Enemy> mAttackTargets = new(8);

		// 攻击范围内检测到敌人 → 按距离排序后取前 N 个，各生成一把剑
		private void TryAttackAllInRange()
		{
			// 攻击 CD
			if (Time.time - lastAttackTime < AttackInterval) return;

			// Unity 6 无 GC 版本：结果写入预分配的缓冲数组，返回实际命中数量
			var hitCount = Physics2D.OverlapCircle(transform.position, AttackRadius, mAttackFilter, mAttackHits);

			// 收集范围内的敌人 + 计算距离（避免每帧 new，复用 List）
			mAttackTargets.Clear();
			for (int i = 0; i < hitCount; i++)
			{
				if (mAttackHits[i].gameObject.CompareTag("Enemy") &&
					mAttackHits[i].TryGetComponent<Enemy>(out var enemy))
				{
					mAttackTargets.Add(enemy);
				}
			}

			if (mAttackTargets.Count == 0) return;

			// 按距玩家距离升序：近的优先打
			var playerPos = transform.position;
			mAttackTargets.Sort((a, b) =>
				(a.transform.position - playerPos).sqrMagnitude
					.CompareTo((b.transform.position - playerPos).sqrMagnitude));

			// 取前 WeaponCount 个（未来升级可大于 1），每个生成一把剑
			lastAttackTime = Time.time;
			AudioKit.PlaySound(AudioNames.Attack); // 挥砍音效
			var n = Mathf.Min(mModel.WeaponCount.Value, mAttackTargets.Count);
			for (int i = 0; i < n; i++)
			{
				var enemy = mAttackTargets[i];
				mAssetsSystem.SpawnWeapon(enemy.transform.position, AttackDamage, enemy);
			}

			// 武器能力齐发：穿透剑等已解锁的武器能力在此各自发射
			//（分支升级数值由 AbilitySystem 按能力等级聚合，PlayerController 不感知具体武器）
			mWeaponSystem.OnPlayerAttack(transform.position, mAttackTargets);
		}

		// 玩家被敌人碰到 → 扣血（固定 1 秒受击间隔）
		private void OnTriggerStay2D(Collider2D collision)
		{
			if (!collision.gameObject.CompareTag("Enemy")) return;

			// 只有身体（方形）碰到敌人才扣血
			if (!SelfBoxCollider2D.IsTouching(collision)) return;
			if (Time.time - lastHitTime < 1f) return;
			lastHitTime = Time.time;

			// 扣血走 Command（QF 规范：改数据必须经过 Command，表现层不得直写 Model）
			this.SendCommand(new DamagePlayerCommand());
			AudioKit.PlaySound(AudioNames.PlayerHurt); // 受击音效
			if (mModel.HP.Value <= 0)
			{
				AudioKit.PlaySound(AudioNames.GameOver); // 失败音效
				GameOver();
			}
		}

		private void GameOver()
		{
			if(isOver) return;
			// WebGL 下 AB 只能异步加载，用 OpenPanelAsync；
			// 协程宿主用常驻 GameRoot（本对象马上 SetActive(false) 会杀掉自己身上的协程）
			Debug.Log("GameOver");
			isOver = true;
			AudioKit.StopMusic(); // 死亡/胜利结算，停止战斗 BGM
			// 死亡/胜利结算：上报本局存活时间到微信好友排行榜（未破纪录时内部静默跳过）
			this.GetSystem<IWXPlatformSystem>().ReportSurviveTime(Mathf.CeilToInt(this.GetSystem<IWaveSystem>().ElapsedTime));
			// 面板打开的协程必须挂在常驻 GameRoot 上：下面一行本对象就 SetActive(false)，
			// 挂在自己身上的协程会被杀——首次死亡时 GameOverPanel 的 AB 还没缓存，
			// OpenPanelAsync 还在异步等待 → 协程被杀后面板永远打不开（且 timeScale=0 卡死）。
			// 第二次起 AB 已缓存、回调同步完成才"看起来正常"。
			var host = FindFirstObjectByType<GameRoot>();
			if (host != null)
			{
				host.StartCoroutine(UIKit.OpenPanelAsync<GameOverPanel>());
			}
			else
			{
				StartCoroutine(UIKit.OpenPanelAsync<GameOverPanel>()); // 兜底：找不到 GameRoot 只能挂自己
			}
			gameObject.SetActive(false); // 主角消失
			Time.timeScale = 0f;         // 暂停
		}
	}
}
