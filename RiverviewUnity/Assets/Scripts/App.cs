using UnityEngine;
using NotABear;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;

public class App : MonoBehaviour
{
	[SerializeField]
	private GameObject cameraPrefab;

	[SerializeField]
	private PlannerData plannerData;

	[SerializeField]
	private SaveData defaultSave;

	DataItemConverter dataItemConverter;

	SaveData saveData;

	public static App instance;

	private class DataUserCollection<DataT>
	{
		List<IDataUser<DataT>> toInitialise = new List<IDataUser<DataT>>();
		List<IDataUser<DataT>> initialised = new List<IDataUser<DataT>>();

		public void Add(IDataUser<DataT> dataUser)
		{
			this.toInitialise.Add(dataUser);
		}

		public void Remove(IDataUser<DataT> dataUser)
		{
			bool removed = this.initialised.Remove(dataUser);
			if (!removed)
			{
				this.toInitialise.Remove(dataUser);
			}
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

		if (instance != null)
		{
			instance.DelayInit();
		}
	}

	public void Awake()
	{
		Object[] existingApps = Object.FindObjectsOfType(typeof(App));
		if (existingApps.Length > 1)
		{
			Object.Destroy(this.gameObject);
		}
		Object.DontDestroyOnLoad(this.gameObject);

		instance = this;

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

		Object.Instantiate(this.cameraPrefab);
	}

	public void Start()
	{
		this.Initialise();

		Save();
	}

	public void Initialise()
	{
		this.InitialiseDataUsers(this.saveData);
		this.InitialiseDataUsers(this.plannerData);
	}

	void DelayInit()
	{
		this.StartCoroutine(this.DelayInitCoroutine());
	}

	IEnumerator DelayInitCoroutine()
	{
		yield return new WaitForEndOfFrame();
		Initialise();
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
