using System.Collections;
using QFramework;
using QFramework.UI;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 启动入口（挂在 Boot 场景，Boot 是唯一打进主包的场景）。
    /// 本对象常驻不销毁：GameStart/MainGame 全在 AB 里，由这里统一加载切换；
    /// 场景 loader 与 GameRoot 同生命周期，切场景期间 AB 不会被卸载、异步任务不会被注销。
    /// WebGL 平台必须异步初始化 ResKit，因此用协程驱动 InitAsync。
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        ResLoader mSceneLoader;

        void Start()
        {
            StartCoroutine(BootRoutine());
        }

        /// <summary>
        /// 冷启动:先完成 ResKit/架构初始化,再进统一过场。
        /// 注意顺序——过场 UI 已改为 UIKit 正规面板(LoadingPanel,预制体走 AB),
        /// 面板加载本身依赖 ResKit,所以初始化必须先行(这段期间是 Unity 自身启动画面)。
        /// </summary>
        IEnumerator BootRoutine()
        {
            // WebGL 平台只支持异步初始化
            yield return ResKit.InitAsync();

            // 初始化架构（触发各 System/Model 的 OnInit）
            _ = GameArchitecture.Interface;
            mSceneLoader = ResLoader.Allocate();

            // 统一过场:圆形遮罩扩散切入 → 异步加载开始场景 → 圆收回
            LoadingPanel.SwitchScene("GameStart", () => mSceneLoader);
        }
    }
}
