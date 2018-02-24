using UnityEngine;
using UnityEngine.SceneManagement;

namespace NotABear
{

	public enum MenuType
	{
		Solo,
		Overlay,
		FullscreenOverlay
	}

	[CreateAssetMenu(fileName="Menu.asset", menuName="Riverview/System/Menu")]
	public class MenuData : ScriptableObject
	{
		public SceneField scene;
		public MenuType type;
		public bool allowPreload = true;
	}

}