using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 伤害飘字池（MonoBehaviour 版，挂在 MainGame 场景）。
    /// 生命周期 = MainGame 场景：场景加载即就绪，场景卸载整池销毁——
    /// 天然规避静态池的跨场景对象存活问题。
    /// Update 统一驱动所有飘字（无协程、零 GC），unscaledDeltaTime 保证暂停时也能飘完。
    /// </summary>
    public class DamageTextPool : MonoBehaviour
    {
        public static DamageTextPool Instance { get; private set; }

        private const float Duration = 0.5f;
        private const float FloatHeight = 1f;
        private const int PreWarm = 8;

        // 飘字实例缓存（等价 SimpleObjectPool 的手写版：栈 + OnDisable 复用）
        private readonly Stack<Transform> mIdle = new Stack<Transform>(PreWarm);
        private readonly List<ActiveText> mActive = new List<ActiveText>(16);

        // 用 class（引用类型）：Update 里直接改字段生效（struct 会拷贝导致 Elapsed 不增长）
        private class ActiveText
        {
            public Transform Root;
            public TextMeshPro Tmp;
            public Vector3 StartPos;
            public float Elapsed;
        }

        private ResLoader mResLoader;
        private GameObject mPrefab;

        void Awake()
        {
            Instance = this;

            // WebGL 必须异步加载（与 GameAssetsSystem 同范式）
            mResLoader = ResLoader.Allocate();
            mResLoader.Add2Load<GameObject>("Txt_Damage", (b, res) =>
            {
                if (b) mPrefab = res.Asset.As<GameObject>();
                else LogKit.E("[DamageTextPool] 加载 DamageText 预制体失败！请确认已标记 AB");
            });
            mResLoader.LoadAsync(() =>
            {
                for (int i = 0; i < PreWarm; i++) mIdle.Push(CreateOne());
            });
        }

        void OnDestroy()
        {
            // 场景卸载：只回收 ResLoader（引用计数正确归位），实例由场景销毁
            mResLoader?.Recycle2Cache();
            mResLoader = null;
            mPrefab = null;
            Instance = null;
        }

        private Transform CreateOne()
        {
            if (mPrefab == null) return null;
            var go = Instantiate(mPrefab, transform);  // 挂在池物体下，位置跟随
            go.SetActive(false);
            return go.transform;
        }

        /// <summary>在指定位置弹出伤害数字（Enemy 调用方式不变）</summary>
        public void Show(Vector3 worldPos, float damage, Color? color = null)
        {
            if (mPrefab == null) return; // 预制体还没加载好（战斗开始头几百毫秒）

            var root = mIdle.Count > 0 ? mIdle.Pop() : CreateOne();
            if (root == null) return;

            root.gameObject.SetActive(true);
            root.position = worldPos;
            var tmp = root.GetComponentInChildren<TextMeshPro>();
            tmp.text = Mathf.RoundToInt(damage).ToString();
            tmp.color = color ?? Color.yellow;

            mActive.Add(new ActiveText
            {
                Root = root, Tmp = tmp,
                StartPos = worldPos, Elapsed = 0f
            });
        }

        void Update()
        {
            for (int i = mActive.Count - 1; i >= 0; i--)
            {
                var a = mActive[i];
                a.Elapsed += Time.unscaledDeltaTime; // 不受 timeScale=0 影响，暂停时飘字也能播完
                var p = a.Elapsed / Duration;

                if (p >= 1f || a.Root == null)
                {
                    if (a.Root != null) a.Root.gameObject.SetActive(false);
                    mIdle.Push(a.Root);
                    mActive.RemoveAt(i);
                }
                else
                {
                    a.Root.position = a.StartPos + Vector3.up * (p * FloatHeight);
                    var c = a.Tmp.color;
                    c.a = 1f - p;
                    a.Tmp.color = c;
                }
            }
        }
    }
}