using QFramework;
using QFramework.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.UI
{
	public class SettingsPanelData : UIPanelData
	{
	}

	/// <summary>
	/// 设置面板：音乐/音效总开关 + 音量条。
	///
	/// 数据源：AudioKit.Settings（AudioKitSettingsModel，AbstractModel 子类）——
	/// 自带 BindableProperty + PlayerPrefs 持久化，业务方只需读写 .Value 即可。
	/// AudioManager 内部已 Register 监听这些字段，赋值后即时生效（音乐变小声/静音）。
	/// 本面板不引入新的 Model/Command：AudioKit 已经按 QF 规范把音频数据接入架构，
	/// 加一层"设置 Command"是冗余间接。
	///
	/// QF 规范说明：UIPanel 基类不实现 IController，无法用 this.GetModel 扩展方法；
	/// AudioKit.Settings 是静态访问器，直接读写即可。
	/// </summary>
	public partial class SettingsPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SettingsPanelData ?? new SettingsPanelData();

			// 关闭按钮：先播右滑出动画，动画完成后再真正关面板
			// 使用侧边抽屉式动画（PlaySlideIn/PlaySlideOut）而非通用的缩放（PlayOpen/PlayClose），
			// 与本面板"从右滑入、右滑出"的设计语言保持一致。
			Btn_Close.onClick.AddListener(() =>
			{
				AudioKit.PlaySound(AudioNames.ButtonClick);
				this.PlaySlideOut(CloseSelf);
			});

			// 点击背景空白处也能关面板（兜底：用户找不到 X 也能退）
			if (Img_BG != null)
			{
				// 仅当点击在"内容区外"（用 Btn_Close 区域判定）才拦截——面板主体区域由 Slider/Toggle 自然接住
				Img_BG.GetComponent<Button>()?.onClick.AddListener(() =>
				{
					AudioKit.PlaySound(AudioNames.ButtonClick);
					this.PlaySlideOut(CloseSelf);
				});
			}

			// —— 音乐总开关 ——
			// Toggle 改时同步到 Model（AudioKit 的 IsMusicOn 是 PlayerPrefsBooleanProperty，
			// 赋值即自动写 PlayerPrefs）。AudioManager 内部已 Register IsMusicOn 监听，
			// 关闭时会自动停音乐，开启时会尝试恢复上次 CurrentMusicName。
			Tog_Music.onValueChanged.AddListener(v =>
			{
				AudioKit.Settings.IsMusicOn.Value = v;
				// 开关关闭时禁用音量条（视觉反馈：音量此时无意义）
				Sld_MusicVolume.interactable = v;
			});

			// —— 音乐音量 ——
			// Slider 0~1 直接对应 AudioKit 的 0~1 音量档位（标准映射，无需换算）。
			// MusicPlayer 内部引用了 MusicVolume（构造函数传入），改值后音乐即时跟随。
			Sld_MusicVolume.onValueChanged.AddListener(v =>
			{
				AudioKit.Settings.MusicVolume.Value = v;
				RefreshMusicValueText(v);
			});

			// —— 音效总开关 ——
			Tog_Sound.onValueChanged.AddListener(v =>
			{
				AudioKit.Settings.IsSoundOn.Value = v;
				Sld_SoundVolume.interactable = v;
			});

			// —— 音效音量 ——
			Sld_SoundVolume.onValueChanged.AddListener(v =>
			{
				AudioKit.Settings.SoundVolume.Value = v;
				RefreshSoundValueText(v);
			});
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			AudioKit.PlaySound(AudioNames.PanelOpen);

			// 打开面板时按当前 Model 值刷新 UI（用 *WithoutNotify 系列避免触发
			// onValueChanged 回环到 Model 写入——造成 AudioKit.Settings 再次写 PlayerPrefs 冗余）：
			// - 总开关、Slider 初值必须来自 Model，不能用 prefab 上的默认值
			// - 总开关关掉时，音量条应禁用
			// - 百分比文字要显示当前音量
			var settings = AudioKit.Settings;
			Tog_Music.SetIsOnWithoutNotify(settings.IsMusicOn.Value);
			Tog_Sound.SetIsOnWithoutNotify(settings.IsSoundOn.Value);
			Sld_MusicVolume.SetValueWithoutNotify(settings.MusicVolume.Value);
			Sld_SoundVolume.SetValueWithoutNotify(settings.SoundVolume.Value);
			Sld_MusicVolume.interactable = settings.IsMusicOn.Value;
			Sld_SoundVolume.interactable = settings.IsSoundOn.Value;
			RefreshMusicValueText(settings.MusicVolume.Value);
			RefreshSoundValueText(settings.SoundVolume.Value);

			// 预设面板根到屏外右侧 + 透明度 0：
			// UIManager 的 Open 顺序是先 OnOpen 再 SetActive(true) 再 OnShow，
			// OnOpen 在 SetActive 前，所以这里改 localPosition / alpha 不会触发任何渲染。
			// 这样 SetActive 后第一帧渲染时面板已在屏外不可见，避免“面板闪一下到中心位置”
			// （Single 模式复用时重用同一个实例，预设位至关重要）。
			var root = (RectTransform)transform;
			var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
			float offRight = root.rect.width;
			root.localPosition = new Vector3(offRight, root.localPosition.y, root.localPosition.z);
			cg.alpha = 0f;
			cg.blocksRaycasts = false;
			cg.interactable = false;
		}

		protected override void OnShow()
		{
			// 侧边抽屉式打开动画：透明度 0→1 + 面板根从屏外右侧滑到 x=0。
			// 不用通用 PlayOpen（缩放弹出），与"右滑入、右滑出"设计语言一致。
			// Single 模式复用重开时也会再次触发，PlaySlideIn 内部 DOKill 防残留。
			this.PlaySlideIn();
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			AudioKit.PlaySound(AudioNames.PanelClose);
		}

		// 音量百分比文字：100% 比 "1.0" 更直观，且带 % 符号让玩家一眼看懂
		void RefreshMusicValueText(float v)
		{
			if (Txt_MusicValue != null) Txt_MusicValue.text = $"{Mathf.RoundToInt(v * 100)}%";
		}

		void RefreshSoundValueText(float v)
		{
			if (Txt_SoundValue != null) Txt_SoundValue.text = $"{Mathf.RoundToInt(v * 100)}%";
		}
	}
}
