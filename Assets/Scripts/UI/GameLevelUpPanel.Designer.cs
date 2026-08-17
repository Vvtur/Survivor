using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:a039d7f5-29a6-44bc-a605-bb5034be54e5
	public partial class GameLevelUpPanel
	{
		public const string Name = "GameLevelUpPanel";
		
		[SerializeField]
		public UnityEngine.UI.Image Panel;
		
		private GameLevelUpPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Panel = null;
			
			mData = null;
		}
		
		public GameLevelUpPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameLevelUpPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameLevelUpPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
