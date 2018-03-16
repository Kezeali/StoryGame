using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace Cloverview
{

public class Nav : MonoBehaviour
{
	[SerializeField]
	[Tooltip("A standin menu to use for whatever scene is started from when playing in editor")]
	private MenuData bootMenuForPlayInEditor;

	[SerializeField]
	private int maxPreloadedScenes = 10;

	private enum RequestOp
	{
		AddPreloadRequest,
		RemovePreloadRequest,
		GoTo
	}

	private class MenuSceneToLoad
	{
		public MenuData def;
		public string preloadRequesterId;
	}

	private class EnvSceneToLoad
	{
		public SceneData def;
		public string preloadRequesterId;
		public RequestOp operation;
		public ActiveActivity activeActivity;
	}

	public class PreloadedScene
	{
		public string scenePath;
		public List<string> preloadRequesterIds;
		[System.NonSerialized]
		public AsyncOperation loadOp;
		[System.NonSerialized]
		public Scene scene;

		public bool AddRequester(string requesterId)
		{
			if (!string.IsNullOrEmpty(requesterId)
				&& !this.preloadRequesterIds.Contains(requesterId))
			{
				this.preloadRequesterIds.Add(requesterId);
				return true;
			}
			return false;
		}
	}

	public class VisibleMenu
	{
		public MenuData def;
		public PreloadedScene loadedScene;
	}

	[System.Serializable]
	public class VisibleEnvScene
	{
		public SceneData def;
		public EnvSceneController controller;
		public ActiveActivity activeActivity;
		public PreloadedScene loadedScene;
	}

	Queue<MenuSceneToLoad> menuToLoadQueue = new Queue<MenuSceneToLoad>();
	Queue<EnvSceneToLoad> envSceneToLoadQueue = new Queue<EnvSceneToLoad>();
	bool processingQueue = false;
	Queue<MenuSceneToLoad> menuToGoToQueue = new Queue<MenuSceneToLoad>();
	Queue<EnvSceneToLoad> envSceneToGoToQueue = new Queue<EnvSceneToLoad>();
	bool processingGoToQueue = false;

	List<PreloadedScene> preloadedScenes = new List<PreloadedScene>();

	VisibleMenu nextActiveMenu;
	VisibleMenu activeMenu;
	List<VisibleMenu> popupStack = new List<VisibleMenu>();
	Stack<MenuData> breadcrumbs = new Stack<MenuData>();

	List<VisibleEnvScene> visibleEnvScenes = new List<VisibleEnvScene>();

	SaveData saveData;

#if UNITY_EDITOR
	bool loadSave = false;
	Scene bootScene;
#endif

	public void OnEnable()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded += HandleSceneUnloaded;

	#if UNITY_EDITOR
		// Intialise the current scene as a menu
		{
			this.bootScene = SceneManager.GetActiveScene();
			if (this.bootScene.buildIndex != 0)
			{
				var bootLoadedScene = new PreloadedScene()
				{
					scenePath = this.bootScene.path,
					preloadRequesterIds = new List<string>(),
					scene = this.bootScene
				};
				var bootMenu = new VisibleMenu()
				{
					def = this.bootMenuForPlayInEditor,
					loadedScene = bootLoadedScene
				};
				this.nextActiveMenu = null;
				this.activeMenu = bootMenu;
				this.popupStack.Add(bootMenu);

				this.loadSave = false;
			}
			else
			{
				this.loadSave = true;
			}
		}
	#endif
	}

	public void Start()
	{
	#if UNITY_EDITOR
		if (!this.loadSave)
		{
			SetRootObjectsActive(bootScene, true);
		}
	#endif
	}

	public void OnDestroy()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded -= HandleSceneUnloaded;
	}

	public void Initialise(SaveData saveData)
	{
		this.saveData = saveData;
		if (this.saveData.nav == null)
		{
			this.saveData.nav = new NavSaveData();
		}
	#if UNITY_EDITOR
		if (this.loadSave)
	#endif
		{
			var savedPopupStack = saveData.nav.popupStack;
			// var savedVisibleEnvScenes = saveData.nav.visibleEnvScenes;
			saveData.nav.popupStack = this.popupStack;
			saveData.nav.visibleEnvScenes = this.visibleEnvScenes;
			for (int savedPopupIndex = 0; savedPopupIndex < savedPopupStack.Count; ++savedPopupIndex)
			{
				VisibleMenu savedVisibleMenu = savedPopupStack[savedPopupIndex];
				PreloadedScene loadedScene = savedVisibleMenu.loadedScene;
				if (loadedScene != null)
				{
					for (int requesterIndex = 0; requesterIndex < loadedScene.preloadRequesterIds.Count; ++requesterIndex)
					{
						string requesterId = loadedScene.preloadRequesterIds[savedPopupIndex];
						GoTo(savedVisibleMenu.def, requesterId);
					}
				}
			}
			// for (int savedVisibleEnvIndex = 0; savedVisibleEnvIndex < savedVisibleEnvScenes.Count; ++savedVisibleEnvIndex)
			// {
			// 	VisibleEnvScene savedVisibleEnv = savedVisibleEnvScenes[savedVisibleEnvIndex];
			// 	PreloadedScene loadedScene = savedVisibleEnv.loadedScene;
			// 	if (loadedScene != null)
			// 	{
			// 		for (int requesterIndex = 0; requesterIndex < loadedScene.preloadRequesterIds.Count; ++requesterIndex)
			// 		{
			// 			string requesterId = loadedScene.preloadRequesterIds[savedPopupIndex];
			// 		}
			// 	}
			// }
		}
	}

	public void GoTo(MenuData def, string requesterId = null)
	{
		var item = new MenuSceneToLoad()
		{
			def = def,
			preloadRequesterId = requesterId
		};

		this.menuToGoToQueue.Enqueue(item);

		if (!this.processingGoToQueue)
		{
			this.StartCoroutine(this.ProcessGoToQueueCoroutine());
		}
	}

	public void GoToActivity(ActiveActivity activity, string requesterId)
	{
		SceneData sceneDef = activity.def.scene;
		if (sceneDef != null)
		{
			var item = new EnvSceneToLoad()
			{
				def = sceneDef,
				preloadRequesterId = requesterId,
				operation = RequestOp.AddPreloadRequest,
				activeActivity = activity
			};

			this.envSceneToGoToQueue.Enqueue(item);

			if (!this.processingGoToQueue)
			{
				this.StartCoroutine(this.ProcessGoToQueueCoroutine());
			}
		}
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

	public void Preload(MenuData def, string requesterId)
	{
		if (def.allowPreload
			&& def.type != MenuType.Back && def.type != MenuType.ClosePopup)
		{
			// Validate the requester id: must be a visible scene
			{
				PreloadedScene loadedScene = this.FindLoadedScene(requesterId);
				if (loadedScene != null && !this.IsVisible(loadedScene))
				{
					Debug.LogWarningFormat("Invalid requester {0} tried to preload menu {1}. Preload requesters for menus must be loaded menus themselves.", requesterId, def.name);
					requesterId = null;
				}
			}

			var item = new MenuSceneToLoad()
			{
				def = def,
				preloadRequesterId = requesterId
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

	public void Preload(SceneData def, string requesterId)
	{
		var item = new EnvSceneToLoad()
		{
			def = def,
			preloadRequesterId = requesterId,
			operation = RequestOp.AddPreloadRequest
		};

		this.envSceneToLoadQueue.Enqueue(item);

		if (!this.processingQueue)
		{
			this.StartCoroutine(this.ProcessQueueCoroutine());
		}
	}

	public void RemovePreloadRequest(SceneData def, string requesterId)
	{
		var item = new EnvSceneToLoad()
		{
			def = def,
			preloadRequesterId = requesterId,
			operation = RequestOp.RemovePreloadRequest
		};

		this.envSceneToLoadQueue.Enqueue(item);

		if (!this.processingQueue)
		{
			this.StartCoroutine(this.ProcessQueueCoroutine());
		}
	}

	public string GeneratePreloadIdForEnvScenes()
	{
		if (this.saveData.nav.nextPreloadId == int.MaxValue)
		{
			this.saveData.nav.nextPreloadId = int.MinValue;
		}
		else
		{
			++this.saveData.nav.nextPreloadId;
		}
		return this.saveData.nav.nextPreloadId.ToString();
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

		int maxLoadedScenes = this.maxPreloadedScenes + this.visibleEnvScenes.Count + this.popupStack.Count;

		while (this.menuToLoadQueue.Count > 0 || this.envSceneToLoadQueue.Count > 0)
		{
			// Only load menus if there are no more env scenes to load
			if (this.envSceneToLoadQueue.Count > 0)
			{
				EnvSceneToLoad envSceneRequest = this.envSceneToLoadQueue.Dequeue();

				if (envSceneRequest.def != null)
				{
					if (envSceneRequest.operation == RequestOp.AddPreloadRequest)
					{
						if (this.preloadedScenes.Count <= maxLoadedScenes)
						{
							PreloadedScene preloadedScene =

							this.FindOrLoadEnvScene(envSceneRequest.def, envSceneRequest.preloadRequesterId);

							Debug.LogFormat("Preload requested for {0}. Current load progress: {1}", preloadedScene.scenePath, preloadedScene.loadOp.progress);
						}
					}
					else if (envSceneRequest.operation == RequestOp.RemovePreloadRequest)
					{
						PreloadedScene preloadedScene = this.FindLoadedScene(envSceneRequest.def.scene);
						if (preloadedScene != null)
						{
							if (preloadedScene.preloadRequesterIds.Remove(envSceneRequest.preloadRequesterId))
							{
								UnloadUnreferencedScenes();
							}
						}
					}
				}
			}
			else if (this.menuToLoadQueue.Count > 0)
			{
				MenuSceneToLoad menuToLoad = this.menuToLoadQueue.Dequeue();
				if (menuToLoad.def != null)
				{
					if (menuToLoad.def.type.IsPreloadable())
					{
						if (this.preloadedScenes.Count <= maxLoadedScenes)
						{
							PreloadedScene preloadedScene =

							this.FindOrLoadMenuScene(menuToLoad.def, menuToLoad.preloadRequesterId);

							Debug.LogFormat("Preload requested for {0}. Current load progress: {1}", preloadedScene.scenePath, preloadedScene.loadOp.progress);
						}
					}
					else
					{
						Debug.LogErrorFormat("Tried to pre-load an menu type that can't be pre-loaded: {0}", menuToLoad.def);
					}
				}
			}

			yield return 0;
		}
		this.processingQueue = false;
	}

	IEnumerator ProcessGoToQueueCoroutine()
	{
		this.processingGoToQueue = true;

		yield return 0;

		while (this.menuToGoToQueue.Count > 0 || this.envSceneToGoToQueue.Count > 0)
		{
			// Only load menus if there are no more env scenes to load
			if (this.envSceneToLoadQueue.Count > 0)
			{
				EnvSceneToLoad envSceneRequest = this.envSceneToLoadQueue.Dequeue();

				if (envSceneRequest.def != null)
				{
					if (envSceneRequest.operation == RequestOp.AddPreloadRequest)
					{
						IEnumerator op = this.GoToEnvSceneCoroutine(envSceneRequest.def, envSceneRequest.preloadRequesterId);
						while (op.MoveNext())
						{
							yield return 0;
						}
						if (envSceneRequest.activeActivity != null)
						{
							// Provide a scene reference to the interested activity
							VisibleEnvScene visibleScene = this.FindVisibleEnvScene(envSceneRequest.def);
							if (visibleScene != null)
							{
								if (visibleScene.activeActivity != null)
								{
									visibleScene.activeActivity.envScene = null;
								}
								visibleScene.activeActivity = envSceneRequest.activeActivity;
								visibleScene.controller.SetActivity(envSceneRequest.activeActivity);
							}
							// NOTE: is ok to be null
							envSceneRequest.activeActivity.envScene = visibleScene;
						}
					}
					else if (envSceneRequest.operation == RequestOp.RemovePreloadRequest)
					{
						PreloadedScene preloadedScene = this.FindLoadedScene(envSceneRequest.def.scene);
						if (preloadedScene != null)
						{
							if (preloadedScene.preloadRequesterIds.Remove(envSceneRequest.preloadRequesterId))
							{
								UnloadUnreferencedScenes();
							}
						}
						if (envSceneRequest.activeActivity != null)
						{
							// Provide a scene reference to the interested activity
							VisibleEnvScene visibleScene = this.FindVisibleEnvScene(envSceneRequest.def);
							if (visibleScene != null)
							{
								if (visibleScene.activeActivity == envSceneRequest.activeActivity)
								{
									visibleScene.controller.ClearActivity();
									visibleScene.activeActivity = null;
								}
							}
							envSceneRequest.activeActivity.envScene = null;
						}
					}
				}
			}
			else if (this.menuToGoToQueue.Count > 0)
			{
				MenuSceneToLoad menuToLoad = this.menuToGoToQueue.Dequeue();

				if (menuToLoad.def != null)
				{
					IEnumerator op = this.GoToCoroutine(menuToLoad.def, menuToLoad.preloadRequesterId);
					while (op.MoveNext())
					{
						yield return 0;
					}
				}
			}

			yield return 0;
		}

		this.processingGoToQueue = false;
	}

	IEnumerator GoToCoroutine(MenuData def, string requesterId)
	{
		while (this.processingQueue)
		{
			yield return 0;
		}

		VisibleMenu defaultVisibleMenu = null;
		MenuData resolvedDef = null;
		if (def.type == MenuType.Back)
		{
			VisibleMenu newTopPopup = PopTopPopup();
			if (newTopPopup != null)
			{
				defaultVisibleMenu = newTopPopup;
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
				defaultVisibleMenu = newTopPopup;
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
			this.breadcrumbs.Clear();
		}

		// Show the environment scene
		if (resolvedDef.envScene != null)
		{
			IEnumerator op = this.GoToEnvSceneCoroutine(resolvedDef.envScene, resolvedDef.menuScene);
			while (op.MoveNext())
			{
				yield return 0;
			}
		}

		// Show the menu scene
		{
			VisibleMenu visibleMenu = defaultVisibleMenu ?? this.FindOrMakeVisibleMenu(resolvedDef, requesterId);
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

						if (!this.breadcrumbs.Contains(visibleMenu.def))
						{
							this.breadcrumbs.Push(visibleMenu.def);
						}
						else
						{
							// Pop breadcrumbs until the new menu active is at the end
							MenuData breadcrumb = this.breadcrumbs.Peek();
							while (breadcrumb != visibleMenu.def && this.breadcrumbs.Count > 0)
							{
								this.breadcrumbs.Pop();
								breadcrumb = this.breadcrumbs.Peek();
							}
						}

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

	IEnumerator GoToEnvSceneCoroutine(SceneData envScene, string requesterId)
	{
		VisibleEnvScene visibleEnvScene = this.FindOrMakeVisibleEnvScene(envScene, requesterId);
		if (visibleEnvScene != null)
		{
			PreloadedScene preloadedScene = visibleEnvScene.loadedScene;

			if (preloadedScene.loadOp != null
				|| (preloadedScene.scene.IsValid() && preloadedScene.scene.isLoaded))
			{
				if (preloadedScene.loadOp != null && !preloadedScene.loadOp.isDone)
				{
					preloadedScene.loadOp.allowSceneActivation = true;
					while (!preloadedScene.loadOp.isDone)
					{
						yield return 0;
					}
				}

				Scene scene = SceneManager.GetSceneByPath(preloadedScene.scenePath);
				if (scene.IsValid() && scene.isLoaded)
				{
					this.visibleEnvScenes.Add(visibleEnvScene);

					SetRootObjectsActive(scene, true);
				}
				else
				{
					Debug.LogErrorFormat("Failed to load env scene {0} (requester: {1})", envScene, requesterId);
				}
			}
			else
			{
				Debug.LogErrorFormat("Failed to load env scene {0} (requester {1})", envScene, requesterId);
			}
		}
		else
		{
			Debug.LogErrorFormat("Failed to make the scene at {0} visible", envScene);
		}
	}

	VisibleEnvScene FindVisibleEnvScene(SceneData def)
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
		return visible;
	}

	VisibleEnvScene FindOrMakeVisibleEnvScene(SceneData def, string requesterId)
	{
		VisibleEnvScene visible = this.FindVisibleEnvScene(def);
		if (visible == null)
		{
			visible = new VisibleEnvScene()
			{
				def = def,
				loadedScene = this.FindOrLoadEnvScene(def, requesterId)
			};
		}
		return visible;
	}

	VisibleMenu FindOrMakeVisibleMenu(MenuData def, string requesterId)
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
				loadedScene = this.FindOrLoadMenuScene(def, requesterId)
			};
		}
		return visible;
	}

	PreloadedScene FindOrLoadEnvScene(SceneData def, string requesterId)
	{
		PreloadedScene preloadedScene = this.FindLoadedScene(def.scene);
		if (preloadedScene == null)
		{
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(def.scene, LoadSceneMode.Additive);
			preloadedScene = new PreloadedScene()
			{
				scenePath = def.scene,
				preloadRequesterIds = new List<string>(),
				loadOp = asyncLoad
			};
			this.preloadedScenes.Add(preloadedScene);
		}
		preloadedScene.AddRequester(requesterId);
		return preloadedScene;
	}

	PreloadedScene FindOrLoadMenuScene(MenuData def, string requesterId)
	{
		PreloadedScene preloadedScene = this.FindLoadedScene(def.menuScene);
		if (preloadedScene == null)
		{
			AsyncOperation asyncLoad = this.LoadMenu(def);
			preloadedScene = new PreloadedScene()
			{
				scenePath = def.menuScene,
				preloadRequesterIds = new List<string>(),
				loadOp = asyncLoad
			};
			this.preloadedScenes.Add(preloadedScene);
		}
		preloadedScene.AddRequester(requesterId);
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

	bool ShouldBeVisible(VisibleEnvScene envScene)
	{
		if (envScene.activeActivity != null)
		{
			return true;
		}
		for (int popupIndex = 0; popupIndex < this.popupStack.Count; ++popupIndex)
		{
			if (this.popupStack[popupIndex].def.envScene == envScene.def)
			{
				return true;
			}
		}

		return false;
	}

	void OnSceneHidden(PreloadedScene hiddenScene)
	{
		Debug.Assert(hiddenScene != null);
		if (hiddenScene != null)
		{
			for (int i = 0; i < this.preloadedScenes.Count; ++i)
			{
				PreloadedScene otherPreloadedScene = this.preloadedScenes[i];
				
				otherPreloadedScene.preloadRequesterIds.Remove(hiddenScene.scenePath);
			}
		}

		UnloadUnreferencedScenes();
	}

	void UnloadUnreferencedScenes()
	{
		// Hide env scenes that are no longer referenced by visible menus
		for (int i = this.visibleEnvScenes.Count-1; i >= 0; --i)
		{
			VisibleEnvScene visibleEnvScene = this.visibleEnvScenes[i];
			if (!this.ShouldBeVisible(visibleEnvScene))
			{
				PreloadedScene loadedScene = visibleEnvScene.loadedScene;
				this.visibleEnvScenes.RemoveAt(i);
				SetRootObjectsActive(loadedScene.scene, false);
			}
		}

		// Unload scenes that have no preload requests and are not visible
		for (int i = this.preloadedScenes.Count-1; i >= 0; --i)
		{
			PreloadedScene otherPreloadedScene = this.preloadedScenes[i];
			if (otherPreloadedScene.preloadRequesterIds.Count == 0)
			{
				// This scene has no more active requesters

				// Check that the given scene isn't visible
				if (!this.IsVisible(otherPreloadedScene))
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
