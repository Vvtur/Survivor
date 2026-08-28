using QFramework;
using QFramework.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.UI
{
	public class GameLevelUpPanelData : UIPanelData
	{
		public AbilityConfig[] Options;   // 传入几个就生成几个
	}
	public partial class GameLevelUpPanel : UIPanel
	{
		private readonly ResLoader mResLoader = ResLoader.Allocate();

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameLevelUpPanelData ?? new GameLevelUpPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as GameLevelUpPanelData ?? new GameLevelUpPanelData();
			AudioKit.PlaySound(AudioNames.PanelOpen); // 面板打开音效

			// 先清空容器（面板可能被复用）
			foreach (Transform child in Panel.rectTransform) Destroy(child.gameObject);

			// 没传能力就直接不显示
			if (mData.Options == null || mData.Options.Length == 0) return;

			// 异步加载选项预制体（WebGL 平台不支持同步加载），加载完成后再生成按钮
			mResLoader.Add2Load<GameObject>("Btn_Option", (b, res) =>
			{
				if (!b)
				{
					Debug.LogError("加载 Btn_Option 预制体失败！请确认已标记 AB（资源名 Btn_Option）");
					return;
				}

				var itemPrefab = res.Asset.As<GameObject>();

				// 传进来几个能力，就生成几个按钮
				foreach (var ability in mData.Options)
				{
					var item = Instantiate(itemPrefab, Panel.rectTransform).GetComponent<AbilityOptionItem>();
					if (item == null)
					{
						Debug.LogError("Btn_Option 预制体根节点缺少 AbilityOptionItem 组件，请在预制体上手动添加");
						continue;
					}
					item.SetData(ability);
					item.GetComponent<Button>().onClick.AddListener(() =>
					{
						AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
						// 选择能力 → 通过 Command 修改 Model 属性
						// 注：UIPanel 基类不实现 IController，无法用 this.SendCommand 扩展方法，
						// 只能通过 GameArchitecture.Interface 静态单例访问
						GameArchitecture.Interface.SendCommand(new ChooseAbilityCommand(ability));
						Time.timeScale = 1f; // 恢复游戏
						UIKit.ClosePanel<GameLevelUpPanel>();
					});
				}
			});
			mResLoader.LoadAsync();
		}

		protected override void OnShow()
		{
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			AudioKit.PlaySound(AudioNames.PanelClose); // 面板关闭音效
			// 面板关闭（被 UIKit 移除）时清理
		}

		protected override void OnBeforeDestroy()
		{
			mResLoader.Recycle2Cache();
			base.OnBeforeDestroy();
		}
	}
}
