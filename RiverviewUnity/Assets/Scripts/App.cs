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
	PlannerDataIndex plannerData;

	[SerializeField]
	DataIndex dataIndex;

	[SerializeField]
	NavDataIndex navData;

	[SerializeField]
	DefaultSaveData defaultSaveData;

	[SerializeField]
	DefaultSaveData[] editorSaves;

	[SerializeField]
	PlanExecutor planExecutor;

	[System.NonSerialized]
	public List<string> saveFilesAvailableForCurrentProfile = new List<string>();

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
			this.Remove(user);
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
	#if UNITY_EDITOR
		// Ignore service users being registered by objects in scenes that have just been preloaded
		var monobehaviour = user as MonoBehaviour;
		if (monobehaviour && !InitialisedScenes.IsInitialised(monobehaviour.gameObject.scene)) {
			Debug.LogWarningFormat("App.Register: Ignoring service user: {0}, scene '{1}': The containing scene is not yet initialised.\nIt is normal for this to occur in the editor whenever scenes are preloaded, but may cause unexpected behaviour so keep an eye out!", user, monobehaviour.gameObject.scene.name);
			return;
		}
	#endif
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
			instance.DelayedInit();
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
		this.dataItemConverter.AddDataItemRange(this.plannerData.calendars);
		this.dataItemConverter.AddDataItemRange(this.plannerData.subjects);
		this.dataItemConverter.AddDataItemRange(this.plannerData.planActivities);
		this.dataItemConverter.AddDataItemRange(this.plannerData.events);
		this.dataItemConverter.AddDataItemRange(this.plannerData.stageMarks);
		this.dataItemConverter.AddDataItemRange(this.dataIndex.roles);
		this.dataItemConverter.AddDataItemRange(this.dataIndex.outfitItems);
		this.dataItemConverter.AddDataItemRange(this.dataIndex.qualities);
		this.dataItemConverter.AddDataItemRange(this.dataIndex.characterStats);
		this.dataItemConverter.AddDataItemRange(this.dataIndex.statBonuses);
		this.dataItemConverter.AddDataItemRange(this.navData.menus);
		this.dataItemConverter.AddDataItemRange(this.navData.envScenes);
		this.dataItemConverter.AddDataItemRange(this.navData.commutes);

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
			this.LoadUserProfileInternal(this.appData.selectedProfileName);
		}
		if (this.profileData == null)
		{
			this.profileData = new ProfileSaveData();
		}
		SavingStuff.SetDefault(ref this.profileData.name, "Player");
		SavingStuff.SetDefault(ref this.profileData.selectedSaveName, "0");

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
		Debug.Log("Initialising service users.");
		this.InitialiseServiceUsers(this.nav);
		this.InitialiseServiceUsers(this.plannerData);
		this.InitialiseServiceUsers(this.dataIndex);
		this.InitialiseServiceUsers(this.planExecutor);
		if (this.profileData != null)
		{
			this.InitialiseServiceUsers(this.profileData);
		}
		if (this.saveData != null)
		{
			this.InitialiseServiceUsers(this.saveData);
		}
		this.CompleteInitialisation();
	}

	void DelayedInit()
	{
		if (!this.waitingForInit)
		{
			this.StartCoroutine(this.DelayedInitCoroutine());
		}
	}

	IEnumerator DelayedInitCoroutine()
	{
		Debug.Log("DelayedInit Started.");
		this.waitingForInit = true;
		yield return new WaitForEndOfFrame();
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
		this.LoadUserProfileInternal(name);
		if (this.profileData != null)
		{
			this.ReInitService(this.profileData);
		}
	}

	void LoadUserProfileInternal(string name)
	{
		if (this.appData != null)
		{
			SavingStuff.Load(this.appData.selectedProfileName, out this.profileData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);

			SavingStuff.FindSaveGames(this.saveFilesAvailableForCurrentProfile, this.appData.selectedProfileName);
		}
	}

	public void LoadGame(string saveName = null)
	{
		Debug.LogFormat("Loading saved game '{0}'", saveName ?? this.profileData.selectedSaveName);

		this.StopAllCoroutines();

		if (!string.IsNullOrEmpty(saveName)) {
			this.profileData.selectedSaveName = saveName;
		}
		
		this.LoadGameInternal();

		Debug.Log("Loading Nav");
		this.nav.Load(this.saveData);

		this.ReInitService(this.saveData);
	}

	IEnumerator DelayReInitSaveDataCoroutine()
	{
		Debug.Log("DelayReInitSaveData Started");
		this.waitingForInit = true;
		yield return 0;
		this.waitingForInit = false;
		this.ReInitService(this.saveData);
	}

	void ReInitService<ServiceT>(ServiceT service)
	{
		Debug.LogFormat("Re-initialising {0} service users", service);
		this.ReInitialiseServiceUsers(service);
		this.ReCompleteInitialisation();
	}

	void LoadGameInternal()
	{
		Debug.Log("Load save data");

		if (this.profileData != null)
		{
			string saveFileName = SavingStuff.DetermineGameFileName(this.profileData.name, this.profileData.selectedSaveName);
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
		this.saveData.cast.PostLoadCleanup(this.dataIndex);
		this.saveData.cast.FixReferences();

		this.nav.SetSaveData(this.saveData);
	}

	public void NewGame(string name = null)
	{
		Debug.Log("Create new save from default data");
		if (this.profileData != null)
		{
			name = name ?? saveFilesAvailableForCurrentProfile.Count.ToString();

			SaveData newSaveData = this.defaultSaveData.saveData;
			newSaveData.name = name;

			string saveFileName = SavingStuff.DetermineGameFileName(this.profileData.name, newSaveData.name);

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
				string saveFileName = SavingStuff.DetermineGameFileName(this.profileData.name, this.saveData.name);
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

public interface IPlanExecutorController
{
	void ReceiveExecutor(PlanExecutor executor);
}

}