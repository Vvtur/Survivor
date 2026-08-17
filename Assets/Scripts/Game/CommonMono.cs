using System;
using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 全局 Update 宿主：为纯 C# 的 System 提供每帧驱动入口（SnakeGame 同款模式）。
    /// 用 QF SingletonKit 的 MonoSingletonProperty 实现单例，自动创建、常驻。
    /// </summary>
    [MonoSingletonPath("[Game]/CommonMono")]
    public class CommonMono : MonoBehaviour, ISingleton
    {
        /// <summary>单例实例（QF SingletonKit 自动创建 + DontDestroyOnLoad）</summary>
        public static CommonMono Instance => MonoSingletonProperty<CommonMono>.Instance;

        private static Action mUpdateAction;

        void ISingleton.OnSingletonInit()
        {
        }

        public static void AddUpdateAction(Action action)
        {
            _ = Instance; // 确保宿主已创建
            mUpdateAction += action;
        }

        public static void RemoveUpdateAction(Action action)
        {
            mUpdateAction -= action;
        }

        private void Update()
        {
            mUpdateAction?.Invoke();
        }
    }
}
