using System;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.UI
{
	// Generate Id:0d4d9a7c-6f2e-4c1b-9a55-7f3b8c2e1d01
	public partial class LoadingPanel
	{
		public const string Name = "LoadingPanel";

		[SerializeField]
		public UnityEngine.UI.Image Img_Wipe;          // 擦除转场层( UITransitionWipe 材质)

		[SerializeField]
		public UnityEngine.RectTransform Rt_Content;   // 内容组(标题/百分比/进度条),动效整体位移

		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Title;

		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Percent;

		[SerializeField]
		public UnityEngine.UI.Image Img_BarBG;

		[SerializeField]
		public UnityEngine.UI.Image Img_BarFill;

		private LoadingPanelData mPrivateData = null;

		protected override void ClearUIComponents()
		{
			Img_Wipe = null;
			Rt_Content = null;
			Txt_Title = null;
			Txt_Percent = null;
			Img_BarBG = null;
			Img_BarFill = null;

			mData = null;
		}

		public LoadingPanelData Data
		{
			get
			{
				return mData;
			}
		}

		LoadingPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LoadingPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
