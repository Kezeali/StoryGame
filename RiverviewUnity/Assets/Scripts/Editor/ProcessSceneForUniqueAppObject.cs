using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Cloverview
{

class ProcessSceneForUniqueAppObject : IProcessSceneWithReport
{
	public int callbackOrder { get { return 0; } }

	public void OnProcessScene(Scene scene, BuildReport report)
	{
		if (!Application.isPlaying)
		{
			Debug.Log("ProcessSceneForUniqueAppObject.OnProcessScene " + scene.path);

			if (scene.buildIndex != 0)
			{
				GameObject[] roots = scene.GetRootGameObjects();
				for (int rootObjectIndex = 0; rootObjectIndex < roots.Length; ++rootObjectIndex)
				{
					App app = roots[rootObjectIndex].GetComponent<App>();
					if (app != null)
					{
						Object.DestroyImmediate(roots[rootObjectIndex]);
					}
				}
			}
			else
			{
				Debug.Log("ProcessSceneForUniqueAppObject: Startup scene, skipping.");
			}
		}
	}
}

}