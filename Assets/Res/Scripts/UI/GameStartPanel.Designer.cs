using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:2074a6a1-15b2-4a37-87b8-854ca55f11d0
	public partial class GameStartPanel
	{
		public const string Name = "GameStartPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Btn_Start;

		[SerializeField]
		public UnityEngine.UI.Button Btn_Board;

		private GameStartPanelData mPrivateData = null;

		protected override void ClearUIComponents()
		{
			Btn_Start = null;
			Btn_Board = null;

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
