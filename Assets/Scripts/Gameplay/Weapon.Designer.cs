// Generate Id:17e5d622-93f0-4118-904b-0d973317ef96
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class Weapon : QFramework.IController
	{
		public UnityEngine.BoxCollider2D SelfBoxCollider2D;
		
		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>QFramework.Gameplay.GameArchitecture.Interface;
	}
}
