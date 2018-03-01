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
		public MenuData prevScene;
	}

	private class LoadedScene
	{
		public MenuData def;
		public MenuData prevScene;
		public AsyncOperation loadOp;
		public Scene scene;
	}

	List<ToLoad> toLoad = new List<ToLoad>();
	bool processingQueue = false;

	List<LoadedScene> loadedScenes = new List<LoadedScene>();
	MenuData nextActiveScene;
	MenuData activeScene;

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

	public void GoTo(MenuData def)
	{
		this.StartCoroutine(this.GoToCoroutine(def));
	}

	public void Close(MenuData def)
	{
		this.StartCoroutine(this.CloseCoroutine(def));
	}

	public void Preload(MenuData def)
	{
		if (!this.QueuedToLoad(def) && !this.AlreadyLoaded(def))
		{
			MenuData prevScene = this.nextActiveScene ?? this.activeScene;
			var item = new ToLoad()
			{
				def = def,
				prevScene = prevScene
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
			asyncLoad.allowSceneActivation = false;

			LoadedScene loadedScene = new LoadedScene()
			{
				def = toLoad.def,
				prevScene = toLoad.prevScene,
				loadOp = asyncLoad
			};
			this.loadedScenes.Add(loadedScene);

			yield return 0;
		}
		this.processingQueue = false;
	}

	IEnumerator GoToCoroutine(MenuData def)
	{
		while (this.processingQueue)
		{
			yield return 0;
		}


		LoadedScene loadedScene = this.FindLoadedScene(def);
		if (loadedScene == null)
		{
			MenuData prevScene = this.nextActiveScene ?? this.activeScene;

			AsyncOperation asyncLoad = this.LoadScene(def);

			loadedScene = new LoadedScene()
			{
				def = def,
				prevScene = prevScene,
				loadOp = asyncLoad
			};
			this.loadedScenes.Add(loadedScene);
		}

		if (loadedScene.loadOp != null)
		{
			loadedScene.loadOp.allowSceneActivation = true;
			this.nextActiveScene = def;

			yield return loadedScene.loadOp;

			this.nextActiveScene = null;
			this.activeScene = def;
		}
		else
		{
			Debug.LogErrorFormat("Failed to load menu scene {0}", def.scene);
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
			else if (loadedScene.prevScene != null && loadedScene.prevScene.scene == scene.path)
			{
				SceneManager.UnloadSceneAsync(loadedScene.scene);
				this.loadedScenes.RemoveAt(i);
			}
		}
	}
}

}
