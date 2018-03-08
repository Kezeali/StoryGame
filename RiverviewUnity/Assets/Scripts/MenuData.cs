using UnityEngine;
using UnityEngine.SceneManagement;

namespace NotABear
{

	public enum MenuType
	{
		Start,
		Root,
		Overlay,
		OpaqueOverlay,
		Back,
		ClosePopup
	}

	[CreateAssetMenu(fileName="Menu.asset", menuName="Cloverview/System/Menu Definition")]
	public class MenuData : ScriptableObject
	{
		public SceneField menuScene;
		public SceneData envScene;
		public MenuType type;
		public bool allowPreload;

		public void Reset()
		{
			this.menuScene = new SceneField() { scenePath = "" };

			this.type = MenuType.Overlay;
			this.allowPreload = true;
		}
	}

}