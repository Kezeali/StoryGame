using UnityEngine;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Cloverview
{
public static class SavingStuff
{
	public static void SetDefault<T>(ref T field, T value)
	{
		if (EqualityComparer<T>.Default.Equals(field, default(T)))
		{
			field = value;
		}
	}

	public static bool SaveExists(string fileName)
	{
		return System.IO.File.Exists(fileName + ".txt");
	}

	public static void Save<SaveDataT>(string fileName, SaveDataT saveData, DataItemConverter dataItemConverter, System.Func<ITypeInspector, ITypeInspector> extraTypeInspector)
	{
		try
		{
			using (var buffer = new System.IO.StringWriter())
			{
				var serializer = new SerializerBuilder()
					.EnsureRoundtrip()
					.EmitDefaults()
					.WithTypeConverter(dataItemConverter)
					.WithTypeInspector(extraTypeInspector)
					.Build();
				
				serializer.Serialize(buffer, saveData, typeof(SaveDataT));

				System.IO.File.WriteAllText(fileName + ".txt", buffer.ToString());
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	public static void Load<SaveDataT>(string fileName, out SaveDataT saveData, DataItemConverter dataItemConverter, System.Func<ITypeInspector, ITypeInspector> extraTypeInspector)
	{
		string data = System.IO.File.ReadAllText(fileName + ".txt");

		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(new CamelCaseNamingConvention())
			.WithTypeConverter(dataItemConverter)
			.WithTypeInspector(extraTypeInspector)
			.IgnoreUnmatchedProperties()
			.Build();

		try
		{
			saveData = deserializer.Deserialize<SaveDataT>(data);
		}
		catch (System.Exception ex)
		{
			Debug.LogException(ex);
			saveData = default(SaveDataT);
		}
	}
}
}
