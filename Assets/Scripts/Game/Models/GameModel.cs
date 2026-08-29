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
        /// <summary>本局击杀数（主游戏 HUD 显示用，每局重置）</summary>
        public BindableProperty<int> KillCount { get; } = new(0);
        // 玩家成长属性（供能力系统修改）
        public BindableProperty<int> Level { get; } = new(1);
        public BindableProperty<int> Exp { get; } = new(0);
        /// <summary>
        /// 待选升级次数：磁铁一次吸多颗宝石可能连升多级，LevelUpSystem 在 while 循环里
        /// 每升一级 +1，表现层在 GameLevelUpPanel 关闭时 -1，直到 0 才真正恢复 timeScale。
        /// 0 = 没有待选升级；N = 还有 N 次能力未选。
        /// </summary>
        public BindableProperty<int> PendingLevelUps { get; } = new(0);

        /// <summary>
        /// 升级所需经验（公式唯一出处）。
        /// LevelUpSystem 判断升级与 PlayerInfoPanel 显示经验条共用，
        /// 勿在别处重写 "Level * 2 + 1"。
        /// </summary>
        public int ExpToNextLevel => Level.Value * 2 + 1;
        public BindableProperty<float> AttackDamage { get; } = new(1f);
        public BindableProperty<float> MoveSpeed { get; } = new(5f);
        public BindableProperty<int> MaxHp { get; } = new(3);
        public BindableProperty<int> HP { get; } = new(3);   // 当前血量，初始等于 MaxHp
        public BindableProperty<float> AttackInterval { get; } = new(1f); // 玩家攻击间隔（秒），能力可缩短
        public BindableProperty<float> AttackRadius { get; } = new(5f);   // 攻击范围半径，能力可扩大
        public BindableProperty<int> Money { get; } = new(0);   // 金币（跨局保留）
        public BindableProperty<float> Attack { get; } = new(0);
        public BindableProperty<int> WeaponCount { get; } = new(1); // 同时生成的剑数（升级可加，默认 1）
        /// <summary>穿透剑等级：0 = 未解锁；首次选中 = 解锁（等级 1），再次选中 = 穿透 +1</summary>
        public BindableProperty<int> PierceSwordLevel { get; } = new(0);

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
            PendingLevelUps.Value = 0; // 局内重置：清理可能残留的待选升级计数
            MaxHp.Value = mConfigMaxHp;
            HP.Value = mConfigMaxHp;
            AttackDamage.Value = mConfigAttackDamage;
            MoveSpeed.Value = mConfigMoveSpeed;
            AttackInterval.Value = mConfigAttackInterval;
            AttackRadius.Value = mConfigAttackRadius;
            WeaponCount.Value = mConfigWeaponCount;
            PierceSwordLevel.Value = 0; // 局内解锁的能力每局重置（重回未解锁）
            AliveEnemies.Value = 0;
            KillCount.Value = 0;
        }

        /// <summary>
        /// 应用云端存档（由 CloudSaveSystem 启动拉取后调用）。
        /// Last-Write-Win：云端时间戳较新才覆盖本地长期数据（金币/商店攻击）；
        /// 赋值会自动触发本地持久化回调（Money/Attack 的 Register），本地随之更新。
        /// SaveTs 的语义 = "本地数据已同步到的云端版本"，推送成功时由 CloudSaveSystem 写入。
        /// </summary>
        public void ApplyCloudSave(SaveData cloud, long cloudTsSec)
        {
            var storage = this.GetUtility<Storage>();
            int localTs = storage.GetInt("SaveTs", 0);

            if (cloudTsSec <= localTs)
            {
                LogKit.I($"[云存档] 本地已是最新(SaveTs={localTs} >= 云端{cloudTsSec})，跳过覆盖");
                return;
            }

            Money.Value = cloud.Money;
            Attack.Value = cloud.Attack;
            storage.SaveInt("SaveTs", (int)cloudTsSec);
            LogKit.I($"[云存档] 已应用云端存档: Money={cloud.Money} Attack={cloud.Attack} ts={cloudTsSec}");
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
