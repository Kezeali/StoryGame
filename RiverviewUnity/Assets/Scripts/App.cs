using UnityEngine;
using Cloverview;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Cinemachine;

namespace Cloverview
{

public class App : MonoBehaviour
{
	[SerializeField]
	GameObject menuCameraPrefab;

	[SerializeField]
	GameObject envTransitionCameraPrefab;

	[System.Serializable]
	public struct EnvCameraPrefabDefinition
	{
		public EnvCameraType type;
		public GameObject prefab;
	}
	[SerializeField]
	EnvCameraPrefabDefinition[] envCameraPrefabs;

	[SerializeField]
	Nav nav;

	[SerializeField]
	PlannerData plannerData;

	[SerializeField]
	GameData gameData;

	[SerializeField]
	DefaultSaveData defaultSaveData;

	DataItemConverter dataItemConverter;
	System.Func<ITypeInspector, ITypeInspector> unitySerialisationTypeInspectorConstructor;

	AppSaveData appData;
	ProfileSaveData profileData;
	SaveData saveData;

	bool waitingForInit = true;

	CinemachineBrain envCameraCinemachineBrain;

	public static App instance;

	private class ServiceUserCollection<ServiceT>
	{
		List<IServiceUser<ServiceT>> toInitialise = new List<IServiceUser<ServiceT>>();
		List<IServiceUser<ServiceT>> initialised = new List<IServiceUser<ServiceT>>();

		public void Add(IServiceUser<ServiceT> user)
		{
			Remove(user);
			this.toInitialise.Add(user);
		}

		public void Remove(IServiceUser<ServiceT> user)
		{
			this.initialised.Remove(user);
			this.toInitialise.Remove(user);
		}

		public void Initialise(ServiceT data)
		{
			this.toInitialise.RemoveAll(IsNull);
			this.initialised.RemoveAll(IsNull);

			// Copy everything over to initialised & clear toInitilise before calling anything, as code called from the other types here can add more things to toInitialise which shouldn't be initialised yet
			int i = this.initialised.Count;
			this.initialised.AddRange(this.toInitialise);
			this.toInitialise.Clear();

			for (; i < this.initialised.Count; ++i)
			{
			#if UNITY_EDITOR
				// Due to scene post-processing running a little late in the editor (after OnEnabled has been called for objects in the scene), need to check that the to-initialise object is still enabled!
				var component = this.initialised[i] as MonoBehaviour;
				if (component != null)
				{
					if (!component.isActiveAndEnabled)
					{
						continue;
					}
				}
				Debug.LogFormat("Initialising {0} with {1}", this.initialised[i], data);
			#endif
				this.initialised[i].Initialise(data);
			}
		}

		static bool IsNull(IServiceUser<ServiceT> v)
		{
			return v == null;
		}

		public void ReInit(ServiceT data)
		{
			this.toInitialise.AddRange(this.initialised);
			this.initialised.Clear();
			this.Initialise(data);
		}
	}

	private class ServiceUserCollection
	{
		List<IServiceUser> toInitialise = new List<IServiceUser>();
		List<IServiceUser> initialised = new List<IServiceUser>();

		public void Add(IServiceUser user)
		{
			Remove(user);
			this.toInitialise.Add(user);
		}

		public void Remove(IServiceUser user)
		{
			this.initialised.Remove(user);
			this.toInitialise.Remove(user);
		}

		public void CompleteInitialisation()
		{
			this.toInitialise.RemoveAll(IsNull);
			this.initialised.RemoveAll(IsNull);

			// Copy everything over to initialised & clear toInitilise before calling anything, as code called from the other types here can add more things to toInitialise which shouldn't be called yet
			int i = this.initialised.Count;
			this.initialised.AddRange(this.toInitialise);
			this.toInitialise.Clear();

			for (; i < this.initialised.Count; ++i)
			{
			#if UNITY_EDITOR
				// Due to scene post-processing running a little late in the editor (after OnEnabled has been called for objects in the scene), need to check that the to-initialise object is still enabled!
				var component = this.initialised[i] as MonoBehaviour;
				if (component != null)
				{
					if (!component.isActiveAndEnabled)
					{
						continue;
					}
				}
			#endif
				this.initialised[i].CompleteInitialisation();
			}
		}

		static bool IsNull(IServiceUser v)
		{
			return v == null;
		}

		public void ReCompleteInitialisation()
		{
			this.toInitialise.AddRange(this.initialised);
			this.initialised.Clear();
			this.CompleteInitialisation();
		}
	}

	static Dictionary<System.Type, object> userCollections = new Dictionary<System.Type, object>();
	// Used to call the initialisation complete method
	static ServiceUserCollection allServiceUsers = new ServiceUserCollection();

	public static void Register<ServiceT>(IServiceUser<ServiceT> user)
	{
		{
			ServiceUserCollection<ServiceT> collection = null;
			object value;
			if (!userCollections.TryGetValue(typeof(ServiceT), out value))
			{
				collection = new ServiceUserCollection<ServiceT>();
				userCollections.Add(typeof(ServiceT), collection);
			}
			else
			{
				collection = value as ServiceUserCollection<ServiceT>;
			}
			collection.Add(user);
		}

		allServiceUsers.Add(user);

		Debug.LogFormat("Service user added: {0}", user);

		if (instance != null)
		{
			instance.DelayInit();
		}
	}

	public static void Deregister<ServiceT>(IServiceUser<ServiceT> user)
	{
		ServiceUserCollection<ServiceT> collection = null;
		object value;
		if (userCollections.TryGetValue(typeof(ServiceT), out value))
		{
			collection = value as ServiceUserCollection<ServiceT>;
			collection.Remove(user);

			Debug.LogFormat("Service user removed: {0}", user);
		}

		allServiceUsers.Remove(user);
	}

	static ServiceUserCollection<ServiceT> GetServiceUserCollection<ServiceT>()
	{
		ServiceUserCollection<ServiceT> collection = null;
		object value;
		if (userCollections.TryGetValue(typeof(ServiceT), out value))
		{
			collection = value as ServiceUserCollection<ServiceT>;
		}
		return collection;
	}

	List<PlanExecutor> executingExecutors = new List<PlanExecutor>();
	struct PlanExecutorUser
	{
		public string executorId;
		public IServiceUser<PlanExecutor> user;
	}
	List<PlanExecutorUser> planExecutorUsers = new List<PlanExecutorUser>();
	public void AddExecutor(PlanExecutor executor)
	{
		if (executor == null)
		{
			return;
		}
		bool foundExisting = false;
		for (int i = 0; i < this.executingExecutors.Count; ++i)
		{
			if (this.executingExecutors[i] != null)
			{
				if (this.executingExecutors[i].id == executor.id)
				{
					foundExisting = true;
					break;
				}
			}
		}
		if (!foundExisting)
		{
			this.executingExecutors.Add(executor);
			for (int i = this.planExecutorUsers.Count-1; i >= 0; --i)
			{
				if (this.planExecutorUsers[i].user != null)
				{
					if (this.planExecutorUsers[i].executorId == executor.id)
					{
						this.planExecutorUsers[i].user.Initialise(executor);
						this.planExecutorUsers.RemoveAt(i);
					}
				}
			}
		}
		else
		{
			Debug.LogErrorFormat("Tried to add plan executor with duplicate ID {0}", executor.id);
		}
	}

	public void RemoveExecutor(PlanExecutor executor)
	{
		for (int i = this.executingExecutors.Count-1; i >= 0 ; --i)
		{
			if (this.executingExecutors[i] != null)
			{
				if (this.executingExecutors[i] == executor)
				{
					this.executingExecutors.RemoveAt(i);
				}
			}
		}
	}

	public void GetExecutor(string id, IServiceUser<PlanExecutor> user)
	{
		PlanExecutor executor = null;
		for (int i = 0; i < this.executingExecutors.Count; ++i)
		{
			if (this.executingExecutors[i] != null)
			{
				if (this.executingExecutors[i].id == id)
				{
					executor = this.executingExecutors[i];
					break;
				}
			}
		}
		if (executor != null)
		{
			user.Initialise(executor);
		}
		else
		{
			PlanExecutorUser entry = new PlanExecutorUser()
			{
				executorId = id,
				user = user
			};
			this.planExecutorUsers.Add(entry);
		}
	}

	public void CancelRequestForExecutor(string id, IServiceUser<PlanExecutor> user)
	{
		for (int i = this.planExecutorUsers.Count-1; i >= 0; --i)
		{
			if (this.planExecutorUsers[i].executorId == id && this.planExecutorUsers[i].user == user)
			{
					this.planExecutorUsers.RemoveAt(i);
			}
		}
	}

	void TidyUpExecutors()
	{
		for (int i = this.executingExecutors.Count-1; i >= 0 ; --i)
		{
			if (this.executingExecutors[i] == null)
			{
				this.executingExecutors.RemoveAt(i);
			}
		}
	}

	public void Awake()
	{
	#if !UNITY_EDITOR
		Debug.Assert(instance == null);
	#else
		if (instance != null)
		{
			Object.DestroyImmediate(this.gameObject);
			return;
		}
	#endif
		Object.DontDestroyOnLoad(this.gameObject);

		instance = this;

		Debug.Assert(this.menuCameraPrefab != null);
		Debug.Assert(this.envTransitionCameraPrefab != null);
		Debug.Assert(this.envCameraPrefabs != null);
		Debug.Assert(this.nav != null);
		Debug.Assert(this.defaultSaveData != null);

		this.dataItemConverter = new DataItemConverter();
		this.dataItemConverter.AddDataItemRange(this.plannerData.items);
		this.dataItemConverter.AddDataItemRange(this.plannerData.characterStats);
		this.dataItemConverter.AddDataItemRange(this.plannerData.subjects);
		this.dataItemConverter.AddDataItemRange(this.plannerData.planActivities);
		this.dataItemConverter.AddDataItemRange(this.gameData.roles);

		this.unitySerialisationTypeInspectorConstructor = (inner) => { return new UnitySerialisationTypeInspector(inner); };

		// NOTE(elliot): Cameras are created here!
		Object.Instantiate(this.menuCameraPrefab, this.transform);
		Object.Instantiate(this.envTransitionCameraPrefab, this.transform);
		// Init the env cameras and pass the instances to Nav
		for (int i = 0; i < this.envCameraPrefabs.Length; ++i)
		{
			EnvCameraPrefabDefinition def = this.envCameraPrefabs[i];

			GameObject envCamera = Object.Instantiate(def.prefab, this.transform);
			var envCameraCinemachineBrain = envCamera.GetComponent<CinemachineBrain>();
			envCamera.SetActive(false);

			this.nav.SetEnvCamera(def.type, envCameraCinemachineBrain);
		}

		// NOTE: Load/create App Data
		if (SavingStuff.SaveExists("global"))
		{
			this.LoadAppData();
		}
		if (this.appData == null)
		{
			this.appData = new AppSaveData();
		}
		SavingStuff.SetDefault(ref this.appData.selectedProfileName, "Player");

		// NOTE: Load/create player profile
		if (SavingStuff.SaveExists(this.appData.selectedProfileName))
		{
			this.LoadUserProfile(this.appData.selectedProfileName);
		}
		if (this.profileData == null)
		{
			this.profileData = new ProfileSaveData();
		}
		SavingStuff.SetDefault(ref this.profileData.name, "Player");
		SavingStuff.SetDefault(ref this.profileData.selectedSaveName, "1");

	#if UNITY_EDITOR
		this.nav.DetermineBootScene();
		if (!this.nav.loadedInActualBootScene)
		{
			// Load a save file so the scene being tested can initialise
			this.LoadGameInternal();
		}
	#endif
	}

	public void Start()
	{
		this.Initialise();
		this.waitingForInit = false;

		this.Save();
	}

	public void OnDestroy()
	{
		userCollections.Clear();
	}

	public void Initialise()
	{
		Debug.Log("Initialise");
		this.InitialiseServiceUsers(this.saveData);
		this.InitialiseServiceUsers(this.plannerData);
		this.InitialiseServiceUsers(this.nav);
		this.CompleteInitialisation();
	}

	void DelayInit()
	{
		if (!this.waitingForInit)
		{
			this.StartCoroutine(this.DelayInitCoroutine());
		}
	}

	IEnumerator DelayInitCoroutine()
	{
		Debug.Log("DelayInit Started");
		this.waitingForInit = true;
		yield return 0;
		this.waitingForInit = false;
		this.Initialise();
	}

	void InitialiseServiceUsers<ServiceT>(ServiceT data)
	{
		var usersCollection = App.GetServiceUserCollection<ServiceT>();
		if (usersCollection != null)
		{
			usersCollection.Initialise(data);
		}
	}

	void CompleteInitialisation()
	{
		var usersCollection = App.allServiceUsers;
		if (usersCollection != null)
		{
			usersCollection.CompleteInitialisation();
		}
	}

	void ReInitialiseServiceUsers<ServiceT>(ServiceT data)
	{
		var usersCollection = App.GetServiceUserCollection<ServiceT>();
		if (usersCollection != null)
		{
			usersCollection.ReInit(data);
		}
	}

	void ReCompleteInitialisation()
	{
		var usersCollection = App.allServiceUsers;
		if (usersCollection != null)
		{
			usersCollection.ReCompleteInitialisation();
		}
	}

	public void OnApplicationFocus(bool focus)
	{
		if (!focus)
		{
			this.Save();
		}
	}

	public void OnApplicationQuit()
	{
		this.Save();
	}

	void LoadAppData()
	{
		// Load values used at the app level, before even selecting a profile.
		SavingStuff.Load("global", out this.appData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);
	}

	public void NewUserProfile(string name)
	{
		this.appData.selectedProfileName = name;
		this.Save();
	}

	public void DeleteUserProfile(string name)
	{
		Debug.LogError("Not implemented");
	}

	public void LoadUserProfile(string name)
	{
		// Load values needed for the main menu, like control options and volume
		if (this.appData != null)
		{
			SavingStuff.Load(this.appData.selectedProfileName, out this.profileData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);
		}
	}

	public void LoadGame()
	{
		Debug.Log("Loading saved game");

		this.StopAllCoroutines();
		
		this.LoadGameInternal();

		Debug.Log("Loading Nav");
		this.nav.Load(this.saveData);

		this.StartCoroutine(this.DelayReInitSaveDataCoroutine());
	}

	IEnumerator DelayReInitSaveDataCoroutine()
	{
		Debug.Log("DelayReInitSaveData Started");
		this.waitingForInit = true;
		yield return 0;
		this.waitingForInit = false;
		Debug.Log("Re-initialising other save-data service users");
		this.ReInitialiseServiceUsers(this.saveData);
		this.ReCompleteInitialisation();
	}

	void LoadGameInternal()
	{
		Debug.Log("Load save data");

		if (this.profileData != null)
		{
			string saveFileName = ProfileSaveData.DetermineSaveFileName(this.profileData.name, this.profileData.selectedSaveName);
			SavingStuff.Load(saveFileName, out this.saveData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);

			this.InitSave();
		}
		else
		{
			Debug.LogError("Can't load game save without profile data!");
		}
	}

	void InitSave()
	{
		if (this.saveData == null)
		{
			this.saveData = this.defaultSaveData.saveData;
		}
		if (this.saveData.pc == null)
		{
			this.saveData.pc = this.defaultSaveData.saveData.pc;
		}
		if (this.saveData.leadNpcs == null)
		{
			this.saveData.leadNpcs = new List<Character>();
		}
		this.saveData.pc.PostLoadCleanup();
		this.saveData.pc.CalculateStatus();

		this.nav.SetSaveData(this.saveData);
	}

	public void NewGame()
	{
		Debug.Log("Create new save from default data");
		if (this.profileData != null)
		{
			SaveData newSaveData = this.defaultSaveData.saveData;
			string saveFileName = ProfileSaveData.DetermineSaveFileName(this.profileData.name, newSaveData.name);

			this.profileData.selectedSaveName = newSaveData.name;

			SavingStuff.Save(saveFileName, newSaveData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);

			this.LoadGameInternal();
		}
	}

	public void Save()
	{
		if (this.appData != null)
		{
			SavingStuff.Save("global", this.appData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);
		}
		if (this.profileData != null)
		{
			if (this.saveData != null)
			{
				string saveFileName = ProfileSaveData.DetermineSaveFileName(this.profileData.name, this.saveData.name);
				SavingStuff.Save(saveFileName, this.saveData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);

				this.profileData.selectedSaveName = this.saveData.name;
			}

			SavingStuff.Save(this.profileData.name, this.profileData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);
		}
	}
}

public interface IServiceUser<ServiceT> : IServiceUser
{
	void Initialise(ServiceT data);
}

public interface IServiceUser
{
	void CompleteInitialisation();
}

}