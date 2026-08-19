using QFramework;
using UnityEngine;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 波次系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IWaveSystem : ISystem
    {
        /// <summary>每帧驱动（由 MainGame 场景的 WaveDriver MonoBehaviour 调用）</summary>
        void OnUpdate();
    }

    /// <summary>
    /// 系统层：波次生成系统。
    /// 由 MainGame 场景中的 WaveDriver 每帧驱动，管理波次推进、波内生成节奏；
    /// 所有波次打完后发胜利事件。
    /// </summary>
    public class WaveSystem : AbstractSystem, IWaveSystem
    {
        private float mSpawnTimer;
        GameModel model;

        protected override void OnInit()
        {
            mSpawnTimer = 0f;
            model = this.GetModel<GameModel>();

            // 初始化第一波（数量从配置读取）
            
            model.CurrentWave.Value = 1;
            model.WaveEnemiesLeft.Value = GetWaveEnemyCount(1);
            model.AliveEnemies.Value = 0;
        }

        public void OnUpdate()
        {
            var spawnInterval = model.SpawnInterval.Value;
            var maxAliveEnemies = model.MaxAliveEnemies.Value;

            // 所有波次已打完：不再生成，检查胜利
            if (model.CurrentWave.Value > model.TotalWaves.Value)
            {
                TryCheckWin();
                return;
            }

            // 本波敌人已全部生成：等待场上清空后再推进下一波
            if (model.WaveEnemiesLeft.Value <= 0)
            {
                TryAdvanceWave();
                return;
            }

            // 场上达到上限则暂停生成
            if (maxAliveEnemies > 0 && model.AliveEnemies.Value >= maxAliveEnemies)
            {
                return;
            }

            mSpawnTimer += Time.deltaTime;
            if (mSpawnTimer >= spawnInterval)
            {
                mSpawnTimer = 0f;
                model.WaveEnemiesLeft.Value--;
                SpawnEnemy();
            }
        }

        /// <summary>
        /// 生成一个敌人到屏幕外随机位置（System 不能直接发 Command，走架构入口）
        /// </summary>
        private void SpawnEnemy()
        {
            GameArchitecture.Interface.SendCommand(new SpawnEnemyCommand(GetSpawnPosition()));
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

        /// <summary>
        /// 尝试推进到下一波：本波生成完毕且场上清空时进入下一波
        /// </summary>
        private void TryAdvanceWave()
        {
            // 本波还有敌人没生成完，或场上还有敌人存活 → 不推进
            if (model.WaveEnemiesLeft.Value > 0) return;
            if (model.AliveEnemies.Value > 0) return;

            // 进入下一波
            model.CurrentWave.Value++;

            // 还有下一波则初始化本波待生成数
            if (model.CurrentWave.Value <= model.TotalWaves.Value)
            {
                model.WaveEnemiesLeft.Value = GetWaveEnemyCount(model.CurrentWave.Value);
            }
            else
            {
                // 所有波次打完
                TryCheckWin();
            }
        }

        /// <summary>
        /// 胜利判定：所有波次打完且场上没有敌人
        /// </summary>
        private void TryCheckWin()
        {
            var model = this.GetModel<GameModel>();
            if (model.CurrentWave.Value > model.TotalWaves.Value && model.AliveEnemies.Value <= 0)
            {
                this.SendEvent(new GameWinEvent());
            }
        }

        /// <summary>
        /// 每波敌人数（从 Model 读取，按波次递增）
        /// </summary>
        private int GetWaveEnemyCount(int wave)
        {
            var model = this.GetModel<GameModel>();
            return model.FirstWaveEnemies.Value + (wave - 1) * model.WaveEnemyIncrement.Value;
        }
    }
}
