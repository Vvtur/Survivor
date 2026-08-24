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
		
		private GameStartPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Btn_Start = null;
			Btn_Board = null;
			Btn_Invite = null;
			
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
