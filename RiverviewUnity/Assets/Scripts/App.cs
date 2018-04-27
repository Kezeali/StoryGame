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
			for (int i = 0; i < this.toInitialise.Count; ++i)
			{
			#if UNITY_EDITOR
				var component = this.toInitialise[i] as MonoBehaviour;
				if (component != null)
				{
					if (!component.isActiveAndEnabled)
					{
						continue;
					}
				}
			#endif
				this.toInitialise[i].Initialise(data);
			}
			this.initialised.AddRange(this.toInitialise);
			this.toInitialise.Clear();
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
			for (int i = 0; i < this.toInitialise.Count; ++i)
			{
			#if UNITY_EDITOR
				var component = this.toInitialise[i] as MonoBehaviour;
				if (component != null)
				{
					if (!component.isActiveAndEnabled)
					{
						continue;
					}
				}
			#endif
				this.toInitialise[i].CompleteInitialisation();
			}
			this.initialised.AddRange(this.toInitialise);
			this.toInitialise.Clear();
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

		Debug.LogFormat("Data user added: {0}", user);

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

			Debug.LogFormat("Data user removed: {0}", user);
		}
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

		{
			string data = System.IO.File.ReadAllText("save.txt");

			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(new CamelCaseNamingConvention())
				.WithTypeConverter(this.dataItemConverter)
				.WithTypeInspector(this.unitySerialisationTypeInspectorConstructor)
				.IgnoreUnmatchedProperties()
				.Build();

			try
			{
				this.saveData = deserializer.Deserialize<SaveData>(data);
			}
			catch (System.Exception ex)
			{
				Debug.LogException(ex);
			}
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
		}

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

		// Let nav load immediately
		this.nav.Initialise(this.saveData);
	}

	public void Start()
	{
		this.Initialise();
		this.waitingForInit = false;

		Save();
	}

	public void OnDestroy()
	{
		userCollections.Clear();
	}

	public void Initialise()
	{
		Debug.Log("Initialise()");
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

	public void OnApplicationFocus(bool focus)
	{
		if (!focus)
		{
			Save();
		}
	}

	public void OnApplicationQuit()
	{
		Save();
	}

	public void Save()
	{
		Save(this.saveData, this.dataItemConverter, this.unitySerialisationTypeInspectorConstructor);
	}

	public static void Save(SaveData saveData, DataItemConverter dataItemConverter, System.Func<ITypeInspector, ITypeInspector> unitySerialisationTypeInspectorConstructor)
	{
		try
		{
			using (var buffer = new System.IO.StringWriter())
			{
				var serializer = new SerializerBuilder()
					.EnsureRoundtrip()
					.EmitDefaults()
					.WithTypeConverter(dataItemConverter)
					.WithTypeInspector(unitySerialisationTypeInspectorConstructor)
					.Build();
				
				serializer.Serialize(buffer, saveData, typeof(SaveData));

				System.IO.File.WriteAllText("save.txt", buffer.ToString());
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	public void OpenScene(string sceneName)
	{
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