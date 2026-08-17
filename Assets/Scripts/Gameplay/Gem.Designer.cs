// Generate Id:d4678e89-53fd-4261-a604-a0c04cc4ef81
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class Gem : QFramework.IController
	{
		public UnityEngine.CircleCollider2D SelfCircleCollider2D;
		
		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>QFramework.Gameplay.GameArchitecture.Interface;
	}
}
