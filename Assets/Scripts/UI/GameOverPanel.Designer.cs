using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:c8ceb13f-b23e-439e-9690-98b70eda8afb
	public partial class GameOverPanel
	{
		public const string Name = "GameOverPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Btn_ReStart;
		
		private GameOverPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Btn_ReStart = null;
			
			mData = null;
		}
		
		public GameOverPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameOverPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameOverPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
