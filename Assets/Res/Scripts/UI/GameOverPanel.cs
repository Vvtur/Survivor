using UnityEngine;
using QFramework;
using QFramework.Gameplay;

namespace QFramework.UI
{
	public class GameOverPanelData : UIPanelData
	{
	}
	public partial class GameOverPanel : UIPanel
	{
		ResLoader mSceneLoader;
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameOverPanelData ?? new GameOverPanelData();

			Btn_ReStart.onClick.AddListener(() =>
			{
				if (LoadingPanel.IsTransitioning) return; // 过场中防重入
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				Time.timeScale = 1f; // 恢复时间缩放，否则刚体物理仍被暂停（过场动画本身全程 unscaled，不受影响）

				// 带过场动画的异步切换（圆扩散切入 → 异步加载 → 圆收回），完成后关掉本弹窗
				LoadingPanel.SwitchScene("GameStart", () => mSceneLoader ??= ResLoader.Allocate(), CloseSelf);
			});
		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}

		protected override void OnShow()
		{
			// 弹窗打开动画（OnShow 时物体已激活，可安全启动 tween）
			this.PlayOpen();
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			mSceneLoader.Recycle2Cache();
			mSceneLoader = null;
		}
	}
}
