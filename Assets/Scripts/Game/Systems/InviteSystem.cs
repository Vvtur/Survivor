using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 邀请有礼系统（分享带 inviter / 好友进入上报 / 领奖资格校验）。
    /// 防刷约定：领取资格与领取状态完全以云端记录为准（invite_events / invite_rewards 集合），
    /// 本地标记（InviteSharedClicked / InviteRewardClaimed）仅用于 UI 文案区分与加速显示，不作资格依据。
    /// 云函数：invite（action: get_openid / track / status / claim），见 WX/custom/cloudfunctions/invite。
    /// 仅微信真机（WebGL 构建）真正生效；编辑器下为空操作，不影响本地流程。
    /// </summary>
    public interface IInviteSystem : ISystem
    {
        /// <summary>邀请分享（query 带 inviter=自身 openid），发起分享后回调（微信拿不到分享成功回调，回调仅表示已拉起分享面板）</summary>
        void ShareWithInvite(System.Action onShared);

        /// <summary>查询领取资格（云端为准）：invited=有好友通过链接进入过，claimed=已领过奖</summary>
        void CheckStatus(System.Action<bool, bool> onResult);

        /// <summary>领取奖励：云端校验通过后客户端加金币。code: 0成功 1已领过 2无资格(未分享/无好友游玩) -1网络失败</summary>
        void ClaimReward(System.Action<int> onResult);

        /// <summary>进入游戏时检测启动参数里的 inviter 并上报（静默，失败不打扰玩家）</summary>
        void TrackInviteFromLaunch();
    }

    /// <summary>云函数 invite 的响应（res.result 为 JSON 字符串，JsonUtility 解析）</summary>
    [System.Serializable]
    public class InviteResponse
    {
        public int code;
        public string openid;
        public bool invited;
        public bool claimed;
        public string msg;
    }

    /// <summary>云函数 invite 的调用载荷（注意：字段不能为 null，微信插件 fixCallFunctionData 会崩）</summary>
    [System.Serializable]
    public class InviteCallData
    {
        public string action = "";
        public string inviter = "";
    }

    /// <summary>
    /// invite-query.js 存入本地存储的 query 快照（只取我们关心的字段；JsonUtility 不能解析字典但字符串字段没问题）
    /// </summary>
    [System.Serializable]
    public class InviteQueryData
    {
        public string inviter = "";
        public string from = "";
    }

    /// <summary>
    /// 邀请有礼实现。
    /// 数据流：
    ///   被邀请方进入游戏（冷启动 GetLaunchOptionsSync / 热启动 OnShow）
    ///     → query.inviter → 云函数 track → invite_events doc(被邀请者openid) 幂等写入
    ///   邀请方分享 → 云函数 get_openid（缓存 Storage）→ 分享 query 带 inviter
    ///   邀请方领取 → 云函数 claim（云端校验防自邀/防重复）→ code=0 时 Money += RewardMoney
    /// 注册顺序：在 ICloudSaveSystem 之后（复用其 WX.cloud.Init）。
    /// </summary>
    public class InviteSystem : AbstractSystem, IInviteSystem
    {
        /// <summary>邀请奖励金币数</summary>
        public const int RewardMoney = 100;

        /// <summary>自身 openid 缓存 key（Storage）</summary>
        const string OpenIdKey = "OpenId";

        /// <summary>本地"点击过分享"标记（仅文案提示用，资格以云端为准）</summary>
        const string SharedClickedKey = "InviteSharedClicked";

        /// <summary>invite-query.js 写入的启动 query 快照 key（Storage，与 JS 侧 KEY 约定一致）</summary>
        const string LaunchQueryKey = "invite_launch_query";

        protected override void OnInit()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // cloud.Init 由 CloudSaveSystem.OnInit 完成（注册在前），这里直接可用
            // 热启动：SDK 把 OnShow 的 res 传给 C# 时 Dictionary 类型的 query 会丢失（实测空字典，
            // 插件用 JsonUtility 反序列化不支持字典）→ 改读 invite-query.js（game.js 注入）在 JS 侧
            // 存好的 query 快照。JS 监听注册远早于 C#，读到时快照必已更新。
            WeChatWASM.WX.OnShow(res =>
            {
                string stashInviter = ReadStashedInviter();
                LogKit.I("[邀请] OnShow 触发，SDK query=" + DumpQuery(res?.query) + "，JS stash inviter=" + stashInviter);
                TrackInviter(stashInviter);
            });
            // 冷启动入口由 TrackInviteFromLaunch 提供（面板/场景加载后调用），OnInit 里先取 openid 缓存
            FetchOpenId(null);
#endif
        }

        public void TrackInviteFromLaunch()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var options = WeChatWASM.WX.GetLaunchOptionsSync();
            string nativeInviter = GetInviterFromDict(options?.query);
            string stashInviter = ReadStashedInviter();
            LogKit.I("[邀请] 冷启动检查，SDK query=" + DumpQuery(options?.query) + "，JS stash inviter=" + stashInviter);
            // SDK 原生解析优先（冷启动路径此前验证可用），快照兜底
            TrackInviter(string.IsNullOrEmpty(nativeInviter) ? stashInviter : nativeInviter);
#else
            // 编辑器：无微信运行时，不处理
#endif
        }

        // 上报邀请来源（inviter 为空表示非邀请进入，静默跳过）
        void TrackInviter(string inviter)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (string.IsNullOrEmpty(inviter))
            {
                LogKit.I("[邀请] 无 inviter（非邀请链接进入），跳过 track");
                return;
            }

            WeChatWASM.WX.cloud.CallFunction(new WeChatWASM.CallFunctionParam
            {
                name = "invite",
                data = new InviteCallData { action = "track", inviter = inviter },
                success = res =>
                {
                    var resp = JsonUtility.FromJson<InviteResponse>(res.result);
                    if (resp != null && resp.code == 0)
                        LogKit.I("[邀请] 已记录邀请来源（inviter=" + inviter + "）");
                    else
                        // code 3=自邀/空参数 9=内部错误（msg 可见具体原因，如集合不存在） 4=未知action
                        LogKit.W("[邀请] track 失败 code=" + (resp != null ? resp.code : -1)
                            + " msg=" + (resp != null && resp.msg != null ? resp.msg : "") + " 原始返回=" + res.result);
                },
                fail = err => LogKit.W("[邀请] track 上报失败（静默）：" + err.errMsg)
            });
#endif
        }

        // 读 invite-query.js 存的 query 快照（JsonUtility 解析字符串字段没问题，字典不行）
        string ReadStashedInviter()
        {
            string raw = this.GetUtility<Storage>().GetString(LaunchQueryKey, "{}");
            var q = JsonUtility.FromJson<InviteQueryData>(raw);
            return q != null && q.inviter != null ? q.inviter : "";
        }

        // SDK 原生 query 字典取 inviter（冷启动路径可用；热启动该字典实测为空）
        static string GetInviterFromDict(System.Collections.Generic.Dictionary<string, string> query)
        {
            return query != null
                && query.TryGetValue("inviter", out string v)
                && !string.IsNullOrEmpty(v) ? v : "";
        }

        // query 字典转可读字符串（诊断用）
        static string DumpQuery(System.Collections.Generic.Dictionary<string, string> query)
        {
            return query == null
                ? "null"
                : string.Join(",", System.Linq.Enumerable.Select(query, kv => kv.Key + "=" + kv.Value));
        }

        public void ShareWithInvite(System.Action onShared)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // force=true：每次分享现取 openid，不信本地缓存——
            // 缓存会因换账号/清缓存过期（分享带旧号 openid，云端按新号查 → 永远查不到）
            FetchOpenId(openid =>
            {
                if (!string.IsNullOrEmpty(openid))
                {
                    this.GetSystem<IWXPlatformSystem>().ShareToFriend("from=invite&inviter=" + openid);
                    this.GetUtility<Storage>().SaveInt(SharedClickedKey, 1); // 仅作文案参考
                    LogKit.I("[邀请] 已发起分享，inviter=" + openid);
                }
                else
                {
                    LogKit.W("[邀请] openid 获取失败，未拉起分享");
                }
                onShared?.Invoke();
            }, force: true);
#else
            // 编辑器：走通用分享预览（WXPlatformSystem 内部打印标题），并记本地标记便于测试文案
            this.GetSystem<IWXPlatformSystem>().ShareToFriend("from=invite&inviter=editor");
            this.GetUtility<Storage>().SaveInt(SharedClickedKey, 1);
            onShared?.Invoke();
#endif
        }

        public void CheckStatus(System.Action<bool, bool> onResult)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WeChatWASM.WX.cloud.CallFunction(new WeChatWASM.CallFunctionParam
            {
                name = "invite",
                data = new InviteCallData { action = "status", inviter = "" },
                success = res =>
                {
                    var resp = JsonUtility.FromJson<InviteResponse>(res.result);
                    if (resp != null && resp.code == 0)
                    {
                        LogKit.I($"[邀请] status: invited={resp.invited} claimed={resp.claimed}");
                        onResult?.Invoke(resp.invited, resp.claimed);
                    }
                    else
                    {
                        LogKit.W("[邀请] status 查询异常: " + res.result);
                        onResult?.Invoke(false, false);
                    }
                },
                fail = _ =>
                {
                    LogKit.W("[邀请] status 调用失败（检查 invite 云函数是否已部署）");
                    onResult?.Invoke(false, false);
                }
            });
#else
            // 编辑器：无云端，直接回调未完成状态（面板显示默认文案）
            onResult?.Invoke(false, false);
#endif
        }

        public void ClaimReward(System.Action<int> onResult)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WeChatWASM.WX.cloud.CallFunction(new WeChatWASM.CallFunctionParam
            {
                name = "invite",
                data = new InviteCallData { action = "claim", inviter = "" },
                success = res =>
                {
                    var resp = JsonUtility.FromJson<InviteResponse>(res.result);
                    int c = resp != null ? resp.code : -1;
                    LogKit.I("[邀请] claim 结果 code=" + c + " " + res.result);
                    if (c == 0)
                    {
                        // 云端校验通过才加钱：Money 的 Register 自动落本地，CloudSaveSystem 防抖推云
                        this.GetModel<GameModel>().Money.Value += RewardMoney;
                        this.GetUtility<Storage>().SaveInt("InviteRewardClaimed", 1);
                        LogKit.I($"[邀请] 领取成功，金币 +{RewardMoney}");
                    }
                    onResult?.Invoke(c);
                },
                fail = _ =>
                {
                    LogKit.W("[邀请] claim 调用失败（检查 invite 云函数是否已部署）");
                    onResult?.Invoke(-1);
                }
            });
#else
            // 编辑器：无云端，回调无资格
            onResult?.Invoke(2);
#endif
        }

        // 取自身 openid：force=true 跳过缓存直接调云函数（防换账号后缓存过期）
        void FetchOpenId(System.Action<string> onDone, bool force = false)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var storage = this.GetUtility<Storage>();
            if (!force)
            {
                string cached = storage.GetString(OpenIdKey, "");
                if (!string.IsNullOrEmpty(cached))
                {
                    onDone?.Invoke(cached);
                    return;
                }
            }

            WeChatWASM.WX.cloud.CallFunction(new WeChatWASM.CallFunctionParam
            {
                name = "invite",
                data = new InviteCallData { action = "get_openid", inviter = "" },
                success = res =>
                {
                    var resp = JsonUtility.FromJson<InviteResponse>(res.result);
                    if (resp != null && resp.code == 0 && !string.IsNullOrEmpty(resp.openid))
                    {
                        storage.SaveString(OpenIdKey, resp.openid);
                        LogKit.I("[邀请] openid=" + resp.openid + (force ? "（已强制刷新）" : " 已缓存"));
                        onDone?.Invoke(resp.openid);
                    }
                    else
                    {
                        LogKit.W("[邀请] 获取 openid 失败，resp=" + res.result);
                        onDone?.Invoke(null);
                    }
                },
                fail = err =>
                {
                    LogKit.W("[邀请] 获取 openid 失败（检查 invite 云函数是否已部署）：" + err.errMsg);
                    onDone?.Invoke(null);
                }
            });
#else
            onDone?.Invoke(null);
#endif
        }
    }
}
