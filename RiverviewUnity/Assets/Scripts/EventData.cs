using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

	// TODO(elliot): narritive editor that loads the ink story and generates / updates EventData for all weaves tagged # event.

	[CreateAssetMenu(fileName="Event.asset", menuName="Cloverview/Event Definition")]
	public class EventData : ScriptableObject, IDataItem
	{
		public EventConditions conditions;
		public string narritiveId;
		public EventSceneScript[] leadRoles;
		public CastingCharacterDescription[] extrasDescriptions;
		// TODO(elliot): would be cool if characters could pick up Set Props as part of event scripts
		public SetProp[] props;
		public StatBonusData[] statBonuses;
	}

	// NOTE(elliot): events are sorted as follows:
	//  1) priority -- Higher priority events which can occur will /always/ occur before lower priority events

	public enum EventPriority
	{
		Higest, // Plot events
		High,
		Normal,
		Low,
		Lowest
	}

	[System.Serializable]
	public struct EventConditions
	{
		public EventPriority priority;
		public DesiredStat[] requiredPcStats;
		public DesiredSlot[] slotConditions;
	}

	public enum EventSide
	{
		Before,
		During,
		After
	}

	[System.Serializable]
	public struct DesiredSlot
	{
		// Must match either type OR time if they are set (if both are set only one must match)
		public SlotType type;
		public int time; // Match the slot overlapping time
		// Must match section index if it is set
		public int sectionIndex;
		// So if sectionIndex is set, but if type is None and time is -1, this matches any slot in the section.

		public EventSide when;
		[Range(0.0f, 1.0f)]
		public float chance;
	}

	[System.Serializable]
	public struct SetProp
	{
		public StageMarkData mark;
		public GameObject prop;
	}

	[System.Serializable]
	public struct CharacterProp
	{
		public string slotName;
		public GameObject prop;
	}

	[System.Serializable]
	public struct EventSceneScript
	{
		public RoleData role;
		public CharacterProp[] props;
	}

	[System.Serializable]
	public struct CastingCharacterDescription : IComparer<Character>
	{
		public StageMarkData mark;
		public DesiredStat[] stats;

		// returns the index of the first excluded actor
		public int SortActors(List<Character> actors)
		{
			var desiredStats = this.stats;
			int firstExcluded = 0;
			for (int desiredStatIndex = 0; desiredStatIndex < desiredStats.Length; ++desiredStatIndex)
			{
				DesiredStat desiredStat = desiredStats[desiredStatIndex];
				firstExcluded = actors.ExcludeAll(DesiredStat.DetermineHardNo, desiredStat, 0, firstExcluded);
			}
			actors.Sort(0, firstExcluded, this);
			return firstExcluded;
		}

		public int Compare(Character a, Character b)
		{
			float ratingA = DesiredStat.Rate(a.status, this.stats);
			float ratingB = DesiredStat.Rate(b.status, this.stats);
			// Higher ratings come first
			if (ratingA > ratingB)
			{
				return -1;
			}
			else if (ratingA < ratingB)
			{
				return 1;
			}
			else
			{
				return 0;
			}
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
			return DetermineHardNo(actor.status, desiredStat);
		}

		public static bool DetermineHardNo(Character.Status actorStatus, DesiredStat desiredStat)
		{
			return Rate(actorStatus, desiredStat) == float.NegativeInfinity;
		}

		public static bool DetermineHardNo(Character actor, DesiredStat[] desiredStats)
		{
			return DetermineHardNo(actor.status, desiredStats);
		}

		public static bool DetermineHardNo(Character.Status actorStatus, DesiredStat[] desiredStats)
		{
			bool result = false;
			if (desiredStats != null)
			{
				for (int desiredStatIndex = 0; desiredStatIndex < desiredStats.Length; ++desiredStatIndex)
				{
					if (DesiredStat.Rate(actorStatus, desiredStats[desiredStatIndex]) == float.NegativeInfinity)
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		public static float Rate(Character.Status actorStatus, DesiredStat[] desiredStats)
		{
			float result = 0;
			if (desiredStats != null)
			{
				for (int desiredStatIndex = 0; desiredStatIndex < desiredStats.Length; ++desiredStatIndex)
				{
					result += DesiredStat.Rate(actorStatus, desiredStats[desiredStatIndex]);
				}
			}
			return result;
		}

		public static float Rate(Character.Status actorStatus, DesiredStat desiredStat)
		{
			Debug.Assert(desiredStat.minValue <= desiredStat.maxValue);
			Debug.Assert(desiredStat.importance >= 0 && desiredStat.importance <= 1);

			float rating = 0;
			Character.Stat stat = actorStatus.GetStat(desiredStat.stat);
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