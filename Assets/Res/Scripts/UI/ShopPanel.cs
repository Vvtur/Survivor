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
			// 注：UIPanel 基类不实现 IController，无法用 this.GetModel / this.SendCommand 扩展方法
			model = GameArchitecture.Interface.GetModel<GameModel>();
			// please add init code here
			Btn_AttackAdd.onClick.AddListener(() =>
			{
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				GameArchitecture.Interface.SendCommand(new BuyCommand(1));
			});
			Btn_Close.onClick.AddListener(() =>
			{
				AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
				Hide();
			});

			model.Money.RegisterWithInitValue(_ => RefreshMoney()).UnRegisterWhenGameObjectDestroyed(this);
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			AudioKit.PlaySound(AudioNames.PanelOpen); // 面板打开音效
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			AudioKit.PlaySound(AudioNames.PanelClose); // 面板关闭音效
		}

		private void RefreshMoney()
		{
			Txt_Money.text = "金币:" + model.Money.Value;
		}
	}
}
