using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

	[CreateAssetMenu(fileName="Event.asset", menuName="Cloverview/Event Definition")]
	public class EventData : ScriptableObject, IDataItem
	{
		public RoleProp[] props;
		public CastingCharacterDescription[] characterDescriptions;
		public StatBonusData[] statBonuses;
	}

	[System.Serializable]
	public struct RoleProp
	{
		public StageMarkData mark;
		public GameObject prop;
	}

	[System.Serializable]
	public struct CastingCharacterDescription
	{
		public StageMarkData mark;
		public DesiredStat[] stats;

		// returns the index of the first excluded actor
		public static int SortActors(List<Character> actors, DesiredStat[] desiredStats)
		{
			int firstExcluded = 0;
			return firstExcluded;
		}
	}

	[System.Serializable]
	public struct DesiredStat
	{
		public enum PreferredValue
		{
			Highest,
			Lowest,
			Exclude // Try to exclude any characters with the given with values in the min-max range
		}

		public CharacterStatDefinition stat;
		public float minValue;
		public float maxValue;
		[Range(0.0f, 1.0f)]
		public float importance;
		public PreferredValue prefferedValue;

		public static float Rate(Character actor, DesiredStat desiredStat)
		{
			float rating = 0;
			Character.Stat stat = actor.status.GetStat(desiredStat.stat);
			float scaledValue = stat.value - desiredStat.maxValue;
			if (stat.value > desiredStat.minValue)
			{
			}
			return rating;
		}
	}
}