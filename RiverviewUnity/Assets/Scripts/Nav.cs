using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace NotABear
{

public class Nav : MonoBehaviour
{
	private class ToLoad
	{
		public MenuData def;
		public LoadedScene parentScene;
	}

	private class LoadedScene
	{
		public MenuData def;
		public LoadedScene parentScene;
		public AsyncOperation loadOp;
		public Scene scene;
	}

	List<ToLoad> toLoad = new List<ToLoad>();
	bool processingQueue = false;

	List<LoadedScene> loadedScenes = new List<LoadedScene>();
	LoadedScene nextActiveScene;
	LoadedScene activeScene;
	Stack<LoadedScene> popupStack = new Stack<LoadedScene>();
	Stack<MenuData> breadcrumbs = new Stack<MenuData>();

	public void Awake()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded += HandleSceneUnloaded;
	}

	public void OnDestroy()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded -= HandleSceneUnloaded;
	}

	public void GoTo(MenuData def, MenuData parentMenuDef = null)
	{
		this.StartCoroutine(this.GoToCoroutine(def, parentMenuDef));
	}

	public void Close(MenuData def)
	{
		this.StartCoroutine(this.CloseCoroutine(def));
	}

	public void ClearBreadcrumbs()
	{
		this.breadcrumbs.Clear();
	}

	public void Preload(MenuData def, MenuData parentMenuDef = null)
	{
		if (!this.QueuedToLoad(def) && !this.AlreadyLoaded(def))
		{
			LoadedScene parentScene = FindLoadedScene(parentMenuDef) ?? this.nextActiveScene ?? this.activeScene;
			var item = new ToLoad()
			{
				def = def,
				parentScene = parentScene
			};

			this.toLoad.Add(item);

			if (!this.processingQueue)
			{
				this.StartCoroutine(this.ProcessQueueCoroutine());
			}
		}
	}

	bool QueuedToLoad(MenuData def)
	{
		for (int i = 0; i < this.toLoad.Count; ++i)
		{
			if (this.toLoad[i].def == def)
			{
				return true;
			}
		}
		return false;
	}

	bool AlreadyLoaded(MenuData def)
	{
		for (int i = 0; i < this.loadedScenes.Count; ++i)
		{
			if (this.loadedScenes[i].def == def)
			{
				return true;
			}
		}
		return false;
	}

	IEnumerator ProcessQueueCoroutine()
	{
		this.processingQueue = true;
		while (this.toLoad.Count > 0)
		{
			ToLoad toLoad = this.toLoad[0];
			this.toLoad.RemoveAt(0);
			AsyncOperation asyncLoad = this.LoadScene(toLoad.def);

			LoadedScene loadedScene = new LoadedScene()
			{
				def = toLoad.def,
				parentScene = toLoad.parentScene,
				loadOp = asyncLoad
			};
			this.loadedScenes.Add(loadedScene);

			yield return 0;
		}
		this.processingQueue = false;
	}

	IEnumerator GoToCoroutine(MenuData def, MenuData parentMenuDef)
	{
		while (this.processingQueue)
		{
			yield return 0;
		}

		MenuData resolvedDef = null;
		if (def.type == MenuType.Back)
		{
			if (this.popupStack.Count > 0)
			{
				LoadedScene currentPopup = this.popupStack.Pop();
				AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentPopup.scene);
				while (!unloadOp.isDone)
				{
					yield return 0;
				}
				if (this.popupStack.Count > 0)
				{
					LoadedScene prevPopup = this.popupStack.Peek();
					resolvedDef = prevPopup.def;
				}
			}
			if (resolvedDef == null)
			{
				if (this.breadcrumbs.Count > 0)
				{
					resolvedDef = this.breadcrumbs.Pop();
				}
			}
		}
		else if (def.type == MenuType.Close)
		{
			if (this.popupStack.Count > 0)
			{
				LoadedScene currentPopup = this.popupStack.Pop();
				AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentPopup.scene);
				while (!unloadOp.isDone)
				{
					yield return 0;
				}
				if (this.popupStack.Count > 0)
				{
					LoadedScene prevPopup = this.popupStack.Peek();
					resolvedDef = prevPopup.def;
				}
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

		LoadedScene loadedScene = this.FindLoadedScene(resolvedDef);
		if (loadedScene == null)
		{
			LoadedScene parentScene = this.FindLoadedScene(parentMenuDef) ?? this.nextActiveScene ?? this.activeScene;

			AsyncOperation asyncLoad = this.LoadScene(resolvedDef);

			loadedScene = new LoadedScene()
			{
				def = resolvedDef,
				parentScene = parentScene,
				loadOp = asyncLoad
			};
			this.loadedScenes.Add(loadedScene);
		}

		if (loadedScene.loadOp != null)
		{
			loadedScene.loadOp.allowSceneActivation = true;
			this.nextActiveScene = loadedScene;

			yield return loadedScene.loadOp;

			this.nextActiveScene = null;
			this.activeScene = loadedScene;

			if (resolvedDef.type == MenuType.Overlay || resolvedDef.type == MenuType.FullscreenOverlay)
			{
				this.popupStack.Push(loadedScene);
			}

			SceneManager.SetActiveScene(SceneManager.GetSceneByPath(loadedScene.def.scene));
		}
		else
		{
			Debug.LogErrorFormat("Failed to load menu scene {0} (going {1})", resolvedDef.scene, def.name);
		}
	}

	IEnumerator CloseCoroutine(MenuData def)
	{
		LoadedScene loadedScene = this.FindLoadedScene(def);
		AsyncOperation asyncUnload;
		if (loadedScene != null && loadedScene.scene.IsValid())
		{
			asyncUnload = SceneManager.UnloadSceneAsync(loadedScene.scene);
		}
		else
		{
			asyncUnload = SceneManager.UnloadSceneAsync(def.scene);
		}

		yield return asyncUnload;
	}

	LoadedScene FindLoadedScene(MenuData def)
	{
		if (def == null)
		{
			return null;
		}
		LoadedScene result = null;
		for (int i = 0; i < this.loadedScenes.Count; ++i)
		{
			if (this.loadedScenes[i].def == def)
			{
				result = this.loadedScenes[i];
				break;
			}
		}
		return result;
	}

	AsyncOperation LoadScene(MenuData def)
	{
		AsyncOperation asyncLoad;
		if (def.type == MenuType.Solo)
		{
			asyncLoad = SceneManager.LoadSceneAsync(def.scene, LoadSceneMode.Single);
		}
		else
		{
			asyncLoad = SceneManager.LoadSceneAsync(def.scene, LoadSceneMode.Additive);
		}
		return asyncLoad;
	}

	void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		for (int i = 0; i < this.loadedScenes.Count; ++i)
		{
			LoadedScene loadedScene = this.loadedScenes[i];
			if (loadedScene.def.scene == scene.path)
			{
				loadedScene.scene = scene;
				break;
			}
		}
	}

	void HandleSceneUnloaded(Scene scene)
	{
		for (int i = this.loadedScenes.Count-1; i >= 0; --i)
		{
			LoadedScene loadedScene = this.loadedScenes[i];
			if (loadedScene.scene == scene)
			{
				this.loadedScenes.RemoveAt(i);
			}
			else if (loadedScene.parentScene != null && loadedScene.parentScene.scene.path == scene.path)
			{
				SceneManager.UnloadSceneAsync(loadedScene.scene);
				this.loadedScenes.RemoveAt(i);
			}
		}
	}
}

}
