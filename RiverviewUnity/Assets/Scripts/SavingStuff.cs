using UnityEngine;
using System.IO;
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
		return File.Exists(SavePath(fileName));
	}

	public static string DetermineGameFileName(string profileName, string saveName)
	{
		return Path.Combine(profileName, saveName);
	}

	public static string SavePath(string fileName)
	{
		string savePath = Path.Combine(Application.persistentDataPath, fileName + ".txt");
		return savePath;
	}

	public static string SavePath(string profileName, string fileName)
	{
		string savePath = Path.Combine(Application.persistentDataPath, profileName, fileName + ".txt");
		return savePath;
	}

	public static void FindSaveGames(List<string> result, string profileFileName)
	{
		string savePath = Path.Combine(Application.persistentDataPath, profileFileName);

		if (!Directory.Exists(savePath))
		{
			Directory.CreateDirectory(savePath);
		}

		if (result != null)
		{
			result.Clear();
			string[] files = Directory.GetFiles(savePath);
			for (int i = 0; i < files.Length; ++i)
			{
				if (files[i].EndsWith(".txt")) {
					result.Add(files[i]);
				}
			}
		}
		else
		{
			Debug.LogError("Argument must not be null: result");
		}
	}

	public static void Save<SaveDataT>(string fileName, SaveDataT saveData, DataItemConverter dataItemConverter, System.Func<ITypeInspector, ITypeInspector> extraTypeInspector)
	{
		try
		{
			using (var buffer = new StringWriter())
			{
				var serializer = new SerializerBuilder()
					.EnsureRoundtrip()
					.EmitDefaults()
					.WithTypeConverter(dataItemConverter)
					.WithTypeInspector(extraTypeInspector)
					.Build();
				
				serializer.Serialize(buffer, saveData, typeof(SaveDataT));

				string filePath = SavePath(fileName);
				string directoryPath = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(directoryPath))
				{
					Directory.CreateDirectory(directoryPath);
				}

				File.WriteAllText(filePath, buffer.ToString());
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	public static void Load<SaveDataT>(string fileName, out SaveDataT saveData, DataItemConverter dataItemConverter, System.Func<ITypeInspector, ITypeInspector> extraTypeInspector)
	{
		string data = File.ReadAllText(SavePath(fileName));

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
