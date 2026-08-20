// Generate Id:94f16b1c-3040-48eb-a644-b4a1eb983fd2
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class BGController : QFramework.IController
	{
		public UnityEngine.GameObject Player;
		
		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>QFramework.Gameplay.GameArchitecture.Interface;
	}
}
