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
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameOverPanelData ?? new GameOverPanelData();

			Btn_ReStart.onClick.AddListener(() =>
			{
				Time.timeScale = 1f; // 恢复时间缩放，否则刚体物理仍被暂停

				// 场景切换统一走常驻 GameRoot 的 loader（单参写法，见 GameRoot.SwitchScene 注释）
				GameRoot.SwitchScene("GameStart");
				CloseSelf();
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
		}
	}
}
