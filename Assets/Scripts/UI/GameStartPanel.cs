using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	public class GameStartPanelData : UIPanelData
	{
	}
	public partial class GameStartPanel : UIPanel
	{
		// 申请一个 ResLoader 供本面板使用，OnDestroy 时统一回收
		readonly ResLoader mResLoader = ResLoader.Allocate();

		// 直接挂在场景里的面板不会经过 UIKit 的 Init() 生命周期，
		// 因此按钮绑定放在 Awake 中，保证挂场景时也能注册点击事件
		void Awake()
		{
			// WebGL 平台必须用异步初始化加载 AB 配置表（同步 Init 在 WebGL 下读不到 StreamingAssets），
			// 初始化完成后再注册按钮点击，否则点击 start 会因配置表为空而无效
			StartCoroutine(InitAndBindStart());
		}

		IEnumerator InitAndBindStart()
		{
			// 若已初始化（如编辑器模拟模式自动初始化）会立即返回，不会重复初始化
			yield return ResKit.InitAsync();

			Btn_Start.onClick.AddListener(() =>
			{
				// 场景走 ResKit AB 加载（模拟模式/真机均适用）
				mResLoader.LoadSceneAsync("maingame_unity", "MainGame");
				CloseSelf();
			});
		}

		protected override void OnDestroy()
		{
			mResLoader.Recycle2Cache(); // 释放资源引用，归还对象池
			base.OnDestroy();
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameStartPanelData ?? new GameStartPanelData();
			// please add init code here
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
