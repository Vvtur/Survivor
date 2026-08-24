using QFramework;
using QFramework.Gameplay;

namespace QFramework.UI
{
	public class InvitePanelData : UIPanelData
	{
	}

	/// <summary>
	/// 邀请有礼面板：分享按钮 + 领取按钮。
	/// 分享走 IInviteSystem.ShareWithInvite（query 带 inviter=自身 openid）；
	/// 领取资格与领取状态以云端记录为准（invite_events / invite_rewards），
	/// 本面板只负责拉取状态展示与领取交互，金币发放与持久化在 InviteSystem/CloudSaveSystem 内完成。
	/// </summary>
	public partial class InvitePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as InvitePanelData ?? new InvitePanelData();

			Btn_Close.onClick.AddListener(() =>
			{
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				CloseSelf();
			});

			// 分享：拉起微信转发面板（query 带 inviter）；微信拿不到分享成功回调，提示文案按"已发起分享"表述
			Btn_Share.onClick.AddListener(() =>
			{
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				GameArchitecture.Interface.GetSystem<IInviteSystem>()
					.ShareWithInvite(() => SetTip("已发起分享！好友进入游戏后即可领取金币"));
			});

			// 领取：云端校验（防自邀/防重复），code=0 时 InviteSystem 内部已加金币
			Btn_Claim.onClick.AddListener(() =>
			{
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				GameArchitecture.Interface.GetSystem<IInviteSystem>()
					.ClaimReward(OnClaimResult);
			});
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			AudioKit.PlaySound(AudioNames.PanelOpen); // 面板打开音效
			RefreshStatus();
		}

		protected override void OnClose()
		{
			AudioKit.PlaySound(AudioNames.PanelClose); // 面板关闭音效
		}

		// 打开面板即查云端状态：刷新领取按钮可用性与文案
		void RefreshStatus()
		{
			Btn_Claim.interactable = false; // 查询期间先禁用，防连点
			SetTip("查询中...");

			GameArchitecture.Interface.GetSystem<IInviteSystem>()
				.CheckStatus((invited, claimed) =>
				{
					if (claimed)
					{
						Btn_Claim.interactable = false;
						SetTip($"奖励已领取（金币 +{InviteSystem.RewardMoney}）");
					}
					else if (invited)
					{
						Btn_Claim.interactable = true;
						SetTip($"好友已通过你的链接进入游戏，快领取 {InviteSystem.RewardMoney} 金币！");
					}
					else
					{
						Btn_Claim.interactable = true; // 允许点击，点了给"需要分享"的引导提示
						SetTip($"分享给好友，好友进入游戏后可领 {InviteSystem.RewardMoney} 金币");
					}
				});
		}

		// 领取结果：0成功 1已领过 2无资格(区分未分享/无好友游玩) -1网络失败
		void OnClaimResult(int code)
		{
			switch (code)
			{
				case 0:
					Btn_Claim.interactable = false;
					SetTip($"领取成功，金币 +{InviteSystem.RewardMoney}！");
					break;
				case 1:
					Btn_Claim.interactable = false;
					SetTip("已经领取过了");
					break;
				case 2:
					// 微信拿不到分享成功回调：本地"点击过分享"标记仅作文案区分，资格以云端为准
					bool sharedClicked = GameArchitecture.Interface.GetUtility<Storage>()
						.GetInt("InviteSharedClicked", 0) == 1;
					SetTip(sharedClicked
						? "还没有好友通过你的链接进入游戏，再等等吧"
						: "请先分享游戏给好友");
					break;
				default:
					SetTip("网络不给力，稍后再试");
					break;
			}
		}

		void SetTip(string text)
		{
			if (Txt_Tip != null) Txt_Tip.text = text;
		}
	}
}
