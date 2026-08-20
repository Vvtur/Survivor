// Generate Id:0cd535fb-6c3b-4015-b9dc-db8456948b21
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class PlayerController : QFramework.IController
	{
		public UnityEngine.BoxCollider2D SelfBoxCollider2D;
		
		public UnityEngine.Rigidbody2D SelfRigidbody2D;
		
		public UnityEngine.Animator SelfAnimator;
		
		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>QFramework.Gameplay.GameArchitecture.Interface;
	}
}
