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
			// 注意：Btn_Start 的绑定只在 OnInit 做一次。
			// （旧代码在 Awake 里也绑过一次 → 点击后 LoadSceneAsync 被调用两次，已移除）
			Btn_Start.onClick.AddListener(() =>
			{
				// 场景切换统一走常驻 GameRoot 的 loader（单参写法，见 GameRoot.SwitchScene 注释）
				// GameRoot.SwitchScene("MainGame");
				mSceneLoader ??= ResLoader.Allocate();
				mSceneLoader.LoadSceneAsync("MainGame", LoadSceneMode.Single, LocalPhysicsMode.None, (op) =>
				{
					op.completed += (a) =>
					{
						// CloseSelf();
					};
				});
			});

			// 好友排行榜入口：打开面板（面板内部把开放数据域画布贴到 RawImage 上）
			Btn_Board.onClick.AddListener(() =>
			{
				StartCoroutine(UIKit.OpenPanelAsync<LeaderboardPanel>());
			});

			// 邀请有礼入口：打开面板（分享带 inviter / 领取好友进入奖励）
			Btn_Invite.onClick.AddListener(() =>
			{
				StartCoroutine(UIKit.OpenPanelAsync<InvitePanel>());
			});
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameStartPanelData ?? new GameStartPanelData();

			// 面板由常驻 GameRoot 在 ResKit 初始化完成后打开，此处可直接绑定按钮
			// Btn_Start.onClick.AddListener(() =>
			// {
			// 	// 场景切换统一走常驻 GameRoot 的 loader（单参写法，见 GameRoot.SwitchScene 注释）
			// 	// GameRoot.SwitchScene("MainGame");
			// 	mSceneLoader ??= ResLoader.Allocate();
			// 	mSceneLoader.LoadSceneAsync("MainGame", LoadSceneMode.Single, LocalPhysicsMode.None, (op) =>
			// 	{
			// 		op.completed += (a) =>
			// 		{
			// 			CloseSelf();
			// 		};
			// 	});
			// });

			// // 好友排行榜入口：打开面板（面板内部把开放数据域画布贴到 RawImage 上）
			// Btn_Board.onClick.AddListener(() =>
			// {
			// 	StartCoroutine(UIKit.OpenPanelAsync<LeaderboardPanel>());
			// });
		}

		protected override void OnOpen(IUIData uiData = null)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			// 邀请有礼（被邀请方）：冷启动参数里带 inviter 则上报（幂等，一人只计一次；无效参数云端静默跳过）
			// 热启动（游戏开着时从分享卡片再进入）由 InviteSystem.OnInit 注册的 WX.OnShow 处理
			GameArchitecture.Interface.GetSystem<IInviteSystem>().TrackInviteFromLaunch();

			// 进游戏即处理"头像昵称"授权（避免开榜时才弹、新玩家看到脱敏数据）：
			// 未授权时在排行榜按钮上盖透明授权层，点它 → 授权 → 进排行榜；
			// 已授权则不盖，Btn_Board 的 onClick 正常直接进排行榜。
			// 真实授权态以微信 GetSetting 为准，换账号/清缓存后会自动重新弹。
			if (Btn_Board != null)
			{
				var corners = new Vector3[4];
				Btn_Board.GetComponent<RectTransform>().GetWorldCorners(corners); // [0]左下 [1]左上 [2]右上
				int x = (int)corners[0].x;
				int y = (int)(Screen.height - corners[1].y); // 左下原点 → 左上原点
				int w = (int)(corners[2].x - corners[0].x);
				int h = (int)(corners[1].y - corners[0].y);

				GameArchitecture.Interface.GetSystem<IWXPlatformSystem>()
					.PromptAuthIfNeeded(x, y, w, h, _ => StartCoroutine(UIKit.OpenPanelAsync<LeaderboardPanel>()));
			}
#endif
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
