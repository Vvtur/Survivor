using UnityEngine;
using UnityEngine.UI;
using QFramework;
using QFramework.Gameplay;

namespace QFramework.UI
{
	public class ShopPanelData : UIPanelData
	{
	}
	public partial class ShopPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ShopPanelData ?? new ShopPanelData();
			// please add init code here
			Btn_AttackAdd.onClick.AddListener(() =>
			{
				GameArchitecture.Interface.SendCommand(new BuyCommand(1));
			});
			Btn_Close.onClick.AddListener(() =>
			{
				Hide();
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
