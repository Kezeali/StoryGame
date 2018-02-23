using UnityEngine;
using NotABear;
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

	private class DataUserCollection<DataT>
	{
		static List<IDataUser<DataT>> toInitialise = new List<IDataUser<DataT>>();
		static List<IDataUser<DataT>> initialised = new List<IDataUser<DataT>>();

		public void Add(IDataUser<DataT> dataUser)
		{
		}

		public void Remove(IDataUser<DataT> dataUser)
		{
		}

		public void Initialise(DataT data)
		{
		}
	}

	static List<ISaveDataUser> saveDataUsersToInitalise = new List<ISaveDataUser>();
	static List<ISaveDataUser> saveDataUsers = new List<ISaveDataUser>();

	static List<IPlannerDataUser> plannerDataUsersToInitalise = new List<IPlannerDataUser>();
	static List<IPlannerDataUser> plannerDataUsers = new List<IPlannerDataUser>();

	public static void Register(ISaveDataUser saveGameUser)
	{
		saveDataUsersToInitalise.Add(saveGameUser);
	}

	public void Awake()
	{
		Object[] existingApps = Object.FindObjectsOfType(typeof(App));
		if (existingApps.Length > 0)
		{
			Object.Destroy(this.gameObject);
		}
		Object.DontDestroyOnLoad(this.gameObject);

		this.dataItemConverter = new DataItemConverter();
		this.dataItemConverter.AddDataItemRange(this.plannerData.items);
		this.dataItemConverter.AddDataItemRange(this.plannerData.characterStats);
		this.dataItemConverter.AddDataItemRange(this.plannerData.subjects);
		this.dataItemConverter.AddDataItemRange(this.plannerData.planActivities);

		string data = System.IO.File.ReadAllText("save.txt");

		// Serialiser.Deserialise(ref plan, data, dataItemSource);
		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(new CamelCaseNamingConvention())
			.WithTypeConverter(this.dataItemConverter)
			.Build();

		try
		{
			saveData = deserializer.Deserialize<SaveData>(data);
		}
		catch (System.Exception ex)
		{
			Debug.LogException(ex);
		}
		if (saveData == null)
		{
			saveData = this.defaultSave;
		}

		Object.Instantiate(this.cameraPrefab);
	}

	public void Start()
	{
		this.Initialise();
	}

	public void Initialise()
	{
		for (int i = 0; i < saveDataUsersToInitalise.Count; ++i)
		{
			ISaveDataUser saveDataUser = saveDataUsersToInitalise[i];
			saveDataUser.Initialise(this.saveData);
			saveDataUsers.Add(saveDataUser);
		}
		saveDataUsersToInitalise.Clear();

		Object[] planOptionSelectorObjects = Object.FindObjectsOfType(typeof(PlanOptionSelectorUI));
		for (int i = 0; i < planOptionSelectorObjects.Length; ++i)
		{
			var planOptionSelectorUI = planOptionSelectorObjects[i] as PlanOptionSelectorUI;
			planOptionSelectorUI.Initialise(this.plannerData);
		}

		Object[] planObjects = Object.FindObjectsOfType(typeof(PlanUI));
		for (int i = 0; i < planObjects.Length; ++i)
		{
			var planUI = planObjects[i] as PlanUI;
			planUI.Initialise(this.saveData);
		}

		// Object[] statsUis = Object.FindObjectsOfType(typeof(CharacterStatsCollectionUI));
		// for (int i = 0; i < statsUis.Length; ++i)
		// {
		// 	var statsUi = statsUis[i] as CharacterStatsCollectionUI;
		// 	statsUi.Initialise(this.saveData);
		// }

		Save();
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
}

public interface ISaveDataUser
{
	void Initialise(SaveData saveData);
}

public interface IPlannerDataUser
{
	void Initialise(PlannerData plannerData);
}

public interface IDataUser<DataT>
{
	void Initialise(DataT data);
}
