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
	public partial class PlayerInfoPanel : UIPanel, IUnRegisterList
	{
		// 订阅句柄集合：所有注册自动收集，UnRegisterAll 一次性取消
		public List<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();

		private GameModel mModel;

        private void Awake()
        {
			mModel = GameArchitecture.Interface.GetModel<GameModel>();

            mModel.HP.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
			mModel.MaxHp.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
			mModel.Exp.RegisterWithInitValue(_ => RefreshExp()).AddToUnregisterList(this);
			mModel.Level.RegisterWithInitValue(_ => RefreshLevel()).AddToUnregisterList(this);
			mModel.AttackDamage.RegisterWithInitValue(_ => RefreshAttack()).AddToUnregisterList(this);
        }

        protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as PlayerInfoPanelData ?? new PlayerInfoPanelData();

			mModel = GameArchitecture.Interface.GetModel<GameModel>();

			// 链式订阅：注册即立即刷新一次，之后每次变化自动刷新
			mModel.HP.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
			mModel.MaxHp.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
			mModel.Exp.RegisterWithInitValue(_ => RefreshExp()).AddToUnregisterList(this);
			mModel.Level.RegisterWithInitValue(_ => RefreshLevel()).AddToUnregisterList(this);
			mModel.AttackDamage.RegisterWithInitValue(_ => RefreshAttack()).AddToUnregisterList(this);
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

		protected override void OnOpen(IUIData uiData = null)
		{
		}

		protected override void OnShow()
		{
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			// 一次性取消所有订阅，防止面板复用时重复订阅
			this.UnRegisterAll();
		}

        protected override void OnDestroy()
        {
            base.OnDestroy();
			this.UnRegisterAll();
        }

    }
}
