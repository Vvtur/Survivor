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
			// 防误触：从"游戏结束→重新开始"切回本场景时，点"重新开始"那一下的指针事件
			// 可能被 EventSystem 重新派发到本场景按钮上，导致刚回到开始界面就误触发。
			// 场景出现后 0.75 秒内忽略点击（覆盖入场动画最长错峰时长 0.36+0.35≈0.71s，
			// 否则残留点击会落在视觉还没弹出来的按钮上；残留点击本身由新增的
			// Img_ClickBlocker 透明图整体接住，见预制体）。
			var clickableAt = Time.unscaledTime + 0.75f;

			// 注意：Btn_Start 的绑定只在 OnInit 做一次。
			// （旧代码在 Awake 里也绑过一次 → 点击后 LoadSceneAsync 被调用两次，已移除）
			Btn_Start.onClick.AddListener(() =>
			{
				if (Time.unscaledTime < clickableAt) return;
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				AudioKit.PlaySound(AudioNames.GameStart, volume: 0.05f);  // 一半音量// 开始游戏音效
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
				if (Time.unscaledTime < clickableAt) return;
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				StartCoroutine(UIKit.OpenPanelAsync<LeaderboardPanel>());
			});

			// 邀请有礼入口：打开面板（分享带 inviter / 领取好友进入奖励）
			Btn_Invite.onClick.AddListener(() =>
			{
				if (Time.unscaledTime < clickableAt) return;
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				StartCoroutine(UIKit.OpenPanelAsync<InvitePanel>());
			});

			// 设置入口（音乐/音效）：打开设置面板，数据源 AudioKit.Settings（自带 PlayerPrefs 持久化）
			Btn_Settings.onClick.AddListener(() =>
			{
				if (Time.unscaledTime < clickableAt) return;
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				StartCoroutine(UIKit.OpenPanelAsync<SettingsPanel>());
			});
		}

		private void Start()
		{
			// 首次进入开始界面的入场动效：标题 → 开始游戏 → 底部三按钮，错峰弹出。
			// 本面板是场景直挂预制体（不走 UIKit.OpenPanel，OnShow/OnOpen 不会被调），
			// 所以入场动画挂 Unity 的 Start()。
			if (Txt_Title != null) Txt_Title.rectTransform.PlayIntro(0f);
			if (Btn_Start != null) Btn_Start.GetComponent<RectTransform>().PlayIntro(0.12f);
			if (Btn_Board != null) Btn_Board.GetComponent<RectTransform>().PlayIntro(0.24f);
			if (Btn_Shop != null) Btn_Shop.GetComponent<RectTransform>().PlayIntro(0.30f);
			if (Btn_Invite != null) Btn_Invite.GetComponent<RectTransform>().PlayIntro(0.36f);
			// 设置按钮也加入错峰队列：齿轮通常在右上角，与底部按钮分组时序：标题之后最先
			if (Btn_Settings != null) Btn_Settings.GetComponent<RectTransform>().PlayIntro(0.18f);
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
			// 注：UIPanel 基类不实现 IController，无法用 this.GetSystem<> 扩展方法
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
