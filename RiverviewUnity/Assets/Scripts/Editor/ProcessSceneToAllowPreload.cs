using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using System.Collections.Generic;

namespace NotABear
{

class ProcessSceneToAllowPreload : IProcessScene
{
	public int callbackOrder { get { return 1; } }

	Dictionary<string, MenuData> menudatas = new Dictionary<string, MenuData>();

	bool init = false;

	public void OnProcessScene(Scene scene)
	{
		Debug.Log("ProcessSceneToAllowPreload.OnProcessScene " + scene.path);
		if (!init)
		{
			Debug.Log("ProcessSceneToAllowPreload: init");
			init = true;
			var guids = AssetDatabase.FindAssets("t:MenuData");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var menuData = AssetDatabase.LoadAssetAtPath(path, typeof(MenuData)) as MenuData;
				if (menuData != null && !string.IsNullOrEmpty(menuData.scene.ScenePath))
				{
					menudatas.Add(menuData.scene.ScenePath, menuData);
				}
			}
		}

		GameObject[] roots = scene.GetRootGameObjects();
		if (scene.buildIndex != 0)
		{
			for (int rootObjectIndex = 0; rootObjectIndex < roots.Length; ++rootObjectIndex)
			{
				roots[rootObjectIndex].SetActive(false);
			}
		}
		MenuData currentMenuData;
		menudatas.TryGetValue(scene.path, out currentMenuData);
		for (int rootObjectIndex = 0; rootObjectIndex < roots.Length; ++rootObjectIndex)
		{
			MenuNavigation[] navigationComponents = roots[rootObjectIndex].GetComponentsInChildren<MenuNavigation>();
			for (int comIndex = 0; comIndex < navigationComponents.Length; ++comIndex)
			{
				navigationComponents[comIndex].parentScene = currentMenuData;
			}
		}
	}
}

}