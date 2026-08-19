using UnityEngine;
using QFramework;

/// <summary>
/// 本地存储：微信小游戏（WebGL 构建）用微信 SDK 同步存储，编辑器/其他平台用 PlayerPrefs。
/// API 对应 wx.setStorageSync：单用户+单游戏 10MB 上限，按用户隔离。
/// 底层实现为"同步写内存缓存 + 异步队列刷盘"——结算/升级等低频节点写入没问题，勿高频调用。
/// </summary>
public class Storage : IUtility
{
    public void SaveInt(string key, int value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WeChatWASM.WX.StorageSetIntSync(key, value);
#else
        PlayerPrefs.SetInt(key, value);
#endif
    }

    public int GetInt(string key, int defaultValue = 0)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // SDK 的 Get 自带 defaultValue：key 不存在/异常时返回默认值（WXBase.cs 官方注释），
        // 无需先 HasKeySync 探测（还能省一次 JS interop 的 marshalling 开销）
        return WeChatWASM.WX.StorageGetIntSync(key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue);
#endif
    }

    public void SaveFloat(string key, float value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WeChatWASM.WX.StorageSetFloatSync(key, value);
#else
        PlayerPrefs.SetFloat(key, value);
#endif
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return WeChatWASM.WX.StorageGetFloatSync(key, defaultValue);
#else
        return PlayerPrefs.GetFloat(key, defaultValue);
#endif
    }
}
