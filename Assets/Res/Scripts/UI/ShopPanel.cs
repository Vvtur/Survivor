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
		GameModel model;
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ShopPanelData ?? new ShopPanelData();
			model = GameArchitecture.Interface.GetModel<GameModel>();
			// please add init code here
			Btn_AttackAdd.onClick.AddListener(() =>
			{
				GameArchitecture.Interface.SendCommand(new BuyCommand(1));
			});
			Btn_Close.onClick.AddListener(() =>
			{
				Hide();
			});

			model.Money.RegisterWithInitValue(_ => RefreshMoney()).UnRegisterWhenGameObjectDestroyed(this);
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

		private void RefreshMoney()
		{
			Txt_Money.text = "Gold:" + model.Money.Value;
		}
	}
}
