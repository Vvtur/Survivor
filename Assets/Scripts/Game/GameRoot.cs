using System.Collections;
using QFramework;
using QFramework.UI;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 游戏启动入口（挂在场景常驻物体上）。
    /// WebGL 平台必须异步初始化 ResKit，因此用协程驱动 InitAsync。
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(InitAsync());
        }

        private IEnumerator InitAsync()
        {
            // WebGL 平台只支持异步初始化
            yield return ResKit.InitAsync();

            // 初始化架构（触发各 System/Model 的 OnInit）
            _ = GameArchitecture.Interface;

            // 打开开始面板（资源加载模块已就绪）
            UIKit.OpenPanel<GameStartPanel>();
        }
    }
}
