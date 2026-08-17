using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 波次系统接口（依赖倒置：外部通过接口访问）
    /// </summary>
    public interface IWaveSystem : ISystem
    {
    }

    /// <summary>
    /// 系统层：波次生成系统。
    /// 通过 CommonMono 全局宿主自驱动，管理波次推进、波内生成节奏；
    /// 所有波次打完后发胜利事件。
    /// 只在 MainGame 场景激活生成（开始界面等其他场景不生成敌人）。
    /// </summary>
    public class WaveSystem : AbstractSystem, IWaveSystem
    {
        private const string MainGameSceneName = "MainGame";

        private float mSpawnTimer;
        private bool mActive; // 是否处于激活场景

        protected override void OnInit()
        {
            mSpawnTimer = 0f;
            mActive = false;

            // 监听场景切换：只在 MainGame 场景注册每帧驱动
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == MainGameSceneName)
            {
                // 进入 MainGame：重置波次并激活生成
                var model = this.GetModel<GameModel>();
                model.CurrentWave.Value = 1;
                model.WaveEnemiesLeft.Value = GetWaveEnemyCount(1);
                model.AliveEnemies.Value = 0;

                if (!mActive)
                {
                    CommonMono.AddUpdateAction(OnUpdate);
                    mActive = true;
                }
            }
            else
            {
                // 离开 MainGame：暂停生成
                if (mActive)
                {
                    CommonMono.RemoveUpdateAction(OnUpdate);
                    mActive = false;
                }
            }
        }

        private void OnUpdate()
        {
            var model = this.GetModel<GameModel>();
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
            var model = this.GetModel<GameModel>();

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

        protected override void OnDeinit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (mActive)
            {
                CommonMono.RemoveUpdateAction(OnUpdate);
                mActive = false;
            }
            base.OnDeinit();
        }
    }
}
