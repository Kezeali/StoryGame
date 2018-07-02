using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

[CreateAssetMenu(fileName="Role.asset", menuName="Cloverview/Role Definition")]
public class RoleData : ScriptableObject, IDataItem
{
	public NameGenerator nameGenerator;
	public BaseStatsGenerator[] statGenerators;
	public FavouriteQualitiesGenerator[] favouritesGenerators;
	public TagQualitiesGenerator[] tagsGenerators;
}

[System.Serializable]
public class NameGenerator
{
	public string name;

	public string Generate()
	{
		return name;
	}
}

[System.Serializable]
public class BaseStatsGenerator
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
public class FavouriteQualitiesGenerator
{
	public struct Option
	{
		public float weight;

		public QualityData quality;
		public float minAffinity;
		public float maxAffinity;
	}

	public Option[] options;

	private bool sorted;

	public Character.Favourite Generate()
	{
		var generatedFav = new Character.Favourite();

		float totalWeight = 0;
		for (int i = 0; i < options.Length; ++i)
		{
			totalWeight += options[i].weight;
		}

		float selectedWeight = Random.Range(0, totalWeight);

		Option selectedOption = new Option();
		for (int i = 0; i < options.Length; ++i)
		{
			selectedOption = options[i];
			selectedWeight -= selectedOption.weight;
			if (selectedWeight <= 0)
			{
				break;
			}
		}

		float affinity = Random.Range(selectedOption.minAffinity, selectedOption.maxAffinity);
		generatedFav.quality = selectedOption.quality;
		generatedFav.affinity = affinity;

		return generatedFav;
	}
}

[System.Serializable]
public class TagQualitiesGenerator
{
	public struct Option
	{
		public float weight;

		public QualityData quality;
		public int minAmount;
		public int maxAmount;
	}

	public Option[] options;

	public Character.Tag Generate()
	{
		var generatedTag = new Character.Tag();

		float totalWeight = 0;
		for (int i = 0; i < options.Length; ++i)
		{
			totalWeight += options[i].weight;
		}

		float selectedWeight = Random.Range(0, totalWeight);

		Option selectedOption = new Option();
		for (int i = 0; i < options.Length; ++i)
		{
			selectedOption = options[i];
			selectedWeight -= selectedOption.weight;
			if (selectedWeight <= 0)
			{
				break;
			}
		}

		int amount = Random.Range(selectedOption.minAmount, selectedOption.maxAmount);
		generatedTag.quality = selectedOption.quality;
		generatedTag.amount = amount;

		return generatedTag;
	}
}

}
