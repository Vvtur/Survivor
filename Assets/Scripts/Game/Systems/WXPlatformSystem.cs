using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 微信平台能力（分享 / 头像昵称授权 / 好友排行榜上报）。
    /// 依赖倒置：外部通过 IWXPlatformSystem 接口访问，内部统一走 this.GetUtility<Storage>()。
    /// 仅微信真机（WebGL 构建）真正生效；编辑器下为空操作，不影响本地流程。
    /// 注：排行榜成绩与云存档是两套独立存储——排行榜走微信托管（要好友关系链，只有开放数据域能读）。
    /// </summary>
    public interface IWXPlatformSystem : ISystem
    {
        /// <summary>上报本局存活时间到微信好友排行榜（未破纪录自动跳过）</summary>
        void ReportSurviveTime(int seconds);

        /// <summary>主动分享给好友/群（wx.shareAppMessage），标题带最佳存活时间</summary>
        void ShareToFriend();

        /// <summary>带自定义 query 的分享（邀请链接等场景，如 "from=invite&inviter=xxx"）</summary>
        void ShareToFriend(string query);

        /// <summary>本机是否已授权过头像昵称（本地标记，仅作快速判断；真实态以微信侧为准）</summary>
        bool IsUserInfoAuthorized();

        /// <summary>
        /// 在指定屏幕矩形（左上原点、设备像素）上按需弹"头像昵称"授权透明按钮。
        /// 以微信侧 GetSetting 为准（避免本地标记因换号/清缓存过期）：
        ///   已授权 → 刷新本地标记，不弹、不回调；
        ///   未授权 → 弹透明按钮，用户点击（同意/拒绝都销毁）后回调 bool（true=拿到真实头像昵称）。
        /// 用于把授权前置到游戏入口（开始界面）与排行榜打开兜底。
        /// </summary>
        void PromptAuthIfNeeded(int x, int y, int w, int h, System.Action<bool> onTap);
    }

    /// <summary>
    /// 微信平台能力实现。
    /// 说明：
    ///   1. 排行榜成绩 key（RankKey）与开放数据域渲染脚本（WX/minigame/open-data/index.js）约定一致；
    ///   2. 分享拿不到"成功/失败"回调（微信设计如此），转发面板由微信托管；
    ///   3. 头像昵称授权前置到游戏入口（开始界面）：真实授权态以微信 GetSetting 为准，
    ///      本地标记仅作加速判断，避免换账号/清缓存后"标记=1 但实际未授权"导致永不弹窗。
    /// </summary>
    public class WXPlatformSystem : AbstractSystem, IWXPlatformSystem
    {
        /// <summary>排行榜成绩 key（与开放数据域 getFriendCloudStorage 的 keyList 对应）</summary>
        const string RankKey = "bestTime";

        /// <summary>本机"已授权头像昵称"标记</summary>
        const string AuthFlagKey = "UserInfoAuthorized";

        protected override void OnInit()
        {
        }

        public void ReportSurviveTime(int seconds)
        {
            if (seconds <= 0) return;

            var storage = this.GetUtility<Storage>();
            int best = storage.GetInt("BestSurviveTime", 0);
            if (seconds <= best) return; // 没破纪录不上报
            storage.SaveInt("BestSurviveTime", seconds);

#if UNITY_WEBGL && !UNITY_EDITOR
            // 托管数据限制：value 只能是字符串，key+value ≤ 1KB（存秒数绰绰有余）
            WeChatWASM.WX.SetUserCloudStorage(new WeChatWASM.SetUserCloudStorageOption
            {
                KVDataList = new[] { new WeChatWASM.KVData { key = RankKey, value = seconds.ToString() } },
                success = _ => LogKit.I($"[排行榜] 新纪录已托管: {seconds}s"),
                fail = err => LogKit.W("[排行榜] 托管失败: " + err.errMsg)
            });
#endif
            // 编辑器：无微信运行时，只更新本地最佳纪录
        }

        public void ShareToFriend()
        {
            ShareToFriend("from=leaderboard");
        }

        public void ShareToFriend(string query)
        {
            var storage = this.GetUtility<Storage>();
            int best = storage.GetInt("BestSurviveTime", 0);
            string title = best > 0
                ? $"我在《Survivor》坚持了 {FormatTime(best)}，敢来挑战吗？"
                : "来玩《Survivor》看看你能坚持多久！";

#if UNITY_WEBGL && !UNITY_EDITOR
            WeChatWASM.WX.ShareAppMessage(new WeChatWASM.ShareAppMessageOption
            {
                title = title,
                query = query,
                // imageUrl = "share.png", // 可选：小游戏包内相对路径（5:4 图），不填默认用当前画面截图
            });
#else
            // 编辑器：无微信运行时，仅打印标题便于确认文案
            LogKit.I($"[分享] 编辑器环境，仅预览标题: {title} (query: {query})");
#endif
        }

        public bool IsUserInfoAuthorized()
        {
            return this.GetUtility<Storage>().GetInt(AuthFlagKey, 0) == 1;
        }

        public void PromptAuthIfNeeded(int x, int y, int w, int h, System.Action<bool> onTap)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // 真实授权态以微信侧为准：本地标记可能因换账号/清缓存过期（标记=1 但微信侧没授权）
            WeChatWASM.WX.GetSetting(new WeChatWASM.GetSettingOption
            {
                success = res =>
                {
                    // authSetting 是字典：scope.userInfo=true 表示已授权
                    // （兼容 Dictionary<string,bool>/<string,object>，用 TryGetValue + is bool 判定）
                    bool granted = res.authSetting != null
                        && res.authSetting.TryGetValue("scope.userInfo", out var v)
                        && v is bool b && b;
                    if (granted)
                    {
                        this.GetUtility<Storage>().SaveInt(AuthFlagKey, 1);
                        LogKit.I("[授权] 微信侧已授权（scope.userInfo）");
                        // 已授权：不弹按钮、不回调（调用方按正常流程走）
                    }
                    else
                    {
                        ShowAuthButton(x, y, w, h, onTap);
                    }
                },
                fail = _ => ShowAuthButton(x, y, w, h, onTap), // 查询失败也弹按钮，不阻塞
            });
#else
            // 编辑器：无微信运行时，不弹、不回调（面板流程照常走）
#endif
        }

        void ShowAuthButton(int x, int y, int w, int h, System.Action<bool> onTap)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var btn = WeChatWASM.WX.CreateUserInfoButton(x, y, w, h, "zh_CN", false);
            btn.OnTap((res) =>
            {
                btn.Destroy(); // 无论同意/拒绝都销毁，恢复 Unity UI 交互
                // 注意：WXUserInfo 是 struct（值类型），不能判空；拒绝授权时 SDK 返回空结构，nickName 为空串
                bool ok = res != null && !string.IsNullOrEmpty(res.userInfo.nickName);
                if (ok)
                {
                    this.GetUtility<Storage>().SaveInt(AuthFlagKey, 1);
                    LogKit.I("[授权] 已获取头像昵称: " + res.userInfo.nickName);
                }
                else
                {
                    LogKit.W("[授权] 用户未授权头像昵称，排行榜保持脱敏显示");
                }
                onTap?.Invoke(ok);
            });
#else
            onTap?.Invoke(true);
#endif
        }

        // 秒 → "分:秒"（如 312 → 5:12），与开放数据域排行榜的显示格式保持一致
        static string FormatTime(int sec)
        {
            int m = sec / 60;
            int s = sec % 60;
            return m + ":" + (s < 10 ? "0" : "") + s;
        }
    }
}
