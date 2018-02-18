using UnityEngine;
using NotABear;
using System.Collections.Generic;
using YamlDotNet.Serialization;

public class App : MonoBehaviour
{
	[SerializeField]
	private PlannerData plannerData;

	[SerializeField]
	private SaveData defaultSave;

	DataItemConverter dataItemConverter;

	SaveData saveData;

	public void Awake()
	{
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
		catch
		{
			saveData = this.defaultSave;
		}
	}

	public void Start()
	{
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
}
