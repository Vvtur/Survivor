using QFramework;
using QFramework.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QFramework.UI
{
	public class GameStartPanelData : UIPanelData
	{
	}
	public partial class GameStartPanel : UIPanel
	{
		ResLoader mSceneLoader;   // 场景 loader 由面板自己持有
		private void Awake()
		{
			Btn_Start.onClick.AddListener(() =>
			{
				// 场景切换统一走常驻 GameRoot 的 loader（单参写法，见 GameRoot.SwitchScene 注释）
				// GameRoot.SwitchScene("MainGame");
				mSceneLoader ??= ResLoader.Allocate();
				mSceneLoader.LoadSceneAsync("MainGame");/* , LoadSceneMode.Single, LocalPhysicsMode.None, (op) =>
				{
					op.completed += (a) =>
					{
						CloseSelf();
					};
				}); */
			});
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameStartPanelData ?? new GameStartPanelData();

			// 面板由常驻 GameRoot 在 ResKit 初始化完成后打开，此处可直接绑定按钮
			Btn_Start.onClick.AddListener(() =>
			{
				// 场景切换统一走常驻 GameRoot 的 loader（单参写法，见 GameRoot.SwitchScene 注释）
				// GameRoot.SwitchScene("MainGame");
				mSceneLoader ??= ResLoader.Allocate();
				mSceneLoader.LoadSceneAsync("MainGame", LoadSceneMode.Single, LocalPhysicsMode.None, (op) =>
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
			// mSceneLoader.Recycle2Cache();
			// mSceneLoader = null;
        }

	}
}
