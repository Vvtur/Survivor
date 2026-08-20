using QFramework;

namespace QFramework.Gameplay
{
    /// <summary>
    /// 数据层：持有游戏全局数据。
    /// 初始值从 GameConfig 配置资产读取（Inspector 可编辑），读完即回收配置；
    /// 运行时数据用 BindableProperty 承载，运行中不再依赖配置资产。
    /// 成长属性每局重置，金币跨局保留。
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
        public BindableProperty<int> Money { get; } = new(0);   // 金币（跨局保留）
        public BindableProperty<float> Attack { get; } = new(0);
        public BindableProperty<int> WeaponCount { get; } = new(1); // 同时生成的剑数（升级可加，默认 1）   

        // 无限刷怪参数（从配置复制，运行中固定）
        public BindableProperty<float> SpawnInterval { get; } = new(3f);      // 生成间隔（秒）
        public BindableProperty<int> MaxAliveEnemies { get; } = new(10);      // 场上敌人上限
        public BindableProperty<float> EnemyPowerPerSecond { get; } = new(0.02f); // 敌人强度增长系数
        public BindableProperty<float> SurviveTimeToWin { get; } = new(120f); // 存活胜利时间（0=不设胜利）

        // 配置初始值缓存（配置读一次即回收，供每次开局 ResetRunData 使用）
        private int mConfigMaxHp;
        private float mConfigAttackDamage;
        private float mConfigMoveSpeed;
        private float mConfigAttackInterval;
        private float mConfigAttackRadius;
        private int mConfigWeaponCount;

        /// <summary>
        /// 重置局内数据（每局开始时调用）：
        /// 局内成长归零回配置初始值，金币/商店攻击保留。
        /// </summary>
        public void ResetRunData()
        {
            Level.Value = 1;
            Exp.Value = 0;
            MaxHp.Value = mConfigMaxHp;
            HP.Value = mConfigMaxHp;
            AttackDamage.Value = mConfigAttackDamage;
            MoveSpeed.Value = mConfigMoveSpeed;
            AttackInterval.Value = mConfigAttackInterval;
            AttackRadius.Value = mConfigAttackRadius;
            WeaponCount.Value = mConfigWeaponCount;
            AliveEnemies.Value = 0;
        }

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

                // 缓存配置初始值（供每次开局 ResetRunData 使用）
                mConfigMaxHp = config.MaxHp;
                mConfigAttackDamage = config.AttackDamage;
                mConfigMoveSpeed = config.MoveSpeed;
                mConfigAttackInterval = config.AttackInterval;
                mConfigAttackRadius = config.AttackRadius;
                mConfigWeaponCount = config.WeaponCount;

                // 金币：跨局保留的长期货币，从存档读取
                Money.Value = storage.GetInt("Money", 0);
                // 商店升级攻击（跨局保留）：从存档读取，存档优先
                Attack.Value = storage.GetFloat("Attack", config.AttackDamage);
                // 刷怪参数
                SpawnInterval.Value = config.SpawnInterval;
                MaxAliveEnemies.Value = config.MaxAliveEnemies;
                EnemyPowerPerSecond.Value = config.EnemyPowerPerSecond;
                SurviveTimeToWin.Value = config.SurviveTimeToWin;

                // 首次开局也调用一次重置，确保局内数据为初始值
                ResetRunData();

                // 跨局持久化：金币 + 商店攻击（局内 AttackDamage 不存档，每局重置）
                Money.Register(v => storage.SaveInt("Money", v));
                Attack.Register(v => storage.SaveFloat("Attack", v));
            });
            resLoader.LoadAsync();
        }
    }
}
