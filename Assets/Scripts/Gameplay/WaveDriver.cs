using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 波次驱动（挂在 MainGame 场景的空物体上）。
    /// 场景加载时开始驱动 WaveSystem，场景卸载时自动停止——无需全局宿主和场景监听。
    ///
    /// QF 规范：System 不能 SendCommand，所以 WaveSystem 遇到需要生成敌人时
    /// 改为发送 SpawnEnemyRequestEvent 事件；本 Controller 接收事件后用
    /// SpawnEnemyCommand 执行真正的状态变更。
    /// </summary>
    public class WaveDriver : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameArchitecture.Interface;

        private IWaveSystem mWaveSystem;

        private void Awake()
        {
            mWaveSystem = this.GetSystem<IWaveSystem>();  // 只查一次，缓存

            // 订阅 WaveSystem 发出的"敌人生成请求"事件，转发为 Command
            // UnRegisterWhenGameObjectDestroyed：场景销毁时自动注销，避免内存泄漏
            this.RegisterEvent<SpawnEnemyRequestEvent>(OnSpawnEnemyRequest)
                .UnRegisterWhenGameObjectDestroyed(this);
        }

        private void OnSpawnEnemyRequest(SpawnEnemyRequestEvent e)
        {
            this.SendCommand(new SpawnEnemyCommand(e.SpawnPosition, e.Power, e.Tier));
        }

        private void Update()
        {
            mWaveSystem.OnUpdate();  // 每帧直接调用，零查找
        }
    }
}
