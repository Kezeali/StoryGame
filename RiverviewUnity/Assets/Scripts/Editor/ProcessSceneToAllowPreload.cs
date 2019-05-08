using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Collections.Generic;

namespace Cloverview
{

// Runs when building scenes or running in editor (just after the scene finishes loading) to make sure there are no active game objects in scenes that are being pre-loaded in the background.
class ProcessSceneToAllowPreload : IProcessSceneWithReport
{
	public int callbackOrder { get { return 1; } }

	public void OnProcessScene(Scene scene, BuildReport report)
	{
		Debug.Log("ProcessSceneToAllowPreload.OnProcessScene " + scene.path);

		InitialisedScenes.scenes.Add(scene);

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