using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>云存档数据（只放跨局保留的长期数据；局内成长不进云档）</summary>
    [System.Serializable]
    public class SaveData
    {
        public int Money;
        public float Attack;
        public long Ts;   // 存档时间戳（秒）
    }

    /// <summary>云函数 load 的响应：{"code":0,"data":{...},"ts":123}</summary>
    [System.Serializable]
    public class CloudSaveResponse
    {
        public int code;
        public SaveData data;
        public long ts;
    }

    /// <summary>云函数调用载荷（callFunction 的 data 会整体成为云函数的 event）</summary>
    [System.Serializable]
    public class SaveCallData
    {
        public SaveData data;
        public long ts;
    }

    public interface ICloudSaveSystem : ISystem
    {
    }

    /// <summary>
    /// 云存档系统（依赖倒置：外部通过接口访问）。
    /// 策略：本地 Storage 永远先写（离线可玩），云端为异步增强——
    ///   启动: load 云端 → 与本地 SaveTs 比较，云端较新才覆盖（Last-Write-Win）
    ///   变化: 监听 Money/Attack → 防抖 2 秒 → save 推云端（失败静默，随下次变化重试）
    /// 注册顺序必须在 GameModel 和 Storage 之后（OnInit 里会同步 GetModel/GetUtility）。
    /// 仅微信真机生效（#if）；编辑器下推送为空操作，不影响本地流程。
    /// </summary>
    public class CloudSaveSystem : AbstractSystem, ICloudSaveSystem
    {
        // 云开发环境 ID（与静态托管/云函数同环境）
        private const string EnvId = "cloud1-d8gevn4facad5c770";

        private bool mApplyingCloud;   // 正在应用云端存档：跳过推送，避免"云→本地→又推云"回环
        private bool mDirty;           // 有未推送的变化
        private float mLastChangeTime; // 最近一次变化时刻（防抖窗口起点）

        protected override void OnInit()
        {
            var model = this.GetModel<GameModel>();

            // 监听长期数据：变化即标记脏（Register 由 GameModel 在配置加载后挂好本地持久化）
            model.Money.Register(_ => MarkDirty());
            model.Attack.Register(_ => MarkDirty());

            // 轮询防抖：变化静默 2 秒后合并成一次推送（捡金币高频场景不逐次上云）
            // QF 的 System 是纯 C# 类没有帧生命周期（只有 OnInit/OnDeinit），
            // 借用 ActionKit 的全局 Update 事件驱动（其内部 MonoSingleton 自动常驻）
            ActionKit.OnUpdate.Register(Poll);

#if UNITY_WEBGL && !UNITY_EDITOR
            // 初始化云能力（微信私有协议，无需登录；openid 由网关自动注入云函数）
            WeChatWASM.WX.cloud.Init(new WeChatWASM.ICloudConfig { env = EnvId, traceUser = false });
            LoadFromCloud();
#endif
        }

        private void MarkDirty()
        {
            if (mApplyingCloud) return;
            mDirty = true;
            mLastChangeTime = Time.time;
        }

        // ActionKit.OnUpdate 每帧回调：静默满 2 秒才推（期间再变化则继续顺延）
        private void Poll()
        {
            if (!mDirty) return;
            if (Time.time - mLastChangeTime < 2f) return;
            mDirty = false;
            PushToCloud();
        }

        private void LoadFromCloud()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WeChatWASM.WX.cloud.CallFunction(new WeChatWASM.CallFunctionParam
            {
                name = "load",
                // 注意：不能传 new SaveCallData()（其 data 字段为 null）。
                // 微信插件 cloud.js 的 fixCallFunctionData 对 null 做 typeof==='object' 误判会递归 Object.keys(null) 崩溃，
                // 因此 data 必须给一个非 null 的空对象（云函数只用 openid，忽略内容）。
                data = new SaveCallData { data = new SaveData(), ts = 0 },
                success = res =>
                {
                    // res.result = 云函数 return 的 JSON 字符串，解析出 {code, data, ts}
                    var resp = JsonUtility.FromJson<CloudSaveResponse>(res.result);
                    if (resp != null && resp.code == 0 && resp.data != null)
                    {
                        mApplyingCloud = true;
                        this.GetModel<GameModel>().ApplyCloudSave(resp.data, resp.ts);
                        mApplyingCloud = false;
                    }
                    else
                    {
                        LogKit.I("[云存档] 云端无存档（新玩家），使用本地数据");
                    }
                },
                fail = err => LogKit.W("[云存档] 拉取失败（离线模式）：" + err.errMsg)
            });
#endif
        }

        private void PushToCloud()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var model = this.GetModel<GameModel>();
            long nowSec = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;

            WeChatWASM.WX.cloud.CallFunction(new WeChatWASM.CallFunctionParam
            {
                name = "save",
                data = new SaveCallData
                {
                    data = new SaveData { Money = model.Money.Value, Attack = model.Attack.Value, Ts = nowSec },
                    ts = nowSec
                },
                success = _ =>
                {
                    // 推送成功才更新本地时间戳：它表示"本地数据已同步到的云端版本"。
                    // 失败不写 → 下次启动云端 ts 与本地 SaveTs 相等 → 本地较新数据不会被云端覆盖
                    this.GetUtility<Storage>().SaveInt("SaveTs", (int)nowSec);
                },
                fail = err => LogKit.W("[云存档] 推送失败（随下次变化重试）：" + err.errMsg)
            });
#endif
            // 编辑器：不推送也不写 SaveTs（避免污染本地版本标记，挡住真机的云端较新数据）
        }

        protected override void OnDeinit()
        {
            base.OnDeinit();
            ActionKit.OnUpdate.UnRegister(Poll);
        }

    }
}
