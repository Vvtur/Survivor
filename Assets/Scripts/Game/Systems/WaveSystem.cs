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

        protected override void OnInit()
        {
            mSpawnTimer = 0f;
            mElapsedTime = 0f;
            mModel = this.GetModel<GameModel>();

            mModel.AliveEnemies.Value = 0;
        }

        public void OnUpdate()
        {
            var spawnInterval = mModel.SpawnInterval.Value;
            var maxAliveEnemies = mModel.MaxAliveEnemies.Value;

            mElapsedTime += Time.deltaTime;
            mSpawnTimer += Time.deltaTime;

            // 存活时间到即胜利（配置为 0 则不设胜利）
            var winTime = mModel.SurviveTimeToWin.Value;
            if (winTime > 0f && mElapsedTime >= winTime)
            {
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
                SpawnEnemy();
            }
        }

        /// <summary>
        /// 生成一个敌人，强度（血量）随游戏时间递增
        /// </summary>
        private void SpawnEnemy()
        {
            // 强度系数：随时间线性增长，系数从配置读取（如 0.02/s → 1 分钟 2.2 倍血量）
            var power = 1f + mElapsedTime * mModel.EnemyPowerPerSecond.Value;
            GameArchitecture.Interface.SendCommand(new SpawnEnemyCommand(GetSpawnPosition(), power));
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
