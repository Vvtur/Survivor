using System.Collections;
using QFramework;
using QFramework.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        void Awake()
        {

        }

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
            // 从 AB 进入开始场景
            mSceneLoader.LoadSceneAsync("GameStart");/* , onStartLoading: (op) =>
            {
                op.completed += (a) =>
                {
                    // mSceneLoader.Recycle2Cache();
                    // mSceneLoader = null;
                };
            }); */
        }


        void OnEnable()
        {
        }

        void OnDestroy()
        {

        }

        // 场景切换统一入口：任何场景进入时先清理上一场景残留的面板（UIRoot 常驻，
        // 面板不随场景卸载），再打开当前场景需要的面板。这样无需在各面板里手动关闭。
        // WebGL 下 AB 只能异步加载，必须用 OpenPanelAsync（同步 OpenPanel 首次加载 uiprefab 包必失败）
        // void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        // {
        //     Debug.Log($"[GameRoot] sceneLoaded 事件: {scene.name} ({mode})");

        //     // 常驻 UIRoot 下的面板不随场景卸载，任何场景进入都先统一清理
        //     // （关面板逻辑放这里而不是面板 OnDestroy：场景加载回调只在运行中触发，
        //     //   绝不会在析构期碰 UIKit 惰性单例）
        //     UIKit.CloseAllPanel();

        //     if (scene.name == "GameStart")
        //     {
        //         UIKit.OpenPanelAsync<GameStartPanel>().ToAction().Start(this);
        //     }
        //     else if (scene.name == "MainGame")
        //     {
        //         // MainGame 的面板（PlayerInfoPanel 等）由 PlayerController.Start 打开
        //         UIKit.ClosePanel<ShopPanel>();
        //     }
        // }
    }
}
