using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.UI
{
	// Generate Id:8b3f2e7c-5a4d-4e9b-a7c1-6d8e2f1a3b9c
	public partial class SettingsPanel
	{
		public const string Name = "SettingsPanel";

		[SerializeField]
		public UnityEngine.UI.Button Btn_Close;
		/// <summary>背景全屏 Image（兜底点击空白处关面板）</summary>
		[SerializeField]
		public UnityEngine.UI.Image Img_BG;
		/// <summary>面板标题文字</summary>
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_Title;
		/// <summary>音乐开关 Toggle</summary>
		[SerializeField]
		public UnityEngine.UI.Toggle Tog_Music;
		/// <summary>音乐开关标签</summary>
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_MusicLabel;
		/// <summary>音乐音量 Slider（0~1）</summary>
		[SerializeField]
		public UnityEngine.UI.Slider Sld_MusicVolume;
		/// <summary>音乐音量百分比文字</summary>
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_MusicValue;
		/// <summary>音效开关 Toggle</summary>
		[SerializeField]
		public UnityEngine.UI.Toggle Tog_Sound;
		/// <summary>音效开关标签</summary>
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_SoundLabel;
		/// <summary>音效音量 Slider（0~1）</summary>
		[SerializeField]
		public UnityEngine.UI.Slider Sld_SoundVolume;
		/// <summary>音效音量百分比文字</summary>
		[SerializeField]
		public TMPro.TextMeshProUGUI Txt_SoundValue;

		private SettingsPanelData mPrivateData = null;

		protected override void ClearUIComponents()
		{
			Btn_Close = null;
			Img_BG = null;
			Txt_Title = null;
			Tog_Music = null;
			Txt_MusicLabel = null;
			Sld_MusicVolume = null;
			Txt_MusicValue = null;
			Tog_Sound = null;
			Txt_SoundLabel = null;
			Sld_SoundVolume = null;
			Txt_SoundValue = null;

			mData = null;
		}

		public SettingsPanelData Data
		{
			get
			{
				return mData;
			}
		}

		SettingsPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SettingsPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
