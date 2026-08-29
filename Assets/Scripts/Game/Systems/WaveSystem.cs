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
    /// 敌人强度（血量）随游戏时间递增——越到后面越强。
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
        /// 生成一个敌人，强度（血量）随游戏时间递增。
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
            });
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
