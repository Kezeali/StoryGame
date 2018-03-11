using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

	[CreateAssetMenu(fileName="Event.asset", menuName="Cloverview/Event Definition")]
	public class EventData : ScriptableObject, IDataItem
	{
		// public Ink.Story dialogue;
		public EventSceneScript[] leadRoles;
		public CastingCharacterDescription[] extrasDescriptions;
		public RoleProp[] props;
		public StatBonusData[] statBonuses;
	}

	[System.Serializable]
	public struct RoleProp
	{
		public StageMarkData mark;
		public GameObject prop;
	}

	[System.Serializable]
	public struct EventSceneScript
	{
		// public RoleData role;
		// TODO: Dialogue id for this character
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
			for (int desiredStatIndex = 0; desiredStatIndex < desiredStats.Length; ++desiredStatIndex)
			{
				DesiredStat desiredStat = desiredStats[desiredStatIndex];
				firstExcluded = actors.ExcludeAll(DesiredStat.DetermineHardNo, desiredStat, 0, firstExcluded);
			}
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
			Exclude // Try to exclude any characters with the given stat in the min-max range
		}

		public CharacterStatDefinition stat;
		public float minValue;
		public float maxValue;
		[Range(0.0f, 1.0f)]
		public float importance;
		public PreferredValue prefferedValue;

		public static bool DetermineHardNo(Character actor, DesiredStat desiredStat)
		{
			return Rate(actor, desiredStat) == float.NegativeInfinity;
		}

		public static float Rate(Character actor, DesiredStat desiredStat)
		{
			Debug.Assert(desiredStat.minValue <= desiredStat.maxValue);
			Debug.Assert(desiredStat.importance >= 0 && desiredStat.importance <= 1);

			float rating = 0;
			Character.Stat stat = actor.status.GetStat(desiredStat.stat);
			float range = desiredStat.maxValue - desiredStat.minValue;
			float scaledValue = (stat.value - desiredStat.minValue) / range;
			if (scaledValue >= 0 && scaledValue <= 1)
			{
				switch (desiredStat.prefferedValue)
				{
					case PreferredValue.Highest:
					{
						rating = scaledValue * desiredStat.importance;
					} break;
					case PreferredValue.Lowest:
					{
						rating = (1.0f - scaledValue) * desiredStat.importance;
					} break;
					case PreferredValue.Exclude:
					{
						rating = -desiredStat.importance;
					} break;
					default:
					{
						Debug.LogWarning("Unhandled case");
						rating = 0.5f;
					} break;
				}
			}
			else
			{
				switch (desiredStat.prefferedValue)
				{
					case PreferredValue.Highest:
					case PreferredValue.Lowest:
					{
						rating = -desiredStat.importance;
					} break;
					case PreferredValue.Exclude:
					{
						rating = desiredStat.importance;
					} break;
					default:
					{
						Debug.LogWarning("Unhandled case");
						rating = -0.5f;
					} break;
				}
			}
			if (rating < 0 && desiredStat.importance >= 1)
			{
				rating = float.NegativeInfinity;
			}
			return rating;
		}
	}
}