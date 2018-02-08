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

	DataItemSource dataItemSource;

	DataItemConverter dataItemConverter;

	PlanOptionsLoadout loadout;
	SaveData saveData;

	public void Awake()
	{
		dataItemSource = new DataItemSource();
		dataItemSource.AddDataItemRange(this.plannerData.items);

		this.dataItemConverter = new DataItemConverter();
		this.dataItemConverter.AddDataItemRange(this.plannerData.items);

		this.loadout = new PlanOptionsLoadout();
		this.loadout.name = "test";

		this.loadout.planOptions = new List<PlanOption>(this.plannerData.items.Length);

		for (int i = 0; i < this.plannerData.items.Length; ++i)
		{
			PlannerItemData item = this.plannerData.items[i];
			PlanOption option = new PlanOption();
			option.data = item;
			this.loadout.planOptions.Add(option);
		}

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
			planOptionSelectorUI.Initialise(this.loadout);
		}

		Object[] planObjects = Object.FindObjectsOfType(typeof(PlanUI));
		for (int i = 0; i < planObjects.Length; ++i)
		{
			var planUI = planObjects[i] as PlanUI;
			planUI.Initialise(this.saveData);
		}

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
