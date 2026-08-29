using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using QFramework.Gameplay;

namespace QFramework.UI
{
	public class PlayerInfoPanelData : UIPanelData
	{
	}

	/// <summary>
	/// 玩家信息面板（常驻 HUD，对齐吸血鬼幸存者参考图布局）：
	/// 顶部全宽经验条（平滑填充 + 升级脉冲）、左上 等级/生命/攻击、中上 时间、右上 击杀/金币。
	///
	/// 挂载方式：作为预制体挂在 MainGame 场景里，随场景加载，**不走 UIKit.OpenPanel 生命周期**。
	/// 因此 QF 的 UIPanel.OnInit / OnOpen / OnClose / OnDestroy（QF 虚方法）不会被触发，
	/// 订阅逻辑必须放在 Unity 的 Awake / OnDestroy（Unity 消息）里。
	///
	/// 订阅通过 IUnRegisterList 收集，场景销毁时 OnDestroy 一次性 UnRegisterAll，
	/// 避免 BindableProperty 强引用面板导致场景切换后内存泄漏。
	/// </summary>
	public partial class PlayerInfoPanel : UIPanel, IUnRegisterList
	{
		// 订阅句柄集合：所有注册自动收集，UnRegisterAll 一次性取消
		public List<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();

		private GameModel mModel;
		private IWaveSystem mWaveSystem;     // 时间数据源（Awake 缓存，避免 Update 里反复查询架构）
		private bool mSubscribed;

		private int mLastLevel;              // 升级脉冲判定用（只在真正升级时播，防 ResetRunData 误触发）
		private int mLastSecond = -1;        // 时间轮询：整秒变化才刷新字符串（WebGL 省GC）
		private Tweener mFillTween;          // 经验条填充 tween（新变化来了先 Kill 旧的，保证幂等）

		private void Awake()
		{
			// 场景加载时挂载本面板：订阅 Model 全部需要展示的字段
			// 注意：本面板**不走 UIKit 生命周期**（不在 UIKit 注册的 Panel 列表里），
			// QF 的 UIPanel.OnInit 不会被调用，所有初始化逻辑放这里。
			SubscribeModel();

			// 时间数据源（架构在 Boot 阶段初始化，进 MainGame 时必然已就绪）
			mWaveSystem = GameArchitecture.Interface.GetSystem<IWaveSystem>();

			// 经验条首次无动画对齐（避免场景打开瞬间从 0 滚到当前值的突兀动画）
			if (Img_ExpFill != null)
			{
				Img_ExpFill.fillAmount = ComputeExpRatio();
			}
		}

		private void SubscribeModel()
		{
			if (mSubscribed) return;
			mSubscribed = true;

			// 注：UIPanel 基类不实现 IController，无法用 this.GetModel<> 扩展方法
			mModel = GameArchitecture.Interface.GetModel<GameModel>();
			// 先记住当前等级再订阅：防止"注册即回调"在开局瞬间误播升级脉冲
			mLastLevel = mModel.Level.Value;

			// 链式订阅：注册即立即刷新一次，之后每次变化自动刷新
			mModel.HP.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
			mModel.MaxHp.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
			mModel.Exp.RegisterWithInitValue(_ => RefreshExp()).AddToUnregisterList(this);
			mModel.Level.RegisterWithInitValue(_ => RefreshLevel()).AddToUnregisterList(this);
			mModel.AttackDamage.RegisterWithInitValue(_ => RefreshAttack()).AddToUnregisterList(this);
			mModel.KillCount.RegisterWithInitValue(_ => RefreshKill()).AddToUnregisterList(this);
			mModel.Money.RegisterWithInitValue(_ => RefreshMoney()).AddToUnregisterList(this);
		}

		private void Update()
		{
			// 时间显示：每帧轮询但只在整秒变化时刷新文本（减少字符串分配）
			// WaveSystem 内部用 Time.deltaTime 累加，升级暂停（timeScale=0）时时间自动不走
			if (mWaveSystem == null || Txt_Time == null) return;
			var t = (int)mWaveSystem.ElapsedTime;
			if (t == mLastSecond) return;
			mLastSecond = t;
			Txt_Time.text = $"{t / 60:00}:{t % 60:00}";
		}

		// Unity 的 OnDestroy 消息：场景销毁（切到 GameStart）或编辑器停止时触发。
		// 这里必须手动注销所有 BindableProperty 订阅，否则面板虽然销毁了，
		// GameModel 里的 Register 列表还强引用着回调 → 场景切回来会重复订阅 / 内存泄漏。
		// 注：UIPanel 也定义了 protected virtual void OnDestroy()（QF 生命周期，非 Unity 消息），
		// 本面板不走 QF 生命周期，这里用 new 关键字隐藏基类方法，避免 CS0108 警告。
		// 不调 base.OnDestroy() 的原因：ClearUIComponents 只为复用场景清理 SerializeField，
		// 本面板每次随场景加载新建，不需要复用，无需清理。
		private new void OnDestroy()
		{
			if (mFillTween != null && mFillTween.IsActive()) mFillTween.Kill();
			this.UnRegisterAll();
		}

		/// <summary>当前经验占升级所需经验的比例（公式唯一出处：GameModel.ExpToNextLevel）</summary>
		private float ComputeExpRatio()
		{
			var need = mModel.ExpToNextLevel;
			return need > 0 ? Mathf.Clamp01((float)mModel.Exp.Value / need) : 0f;
		}

		private void RefreshHP()
		{
			Txt_HP.text = $"生命: {mModel.HP.Value}/{mModel.MaxHp.Value}";
		}

		private void RefreshExp()
		{
			// 经验条平滑填充（SetUpdate：timeScale=0 也能播）
			if (Img_ExpFill != null)
			{
				if (mFillTween != null && mFillTween.IsActive()) mFillTween.Kill();
				mFillTween = Img_ExpFill.DOFillAmount(ComputeExpRatio(), 0.25f).SetUpdate(true);
			}
			Txt_Exp.text = $"{mModel.Exp.Value}/{mModel.ExpToNextLevel}";
		}

		private void RefreshLevel()
		{
			Txt_Lv.text = $"等级: {mModel.Level.Value}";

			// 升级脉冲：只在"真正升级"时播（每局开局 ResetRunData 把等级改回 1 也会触发本回调，不能播）。
			// 表现：经验条瞬间打满 → 随后的 RefreshExp 平滑回落到新等级的剩余进度，同时条身轻弹一下。
			if (mModel.Level.Value > mLastLevel)
			{
				mLastLevel = mModel.Level.Value;
				if (Img_ExpFill != null)
				{
					if (mFillTween != null && mFillTween.IsActive()) mFillTween.Kill();
					Img_ExpFill.fillAmount = 1f;

					var barRoot = Img_ExpFill.transform.parent; // Img_ExpBG：脉冲弹的是整个条
					if (barRoot != null)
					{
						barRoot.DOKill();
						barRoot.DOPunchScale(Vector3.one * 0.08f, 0.3f, 6, 0.5f).SetUpdate(true);
					}
				}
			}

			RefreshExp();
		}

		private void RefreshAttack()
		{
			Txt_Attack.text = $"攻击: {mModel.AttackDamage.Value + mModel.Attack.Value}";
		}

		private void RefreshKill()
		{
			Txt_Kill.text = $"击杀: {mModel.KillCount.Value}";
		}

		private void RefreshMoney()
		{
			Txt_Money.text = $"金币: {mModel.Money.Value}";
		}

		// ---- 以下是 UIPanel 虚方法（保留以满足继承约束） ----
		// 本面板不走 UIKit 生命周期，以下方法**不会被 QF 调用**，留空实现即可。
		protected override void OnInit(IUIData uiData = null) { }
		protected override void OnOpen(IUIData uiData = null) { }
		protected override void OnShow() { }
		protected override void OnHide() { }
		protected override void OnClose() { }
	}
}
