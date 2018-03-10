using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using System.Collections.Generic;

namespace Cloverview
{

class ProcessSceneToAllowPreload : IProcessScene
{
	public int callbackOrder { get { return 1; } }

	public void OnProcessScene(Scene scene)
	{
		Debug.Log("ProcessSceneToAllowPreload.OnProcessScene " + scene.path);

		GameObject[] roots = scene.GetRootGameObjects();
		if (scene.buildIndex != 0)
		{
			for (int rootObjectIndex = 0; rootObjectIndex < roots.Length; ++rootObjectIndex)
			{
				roots[rootObjectIndex].SetActive(false);
			}
		}
		for (int rootObjectIndex = 0; rootObjectIndex < roots.Length; ++rootObjectIndex)
		{
			INavigator[] navigationComponents = roots[rootObjectIndex].GetComponentsInChildren<INavigator>();
			for (int comIndex = 0; comIndex < navigationComponents.Length; ++comIndex)
			{
				navigationComponents[comIndex].SetParentScene(scene.path);
			}
		}
	}
}

}