using UnityEngine;
using NotABear;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace NotABear
{

public class App : MonoBehaviour
{
	[SerializeField]
	private GameObject cameraPrefab;

	[SerializeField]
	private Nav nav;

	[SerializeField]
	private PlannerData plannerData;

	[SerializeField]
	private SaveData defaultSave;

	DataItemConverter dataItemConverter;

	SaveData saveData;

	bool waitingForInit = true;

	public static App instance;

	private class DataUserCollection<DataT>
	{
		List<IDataUser<DataT>> toInitialise = new List<IDataUser<DataT>>();
		List<IDataUser<DataT>> initialised = new List<IDataUser<DataT>>();

		public void Add(IDataUser<DataT> dataUser)
		{
			Remove(dataUser);
			this.toInitialise.Add(dataUser);
		}

		public void Remove(IDataUser<DataT> dataUser)
		{
			this.initialised.Remove(dataUser);
			this.toInitialise.Remove(dataUser);
		}

		public void Initialise(DataT data)
		{
			for (int i = 0; i < this.toInitialise.Count; ++i)
			{
				this.toInitialise[i].Initialise(data);
			}
			this.initialised.AddRange(this.toInitialise);
			this.toInitialise.Clear();
		}
	}

	static Dictionary<System.Type, object> dataUserCollections = new Dictionary<System.Type, object>();

	public static void Register<DataT>(IDataUser<DataT> dataUser)
	{
		DataUserCollection<DataT> collection = null;
		object value;
		if (!dataUserCollections.TryGetValue(typeof(DataT), out value))
		{
			collection = new DataUserCollection<DataT>();
			dataUserCollections.Add(typeof(DataT), collection);
		}
		else
		{
			collection = value as DataUserCollection<DataT>;
		}
		collection.Add(dataUser);

		Debug.LogFormat("Data user added: {0}", dataUser);

		if (instance != null)
		{
			instance.DelayInit();
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

		Debug.Assert(this.cameraPrefab != null);
		Debug.Assert(this.nav != null);

		this.dataItemConverter = new DataItemConverter();
		this.dataItemConverter.AddDataItemRange(this.plannerData.items);
		this.dataItemConverter.AddDataItemRange(this.plannerData.characterStats);
		this.dataItemConverter.AddDataItemRange(this.plannerData.subjects);
		this.dataItemConverter.AddDataItemRange(this.plannerData.planActivities);

		string data = System.IO.File.ReadAllText("save.txt");

		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(new CamelCaseNamingConvention())
			.WithTypeConverter(this.dataItemConverter)
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
			this.saveData = this.defaultSave;
		}
		if (this.saveData.pc == null)
		{
			this.saveData.pc = this.defaultSave.pc;
		}
		this.saveData.pc.CalculateStatus();

		Object.Instantiate(this.cameraPrefab, this.transform);
	}

	public void Start()
	{
		this.Initialise();
		this.waitingForInit = false;

		Save();
	}

	public void Initialise()
	{
		Debug.Log("Initialise()");
		this.InitialiseDataUsers(this.saveData);
		this.InitialiseDataUsers(this.plannerData);
		this.InitialiseDataUsers(this.nav);
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

	DataUserCollection<DataT> GetDataUserCollection<DataT>()
	{
		DataUserCollection<DataT> collection = null;
		object value;
		if (dataUserCollections.TryGetValue(typeof(DataT), out value))
		{
			collection = value as DataUserCollection<DataT>;
		}
		return collection;
	}

	void InitialiseDataUsers<DataT>(DataT data)
	{
		var dataUsers = this.GetDataUserCollection<DataT>();
		if (dataUsers != null)
		{
			dataUsers.Initialise(data);
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
		Save(this.saveData, this.dataItemConverter);
	}

	public static void Save(SaveData saveData, DataItemConverter dataItemConverter)
	{
		try
		{
			using (var buffer = new System.IO.StringWriter())
			{
				var serializer = new SerializerBuilder()
					.EnsureRoundtrip()
					.EmitDefaults()
					.WithTypeConverter(dataItemConverter)
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

public interface IDataUser<DataT>
{
	void Initialise(DataT data);
}

}