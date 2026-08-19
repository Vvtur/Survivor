using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:da07416b-aeb1-45a2-bf4c-d2afef1441e4
	public partial class ShopPanel
	{
		public const string Name = "ShopPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Btn_AttackAdd;
		[SerializeField]
		public UnityEngine.UI.Button Btn_Close;
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Money;
		
		private ShopPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Btn_AttackAdd = null;
			Btn_Close = null;
			Txt_Money = null;
			
			mData = null;
		}
		
		public ShopPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ShopPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ShopPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
