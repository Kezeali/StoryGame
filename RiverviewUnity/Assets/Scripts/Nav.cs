using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace NotABear
{

public class Nav : MonoBehaviour
{
	[SerializeField]
	[Tooltip("A standin menu to use for whatever scene is started from when playing in editor")]
	private MenuData bootMenuForPlayInEditor;

	private class MenuSceneToLoad
	{
		public MenuData def;
		public string preloadRequesterScenePath;
	}

	private class EnvSceneToLoad
	{
		public SceneData def;
		public string preloadRequesterScenePath;
	}

	private class PreloadedScene
	{
		public string scenePath;
		public List<string> preloadRequesterScenePaths;
		public AsyncOperation loadOp;
		public Scene scene;

		public bool AddRequester(string requesterScenePath)
		{
			if (!string.IsNullOrEmpty(requesterScenePath)
				&& !this.preloadRequesterScenePaths.Contains(requesterScenePath))
			{
				this.preloadRequesterScenePaths.Add(requesterScenePath);
				return true;
			}
			return false;
		}
	}

	private class VisibleMenu
	{
		public MenuData def;
		public PreloadedScene loadedScene;
	}

	private class VisibleEnvScene
	{
		public SceneData def;
		public PreloadedScene loadedScene;
	}

	Queue<MenuSceneToLoad> menuToLoadQueue = new Queue<MenuSceneToLoad>();
	Queue<EnvSceneToLoad> envSceneToLoadQueue = new Queue<EnvSceneToLoad>();
	bool processingQueue = false;

	List<PreloadedScene> preloadedScenes = new List<PreloadedScene>();

	VisibleMenu nextActiveMenu;
	VisibleMenu activeMenu;
	List<VisibleMenu> popupStack = new List<VisibleMenu>();
	Stack<MenuData> breadcrumbs = new Stack<MenuData>();

	List<VisibleEnvScene> visibleEnvScenes = new List<VisibleEnvScene>();

	public void OnEnable()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded += HandleSceneUnloaded;

	#if UNITY_EDITOR
		// Intialise the current scene as a menu
		{
			Scene bootScene = SceneManager.GetActiveScene();
			if (bootScene.buildIndex != 0)
			{
				var bootLoadedScene = new PreloadedScene()
				{
					scenePath = bootScene.path,
					preloadRequesterScenePaths = new List<string>(),
					scene = bootScene
				};
				var bootMenu = new VisibleMenu()
				{
					def = this.bootMenuForPlayInEditor,
					loadedScene = bootLoadedScene
				};
				this.nextActiveMenu = null;
				this.activeMenu = bootMenu;
				this.popupStack.Add(bootMenu);

				SetRootObjectsActive(bootScene, true);
			}
		}
	#endif
	}

	public void OnDestroy()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded -= HandleSceneUnloaded;
	}

	public void GoTo(MenuData def, string requesterScenePath = null)
	{
		this.StartCoroutine(this.GoToCoroutine(def, requesterScenePath));
	}

	public void ClosePopup(MenuData def)
	{
		if (popupStack.Count > 0)
		{
			VisibleMenu topPopup = popupStack[popupStack.Count-1];
			if (topPopup.def == def)
			{
				popupStack.RemoveAt(popupStack.Count-1);
				SetRootObjectsActive(topPopup.loadedScene.scene, false);
				OnSceneHidden(topPopup.loadedScene);
			}
		}
	}

	public void ClearBreadcrumbs()
	{
		this.breadcrumbs.Clear();
	}

	public void Preload(MenuData def, string requesterScenePath = null)
	{
		if (def.type != MenuType.Back && def.type != MenuType.ClosePopup)
		{
			// NOTE: validate the requester: only loaded scenes are allowed to hold references to pre-loaded scene.
			PreloadedScene requester = this.FindLoadedScene(requesterScenePath);
			if (requester == null)
			{
				requesterScenePath = null;
			}

			var item = new MenuSceneToLoad()
			{
				def = def,
				preloadRequesterScenePath = requesterScenePath
			};

			if (def.envScene != null)
			{
				this.Preload(def.envScene, def.menuScene.scenePath);
			}

			this.menuToLoadQueue.Enqueue(item);

			if (!this.processingQueue)
			{
				this.StartCoroutine(this.ProcessQueueCoroutine());
			}
		}
	}

	public void Preload(SceneData def, string requesterScenePath = null)
	{
		// NOTE: validate the requester
		PreloadedScene requester = this.FindLoadedScene(requesterScenePath);
		if (requester == null)
		{
			requesterScenePath = null;
		}

		var item = new EnvSceneToLoad()
		{
			def = def,
			preloadRequesterScenePath = requesterScenePath
		};

		this.envSceneToLoadQueue.Enqueue(item);

		if (!this.processingQueue)
		{
			this.StartCoroutine(this.ProcessQueueCoroutine());
		}
	}

	bool AlreadyLoaded(MenuData def)
	{
		return AlreadyLoaded(def.menuScene);
	}

	bool AlreadyLoaded(string scenePath)
	{
		for (int i = 0; i < this.preloadedScenes.Count; ++i)
		{
			if (this.preloadedScenes[i].scenePath == scenePath)
			{
				return true;
			}
		}
		return false;
	}

	IEnumerator ProcessQueueCoroutine()
	{
		this.processingQueue = true;
		while (this.menuToLoadQueue.Count > 0 || this.envSceneToLoadQueue.Count > 0)
		{
			// Only load menus if there are no more env scenes to load
			if (this.envSceneToLoadQueue.Count > 0)
			{
				EnvSceneToLoad sceneToLoad = this.envSceneToLoadQueue.Dequeue();

				if (sceneToLoad.def != null)
				{
					PreloadedScene preloadedScene =

					this.FindOrLoadEnvScene(sceneToLoad.def, sceneToLoad.preloadRequesterScenePath);

					Debug.LogFormat("Preload requested for {0}. Current load progress: {1}", preloadedScene.scenePath, preloadedScene.loadOp.progress);
				}
			}
			else if (this.menuToLoadQueue.Count > 0)
			{
				MenuSceneToLoad menuToLoad = this.menuToLoadQueue.Dequeue();
				if (menuToLoad.def.type == MenuType.Root || menuToLoad.def.type == MenuType.Overlay || menuToLoad.def.type == MenuType.OpaqueOverlay)
				{
					PreloadedScene preloadedScene =

					this.FindOrLoadMenuScene(menuToLoad.def, menuToLoad.preloadRequesterScenePath);

					Debug.LogFormat("Preload requested for {0}. Current load progress: {1}", preloadedScene.scenePath, preloadedScene.loadOp.progress);
				}
				else
				{
					Debug.LogErrorFormat("Tried to pre-load an menu type that can't be pre-loaded: {0}", menuToLoad.def);
				}
			}

			yield return 0;
		}
		this.processingQueue = false;
	}

	IEnumerator GoToCoroutine(MenuData def, string requesterScenePath)
	{
		while (this.processingQueue)
		{
			yield return 0;
		}

		MenuData resolvedDef = null;
		if (def.type == MenuType.Back)
		{
			VisibleMenu newTopPopup = PopTopPopup();
			if (newTopPopup != null)
			{
				resolvedDef = newTopPopup.def;
			}
			else
			{
				if (this.breadcrumbs.Count > 0)
				{
					resolvedDef = this.breadcrumbs.Pop();
				}
			}
		}
		else if (def.type == MenuType.ClosePopup)
		{
			VisibleMenu newTopPopup = PopTopPopup();
			if (newTopPopup != null)
			{
				resolvedDef = newTopPopup.def;
			}
		}
		else
		{
			resolvedDef = def;
		}

		if (resolvedDef == null)
		{
			yield break;
		}

		// Clear the popups before switching to root menus
		if (resolvedDef.type == MenuType.Start || resolvedDef.type == MenuType.Root)
		{
			while (this.popupStack.Count > 0)
			{
				this.PopTopPopup();
			}
		}

		// Show the environment scene
		if (resolvedDef.envScene != null)
		{
			VisibleEnvScene visibleEnvScene = this.FindOrMakeVisibleEnvScene(resolvedDef.envScene, requesterScenePath);
			if (visibleEnvScene != null)
			{
				PreloadedScene preloadedScene = visibleEnvScene.loadedScene;

				if (preloadedScene.loadOp != null
					|| (preloadedScene.scene.IsValid() && preloadedScene.scene.isLoaded))
				{
					if (preloadedScene.loadOp != null && !preloadedScene.loadOp.isDone)
					{
						preloadedScene.loadOp.allowSceneActivation = true;
						yield return preloadedScene.loadOp;
					}

					Scene scene = SceneManager.GetSceneByPath(preloadedScene.scenePath);
					if (scene.IsValid() && scene.isLoaded)
					{
						this.visibleEnvScenes.Add(visibleEnvScene);

						SetRootObjectsActive(scene, true);
					}
					else
					{
						Debug.LogErrorFormat("Failed to load env scene {0} (menu: {1})", resolvedDef.envScene, resolvedDef.name);
					}
				}
				else
				{
					Debug.LogErrorFormat("Failed to load env scene {0} (menu {1})", resolvedDef.envScene, resolvedDef.name);
				}
			}
			else
			{
				Debug.LogErrorFormat("Failed to make the scene at {0} visible", resolvedDef.envScene);
			}
		}

		// Show the menu scene
		{
			VisibleMenu visibleMenu = this.FindOrMakeVisibleMenu(resolvedDef, requesterScenePath);
			if (visibleMenu != null)
			{
				PreloadedScene preloadedScene = visibleMenu.loadedScene;

				if (preloadedScene.loadOp != null
					|| (preloadedScene.scene.IsValid() && preloadedScene.scene.isLoaded))
				{
					this.nextActiveMenu = visibleMenu;

					if (preloadedScene.loadOp != null && !preloadedScene.loadOp.isDone)
					{
						preloadedScene.loadOp.allowSceneActivation = true;
						yield return preloadedScene.loadOp;
					}

					this.nextActiveMenu = null;

					Scene scene = SceneManager.GetSceneByPath(preloadedScene.scenePath);
					if (scene.IsValid() && scene.isLoaded)
					{
						this.activeMenu = visibleMenu;

						this.popupStack.Add(visibleMenu);

						SceneManager.SetActiveScene(scene);

						SetRootObjectsActive(scene, true);
					}
					else
					{
						Debug.LogErrorFormat("Failed to load menu scene {0} (menu: {1})", resolvedDef.menuScene, resolvedDef.name);
					}
				}
				else
				{
					Debug.LogErrorFormat("Failed to load menu scene {0} (menu: {1})", resolvedDef.menuScene, resolvedDef.name);
				}
			}
			else
			{

			}
		}
	}

	VisibleEnvScene FindOrMakeVisibleEnvScene(SceneData def, string requesterScenePath)
	{
		VisibleEnvScene visible = null;
		for (int i = 0; i < this.visibleEnvScenes.Count; ++i)
		{
			if (this.visibleEnvScenes[i].def == def)
			{
				visible = this.visibleEnvScenes[i];
				break;
			}
		}
		if (visible == null)
		{
			visible = new VisibleEnvScene()
			{
				def = def,
				loadedScene = this.FindOrLoadEnvScene(def, requesterScenePath)
			};
		}
		return visible;
	}

	VisibleMenu FindOrMakeVisibleMenu(MenuData def, string requesterScenePath)
	{
		VisibleMenu visible = null;
		for (int i = 0; i < this.popupStack.Count; ++i)
		{
			if (this.popupStack[i].def == def)
			{
				visible = this.popupStack[i];
				break;
			}
		}
		if (visible == null)
		{
			visible = new VisibleMenu()
			{
				def = def,
				loadedScene = this.FindOrLoadMenuScene(def, requesterScenePath)
			};
		}
		return visible;
	}

	PreloadedScene FindOrLoadEnvScene(SceneData def, string requesterScenePath)
	{
		PreloadedScene preloadedScene = this.FindLoadedScene(def.scene);
		if (preloadedScene == null)
		{
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(def.scene, LoadSceneMode.Additive);
			preloadedScene = new PreloadedScene()
			{
				scenePath = def.scene,
				preloadRequesterScenePaths = new List<string>(),
				loadOp = asyncLoad
			};
			this.preloadedScenes.Add(preloadedScene);
		}
		preloadedScene.AddRequester(requesterScenePath);
		return preloadedScene;
	}

	PreloadedScene FindOrLoadMenuScene(MenuData def, string requesterScenePath)
	{
		PreloadedScene preloadedScene = this.FindLoadedScene(def.menuScene);
		if (preloadedScene == null)
		{
			AsyncOperation asyncLoad = this.LoadMenu(def);
			preloadedScene = new PreloadedScene()
			{
				scenePath = def.menuScene,
				preloadRequesterScenePaths = new List<string>(),
				loadOp = asyncLoad
			};
			this.preloadedScenes.Add(preloadedScene);
		}
		preloadedScene.AddRequester(requesterScenePath);
		return preloadedScene;
	}

	VisibleMenu PopTopPopup()
	{
		VisibleMenu newTopPopup = null;
		if (this.popupStack.Count > 0)
		{
			// Deactivate the current popup
			VisibleMenu currentPopup = this.popupStack[this.popupStack.Count-1];
			this.popupStack.RemoveAt(this.popupStack.Count-1);

			PreloadedScene preloadedScene = currentPopup.loadedScene;
			if (preloadedScene != null)
			{
				SetRootObjectsActive(preloadedScene.scene, false);
				OnSceneHidden(preloadedScene);
			}
			else
			{
				Debug.LogErrorFormat("Visible menu {0} has no loaded scene", currentPopup.def);
			}

			// Select prev popup
			if (this.popupStack.Count > 0)
			{
				newTopPopup = this.popupStack[this.popupStack.Count-1];

				this.activeMenu = newTopPopup;
			}
		}
		return newTopPopup;
	}

	PreloadedScene FindLoadedScene(string scenePath)
	{
		if (string.IsNullOrEmpty(scenePath))
		{
			return null;
		}
		PreloadedScene result = null;
		for (int i = 0; i < this.preloadedScenes.Count; ++i)
		{
			if (this.preloadedScenes[i].scenePath == scenePath)
			{
				result = this.preloadedScenes[i];
				break;
			}
		}
		return result;
	}

	AsyncOperation LoadMenu(MenuData def)
	{
		AsyncOperation asyncLoad;
		if (def.type == MenuType.Start)
		{
			asyncLoad = SceneManager.LoadSceneAsync(def.menuScene, LoadSceneMode.Single);
		}
		else
		{
			asyncLoad = SceneManager.LoadSceneAsync(def.menuScene, LoadSceneMode.Additive);
		}
		return asyncLoad;
	}

	void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		for (int i = 0; i < this.preloadedScenes.Count; ++i)
		{
			PreloadedScene preloadedScene = this.preloadedScenes[i];
			if (preloadedScene.scenePath == scene.path)
			{
				preloadedScene.scene = scene;
				break;
			}
		}
	}

	void HandleSceneUnloaded(Scene scene)
	{
		for (int i = this.preloadedScenes.Count-1; i >= 0; --i)
		{
			PreloadedScene loadedScene = this.preloadedScenes[i];
			if (loadedScene.scene == scene)
			{
				this.preloadedScenes.RemoveAt(i);
				OnSceneHidden(loadedScene);
			}
		}
	}

	bool IsVisible(PreloadedScene preloadedScene)
	{
		VisibleMenu activeMenu = this.nextActiveMenu ?? this.activeMenu;
		if (activeMenu != null && activeMenu.loadedScene == preloadedScene)
		{
			return true;
		}
		for (int popupIndex = 0; popupIndex < this.popupStack.Count; ++popupIndex)
		{
			if (this.popupStack[popupIndex].loadedScene == preloadedScene)
			{
				return true;
			}
		}
		for (int envSceneIndex = 0; envSceneIndex < this.visibleEnvScenes.Count; ++envSceneIndex)
		{
			if (this.visibleEnvScenes[envSceneIndex].loadedScene == preloadedScene)
			{
				return true;
			}
		}

		return false;
	}

	void OnSceneHidden(PreloadedScene hiddenScene)
	{
		for (int i = this.preloadedScenes.Count-1; i >= 0; --i)
		{
			PreloadedScene otherPreloadedScene = this.preloadedScenes[i];
			if (otherPreloadedScene.preloadRequesterScenePaths.Remove(hiddenScene.scenePath)
				&& otherPreloadedScene.preloadRequesterScenePaths.Count == 0)
			{
				// This is a scene that was preloaded for the scene that just got hidden,
				//  and now has no more visible requesters

				// Check that the given scene isn't visible
				if (this.IsVisible(otherPreloadedScene))
				{
					this.preloadedScenes.RemoveAt(i);
					SceneManager.UnloadSceneAsync(otherPreloadedScene.scene);
				}
			}
		}
	}

	static void SetRootObjectsActive(Scene scene, bool active)
	{
		GameObject[] roots = scene.GetRootGameObjects();
		for (int rootObjectIndex = 0; rootObjectIndex < roots.Length; ++rootObjectIndex)
		{
			roots[rootObjectIndex].SetActive(active);
		}
	}
}

public interface INavigator
{
	void SetParentScene(string parentScene);
}

}
