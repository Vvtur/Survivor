using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 数据层：持有游戏全局数据。
    /// 初始值从 GameConfig 配置资产读取（Inspector 可编辑），读完即回收配置；
    /// 运行时数据用 BindableProperty 承载，运行中不再依赖配置资产。
    /// </summary>
    public class GameModel : AbstractModel
    {
        /// <summary>场上剩余敌人数</summary>
        public BindableProperty<int> AliveEnemies { get; } = new BindableProperty<int>();
        public BindableProperty<int> PlayerGem { get; } = new BindableProperty<int>();
        // 玩家成长属性（供能力系统修改）
        public BindableProperty<int> Level { get; } = new(1);
        public BindableProperty<int> Exp { get; } = new(0);
        public BindableProperty<float> AttackDamage { get; } = new(1f);
        public BindableProperty<float> MoveSpeed { get; } = new(5f);
        public BindableProperty<int> MaxHp { get; } = new(3);
        public BindableProperty<int> HP { get; } = new(3);   // 当前血量，初始等于 MaxHp
        public BindableProperty<float> AttackInterval { get; } = new(1f); // 玩家攻击间隔（秒），能力可缩短
        public BindableProperty<float> AttackRadius { get; } = new(5f);   // 攻击范围半径，能力可扩大
        public BindableProperty<int> Money { get; } = new(0);

        // 波次数据
        public BindableProperty<int> CurrentWave { get; } = new(1);          // 当前波次
        public BindableProperty<int> TotalWaves { get; } = new(3);           // 总波次
        public BindableProperty<int> WaveEnemiesLeft { get; } = new(0);      // 本波剩余待生成敌人数
        // 波次参数（从配置复制，运行中固定）
        public BindableProperty<int> FirstWaveEnemies { get; } = new(7);     // 第 1 波敌人数
        public BindableProperty<int> WaveEnemyIncrement { get; } = new(2);   // 每波递增数量
        public BindableProperty<float> SpawnInterval { get; } = new(3f);     // 波内生成间隔（秒）
        public BindableProperty<int> MaxAliveEnemies { get; } = new(10);     // 场上敌人上限（0 表示不限）

        protected override void OnInit()
        {
            var storage = this.GetUtility<Storage>();  // Storage 需注册为 Utility
            // 从配置资产读取初始值（WebGL 平台必须异步加载）
            // GameConfigDate.asset 位于 GameConfig/Data 子文件夹（AB 名 data）
            var resLoader = ResLoader.Allocate();
            resLoader.Add2Load<GameConfig>("data", "GameConfigDate", (b, res) =>
            {
                if (!b)
                {
                    LogKit.E("[GameModel] 加载 GameConfig 失败！请确认 GameConfigDate.asset 已标记 AB（ab 名 data），使用默认值");
                    return;
                }

                var config = res.Asset.As<GameConfig>();
                Money.Value = storage.GetInt("Money", 0);
                MaxHp.Value = config.MaxHp;
                HP.Value = config.MaxHp;
                AttackDamage.Value = storage.GetFloat("AttackDamage", config.AttackDamage);
                MoveSpeed.Value = config.MoveSpeed;
                AttackInterval.Value = config.AttackInterval;
                AttackRadius.Value = config.AttackRadius;
                TotalWaves.Value = config.TotalWaves;
                FirstWaveEnemies.Value = config.FirstWaveEnemies;
                WaveEnemyIncrement.Value = config.WaveEnemyIncrement;
                SpawnInterval.Value = config.SpawnInterval;
                MaxAliveEnemies.Value = config.MaxAliveEnemies;
            });
            resLoader.LoadAsync();
        }
    }
}
