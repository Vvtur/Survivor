using UnityEngine;
using QFramework;

public class Storage : IUtility
{
    public void SaveInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

    public void SaveFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
    public float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);
}
