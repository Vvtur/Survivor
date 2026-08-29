using QFramework;

namespace QFramework.Gameplay
{
    public class GameArchitecture : Architecture<GameArchitecture>
    {
    	protected override void Init()
    	{
    		// 注意：ResKit 初始化由 GameRoot 协程异步完成（WebGL 必须异步）
    		// 面板加载已由 QFramework 的 UIKitWithResKitInit 自动配置为 ResLoader（AB 加载），无需手动设置
    		// 依赖倒置：注册时用接口，外部通过接口访问
    		RegisterSystem<ILevelUpSystem>(new LevelUpSystem());
    		RegisterSystem<IAbilitySystem>(new AbilitySystem());
    		RegisterSystem<IGameAssetsSystem>(new GameAssetsSystem());

    		// 武器能力系统：玩家攻击节奏后统一齐发已解锁的武器能力（穿透剑等），
    		// 生成投射物时惰性解析 GameAssetsSystem/AbilitySystem，注册顺序无硬性要求
    		RegisterSystem<IWeaponAbilitySystem>(new WeaponAbilitySystem());
    		RegisterSystem<IWaveSystem>(new WaveSystem());

    		// 掉落系统：监听 EnemyKilledEvent 按权重掷点，依赖 GameAssetsSystem 生成掉落物（惰性解析）
    		RegisterSystem<IDropSystem>(new DropSystem());
			
    		RegisterModel(new GameModel());

			RegisterUtility<Storage>(new Storage());

    		// 云存档系统放最后注册：OnInit 会同步 GetModel/GetUtility，
			// 必须等 GameModel 和 Storage 都已注册（QF 注册即触发 OnInit）
			RegisterSystem<ICloudSaveSystem>(new CloudSaveSystem());

			// 微信平台能力（分享/头像昵称授权/排行榜上报）：OnInit 为空，方法调用时才取 Utility，顺序无要求
			RegisterSystem<IWXPlatformSystem>(new WXPlatformSystem());

			// 邀请有礼（分享带 inviter / 好友进入上报 / 领奖校验）：
			// OnInit 注册 OnShow 并取 openid，须在 CloudSaveSystem（cloud.Init）之后
			RegisterSystem<IInviteSystem>(new InviteSystem());
    	}
    }
}
