namespace QFramework.Gameplay
{
    /// <summary>
    /// 音效/音乐资源名统一管理。
    /// 资源位于 Assets/Res/Art/Audio（AB 名 audio，子目录 Music/Sounds），
    /// AudioKit 通过 ResKit 按资源名（不带扩展名）加载，WebGL 走异步加载。
    /// 改动资源名时只需维护此处，业务代码全部走常量。
    /// </summary>
    public static class AudioNames
    {
        // ---- BGM（MusicPlayer，同时只播一首，新音乐自动卸载旧音乐） ----
        /// <summary>战斗背景音乐（循环）</summary>
        public const string BgmBattle = "bgm_battle";

        // ---- 音效（SoundPlayer，同一时间可多实例同时播放） ----
        /// <summary>玩家攻击（挥砍）</summary>
        public const string Attack = "attack";
        /// <summary>UI 按钮点击</summary>
        public const string ButtonClick = "button_click";
        /// <summary>敌人死亡</summary>
        public const string EnemyDie = "enemy_die";
        /// <summary>游戏失败</summary>
        public const string GameOver = "game_over";
        /// <summary>开始游戏</summary>
        public const string GameStart = "game_start";
        /// <summary>武器命中敌人</summary>
        public const string Hit = "hit";
        /// <summary>升级</summary>
        public const string LevelUp = "level_up";
        /// <summary>选项切换</summary>
        public const string OptionSwitch = "option_switch";
        /// <summary>面板关闭</summary>
        public const string PanelClose = "panel_close";
        /// <summary>面板打开</summary>
        public const string PanelOpen = "panel_open";
        /// <summary>暂停</summary>
        public const string Pause = "pause";
        /// <summary>拾取金币/宝石</summary>
        public const string Pickup = "pickup";
        /// <summary>玩家受伤</summary>
        public const string PlayerHurt = "player_hurt";
        /// <summary>技能释放</summary>
        public const string Skill = "skill";
        /// <summary>游戏胜利</summary>
        public const string Victory = "victory";
    }
}
