using QFramework;
using QFramework.Gameplay;

namespace QFramework.UI
{
	public class GameStartPanelData : UIPanelData
	{
	}
	public partial class GameStartPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameStartPanelData ?? new GameStartPanelData();

			// 面板由常驻 GameRoot 在 ResKit 初始化完成后打开，此处可直接绑定按钮
			Btn_Start.onClick.AddListener(() =>
			{
				// 场景切换统一走常驻 GameRoot 的 loader（单参写法，见 GameRoot.SwitchScene 注释）
				GameRoot.SwitchScene("MainGame");
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
