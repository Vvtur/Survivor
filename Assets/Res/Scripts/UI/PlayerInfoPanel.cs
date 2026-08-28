using System.Collections.Generic;
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
	/// 玩家信息面板（常驻显示 HP / EXP / LV / 攻击）。
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
		private bool mSubscribed;

		private void Awake()
		{
			// 场景加载时挂载本面板：订阅 Model 全部需要展示的字段
			// 注意：本面板**不走 UIKit 生命周期**（不在 UIKit 注册的 Panel 列表里），
			// QF 的 UIPanel.OnInit 不会被调用，所有初始化逻辑放这里。
			SubscribeModel();
		}

		private void SubscribeModel()
		{
			if (mSubscribed) return;
			mSubscribed = true;

			// 注：UIPanel 基类不实现 IController，无法用 this.GetModel<> 扩展方法
			mModel = GameArchitecture.Interface.GetModel<GameModel>();

			// 链式订阅：注册即立即刷新一次，之后每次变化自动刷新
			mModel.HP.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
			mModel.MaxHp.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
			mModel.Exp.RegisterWithInitValue(_ => RefreshExp()).AddToUnregisterList(this);
			mModel.Level.RegisterWithInitValue(_ => RefreshLevel()).AddToUnregisterList(this);
			mModel.AttackDamage.RegisterWithInitValue(_ => RefreshAttack()).AddToUnregisterList(this);
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
			this.UnRegisterAll();
		}

		private void RefreshHP()
		{
			Txt_HP.text = $"生命: {mModel.HP.Value}/{mModel.MaxHp.Value}";
		}

		private void RefreshExp()
		{
			var need = mModel.Level.Value * 2 + 1; // 与 LevelUpSystem 的升级公式一致
			Txt_Exp.text = $"经验: {mModel.Exp.Value}/{need}";
		}

		private void RefreshLevel()
		{
			Txt_Lv.text = $"等级: {mModel.Level.Value}";
		}

		private void RefreshAttack()
		{
			Txt_Attack.text = $"攻击: {mModel.AttackDamage.Value + mModel.Attack.Value}";
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
