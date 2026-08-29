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
			int optionIndex = 0;
			foreach (var ability in mData.Options)
			{
				var item = Instantiate(itemPrefab, Panel.rectTransform).GetComponent<AbilityOptionItem>();
				if (item == null)
				{
					Debug.LogError("Btn_Option 预制体根节点缺少 AbilityOptionItem 组件，请在预制体上手动添加");
					continue;
				}
				item.SetData(ability);
				// 选项错峰弹出（动画内部 SetUpdate(true)，timeScale=0 也能播）
				item.GetComponent<RectTransform>().PlayIntro(optionIndex * 0.06f);
				optionIndex++;
				item.GetComponent<Button>().onClick.AddListener(() =>
				{
					AudioKit.PlaySound(AudioNames.ButtonClick); // 按钮点击音效
					// 选择能力 → 通过 Command 修改 Model 属性（数据立即生效）
					// 注：UIPanel 基类不实现 IController，无法用 this.SendCommand 扩展方法，
					// 只能通过 GameArchitecture.Interface 静态单例访问
					GameArchitecture.Interface.SendCommand(new ChooseAbilityCommand(ability));

					// 待选升级次数 -1（LevelUpSystem 在 while 循环里连升 N 级时设的计数）。
					// QF 规范：改数据走 Command；这里面板基类不实现 IController，
					// 用表现层专用 Command 解耦"点选 UI"与"Model 计数变更"。
					GameArchitecture.Interface.SendCommand(new ConsumePendingLevelUpCommand());

					// 先播收缩动画（期间禁用所有按钮防连点）。动画完成后：
					//  - 若仍有待选升级（磁铁一次升多级）：保持 timeScale=0，重新开本面板（OnOpen 会清空+重建按钮）
					//  - 若本轮升级已选完：恢复 timeScale=1 并真正 ClosePanel
					// 这样"一次升多级就要选多次能力"的体验就是逐次选、每选完都播收缩动画，不会一次性把 N 次都跳过去。
					foreach (var btn in GetComponentsInChildren<Button>(true)) btn.interactable = false;
					this.PlayClose(() =>
					{
						var model = GameArchitecture.Interface.GetModel<GameModel>();
						if (model.PendingLevelUps.Value > 0)
						{
							// 还有未选升级：保持暂停，重新打开本面板；PlayerController.OnLevelUp 那一帧
							// 只能开一个面板实例，后续连选就由本回调接力。
							var options = GameArchitecture.Interface.GetSystem<IAbilityPoolSystem>().RollOptions(3);
							// 重新开本面板（单例），触发 OnOpen → 清空容器 + 重建按钮。
							// 调用有 4 个同名重载，必须用位置参数 + 全部填齐才能消歧；走
							// OpenPanel<T>(PanelOpenType, UILevel, IUIData, string, string) 全参版本。
							UIKit.OpenPanel<GameLevelUpPanel>(
								PanelOpenType.Single,
								UILevel.Common,
								new GameLevelUpPanelData { Options = options },
								null,
								null);
						}
						else
						{
							Time.timeScale = 1f; // 恢复游戏
							UIKit.ClosePanel<GameLevelUpPanel>();
						}
					});
				});
			}
			});
			mResLoader.LoadAsync();
		}

		protected override void OnShow()
		{
			// 弹窗打开动画（OnShow 时物体已 SetActive(true)，可安全启动 tween；
			// OnOpen 运行在 SetActive 之前，不能在那里播动画）
			this.PlayOpen();
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
