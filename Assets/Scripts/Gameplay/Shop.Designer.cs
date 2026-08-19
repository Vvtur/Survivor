// Generate Id:908072c4-43a6-4bf1-8004-c439eca07fe4
using UnityEngine;

namespace QFramework.Gameplay
{
	public partial class Shop : QFramework.IController
	{
		public UnityEngine.UI.Button SelfButton;
		
		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>QFramework.Gameplay.GameArchitecture.Interface;
	}
}
