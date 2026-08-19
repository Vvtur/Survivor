using UnityEngine;
using QFramework;
using QFramework.UI;

namespace QFramework.Gameplay
{
	public partial class Shop : ViewController
	{
		void Start()
		{
			SelfButton.onClick.AddListener(() =>
			{
				StartCoroutine(UIKit.OpenPanelAsync<ShopPanel>());
			});
		}
	}
}
