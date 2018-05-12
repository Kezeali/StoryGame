using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

namespace Cloverview
{

public class Nav : MonoBehaviour
{
	[SerializeField]
	[Tooltip("A standin menu to use for whatever scene is started from when playing in editor")]
	private MenuData bootMenuForPlayInEditor;

	[SerializeField]
	private int maxPreloadedScenes = 10;

	[SerializeField]
	private CommuteSceneData[] commuteScenes;

	[SerializeField]
	private CommuteSceneData defaultCommuteScene;

	[SerializeField]
	private Animator genericTransitionAnimator;

	private enum RequestOp
	{
		AddPreloadRequest,
		RemovePreloadRequest,
		GoTo
	}

	private enum GoToOp
	{
		Open,
		Close
	}

	private class MenuSceneToLoad
	{
		public MenuData def;
		public string preloadRequesterId;
		public GoToOp operation;
	}

	private class EnvSceneToLoad
	{
		public SceneData def;
		public string preloadRequesterId;
		public RequestOp operation;
		public ActiveActivity activeActivity;
		public ActiveEvent activeEvent;
		public CommuteSceneData commuteDef;
		public CommuteDirection commuteDirection;
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
		public MenuSceneController controller;
		public PreloadedScene loadedScene;
	}

	[System.Serializable]
	public class VisibleEnvScene
	{
		public SceneData def;
		public EnvSceneController controller;
		public ActiveActivity activeActivity;
		public ActiveEvent activeEvent;
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
	[System.NonSerialized]
	public VisibleMenu activeMenu;
	List<VisibleMenu> popupStack = new List<VisibleMenu>();
	Stack<MenuData> breadcrumbs = new Stack<MenuData>();

	List<VisibleEnvScene> visibleEnvScenes = new List<VisibleEnvScene>();
	VisibleEnvScene activeEnvScene;
	CommuteSceneData activeCommuteDef;

	CinemachineBrain[] envCameraBrain;

	SaveData saveData;

#if UNITY_EDITOR
	[System.NonSerialized]
	public bool loadedInActualBootScene = false;
	Scene playInEditorBootScene;
#endif

	public void Awake()
	{
		int cameraTypes = System.Enum.GetValues(typeof(EnvCameraType)).Length;
		this.envCameraBrain = new CinemachineBrain[cameraTypes];
	}

	public void OnEnable()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded += HandleSceneUnloaded;

	#if UNITY_EDITOR
		if (!this.playInEditorBootScene.IsValid())
		{
			this.DetermineBootScene();
		}
	#endif
	}

#if UNITY_EDITOR
	public void DetermineBootScene()
	{
		// Intialise the current scene as a menu
		this.playInEditorBootScene = SceneManager.GetActiveScene();
		if (this.playInEditorBootScene.buildIndex != 0)
		{
			var bootLoadedScene = new PreloadedScene()
			{
				scenePath = this.playInEditorBootScene.path,
				preloadRequesterIds = new List<string>(),
				scene = this.playInEditorBootScene
			};
			this.preloadedScenes.Add(bootLoadedScene);
			var bootMenu = new VisibleMenu()
			{
				def = this.bootMenuForPlayInEditor,
				loadedScene = bootLoadedScene
			};
			this.nextActiveMenu = null;
			this.activeMenu = bootMenu;
			this.popupStack.Add(bootMenu);

			this.loadedInActualBootScene = false;
		}
		else
		{
			this.loadedInActualBootScene = true;
		}
	}
#endif

	public void Start()
	{
	#if UNITY_EDITOR
		if (!this.loadedInActualBootScene)
		{
			Nav.SetRootObjectsActive(playInEditorBootScene, true);
		}
	#endif
	}

	public void OnDestroy()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded -= HandleSceneUnloaded;
	}

	public void Load(SaveData saveData)
	{
		this.saveData = saveData;
		if (this.saveData.nav == null)
		{
			this.saveData.nav = new NavSaveData();
		}

		if (this.saveData.nav.currentRootMenu != null)
		{
			if (this.saveData.nav.currentRootMenu.def != null && this.saveData.nav.currentRootMenu.def.type == MenuType.Root)
			{
				this.GoTo(this.saveData.nav.currentRootMenu.def, null);
			}

			// TODO: save breadcrumbs, like this
			// this.breadcrumbs.Clear();
			// for (int i = 0; i < this.saveData.breadcrumbs.Count; ++i)
			// {
			// 	if (this.saveData.breadcrumbs[i] != null)
			// 	{
			// 		this.breadcrumbs.Add(this.saveData.breadcrumbs[i]);
			// 	}
			// }
			// this.saveData.nav.breadcrumbs = this.breadcrumbs;
		}

		{
			//var savedPopupStack = this.saveData.nav.popupStack;
			//var savedVisibleEnvScenes = saveData.nav.visibleEnvScenes;
			
			// this.saveData.nav.popupStack = this.popupStack;
			// this.saveData.nav.visibleEnvScenes = this.visibleEnvScenes;

			// for (int savedVisibleEnvIndex = 0; savedVisibleEnvIndex < savedVisibleEnvScenes.Count; ++savedVisibleEnvIndex)
			// {
			// 	VisibleEnvScene savedVisibleEnv = savedVisibleEnvScenes[savedVisibleEnvIndex];
			// 	PreloadedScene loadedScene = savedVisibleEnv.loadedScene;
			// 	if (loadedScene != null)
			// 	{
			// 		for (int requesterIndex = 0; requesterIndex < loadedScene.preloadRequesterIds.Count; ++requesterIndex)
			// 		{
			// 			string requesterId = loadedScene.preloadRequesterIds[savedPopupIndex];
			// 			this.GoTo(savedVisibleMenu.def, requesterId);
			// 		}
			// 	}
			// }

			// for (int savedPopupIndex = 0; savedPopupIndex < savedPopupStack.Count; ++savedPopupIndex)
			// {
			// 	VisibleMenu savedVisibleMenu = savedPopupStack[savedPopupIndex];
			// 	PreloadedScene loadedScene = savedVisibleMenu.loadedScene;
			// 	if (loadedScene != null)
			// 	{
			// 		for (int requesterIndex = 0; requesterIndex < loadedScene.preloadRequesterIds.Count; ++requesterIndex)
			// 		{
			// 			string requesterId = loadedScene.preloadRequesterIds[savedPopupIndex];
			// 			this.GoTo(savedVisibleMenu.def, requesterId);
			// 		}
			// 	}
			// }
		}
	}

	public void SetEnvCamera(EnvCameraType type, CinemachineBrain envCameraBrain)
	{
		if (this.envCameraBrain != null && this.envCameraBrain.Length > (int)type)
		{
			this.envCameraBrain[(int)type] = envCameraBrain;
		}
		else
		{
			Debug.LogError("EnvCamera types not initialised properly. Oops");
		}
	}

	CinemachineBrain GetEnvCamera(EnvCameraType type)
	{
		CinemachineBrain result = null;
		Debug.Assert(this.envCameraBrain != null);
		if (this.envCameraBrain != null && this.envCameraBrain.Length > (int)type)
		{
			result = this.envCameraBrain[(int)type];
		}
		return result;
	}

	void ActivateEnvCamera(EnvCameraType type)
	{
		Debug.Assert(this.envCameraBrain != null);
		if (this.envCameraBrain != null)
		{
			for (int i = 0; i < this.envCameraBrain.Length; ++i)
			{
				if (this.envCameraBrain[i] != null)
				{
					this.envCameraBrain[i].gameObject.SetActive(i == (int)type);
				}
			}
		}
	}

	public void MakeCurrentMenuTheActiveScene()
	{
		if (this.activeMenu != null && this.activeMenu.loadedScene != null)
		{
			SceneManager.SetActiveScene(this.activeMenu.loadedScene.scene);
		}
	}

	public void MakeCurrentEnvTheActiveScene()
	{
		if (this.activeEnvScene != null && this.activeEnvScene.loadedScene != null)
		{
			SceneManager.SetActiveScene(this.activeEnvScene.loadedScene.scene);
		}
	}

	public void GoTo(MenuData def, string requesterId = null)
	{
		var item = new MenuSceneToLoad()
		{
			def = def,
			preloadRequesterId = requesterId,
			operation = GoToOp.Open
		};

		this.menuToGoToQueue.Enqueue(item);

		if (!this.processingGoToQueue)
		{
			this.StartCoroutine(this.ProcessGoToQueueCoroutine());
		}
	}

	public void CloseActiveMenu()
	{
		if (this.activeMenu != null && this.activeMenu.def != null)
		{
			var item = new MenuSceneToLoad()
			{
				def = this.activeMenu.def,
				preloadRequesterId = "",
				operation = GoToOp.Close
			};

			this.menuToGoToQueue.Enqueue(item);

			if (!this.processingGoToQueue)
			{
				this.StartCoroutine(this.ProcessGoToQueueCoroutine());
			}
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

	public void GoToCommute(ActiveEvent @event, SceneData destination, string requesterId)
	{
		// Leave any current active commute
		if (this.activeCommuteDef != null)
		{
			this.RemovePreloadRequest(this.activeCommuteDef.commuteScene, requesterId);
		}

		SceneData origin = null;
		if (this.activeEnvScene != null || destination != null)
		{
			origin = this.activeEnvScene != null ? this.activeEnvScene.def : destination;
			destination = destination != null ? destination : origin;
		}

		if (origin != null && destination != null)
		{
			SceneData commuteSceneDef = null;
			CommuteSceneData commuteData = null;
			CommuteDirection direction = CommuteDirection.AtoB;
			if (origin != destination)
			{
				commuteData = FindCommuteScene(out direction, origin, destination);
				commuteSceneDef = commuteData.commuteScene;
			}
			else
			{
				// No commute needs to happen if origin & destination are the same scene
				commuteSceneDef = origin;
			}

			var item = new EnvSceneToLoad()
			{
				def = commuteSceneDef,
				preloadRequesterId = requesterId,
				operation = RequestOp.AddPreloadRequest,
				activeEvent = @event,
				commuteDef = commuteData,
				commuteDirection = direction,
			};

			this.envSceneToGoToQueue.Enqueue(item);

			if (!this.processingGoToQueue)
			{
				this.StartCoroutine(this.ProcessGoToQueueCoroutine());
			}
		}
	}

	public void GoToCommute(SceneData destination, string requesterId)
	{
		// Leave any current active commute
		if (this.activeCommuteDef != null)
		{
			this.RemovePreloadRequest(this.activeCommuteDef.commuteScene, requesterId);
		}

		SceneData sceneDef = destination;
		if (sceneDef != null)
		{
			var item = new EnvSceneToLoad()
			{
				def = sceneDef,
				preloadRequesterId = requesterId,
				operation = RequestOp.AddPreloadRequest,
			};

			this.envSceneToGoToQueue.Enqueue(item);

			if (!this.processingGoToQueue)
			{
				this.StartCoroutine(this.ProcessGoToQueueCoroutine());
			}
		}
	}

	CommuteSceneData FindCommuteScene(out CommuteDirection direction, SceneData from, SceneData to)
	{
		CommuteSceneData result = this.defaultCommuteScene;
		direction = CommuteDirection.AtoB;
		for (int i = 0; i < this.commuteScenes.Length; ++i)
		{
			CommuteSceneData commute = this.commuteScenes[i];
			bool forward = commute.sceneA == from && commute.sceneB == to;
			bool backward = !forward && commute.sceneA == to && commute.sceneB == from;
			if (forward || backward)
			{
				result = commute;
				direction = forward ? CommuteDirection.AtoB : CommuteDirection.BtoA;
				break;
			}
		}
		return result;
	}

	public void FinishCommute(string requesterId)
	{
		if (this.activeCommuteDef != null)
		{
			this.RemovePreloadRequest(this.activeCommuteDef.commuteScene, requesterId);
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
			PreloadedScene loadedScene = this.FindLoadedScene(requesterId);
			if (loadedScene == null || !this.IsVisible(loadedScene))
			{
				Debug.LogWarningFormat("Invalid requester {0} tried to preload menu {1}. Preload requesters for menus must be loaded menus themselves.", requesterId, def.name);
				requesterId = null;
			}

			if (def.envScene != null)
			{
				this.Preload(def.envScene, def.menuScene.scenePath);
			}

			if (requesterId != null)
			{
				var item = new MenuSceneToLoad()
				{
					def = def,
					preloadRequesterId = requesterId
				};

				this.menuToLoadQueue.Enqueue(item);

				if (!this.processingQueue)
				{
					this.StartCoroutine(this.ProcessQueueCoroutine());
				}
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
						// TODO(elliot): remove this condition here and still add the preload request to the PreloadedScene no matter how many scenes are already loaded, but don't actually load the scene until a GoTo request is received for it, or the loaded scene count drops
						// TODO(elliot): also, use an actual memory usage limit rather than this semi-arbitary loaded scene count
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
			if (this.envSceneToGoToQueue.Count > 0)
			{
				EnvSceneToLoad envSceneRequest = this.envSceneToGoToQueue.Dequeue();

				if (envSceneRequest.def != null)
				{
					if (envSceneRequest.operation == RequestOp.AddPreloadRequest)
					{
						IEnumerator op =
							this.GoToEnvSceneCoroutine(envSceneRequest.def, envSceneRequest.preloadRequesterId);
						while (op.MoveNext())
						{
							yield return op.Current;
						}
						VisibleEnvScene visibleScene = null;
						if (envSceneRequest.activeActivity != null || envSceneRequest.activeEvent != null)
						{
							// Provide a scene reference to the interested activity
							visibleScene = this.FindVisibleEnvScene(envSceneRequest.def);
							if (visibleScene != null)
							{
								// Remove the scene from any current activities / events using it
								if (visibleScene.activeActivity != null)
								{
									visibleScene.activeActivity.envScene = null;
								}
								if (visibleScene.activeEvent != null)
								{
									visibleScene.activeEvent.envScene = null;
								}

								visibleScene.activeActivity = envSceneRequest.activeActivity;
								if (visibleScene.controller != null)
								{
									visibleScene.controller.SetActivity(envSceneRequest.activeActivity);
								}
								visibleScene.activeEvent = envSceneRequest.activeEvent;
								if (visibleScene.controller != null)
								{
									visibleScene.controller.SetEvent(envSceneRequest.activeEvent);
								}
							}
							if (envSceneRequest.commuteDef != null)
							{
								this.activeCommuteDef = envSceneRequest.commuteDef;
								
								if (visibleScene == null)
								{
									visibleScene = this.FindVisibleEnvScene(envSceneRequest.def);
								}
								if (visibleScene != null)
								{
									visibleScene.controller.SetCommuteDirection(envSceneRequest.commuteDirection);
								}
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
						if (envSceneRequest.activeActivity != null || envSceneRequest.activeEvent != null)
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
								if (visibleScene.activeEvent == envSceneRequest.activeEvent)
								{
									visibleScene.controller.ClearEvent();
									visibleScene.activeEvent = null;
								}
							}
							if (envSceneRequest.activeActivity != null)
							{
								envSceneRequest.activeActivity.envScene = null;
							}
							if (envSceneRequest.activeEvent != null)
							{
								envSceneRequest.activeEvent.envScene = null;
							}
						}
					}
				}
			}
			else if (this.menuToGoToQueue.Count > 0)
			{
				MenuSceneToLoad menuToLoad = this.menuToGoToQueue.Dequeue();

				if (menuToLoad.def != null)
				{
					if (menuToLoad.operation == GoToOp.Open)
					{
						IEnumerator op = this.GoToCoroutine(menuToLoad.def, menuToLoad.preloadRequesterId);
						while (op.MoveNext())
						{
							yield return op.Current;
						}
					}
					else if (menuToLoad.operation == GoToOp.Close)
					{
						Debug.LogError("Not Implemented: GoToOp.Close");
					}
				}
			}

			yield return 0;
		}

		this.processingGoToQueue = false;
	}

	enum ResolvedVia
	{
		RequestedMenu,
		PopBreadcrumb,
		PopTopPopup
	}

	struct ResolvedMenuNavigation
	{
		public ResolvedVia type;
		public MenuData resolvedDef;
		public VisibleMenu visibleMenu;
	}

	IEnumerator GoToCoroutine(MenuData def, string requesterId)
	{
		while (this.processingQueue)
		{
			yield return 0;
		}

		Debug.Assert(this.nextActiveMenu == null);

		// TODO(elliot): block input until transition finishes

		ResolvedMenuNavigation resolvedNav = default(ResolvedMenuNavigation);
		switch (def.type)
		{
			case MenuType.Back:
			{
				if (this.popupStack.Count > 1)
				{
					resolvedNav.type = ResolvedVia.PopTopPopup;
					resolvedNav.visibleMenu = this.popupStack[this.popupStack.Count-2];
					resolvedNav.resolvedDef = resolvedNav.visibleMenu.def;
				}
				else
				{
					resolvedNav.type = ResolvedVia.PopBreadcrumb;
					resolvedNav.resolvedDef = this.breadcrumbs.Peek();
				}
			} break;
			case MenuType.ClosePopup:
			{
				if (this.popupStack.Count > 1)
				{
					resolvedNav.type = ResolvedVia.PopTopPopup;
					resolvedNav.visibleMenu = this.popupStack[this.popupStack.Count-2];
					resolvedNav.resolvedDef = resolvedNav.visibleMenu.def;
				}
			} break;
			default:
			{
				resolvedNav.type = ResolvedVia.RequestedMenu;
				resolvedNav.resolvedDef = def;
			} break;
		}

		if (resolvedNav.resolvedDef == null)
		{
			yield break;
		}

		// NOTE(elliot): nextActiveMenu can be set early in the case where the navigation resolution above determined that the next menu is one that's already visible. This will prevent some load thrashing.
		this.nextActiveMenu = resolvedNav.visibleMenu;

		// Remember the previous active menu so any specific transition-in animations can be triggered
		VisibleMenu previousActiveMenu = this.activeMenu;

		switch (resolvedNav.type)
		{
			case ResolvedVia.PopBreadcrumb:
			{
				this.breadcrumbs.Pop();
			} break;
			case ResolvedVia.PopTopPopup:
			{
				// NOTE(elliot): calling this after determining the nextActiveMenu so that any menu-to-menu specific transitions can be triggered
				this.TransitionOutOfTopPopup(resolvedNav.resolvedDef);
			} break;
		}

		MenuData resolvedDef = resolvedNav.resolvedDef;
		VisibleMenu alreadyVisibleMenu = resolvedNav.visibleMenu;

		// Clear the popups before switching to root menus
		switch (resolvedDef.type)
		{
			case MenuType.Start:
			case MenuType.Root:
			{
				if (this.GetTopPopup() != alreadyVisibleMenu)
				{
					// Initiate a transition to the new menu
					this.TransitionOutOfTopPopup(resolvedDef);
				}
				RemoveOtherPopups(resolvedDef);
				this.breadcrumbs.Clear();
			} break;
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
			// NOTE(elliot): if there isn't already a visible menu provided by a back / close operation above, request the visible menu now, before closing existing menus, to ensure that this new requester is added and any preloaded scene wont be unloaded!
			VisibleMenu visibleMenu = alreadyVisibleMenu ?? this.FindOrMakeVisibleMenu(resolvedDef, requesterId);

			if (visibleMenu != null)
			{
				this.nextActiveMenu = visibleMenu;

				PreloadedScene preloadedScene = visibleMenu.loadedScene;

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

					if (this.activeMenu != null &&
						(this.activeMenu.controller == null || this.activeMenu.controller.state != MenuSceneController.TransitionState.Out))
					{
						// Active menu doesn't need to transition out (i.e. this menu is an overlay or the same menu)
						this.activeMenu = null;
					}
					else
					{
						// Wait for any existing active menu to be cleared (after transitioning out)
						while (this.activeMenu != null)
						{
							yield return 0;
						}
					}

					if (this.nextActiveMenu != visibleMenu)
					{
						Debug.LogErrorFormat("Multiple menus were loaded simultaniously, resulting in a race.");
						yield break;
					}

					// NOTE(elliot): intentionally setting nextActiveMenu to null here regardless of wheter the next scene loaded successfully (so if it failed, nextActiveMenu is already reset). Local var visibleMenu is used to set the current active menu
					this.nextActiveMenu = null;

					Scene scene = SceneManager.GetSceneByPath(preloadedScene.scenePath);
					if (scene.IsValid() && scene.isLoaded)
					{
						this.activeMenu = visibleMenu;
						// Update navigation save data
						if (visibleMenu.def.type == MenuType.Root)
						{
							this.saveData.nav.currentRootMenu = visibleMenu;
						}

						Debug.LogFormat("Active menu is now {0}.", this.activeMenu.def);

						// Add / move this popup to the top of the stack
						int existingPopupIndex = this.popupStack.IndexOf(visibleMenu);
						if (existingPopupIndex == -1)
						{
							this.popupStack.Add(visibleMenu);
						}
						else if (existingPopupIndex < this.popupStack.Count-1)
						{
							this.popupStack.RemoveAt(existingPopupIndex);
							this.popupStack.Add(visibleMenu);
						}

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

						Nav.SetRootObjectsActive(scene, true);

						// Find controller
						MenuSceneController controller = null;
						// TODO(elliot): FindWithTag("SceneController") ?
						GameObject[] roots = scene.GetRootGameObjects();
						for (int rootObjectIndex = 0; rootObjectIndex < roots.Length; ++rootObjectIndex)
						{
							controller = roots[rootObjectIndex].GetComponent<MenuSceneController>();
							if (controller != null)
							{
								break;
							}
						}
						visibleMenu.controller = controller;

						if (visibleMenu.controller != null)
						{
							if (visibleMenu.controller.displayedMenu != visibleMenu.def)
							{
								MenuData sourceMenu;
								if (previousActiveMenu != null) {
										sourceMenu = previousActiveMenu.def;
								} else {
									sourceMenu = null;
								}
								visibleMenu.controller.TransitionIn(sourceMenu, visibleMenu.def);
							}
						}
					}
					else
					{
						Debug.LogErrorFormat("Failed to load menu scene {0} (menu {1}).", resolvedDef.menuScene, resolvedDef.name);
					}
				}
				else
				{
					Debug.LogErrorFormat("Failed to load menu scene {0} (menu {1}).", resolvedDef.menuScene, resolvedDef.name);
				}
			}
			else
			{
				Debug.LogErrorFormat("Failed to activate menu {0}.", resolvedDef.name);
			}
		}
		// Normally cleared immediately after load succeeded: clearing here in case load failed
		this.nextActiveMenu = null;

		if (this.activeMenu == null)
		{
			// TODO(elliot): save (save should use a circular auto-save slots system so if the game state is broken the player can still revert to an earlier save) & exit
			//Debug.LogError("Failed to load menu. Exiting.");
		}
	}

	void TransitionMenuOutWithoutDeactivating(VisibleMenu visibleMenu, MenuData nextMenu)
	{
		if (visibleMenu.controller != null)
		{
			visibleMenu.controller.TransitionOut(nextMenu, this.OnTransitionOutFinished, visibleMenu);
		}
		else
		{
			this.OnTransitionOutFinished(visibleMenu);
		}
	}

	void OnTransitionOutFinished(VisibleMenu visibleMenu)
	{
		if (visibleMenu == this.activeMenu)
		{
			this.activeMenu = null;
		}
	}

	void TransitionMenuOut(VisibleMenu visibleMenu, MenuData nextMenu)
	{
		if (visibleMenu.controller != null)
		{
			visibleMenu.controller.TransitionOut(nextMenu, this.DeactivatePopup, visibleMenu);
		}
		else
		{
			this.DeactivatePopup(visibleMenu);
		}
	}

	VisibleMenu GetTopPopup()
	{
		VisibleMenu topPopup;
		if (this.popupStack.Count > 0)
		{
			topPopup = this.popupStack[this.popupStack.Count-1];
		}
		else
		{
			topPopup = null;
		}
		return topPopup;
	}

	VisibleMenu TransitionOutOfTopPopup(MenuData transitioningTo = null)
	{
		VisibleMenu menuPopped = null;
		if (this.popupStack.Count > 0)
		{
			int index = this.popupStack.Count-1;
			menuPopped = this.popupStack[index];
			this.TransitionMenuOut(menuPopped, transitioningTo);
			this.popupStack.RemoveAt(index);
		}
		return menuPopped;
	}

	void RemoveOtherPopups(MenuData def)
	{
		int firstFreeIndex = this.popupStack.ExcludeAll(PopupHasOtherDef, def);
		for (int i = firstFreeIndex; i < this.popupStack.Count; ++i)
		{
			this.TransitionMenuOut(this.popupStack[i], null);
		}
		this.popupStack.RemoveRange(firstFreeIndex, this.popupStack.Count-firstFreeIndex);
	}

	static bool PopupHasOtherDef(VisibleMenu popup, MenuData def)
	{
		return popup.def != def;
	}

	void DeactivatePopupAt(int index)
	{
		if (index >= 0 && index < this.popupStack.Count)
		{
			// Deactivate the current popup
			VisibleMenu currentPopup = this.popupStack[index];

			PreloadedScene preloadedScene = currentPopup.loadedScene;
			if (preloadedScene != null)
			{
				Nav.SetRootObjectsActive(preloadedScene.scene, false);
				this.OnSceneHidden(preloadedScene);
			}
			else
			{
				Debug.LogErrorFormat("Visible menu {0} has no loaded scene", currentPopup.def);
			}
		}
	}

	void DeactivatePopup(VisibleMenu visibleMenu)
	{
		Debug.Assert(visibleMenu != null);
	#if UNITY_EDITOR
		if (visibleMenu != null)
		{
			Debug.LogFormat("Menu deactivated {0}", visibleMenu.def);
		}
#endif
            if (visibleMenu == this.activeMenu) {
                this.activeMenu = null;
            }
		int index = this.popupStack.IndexOf(visibleMenu);
		DeactivatePopupAt(index);
	}

	VisibleEnvScene waitingForSceneToBeDeactivated;

	IEnumerator GoToEnvSceneCoroutine(SceneData envScene, string requesterId)
	{
		if (!envScene.background)
		{
			Debug.Assert(this.waitingForSceneToBeDeactivated == null);
			if (this.waitingForSceneToBeDeactivated != null)
			{
				yield break;
			}
		}
		VisibleEnvScene visibleEnvScene = this.FindOrMakeVisibleEnvScene(envScene, requesterId);
		if (visibleEnvScene != null)
		{
			PreloadedScene preloadedScene = visibleEnvScene.loadedScene;

			if (preloadedScene.loadOp != null
				|| (preloadedScene.scene.IsValid() && preloadedScene.scene.isLoaded))
			{
				// NOTE(elliot): only one non-background env scene can be active at a time
				this.waitingForSceneToBeDeactivated = null;
				if (!envScene.background)
				{
					if (this.activeEnvScene != null)
					{
						this.waitingForSceneToBeDeactivated = this.activeEnvScene;
						Debug.LogFormat("Waiting for {0} to be deactivated", this.waitingForSceneToBeDeactivated.def);
						this.TransitionEnvSceneOut(this.activeEnvScene);
					}
				}

				if (preloadedScene.loadOp != null && !preloadedScene.loadOp.isDone)
				{
					preloadedScene.loadOp.allowSceneActivation = true;
					while (!preloadedScene.loadOp.isDone)
					{
						yield return 0;
					}
				}

				// Wait for the existing foreground scene to be deactivated
				if (!envScene.background)
				{
					while (this.activeEnvScene != null && this.activeEnvScene == this.waitingForSceneToBeDeactivated)
					{
						yield return 0;
					}
					this.waitingForSceneToBeDeactivated = null;
				}

				Scene scene = SceneManager.GetSceneByPath(preloadedScene.scenePath);
				if (scene.IsValid() && scene.isLoaded)
				{
					this.visibleEnvScenes.Add(visibleEnvScene);

					if (!envScene.background)
					{
						this.activeEnvScene = visibleEnvScene;
					}

					Nav.SetRootObjectsActive(scene, true);

					// Find controller
					EnvSceneController controller = null;
					// TODO(elliot): FindWithTag("SceneController") ?
					GameObject[] roots = scene.GetRootGameObjects();
					for (int rootObjectIndex = 0; rootObjectIndex < roots.Length; ++rootObjectIndex)
					{
						controller = roots[rootObjectIndex].GetComponent<EnvSceneController>();
						if (controller != null)
						{
							break;
						}
					}
					visibleEnvScene.controller = controller;

					if (visibleEnvScene.controller != null)
					{
						if (!envScene.background)
						{
							// NOTE(elliot): the active env scene defines the active env camera type
							this.ActivateEnvCamera(envScene.cameraType);
							CinemachineBrain cam = this.GetEnvCamera(envScene.cameraType);
							visibleEnvScene.controller.SetCamera(cam);
							visibleEnvScene.controller.SetGlobalTransitionAnimator(this.genericTransitionAnimator);
							visibleEnvScene.controller.TransitionIn(envScene);

							while (visibleEnvScene.controller.state == EnvSceneController.TransitionState.In)
							{
								yield return 0;
							}
						}
					}
				}
				else
				{
					Debug.LogErrorFormat("Failed to load env scene {0}", envScene);
				}
			}
			else
			{
				Debug.LogErrorFormat("Failed to load env scene {0}", envScene);
			}
		}
		else
		{
			Debug.LogErrorFormat("Failed to make the scene at {0} visible", envScene);
		}
	}

	void TransitionEnvSceneOut(VisibleEnvScene visibleEnvScene)
	{
		if (!visibleEnvScene.def.background && visibleEnvScene.controller != null)
		{
			visibleEnvScene.controller.TransitionOut(this.DeactivateEnvScene, visibleEnvScene);
		}
		else
		{
			this.DeactivateEnvScene(visibleEnvScene);
		}
	}

	void DeactivateEnvScene(VisibleEnvScene visibleEnvScene)
	{
		Debug.Assert(visibleEnvScene != null);
	#if UNITY_EDITOR
		if (visibleEnvScene != null)
		{
			Debug.LogFormat("Disabled env {0}", visibleEnvScene.def);
		}
	#endif
		if (visibleEnvScene == this.activeEnvScene)
		{
			this.activeEnvScene = null;
		}
		this.visibleEnvScenes.Remove(visibleEnvScene);
		Nav.SetRootObjectsActive(visibleEnvScene.loadedScene.scene, false);
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
				this.OnSceneHidden(loadedScene);
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
		VisibleMenu activeMenu = this.nextActiveMenu ?? this.activeMenu;
		if (activeMenu != null && activeMenu.def.envScene == envScene.def)
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
			Debug.LogFormat("Scene hidden {0}", hiddenScene.scenePath);

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
				Nav.SetRootObjectsActive(loadedScene.scene, false);

				Debug.LogFormat("Hid env scene {0} ({1})", visibleEnvScene.def, loadedScene.scenePath);
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

					Debug.LogFormat("Unloading scene {0}", otherPreloadedScene.scenePath);
				}
			}
		}
	}

	static void SetRootObjectsActive(Scene scene, bool active)
	{
		Debug.LogFormat("Setting scene active {0} = {1}", scene.path, active);
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
