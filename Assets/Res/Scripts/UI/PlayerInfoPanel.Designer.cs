using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:14944410-4be4-48c9-863a-5678b1ebf237
	public partial class PlayerInfoPanel
	{
		public const string Name = "PlayerInfoPanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_HP;
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Exp;
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Lv;
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Attack;
		[SerializeField]
		public UnityEngine.UI.Image Img_ExpFill;
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Time;
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Kill;
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Money;

		private PlayerInfoPanelData mPrivateData = null;

		protected override void ClearUIComponents()
		{
			Txt_HP = null;
			Txt_Exp = null;
			Txt_Lv = null;
			Txt_Attack = null;
			Img_ExpFill = null;
			Txt_Time = null;
			Txt_Kill = null;
			Txt_Money = null;

			mData = null;
		}
		
		public PlayerInfoPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		PlayerInfoPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new PlayerInfoPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
