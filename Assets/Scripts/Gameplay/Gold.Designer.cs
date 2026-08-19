// Generate Id:6d85f199-871b-42d2-ab55-234d0a26e5d4
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class Gold : QFramework.IController
	{
		public UnityEngine.CircleCollider2D SelfCircleCollider2D;
		
		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>QFramework.Gameplay.GameArchitecture.Interface;
	}
}
