using UnityEngine;
using UnityEngine.SceneManagement;

namespace NotABear
{

	public enum MenuType
	{
		Solo,
		Overlay,
		FullscreenOverlay,
		Back,
		Close
	}

	[CreateAssetMenu(fileName="Menu.asset", menuName="Cloverview/System/Menu")]
	public class MenuData : ScriptableObject
	{
		public SceneField scene;
		public MenuType type;
		public bool allowPreload = true;
	}

}