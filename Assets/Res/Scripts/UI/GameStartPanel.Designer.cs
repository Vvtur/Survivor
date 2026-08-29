using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:ebad5b88-9866-4c45-9ca2-ad976f9a16a4
	public partial class GameStartPanel
	{
		public const string Name = "GameStartPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Btn_Start;
		[SerializeField]
		public UnityEngine.UI.Button Btn_Board;
		[SerializeField]
		public UnityEngine.UI.Button Btn_Invite;
		[SerializeField]
		public UnityEngine.UI.Button Btn_Shop;
		/// <summary>设置入口按钮（右上角齿轮），点击打开 SettingsPanel</summary>
		[SerializeField]
		public UnityEngine.UI.Button Btn_Settings;
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Title;

		private GameStartPanelData mPrivateData = null;

		protected override void ClearUIComponents()
		{
			Btn_Start = null;
			Btn_Board = null;
			Btn_Invite = null;
			Btn_Shop = null;
			Btn_Settings = null;
			Txt_Title = null;

			mData = null;
		}
		
		public GameStartPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameStartPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameStartPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
