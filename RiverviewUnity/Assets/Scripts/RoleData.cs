using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

public interface IGenerator<GeneratedT>
{
	GeneratedT Generate();
}

public interface ICheckValid
{
	bool IsValid();
}

// Asset type defining a character "role"--Used to generate lead characters for the game.
[CreateAssetMenu(fileName="Role.asset", menuName="Cloverview/Role Definition")]
public class RoleData : ScriptableObject, IDataItem
{
	public NameGenerator nameGenerator;
	public BodyGenerator bodyGenerator;
	public OutfitItemGenerator[] outfitItemGenerators;
	public BaseStatsGenerator[] statGenerators;
	public FavouriteQualitiesGenerator[] favouritesGenerators;
	public TagQualitiesGenerator[] tagsGenerators;
}

[System.Serializable]
public struct CharacterName
{
	public string first;
	public string surname;

	public string fullName;

	public static CharacterName Create(string first, string surname)
	{
		var characterName = new CharacterName();
		characterName.first = first;
		characterName.surname = surname;
		characterName.UpdateTranslation();
		return characterName;
	}

	public void UpdateTranslation()
	{
		string fullNameFormat = "{0} {1}";
		bool hasFirstName = !string.IsNullOrEmpty(this.first);
		bool hasSurname = !string.IsNullOrEmpty(this.surname);
		if (hasFirstName && hasSurname) {
			this.fullName = Strf.Format(fullNameFormat, this.first, this.surname);
		} else if (hasFirstName) {
			this.fullName = this.first;
		} else if (hasSurname) {
			this.fullName = this.surname;
		} else {
			this.fullName = "";
		}
	}

	public override string ToString()
	{
		return fullName;
	}
}

[System.Serializable]
public class NameGenerator
{
	[System.Serializable]
	public struct Option : IWeightedOption
	{
		public float weight;
		public float GetWeight() { return weight; }

		public string str;
	}

	public Option[] firstNameOptions;
	public Option[] surnameOptions;

	public CharacterName Generate()
	{
		CharacterName result = default(CharacterName);
		Option firstName = WeightedRandom.Select(this.firstNameOptions);
		Option surname = WeightedRandom.Select(this.surnameOptions);
		result = CharacterName.Create(firstName.str, surname.str);
		return result;
	}
}

[System.Serializable]
public class BodyGenerator
{
	[System.Serializable]
	public struct Option : IWeightedOption
	{
		public float weight;
		public float GetWeight() { return weight; }

		public CharacterBody prefab;
	}

	public Option[] options;

	public CharacterBody Generate()
	{
		Option selectedOption = WeightedRandom.Select(this.options);
		return selectedOption.prefab;
	}
}

[System.Serializable]
public class OutfitItemGenerator : IGenerator<OutfitItemData>
{
	[System.Serializable]
	public struct Option : IWeightedOption
	{
		public float weight;
		public float GetWeight() { return weight; }

		public OutfitItemData item;
	}

	public Option[] options;

	public OutfitItemData Generate()
	{
		Option selectedOption = WeightedRandom.Select(this.options);
		return selectedOption.item;
	}
}

[System.Serializable]
public class BaseStatsGenerator : IGenerator<Character.BaseStat>
{
	public CharacterStatDefinition definition;
	public float minValue;
	public float maxValue;
	public bool treatValuesAsNormalised;
	public bool relativeToBaseValueForStat;
	
	public Character.BaseStat Generate()
	{
		var stat = new Character.BaseStat();
		stat.definition = definition;

		float scaledMinValue = minValue;
		float scaledMaxValue = maxValue;
		if (treatValuesAsNormalised)
		{
			Debug.Assert(minValue >= 0 && minValue <= 1);
			Debug.Assert(maxValue >= 0 && maxValue <= 1);

			float range = definition.maxValue - definition.minValue;
			float baseValue = relativeToBaseValueForStat ? definition.baseValue : definition.minValue;

			scaledMinValue = minValue * range + baseValue;
			scaledMaxValue = maxValue * range + baseValue;
		}
		else if (relativeToBaseValueForStat)
		{
			scaledMinValue += definition.baseValue;
		}

		float desiredValue = Random.Range(scaledMinValue, scaledMaxValue);

		stat.value = Mathf.Clamp(desiredValue, definition.minValue, definition.maxValue);

		return stat;
	}
}

[System.Serializable]
public class FavouriteQualitiesGenerator : IGenerator<Character.Favourite>
{
	[System.Serializable]
	public struct Option : IWeightedOption
	{
		public float weight;
		public float GetWeight() { return weight; }

		public QualityData quality;
		public float minAffinity;
		public float maxAffinity;
	}

	public Option[] options;

	public Character.Favourite Generate()
	{
		var generatedFav = new Character.Favourite();

		Option selectedOption = WeightedRandom.Select(this.options);

		float affinity = Random.Range(selectedOption.minAffinity, selectedOption.maxAffinity);
		generatedFav.quality = selectedOption.quality;
		generatedFav.affinity = affinity;

		return generatedFav;
	}
}

[System.Serializable]
public class TagQualitiesGenerator : IGenerator<Character.Tag>
{
	[System.Serializable]
	public struct Option : IWeightedOption
	{
		public float weight;
		public float GetWeight() { return weight; }

		public QualityData quality;
		public int minAmount;
		public int maxAmount;
	}

	public Option[] options;

	public Character.Tag Generate()
	{
		var generatedTag = new Character.Tag();

		Option selectedOption = WeightedRandom.Select(this.options);

		int amount = Random.Range(selectedOption.minAmount, selectedOption.maxAmount);
		generatedTag.quality = selectedOption.quality;
		generatedTag.amount = amount;

		return generatedTag;
	}
}

public interface IWeightedOption
{
	float GetWeight();
}

public static class WeightedRandom
{
	public static T Select<T>(T[] options)
		where T : IWeightedOption
	{
		int selectedIndex = WeightedRandom.SelectIndex(options);
		if (selectedIndex != -1) {
			return options[selectedIndex];
		} else {
			return default(T);
		}
	}

	public static int SelectIndex<T>(T[] options)
		where T : IWeightedOption
	{
		return WeightedRandom.SelectIndex(options, 0, options.Length);
	}

	public static int SelectIndex<T>(T[] options, int first)
		where T : IWeightedOption
	{
		return WeightedRandom.SelectIndex(options, first, options.Length-first);
	}

	public static int SelectIndex<T>(T[] options, int first, int count)
		where T : IWeightedOption
	{
		int end = first + count;
		if (end > options.Length) end = options.Length;

		float totalWeight = 0;
		for (int i = first; i < end; ++i)
		{
			totalWeight += options[i].GetWeight();
		}

		float selectedWeight = Random.Range(0, totalWeight);

		int selectedIndex = -1;
		for (int i = first; i < end; ++i)
		{
			selectedWeight -= options[i].GetWeight();
			if (selectedWeight <= 0)
			{
				selectedIndex = i;
				break;
			}
		}
		return selectedIndex;
	}
}

}
