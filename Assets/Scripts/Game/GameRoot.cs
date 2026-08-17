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
        public static GameRoot Instance { get; private set; }

        // 常驻场景加载器：与 GameRoot 同生命周期，切场景不回收
        ResLoader mSceneLoader;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (Instance != this) return; // 重复实例守卫
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            // WebGL 平台只支持异步初始化
            yield return ResKit.InitAsync();

            // 初始化架构（触发各 System/Model 的 OnInit）
            _ = GameArchitecture.Interface;

            // 从 AB 进入开始场景
            SwitchScene("GameStart");
        }

        /// <summary>
        /// 场景切换统一入口（只传场景名，不带 AB 名）。
        /// 注意：必须用单参重载 LoadSceneAsync(sceneName)——QF 创建场景 res 时不记录 AB 名，
        /// 带参重载在"重复加载同一场景"时查重失败，回调丢失、切换静默失败（框架 bug）；
        /// 单参写法两次查重条件一致，命中缓存后立即同步切场景。AB 名由配置表自动解析。
        /// </summary>
        public static void SwitchScene(string sceneName)
        {
            if (Instance == null)
            {
                Debug.LogError("[GameRoot] 实例不存在，请确认 Boot 场景已挂载 GameRoot");
                return;
            }

            if (Instance.mSceneLoader == null) Instance.mSceneLoader = ResLoader.Allocate();
            Instance.mSceneLoader.LoadSceneAsync(sceneName, onStartLoading: op =>
            {
                // 此回调在 SceneManager.LoadSceneAsync 被调用的那一刻触发，用于确认加载链路接通
                Debug.Log($"[GameRoot] SceneManager.LoadSceneAsync 已发起，op={(op == null ? "null(失败)" : "OK")}");
            });
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        // 每次进入 GameStart 场景都重新打开开始面板（支持反复进出）。
        // WebGL 下 AB 只能异步加载，必须用 OpenPanelAsync（同步 OpenPanel 首次加载 uiprefab 包必失败）
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameRoot] sceneLoaded 事件: {scene.name} ({mode})");

            if (scene.name == "GameStart")
            {
                // 常驻 UIRoot 下的 PlayerInfoPanel 不随 MainGame 卸载，回到开始场景时关掉。
                // 关面板逻辑放这里而不是 PlayerController.OnDestroy：
                // 场景加载回调只在运行中触发（退出 Play 不会有），绝不会在析构期碰 UIKit 惰性单例
                UIKit.ClosePanel<PlayerInfoPanel>();

                StartCoroutine(UIKit.OpenPanelAsync<GameStartPanel>());
            }
        }
    }
}
