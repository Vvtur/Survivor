using System;
using DG.Tweening;
using UnityEngine;

namespace QFramework.UI
{
	/// <summary>
	/// UI 面板动画工具（纯表现层，零框架侵入：不修改 UIPanel 基类与 UIKit 时序）。
	///
	/// 使用约定：
	/// - 打开动画挂在 UIPanel.OnShow()（基类 Show() 先 SetActive(true) 再调 OnShow，
	///   在 OnShow 里启动 tween 是安全的；OnOpen 运行在 SetActive 之前，禁止在那里启动 tween）。
	/// - UIKit 的关闭是同步 SetActive(false) + Destroy，没有动画等待位——
	///   所以关闭动画先播，onDone 回调里再由调用方执行 CloseSelf / Hide / UIKit.ClosePanel。
	/// - 所有动画一律 SetUpdate(true)：不受 Time.timeScale 影响
	///   （升级面板是在 timeScale=0 的暂停状态下打开的）。
	/// - CanvasGroup 由本工具运行时补挂（GetComponent ?? AddComponent），
	///   预制体上不需要预先配置，也不占用 Designer 字段。
	/// </summary>
	public static class UIPanelAnim
	{
		/// <summary>
		/// 弹窗打开动画：透明度 0→1 + 缩放 0.85→1（OutBack 回弹）。
		/// 先 DOKill 并强制归初值，防止 Single 模式面板复用时的残留状态。
		/// </summary>
		public static void PlayOpen(this MonoBehaviour panel, float duration = 0.25f)
		{
			if (panel == null) return;
			var root = (RectTransform)panel.transform;
			var cg = GetOrAddCanvasGroup(panel.gameObject);

			cg.DOKill();
			root.DOKill();
			cg.blocksRaycasts = true;  // 恢复交互（PlayClose 里被关掉）
			cg.interactable = true;
			cg.alpha = 0f;
			root.localScale = Vector3.one * 0.85f;

			cg.DOFade(1f, duration * 0.8f).SetUpdate(true).SetEase(Ease.OutQuad);
			root.DOScale(1f, duration).SetUpdate(true).SetEase(Ease.OutBack);
		}

		/// <summary>
		/// 弹窗关闭动画：透明度→0 + 缩放→0.9（InQuad 收缩），
		/// 完成回调 onDone 里再执行真正的关闭（CloseSelf / Hide / UIKit.ClosePanel）。
		/// 动画期间关掉 blocksRaycasts/interactable，防止连点重入。
		/// </summary>
		public static void PlayClose(this MonoBehaviour panel, Action onDone, float duration = 0.18f)
		{
			if (panel == null)
			{
				onDone?.Invoke();
				return;
			}
			var root = (RectTransform)panel.transform;
			var cg = GetOrAddCanvasGroup(panel.gameObject);

			cg.DOKill();
			root.DOKill();
			cg.blocksRaycasts = false;
			cg.interactable = false;

			cg.DOFade(0f, duration).SetUpdate(true).SetEase(Ease.InQuad);
			root.DOScale(0.9f, duration).SetUpdate(true).SetEase(Ease.InQuad)
				.OnComplete(() => onDone?.Invoke());
		}

		/// <summary>
		/// 单元素入场动画（透明度 0→1 + 缩放 0.6→1 OutBack），可带延迟错峰。
		/// 用于开始界面首次进入时标题/按钮的弹出（场景直挂面板不走 UIKit 生命周期，
		/// 由 Unity Start() 驱动）。
		/// </summary>
		public static void PlayIntro(this RectTransform target, float delay = 0f, float duration = 0.35f)
		{
			if (target == null) return;
			var cg = GetOrAddCanvasGroup(target.gameObject);

			target.DOKill();
			cg.DOKill();
			cg.alpha = 0f;
			target.localScale = Vector3.one * 0.6f;

			cg.DOFade(1f, duration * 0.6f).SetDelay(delay).SetUpdate(true);
			target.DOScale(1f, duration).SetDelay(delay).SetUpdate(true).SetEase(Ease.OutBack);
		}

		/// <summary>
		/// 右侧滑入动画：透明度 0→1 + 面板根从屏外右侧（+rect.width）滑到 x=0。
		/// 位移量 = 面板根 RectTransform 宽度（面板全屏 stretch 时即为 Screen.width）。
		/// 动画期间开交互，收尾到位才允许背景接住点击。
		/// 用于"侧边抽屉式"面板（如设置面板从右滑入）。
		/// </summary>
		public static void PlaySlideIn(this MonoBehaviour panel, float duration = 0.25f)
		{
			if (panel == null) return;
			var root = (RectTransform)panel.transform;
			var cg = GetOrAddCanvasGroup(panel.gameObject);

			root.DOKill();
			cg.DOKill();

			// 位移量取面板根当前宽度：stretch 全屏时 = 屏幕宽；非全屏时也按自己宽度，
			// 看起来仍然是"从屏外右侧进入"的视觉效果
			float offRight = root.rect.width;
			cg.alpha = 0f;
			cg.blocksRaycasts = false;   // 动画期间不允许点（避免"点中正在飞入的半透明面板"）
			cg.interactable = false;
			root.localPosition = new Vector3(offRight, root.localPosition.y, root.localPosition.z);

			cg.DOFade(1f, duration).SetUpdate(true).SetEase(Ease.OutQuad);
			root.DOLocalMoveX(0f, duration).SetUpdate(true).SetEase(Ease.OutCubic)
				.OnComplete(() =>
				{
					// 收尾开交互（如果面板自身要求能接住点击，例如背景的 Img_BG）
					cg.blocksRaycasts = true;
					cg.interactable = true;
				});
		}

		/// <summary>
		/// 右侧滑出动画：透明度 1→0 + 面板根从 x=0 滑到屏外右侧（+rect.width）。
		/// 动画期间关掉交互防连点；onDone 回调里由调用方执行真正的关闭（CloseSelf / Hide / UIKit.ClosePanel）。
		/// 用于"侧边抽屉式"面板的反向出场。
		/// </summary>
		public static void PlaySlideOut(this MonoBehaviour panel, Action onDone, float duration = 0.22f)
		{
			if (panel == null)
			{
				onDone?.Invoke();
				return;
			}
			var root = (RectTransform)panel.transform;
			var cg = GetOrAddCanvasGroup(panel.gameObject);

			root.DOKill();
			cg.DOKill();
			cg.blocksRaycasts = false;
			cg.interactable = false;

			float offRight = root.rect.width;
			cg.DOFade(0f, duration).SetUpdate(true).SetEase(Ease.InQuad);
			root.DOLocalMoveX(offRight, duration).SetUpdate(true).SetEase(Ease.InCubic)
				.OnComplete(() => onDone?.Invoke());
		}

		static CanvasGroup GetOrAddCanvasGroup(GameObject go)
		{
			var cg = go.GetComponent<CanvasGroup>();
			if (cg == null) cg = go.AddComponent<CanvasGroup>();
			return cg;
		}
	}
}
