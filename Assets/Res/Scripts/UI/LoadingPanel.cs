using System;
using System.Collections;
using DG.Tweening;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QFramework.UI
{
	public class LoadingPanelData : UIPanelData
	{
		/// <summary>提示文案等可扩展字段放这里(通过 uiData 传入)</summary>
	}

	/// <summary>
	/// 过场加载面板 + 场景切换编排(一个文件、一条主协程,从上往下就是玩家看到的顺序)。
	///
	/// 对外只有两个入口:
	///   LoadingPanel.SwitchScene(...)   带过场动画的异步切场景
	///   LoadingPanel.IsTransitioning    过场中(按钮回调用它防重入/防误触)
	///
	/// 时序(全程 unscaled,不受 timeScale 影响):
	///   打开面板 → 进入动效(圆形遮罩扩散 0.55s,内容错峰浮入)
	///   → 异步加载场景(进度条平滑跟随) → 最短停留 1.2s
	///   → 退出动效(内容撤场 + 圆收回 0.5s) → 面板 Hide → onComplete
	///
	/// 预制体层级(Assets/Res/Art/UIPrefab/LoadingPanel.prefab,AB uiprefab):
	///   LoadingPanel
	///    ├─ Img_Blocker   全屏透明 Image(raycastTarget=true,过场期间挡点击)
	///    ├─ Img_Mask      Mask(showMaskGraphic=false)+ 圆形 Sprite(运行时生成),
	///    │   │            其 RectTransform 边长 = 圆直径,由动效驱动
	///    │   └─ Img_Wipe  背景色块(被遮罩裁剪,只在圆内显示)
	///    └─ Rt_Content    标题/百分比/进度条(CanvasGroup 浮入浮出,不在遮罩内——
	///                     浮入时圆已盖过内容区,收缩前内容先撤场)
	///
	/// 面板是 UIKit Single 面板:首次异步走 AB,过场结束只 Hide 不销毁,下次直接复用;
	/// 所以 OnShow 必须把遮罩/内容/进度全部复位。
	/// 样式后期直接改预制体(层级/颜色/字体/排版),节奏改下面的常量;
	/// 动效约定同 UIPanelAnim:所有 tween 一律 SetUpdate(true)。
	/// </summary>
	public partial class LoadingPanel : UIPanel
	{
		// ---- 节奏参数(调手感只改这里)----
		const float EnterDuration = 0.55f;      // 进入:圆扩散总时长
		const float ExitDuration = 0.50f;       // 退出:圆收回总时长
		const float ContentInDelay = 0.22f;     // 内容浮入的错峰延迟(≈ 圆盖过内容区的时刻)
		const float ContentOutDuration = 0.18f; // 内容撤场时长
		const float MinTotalDuration = 1.2f;    // 从进入动效开始算,过场总时长下限(不闪切)
		const float PanelLoadTimeout = 10f;     // 面板加载超时:超时退化为无动画裸切,流程不卡死

		// ============================ 过场编排 ============================

		/// <summary>是否正在过场(按钮回调用它防重入/防误触)</summary>
		public static bool IsTransitioning { get; private set; }

		/// <summary>
		/// 带过场动画的异步场景切换。
		///
		/// 协程宿主用常驻的 UIRoot(UIRoot.Instance 里 DontDestroyOnLoad,且本项目三个场景
		/// 都没有预置 UIRoot 实例,必然走 Instantiate 分支):切场景它不销毁,
		/// 所以整个过场是一条协程跑到底,不需要中途换宿主,也不怕调用方被切场景销毁。
		/// </summary>
		/// <param name="sceneName">目标场景(AB 场景名)</param>
		/// <param name="loaderFactory">场景 loader 工厂(调用方持有,如 () => mSceneLoader ??= ResLoader.Allocate())</param>
		/// <param name="onComplete">过场结束后的回调(可选,如 GameOverPanel.CloseSelf)</param>
		public static void SwitchScene(string sceneName, Func<IResLoader> loaderFactory = null,
			Action onComplete = null)
		{
			if (IsTransitioning) return; // 防重入:过场中忽略新的切换请求
			IsTransitioning = true;
			UIKit.Root.StartCoroutine(TransitionRoutine(sceneName, loaderFactory, onComplete));
		}

		/// <summary>过场主流程</summary>
		static IEnumerator TransitionRoutine(string sceneName, Func<IResLoader> loaderFactory,
			Action onComplete)
		{
			try
			{
				// 1. 打开过场面板:回调把实例送过来,配超时兜底
				LoadingPanel panel = null;
				OpenAsync(p => panel = p);

				var timeLeft = PanelLoadTimeout;
				while (panel == null && timeLeft > 0f)
				{
					timeLeft -= Time.unscaledDeltaTime;
					yield return null;
				}

				if (panel == null)
				{
					// 兜底:面板加载失败不能卡死流程,退化成无动画裸加载
					LogKit.E("[LoadingPanel] 过场面板加载失败,退化为无动画场景切换");
					yield return LoadSceneRoutine(sceneName, loaderFactory, null);
				}
				else
				{
					yield return null; // 等一帧:Canvas 布局就绪(算圆直径要读面板 rect)

					var enterAt = Time.unscaledTime;

					// 2. 进入动效:圆形遮罩从中心扩散盖满全屏,内容错峰浮入
					yield return panel.PlayEnter();

					// 3. 异步加载场景,进度写进面板
					yield return LoadSceneRoutine(sceneName, loaderFactory, panel);

					// 4. 最短停留:保证节奏完整、内容可读
					while (Time.unscaledTime - enterAt < MinTotalDuration) yield return null;

					// 5. 退出动效:内容先撤场,圆反向收回,圆外露出的就是已加载好的新场景
					yield return panel.PlayExit();

					// 6. 收尾:只 Hide 不关闭(Single 面板复用,下次过场不再走 AB)
					panel.Hide();
				}
			}
			finally
			{
				// 无论中途是否异常,过场标志必须复位,否则后续切换全被防重入拦死
				IsTransitioning = false;
			}

			onComplete?.Invoke();
		}

		/// <summary>
		/// 打开过场面板,并把面板实例回调出来。
		///
		/// 为什么不用 UIKit.OpenPanelAsync:它内部是 while(!loaded) yield WaitForEndOfFrame
		/// (UIKit.cs:102),编辑器 Game View 停止渲染时 EndOfFrame 永远不来 → 协程挂死。
		/// 这里直接调它内部同一条链路 UIManager.OpenUIAsync(UIKit.cs:100),用回调拿实例,
		/// 不依赖帧渲染。回调返回时面板的 Open/Show 已经走完(UIManager.cs:51-53)。
		///
		/// 注意:面板资源加载失败时回调不会被触发(UIKitWithResKitInit.cs:53 的 if (success)),
		/// 所以调用方必须配超时兜底。
		/// </summary>
		static void OpenAsync(Action<LoadingPanel> onOpened)
		{
			var keys = PanelSearchKeys.Allocate();
			keys.OpenType = PanelOpenType.Single; // 同类型面板复用
			keys.Level = UILevel.PopUI;
			keys.PanelType = typeof(LoadingPanel);

			UIManager.Instance.OpenUIAsync(keys, panel =>
			{
				keys.Recycle2Cache(); // 回调里才回收:异步加载链路中还要读 keys
				onOpened(panel as LoadingPanel);
			});
		}

		/// <summary>
		/// 异步加载场景并写进度(panel 传 null 就是无动画裸加载,兜底路径复用同一段代码)。
		///
		/// 进度分两段但只用一个循环:AB 模式下 AsyncOperation 要等场景 AB 加载完
		/// 才在回调里创建(ResKit IResLoaderExtensions.cs:296-305),所以
		///   op 到手前:假进度爬到 85% 封顶(不到底,留期待感)
		///   op 到手后:跟随 op.progress,映射到 20%~100%
		/// </summary>
		static IEnumerator LoadSceneRoutine(string sceneName, Func<IResLoader> loaderFactory,
			LoadingPanel panel)
		{
			AsyncOperation op = null;
			loaderFactory?.Invoke()?.LoadSceneAsync(sceneName,
				LoadSceneMode.Single, LocalPhysicsMode.None, operation => op = operation);

			var creep = 0f;
			while (op == null || !op.isDone)
			{
				if (op == null) creep = Mathf.Min(0.85f, creep + Time.unscaledDeltaTime * 0.22f);
				if (panel != null) panel.SetProgress(op == null ? creep : 0.2f + 0.8f * op.progress);
				yield return null;
			}

			if (panel != null) panel.SetProgress(1f);
		}

		// ============================ 面板表现 ============================

		RectTransform mMaskRt;   // Img_Mask 的 RectTransform,边长即圆直径
		CanvasGroup mContentCg;  // Rt_Content 上的 CanvasGroup(运行时补挂)
		Vector2 mContentBasePos; // Rt_Content 的原始位置(浮入动效的落点)
		float mDisplayed;        // 进度显示值(向 mTarget 平滑收敛)
		float mTarget;           // 进度目标值
		int mLastPct = -1;       // 百分比文本脏检查(避免每帧拼字符串 GC)

		static Sprite sCircleSprite; // 运行时生成的圆形 Sprite,全程序共用一份

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LoadingPanelData ?? new LoadingPanelData();
		}

		/// <summary>每次过场开始都会走这里:面板是复用的,遮罩/内容/进度必须全部复位</summary>
		protected override void OnShow()
		{
			SetupRevealMask();

			if (mContentCg == null && Rt_Content != null)
			{
				// CanvasGroup 运行时补挂(预制体不用配);原始位置只记一次——
				// 动效会改 anchoredPosition,每次 Show 都重读会越读越偏
				mContentCg = Rt_Content.GetComponent<CanvasGroup>()
					?? Rt_Content.gameObject.AddComponent<CanvasGroup>();
				mContentCg.blocksRaycasts = false;
				mContentCg.interactable = false;
				mContentBasePos = Rt_Content.anchoredPosition;
			}
			if (mContentCg != null) mContentCg.alpha = 0f; // 内容初始隐藏,由 PlayEnter 错峰浮入

			// 进度复位:上次过场结束时停在 100%,不复位第二次过场会直接满格
			mDisplayed = 0f;
			mTarget = 0f;
			mLastPct = -1;
			if (Img_BarFill != null) Img_BarFill.fillAmount = 0f;
		}

		protected override void OnClose()
		{
			mMaskRt = null;
			mContentCg = null;
		}

		/// <summary>遮罩就位:补圆形 Sprite、关掉遮罩图自显、边长归零(初始全透明)</summary>
		void SetupRevealMask()
		{
			var mask = GetComponentInChildren<Mask>(true);
			if (mask == null)
			{
				LogKit.E("[LoadingPanel] 预制体缺 Img_Mask(Mask 组件),圆形揭示失效");
				mMaskRt = null;
				return;
			}

			mask.showMaskGraphic = false; // 只做裁剪,自身不显示

			var maskImg = mask.GetComponent<Image>();
			if (maskImg != null)
			{
				maskImg.sprite = GetCircleSprite();  // Mask 的裁剪形状 = 这张图的 alpha
				maskImg.type = Image.Type.Simple;    // 拉伸铺满方框:圆直径 = 方框边长
				maskImg.raycastTarget = false;
			}

			mMaskRt = (RectTransform)mask.transform;
			mMaskRt.DOKill();
			mMaskRt.sizeDelta = Vector2.zero;
		}

		/// <summary>圆形 Sprite:运行时生成一次(1024² 抗锯齿圆),之后所有过场复用</summary>
		static Sprite GetCircleSprite()
		{
			if (sCircleSprite != null) return sCircleSprite;

			const int size = 1024;
			var center = (size - 1) * 0.5f;
			var pixels = new Color32[size * size];
			for (var y = 0; y < size; y++)
			{
				for (var x = 0; x < size; x++)
				{
					var dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
					var a = Mathf.Clamp01((center - 2f - dist) / 2f + 0.5f); // 边缘 2px 渐变抗锯齿
					pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
				}
			}

			var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
				{ wrapMode = TextureWrapMode.Clamp };
			tex.SetPixels32(pixels);
			tex.Apply(false, true); // 上传显存并释放 CPU 侧副本

			sCircleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
			sCircleSprite.name = "LoadingRevealCircle";
			return sCircleSprite;
		}

		/// <summary>盖满全屏所需边长 = 面板对角线 + 2:圆心在屏幕中心,直径 ≥ 对角线必盖住四角</summary>
		float ComputeMaskDiameter()
		{
			var size = ((RectTransform)transform).rect.size;
			if (size.x <= 1f || size.y <= 1f) size = new Vector2(4096f, 4096f); // 布局未就绪兜底
			return Mathf.Ceil(Mathf.Sqrt(size.x * size.x + size.y * size.y)) + 2f;
		}

		/// <summary>
		/// 进入动效:圆形遮罩从中心扩散盖满全屏(OutCubic),圆内逐渐显出背景;
		/// 内容组(标题/百分比/进度条)靠 tween 自带的 SetDelay 错峰浮入,不用状态机对齐时间。
		/// </summary>
		IEnumerator PlayEnter()
		{
			if (mMaskRt != null)
			{
				mMaskRt.DOKill();
				mMaskRt.sizeDelta = Vector2.zero;
				var diameter = ComputeMaskDiameter();
				mMaskRt.DOSizeDelta(new Vector2(diameter, diameter), EnterDuration)
					.SetUpdate(true).SetEase(Ease.OutCubic);
			}

			if (mContentCg != null && Rt_Content != null)
			{
				mContentCg.DOKill();
				Rt_Content.DOKill();
				mContentCg.alpha = 0f;
				Rt_Content.anchoredPosition = mContentBasePos + new Vector2(0f, -24f);
				// SetUpdate(true) 下 delay 也走 unscaled,与圆扩散同一时间轴
				mContentCg.DOFade(1f, 0.32f).SetDelay(ContentInDelay).SetUpdate(true).SetEase(Ease.OutQuad);
				Rt_Content.DOAnchorPos(mContentBasePos, 0.4f).SetDelay(ContentInDelay)
					.SetUpdate(true).SetEase(Ease.OutCubic);
			}

			// 圆扩散走完才算进入完成;内容浮入与之并行,不额外等待
			yield return new WaitForSecondsRealtime(EnterDuration);
		}

		/// <summary>
		/// 退出动效:内容先撤场(免得圆收缩时文字被裁一半),圆再反向收回到中心消失,
		/// 圆外露出的就是已加载好的新场景。
		/// </summary>
		IEnumerator PlayExit()
		{
			if (mContentCg != null)
			{
				mContentCg.DOKill();
				if (Rt_Content != null) Rt_Content.DOKill();
				mContentCg.DOFade(0f, ContentOutDuration).SetUpdate(true).SetEase(Ease.InQuad);
				yield return new WaitForSecondsRealtime(ContentOutDuration);
			}

			if (mMaskRt != null)
			{
				mMaskRt.DOKill();
				mMaskRt.DOSizeDelta(Vector2.zero, ExitDuration).SetUpdate(true).SetEase(Ease.InCubic);
			}
			yield return new WaitForSecondsRealtime(ExitDuration);
		}

		/// <summary>写入进度目标值(0~1),显示值在 Update 里平滑收敛过去</summary>
		void SetProgress(float target01)
		{
			mTarget = Mathf.Clamp01(target01);
		}

		void Update()
		{
			// 帧率无关平滑:真实进度常跳变,显示值指数收敛看起来才匀速
			if (!Mathf.Approximately(mDisplayed, mTarget))
			{
				mDisplayed = Mathf.Lerp(mDisplayed, mTarget, 1f - Mathf.Pow(0.02f, Time.unscaledDeltaTime));
				if (Mathf.Abs(mTarget - mDisplayed) < 0.001f) mDisplayed = mTarget;
			}

			if (Img_BarFill != null) Img_BarFill.fillAmount = mDisplayed;

			var pct = Mathf.RoundToInt(mDisplayed * 100f);
			if (pct != mLastPct && Txt_Percent != null)
			{
				mLastPct = pct;
				Txt_Percent.text = $"LOADING {pct}%";
			}
		}
	}
}
