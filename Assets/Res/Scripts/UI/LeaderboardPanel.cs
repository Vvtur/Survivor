using QFramework;
using QFramework.Gameplay;
using UnityEngine;

namespace QFramework.UI
{
	public class LeaderboardPanelData : UIPanelData
	{
	}

	/// <summary>
	/// 好友排行榜面板。
	/// 数据读取与绘制全部在开放数据域（WX/minigame/open-data/index.js）完成——
	/// 好友隐私数据不允许离开微信沙盒，主域能拿到的只有 sharedCanvas 渲染出的这张"图"。
	/// 本面板职责仅两件事：
	///   打开时：把 RawImage 的屏幕矩形交给 WX.ShowOpenData，SDK 每帧把沙盒画布刷进占位纹理
	///   关闭时：WX.HideOpenData 停止刷新
	/// </summary>
	public partial class LeaderboardPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LeaderboardPanelData ?? new LeaderboardPanelData();

			Btn_Close.onClick.AddListener(() =>
			{
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				CloseSelf();
			});
			// 分享按钮：统一走邀请链路（query 带 inviter）——所有分享入口都能带来邀请奖励，
			// 避免玩家从排行榜分享导致好友进入后不计入 invite_events
			Btn_Share.onClick.AddListener(() =>
			{
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				GameArchitecture.Interface.GetSystem<IInviteSystem>().ShareWithInvite(null);
			});
		}

		Texture2D mBoardTex; // 占位纹理（复用，避免每次打开泄漏一张）

		protected override void OnOpen(IUIData uiData = null)
		{
			AudioKit.PlaySound(AudioNames.PanelOpen); // 面板打开音效
#if UNITY_WEBGL && !UNITY_EDITOR
			// 占位纹理：尺寸无所谓，SDK 会按传入的屏幕矩形把 GL 纹理重设为沙盒画布大小并逐帧刷新
			if (mBoardTex == null)
			{
				mBoardTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				RawImage_Board.texture = mBoardTex;
			}
			// Canvas 画布原点在左上、WebGL 纹理原点在左下，texImage2D 上传不翻转 → 纹理里画布是倒的。
			// uvRect 取 (0,1,1,-1)：v 从 1（RawImage 底部）走到 0（顶部），在 [0,1] 边界内完成垂直镜像。
			// 注意不能用 (0,0,1,-1)：v 会落到 [-1,0]，GL 纹理的 CLAMP_TO_EDGE 把全部采样钳在 v=0 一行——
			// 整块 RawImage 只显示画布第一行像素的拉伸（纯色背景可见、画面中部的文字永远不可见）。
			RawImage_Board.uvRect = new Rect(0, 1, 1, -1);

			// 显示开放数据域画布（头像昵称授权已在开始界面前置处理，见 GameStartPanel）
			ShowBoard();
#endif
			// 编辑器：无微信运行时，面板显示占位纹理（全白/无内容属正常）
		}

		// RawImage 屏幕矩形 → 微信要求的"左上角原点"屏幕坐标
		// （UIRoot 为 ScreenSpaceOverlay：worldCorners 直接就是屏幕像素）
		void ShowBoard()
		{
			var corners = new Vector3[4];
			RawImage_Board.rectTransform.GetWorldCorners(corners); // [0]左下 [1]左上 [2]右上
			float x = corners[0].x;
			float y = Screen.height - corners[1].y; // 左下原点 → 左上原点
			float w = corners[2].x - corners[0].x;
			float h = corners[1].y - corners[0].y;

			WeChatWASM.WX.GetOpenDataContext(); // 首次调用会初始化沙盒（可重复调，内部有缓存）
			WeChatWASM.WX.ShowOpenData(mBoardTex, (int)x, (int)y, (int)w, (int)h);
		}

		protected override void OnClose()
		{
			AudioKit.PlaySound(AudioNames.PanelClose); // 面板关闭音效
#if UNITY_WEBGL && !UNITY_EDITOR
			WeChatWASM.WX.HideOpenData();
#endif
		}
	}
}
