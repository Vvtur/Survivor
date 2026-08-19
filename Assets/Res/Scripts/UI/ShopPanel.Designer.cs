using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:7b8b96a4-4879-4fbb-a155-cec6efafab45
	public partial class ShopPanel
	{
		public const string Name = "ShopPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Btn_AttackAdd;
		[SerializeField]
		public UnityEngine.UI.Button Btn_Close;
		
		private ShopPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Btn_AttackAdd = null;
			Btn_Close = null;
			
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
