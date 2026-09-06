using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 无限刷怪系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IWaveSystem : ISystem
    {
        /// <summary>每帧驱动（由 MainGame 场景的 WaveDriver MonoBehaviour 调用）</summary>
        void OnUpdate();

        /// <summary>本局已进行时间（秒），结算上报排行榜用</summary>
        float ElapsedTime { get; }

        /// <summary>重置局内状态（每局开始调用，防止跨局残留）</summary>
        void ResetRun();
    }

    /// <summary>
    /// 系统层：无限刷怪系统。
    /// 由 MainGame 场景中的 WaveDriver 每帧驱动，按固定间隔无限生成敌人；
    /// 敌人强度（血量）随游戏时间递增——越到后面越强；
    /// 怪种按局内时间从分层表解锁（0 秒基础怪 → 后续新怪按权重混刷）。
    /// </summary>
    public class WaveSystem : AbstractSystem, IWaveSystem
    {
        private float mSpawnTimer;
        private float mElapsedTime;   // 本局已进行时间（秒），用于计算敌人强度和胜利判定
        private GameModel mModel;
        private float spawnInterval;
        // 胜利只结算一次：达标后 Update 仍会继续跑（timeScale=0 不影响 Update 调用），
        // 不设标志会每帧重复发 GameWinEvent（表现层每帧重播胜利音效）
        private bool mWon;

        // 分层刷怪：已解锁怪种缓存（每次选怪前重建，避免每只怪都分配新列表）
        private readonly List<EnemyTier> mUnlockedTiers = new List<EnemyTier>(8);
        // 已播报过"新怪解锁"的怪种数（每局从 0 重新解锁、重新播报）
        private int mAnnouncedTierCount;

        protected override void OnInit()
        {
            mModel = this.GetModel<GameModel>();
            ResetRun();
        }

        /// <summary>本局已进行时间（秒）</summary>
        public float ElapsedTime => mElapsedTime;

        /// <summary>
        /// 重置局内状态：计时、刷怪间隔归零。
        /// System 只随架构初始化一次（OnInit 不会在重开局时重跑），
        /// 必须由每局开始的场景侧（PlayerController.Start）显式调用，
        /// 否则上一局的累计时间和衰减过的刷怪间隔会带到下一局（敌人强度沿用、胜利判定提前）。
        /// </summary>
        public void ResetRun()
        {
            mSpawnTimer = 0f;
            mElapsedTime = 0f;
            mWon = false;
            mAnnouncedTierCount = 0; // 分层怪每局从 0 秒重新解锁、重新播报
            mModel.AliveEnemies.Value = 0;
            spawnInterval = mModel.SpawnInterval.Value;
        }

        public void OnUpdate()
        {
            if (mWon) return; // 已胜利：不再计时/刷怪/重复广播

            var maxAliveEnemies = mModel.MaxAliveEnemies.Value;

            mElapsedTime += Time.deltaTime;
            mSpawnTimer += Time.deltaTime;

            // 存活时间到即胜利（配置为 0 则不设胜利）
            var winTime = mModel.SurviveTimeToWin.Value;
            if (winTime > 0f && mElapsedTime >= winTime)
            {
                mWon = true;   // 先置位再广播：回调里即便再次触发 OnUpdate 也不会重复发事件
                this.SendEvent(new GameWinEvent());
                return;
            }

            // 场上达到上限则暂停生成
            if (maxAliveEnemies > 0 && mModel.AliveEnemies.Value >= maxAliveEnemies)
            {
                return;
            }

            if (mSpawnTimer >= spawnInterval)
            {
                mSpawnTimer = 0f;
                if(spawnInterval > .1f && Random.Range(1, 10) >= 8)
                    spawnInterval *= .95f;
                SpawnEnemy();
            }
        }

        /// <summary>
        /// 生成一个敌人：怪种按局内时间解锁（分层表 + 权重随机），
        /// 强度（血量）随游戏时间递增。
        /// QF 规范：System 不能 SendCommand（只有 IController 能），改为发送事件，
        /// 由 IController 层（WaveDriver）接收后用 Command 执行。
        /// </summary>
        private void SpawnEnemy()
        {
            // 强度系数：随时间线性增长，系数从配置读取（如 0.02/s → 1 分钟 2.2 倍血量）
            var power = 1f + mElapsedTime * mModel.EnemyPowerPerSecond.Value;
            this.SendEvent(new SpawnEnemyRequestEvent
            {
                SpawnPosition = GetSpawnPosition(),
                Power = power,
                Tier = PickEnemyTier(),
            });
        }

        /// <summary>
        /// 选怪：把局内时间与分层表比对得到已解锁怪种，按 SpawnWeight 加权随机。
        /// 首次解锁新怪时打一条日志（后续可在此发事件给表现层做横幅/音效播报）。
        /// 分层表为空或没有已解锁项时返回 null = 基础怪。
        /// </summary>
        private EnemyTier PickEnemyTier()
        {
            var tiers = mModel.EnemyTiers;
            if (tiers == null || tiers.Count == 0) return null;

            mUnlockedTiers.Clear();
            var newest = (EnemyTier)null; // 已解锁里 UnlockTime 最大者，用于解锁播报
            for (var i = 0; i < tiers.Count; i++)
            {
                var tier = tiers[i];
                if (tier == null || mElapsedTime < tier.UnlockTime) continue;
                mUnlockedTiers.Add(tier);
                if (newest == null || tier.UnlockTime > newest.UnlockTime) newest = tier;
            }

            if (mUnlockedTiers.Count == 0) return null;

            // 解锁播报：按怪种计数播报一次（每局重置）
            if (mUnlockedTiers.Count > mAnnouncedTierCount)
            {
                mAnnouncedTierCount = mUnlockedTiers.Count;
                LogKit.I($"[WaveSystem] 新怪物解锁：{newest.TierName}（{mElapsedTime:F0} 秒）");
            }

            // 权重随机（权重 ≤ 0 的怪种等于不再刷出）
            float totalWeight = 0f;
            for (var i = 0; i < mUnlockedTiers.Count; i++) totalWeight += mUnlockedTiers[i].SpawnWeight;
            if (totalWeight <= 0f) return mUnlockedTiers[mUnlockedTiers.Count - 1];

            var roll = Random.Range(0f, totalWeight);
            for (var i = 0; i < mUnlockedTiers.Count; i++)
            {
                roll -= mUnlockedTiers[i].SpawnWeight;
                if (roll <= 0f) return mUnlockedTiers[i];
            }

            return mUnlockedTiers[mUnlockedTiers.Count - 1]; // 浮点兜底
        }

        private Vector3 GetSpawnPosition()
        {
            const float margin = 1f; // 屏幕外间距

            var cam = Camera.main;
            if (cam == null) return Vector3.zero;

            var bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
            var topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));

            float minX = bottomLeft.x - margin;
            float maxX = topRight.x + margin;
            float minY = bottomLeft.y - margin;
            float maxY = topRight.y + margin;

            // 随机选一条屏幕外边缘：左/右/上/下
            int edge = Random.Range(0, 4);
            switch (edge)
            {
                case 0: return new Vector3(minX, Random.Range(minY, maxY), 0); // 左
                case 1: return new Vector3(maxX, Random.Range(minY, maxY), 0); // 右
                case 2: return new Vector3(Random.Range(minX, maxX), maxY, 0); // 上
                default: return new Vector3(Random.Range(minX, maxX), minY, 0); // 下
            }
        }
    }
}
