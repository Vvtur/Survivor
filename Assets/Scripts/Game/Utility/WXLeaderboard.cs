using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 微信好友排行榜上报。
    /// 成绩托管到微信后台（wx.setUserCloudStorage），与云存档（云函数+云数据库）是两套独立存储：
    ///   金币/攻击 → 云函数 user_save（要跨设备继承）
    ///   排行榜成绩 → 微信托管（要好友关系链，只有开放数据域能读）
    /// 上报的 key（bestTime）与开放数据域渲染脚本（WX/minigame/open-data/index.js）约定一致。
    /// </summary>
    public static class WXLeaderboard
    {
        /// <summary>排行榜成绩 key（与开放数据域 getFriendCloudStorage 的 keyList 对应）</summary>
        public const string RankKey = "bestTime";

        /// <summary>
        /// 上报本局存活时间（秒）。未破纪录时静默跳过。
        /// 本地 Storage 记录历史最佳，避免把更差的成绩覆盖到排行榜（托管写入是整组覆盖，无 Max 语义）。
        /// </summary>
        public static void ReportSurviveTime(int seconds)
        {
            if (seconds <= 0) return;

            var storage = GameArchitecture.Interface.GetUtility<Storage>();
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
    }
}
