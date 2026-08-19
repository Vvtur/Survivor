using QFramework;
using QFramework.UI;

namespace QFramework.Gameplay
{
    public class GameArchitecture : Architecture<GameArchitecture>
    {
    	protected override void Init()
    	{
    		// 注意：ResKit 初始化由 GameRoot 协程异步完成（WebGL 必须异步）
    		// 面板加载已由 QFramework 的 UIKitWithResKitInit 自动配置为 ResLoader（AB 加载），无需手动设置
    		// 依赖倒置：注册时用接口，外部通过接口访问
    		RegisterSystem<IGameManagerSystem>(new GameManagerSystem());
    		RegisterSystem<ILevelUpSystem>(new LevelUpSystem());
    		RegisterSystem<IAbilityPoolSystem>(new AbilityPoolSystem());
    		RegisterSystem<IGameAssetsSystem>(new GameAssetsSystem());
    		RegisterSystem<IWaveSystem>(new WaveSystem());
			
    		RegisterModel(new GameModel());

			RegisterUtility<Storage>(new Storage());
    	}
    }
}
