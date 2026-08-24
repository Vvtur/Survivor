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
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				Time.timeScale = 1f; // 恢复时间缩放，否则刚体物理仍被暂停

				// 场景切换统一走常驻 GameRoot 的 loader（单参写法，见 GameRoot.SwitchScene 注释）
				mSceneLoader ??= ResLoader.Allocate();
				mSceneLoader.LoadSceneAsync("GameStart", onStartLoading:(op) =>
				{
					op.completed += (a) =>
					{
						CloseSelf();
					};
				});
			});
		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}

		protected override void OnShow()
		{
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
