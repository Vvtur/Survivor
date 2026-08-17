using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	public class GameOverPanelData : UIPanelData
	{
	}
	public partial class GameOverPanel : UIPanel
	{
		// 申请一个 ResLoader 供本面板使用，OnDestroy 时统一回收
		readonly ResLoader mResLoader = ResLoader.Allocate();

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameOverPanelData ?? new GameOverPanelData();
			// please add init code here
			Btn_ReStart.onClick.AddListener(() =>
			{
				Time.timeScale = 1f; // 恢复时间缩放，否则刚体物理仍被暂停
				StartCoroutine(RestartCoroutine());
			});
		}

		// WebGL 平台必须异步初始化/加载，用协程驱动
		private System.Collections.IEnumerator RestartCoroutine()
		{
			yield return ResKit.InitAsync();

			// 场景走 ResKit AB 加载（异步，WebGL 兼容）。注意：GameStart 既是场景名又是 UI 面板名，
			// 必须用"AB名+资源名"重载精确匹配场景，否则会匹配到同名 UI 面板
			mResLoader.LoadSceneAsync("gamestart_unity", "GameStart");
			UIKit.CloseAllPanel();
		}

		protected override void OnDestroy()
		{
			mResLoader.Recycle2Cache(); // 释放资源引用，归还对象池
			base.OnDestroy();
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
