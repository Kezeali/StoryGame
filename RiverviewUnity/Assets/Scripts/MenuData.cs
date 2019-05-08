using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cloverview
{

	public enum MenuType
	{
		Start,
		Root,
		Sub,
		OpaqueSub,
		Back,
		CloseSub
	}

	public static class MenuTypeUtils
	{
		public static bool IsPreloadable(this MenuType type)
		{
			return type == MenuType.Root || type == MenuType.Sub || type == MenuType.OpaqueSub;
		}
	}

	// Asset type for defining menu UI scenes. In combination, these define transitions work between any given scenes.
	[CreateAssetMenu(fileName="Menu.asset", menuName="Cloverview/System/Menu Definition")]
	public class MenuData : ScriptableObject, IDataItem
	{
		public SceneField menuScene;
		public SceneData envScene;
		public MenuType type;
		public bool allowPreload;
		public MenuData transitionAs;

		// TODO(elliot): search for transition override options for the to/from scenes in the scenecontroller animators before doing a regular out/in transition

		public void Reset()
		{
			this.menuScene = new SceneField() { scenePath = "" };

			this.type = MenuType.Sub;
			this.allowPreload = true;
		}
	}

}