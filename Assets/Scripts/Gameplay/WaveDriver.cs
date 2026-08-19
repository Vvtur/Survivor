using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 波次驱动（挂在 MainGame 场景的空物体上）。
    /// 场景加载时开始驱动 WaveSystem，场景卸载时自动停止——无需全局宿主和场景监听。
    /// </summary>
    public class WaveDriver : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    private IWaveSystem mWaveSystem;

    private void Awake()
    {
        mWaveSystem = this.GetSystem<IWaveSystem>();  // 只查一次，缓存
    }

    private void Update()
    {
        mWaveSystem.OnUpdate();  // 每帧直接调用，零查找
    }
}
}
