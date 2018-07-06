#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections.Generic;

namespace Cloverview
{

// As of Unity 2018.1.4, WHEN PLAYING IN EDITOR ProcessSceneToAllowPreload executes late--after OnEnabled has been called on all the objects in the scene. To enable work-arounds for issues that this can cause, the code here is used to indicate which scenes have been processed / don't need to be processed.
[InitializeOnLoad]
public class InitialisedScenes
{
	public static HashSet<Scene> scenes = new HashSet<Scene>();
	static InitialisedScenes()
	{
		scenes.Clear();
	}

	public static bool IsInitialised(Scene scene)
	{
		// buildIndex 0 & buildIndex -1 are allowed because boot scene & scenes generated at runtime (such as DontDestroyOnLoad) are always "initialised" -- they aren't processed by the scene processor.
		return scene.buildIndex == 0 || (scene.buildIndex == -1 && scene.isLoaded) || scenes.Contains(scene);
	}
}

}
#endif
