using System.Collections;
using QFramework;
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
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            // WebGL 平台只支持异步初始化
            yield return ResKit.InitAsync();

            // 初始化架构（触发各 System/Model 的 OnInit）
            _ = GameArchitecture.Interface;
            mSceneLoader = ResLoader.Allocate();
            // 从 AB 进入开始场景（loader 与本对象同生命周期，切场景期间 AB 不卸载）
            mSceneLoader.LoadSceneAsync("GameStart");
        }
    }
}
