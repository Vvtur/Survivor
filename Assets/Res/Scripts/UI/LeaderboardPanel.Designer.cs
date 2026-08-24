using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:8f3d2c41-7a6e-4b52-9d10-3f8c1e5a2b74
	public partial class LeaderboardPanel
	{
		public const string Name = "LeaderboardPanel";

		[SerializeField]
		public UnityEngine.UI.Button Btn_Close;

		[SerializeField]
		public UnityEngine.UI.Button Btn_Share;

		[SerializeField]
		public UnityEngine.UI.RawImage RawImage_Board;

		private LeaderboardPanelData mPrivateData = null;

		protected override void ClearUIComponents()
		{
			Btn_Close = null;
			Btn_Share = null;
			RawImage_Board = null;

			mData = null;
		}

		public LeaderboardPanelData Data
		{
			get
			{
				return mData;
			}
		}

		LeaderboardPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LeaderboardPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
