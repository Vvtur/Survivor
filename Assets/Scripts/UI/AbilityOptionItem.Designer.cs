// Generate Id:c3ec17f6-4c44-405e-98a1-d2ced5326f2f
using UnityEngine;

namespace QFramework.UI
{
	public partial class AbilityOptionItem : QFramework.IController
	{
		public TMPro.TextMeshProUGUI Txt_Des;
		
		public UnityEngine.UI.Image Img_Icon;
		
		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>QFramework.Gameplay.GameArchitecture.Interface;
	}
}
