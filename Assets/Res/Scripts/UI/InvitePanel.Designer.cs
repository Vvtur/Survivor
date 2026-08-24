using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:3f8a2d51-9c4e-4f6a-8b1d-2e7c5a9f0b63
	public partial class InvitePanel
	{
		public const string Name = "InvitePanel";

		[SerializeField]
		public UnityEngine.UI.Button Btn_Close;

		[SerializeField]
		public UnityEngine.UI.Button Btn_Share;

		[SerializeField]
		public UnityEngine.UI.Button Btn_Claim;

		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Tip;

		private InvitePanelData mPrivateData = null;

		protected override void ClearUIComponents()
		{
			Btn_Close = null;
			Btn_Share = null;
			Btn_Claim = null;
			Txt_Tip = null;

			mData = null;
		}

        public InvitePanelData Data
		{
			get
			{
				return mData;
			}
		}

		InvitePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new InvitePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
