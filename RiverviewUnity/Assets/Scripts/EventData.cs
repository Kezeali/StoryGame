using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

	[CreateAssetMenu(fileName="Event.asset", menuName="Cloverview/Event Definition")]
	public class EventData : ScriptableObject, IDataItem
	{
		public EventConditions conditions;
		public string narritiveId;
		public SceneData sceneOverride;
		public SceneRole[] leadRoles;
		public CastingCharacterDescription[] extrasDescriptions;
		public EventSceneScript[] leadRoleScripts;
		// TODO(elliot): would be cool if characters could pick up Set Props as part of event scripts
		public SetProp[] props;
		public StatBonusData[] statBonuses;
	}

	public enum EventPriority
	{
		Plot,
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

	public enum EventIncidenceTime
	{
		BeforeSlot,
		DuringSlot,
		AfterSlot
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

		public EventIncidenceTime when;
		[Range(0.0f, 1.0f)]
		public float chance;
	}

	[System.Serializable]
	public struct SetProp
	{
		public StageMarkData mark;
		public GameObject prefab;
	}

	[System.Serializable]
	public struct CharacterProp
	{
		public string targetPin;
		public GameObject prefab;
	}

	[System.Serializable]
	public struct EventSceneScript
	{
		public RoleData role;
		// TODO: the rest of this
	}

	[System.Serializable]
	public struct CastingCharacterDescription : IComparer<Character>
	{
		public StageMarkData mark;
		public DesiredStat[] desiredStats;
		public DesiredTag[] desiredTags;
		public DesiredFriendship[] desiredFriendships;

		// returns the index of the first excluded actor
		public int SortActors(List<Character> actors, int castAvailableCount)
		{
			int firstExcluded = castAvailableCount;
			for (int requirementIndex = 0; requirementIndex < this.desiredStats.Length; ++requirementIndex)
			{
				DesiredStat desiredStat = this.desiredStats[requirementIndex];
				firstExcluded = actors.ExcludeAll(DesiredStat.DetermineHardNo, desiredStat, 0, firstExcluded);
			}
			// TODO: tags and friendsships
			// for (int requirementIndex = 0; requirementIndex < this.desiredTags.Length; ++requirementIndex)
			// {
			// 	DesiredTag desiredTag = this.desiredTags[requirementIndex];
			// 	firstExcluded = actors.ExcludeAll(DesiredTag.DetermineHardNo, desiredTag, 0, firstExcluded);
			// }
			actors.Sort(0, firstExcluded, this);
			return firstExcluded;
		}

		public int Compare(Character a, Character b)
		{
			float ratingA = DesiredStat.Rate(a.status, this.desiredStats);
			float ratingB = DesiredStat.Rate(b.status, this.desiredStats);
			// ratingA *= DesiredTag.Rate(a.tags, this.desiredTags);
			// ratingB *= DesiredTag.Rate(b.tags, this.desiredTags);
			// ratingA *= DesiredFriendship.Rate(a.tags, this.desiredFriendships);
			// ratingB *= DesiredFriendship.Rate(b.tags, this.desiredFriendships);
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

	public enum PreferredValue
	{
		HighestInRange,
		LowestInRange,
		OutOfRange // Try to exclude any characters with the given stat in the min-max range
	}

	[System.Serializable]
	public struct DesiredStat
	{
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
					case PreferredValue.HighestInRange:
					{
						rating = scaledValue * desiredStat.importance;
					} break;
					case PreferredValue.LowestInRange:
					{
						rating = (1.0f - scaledValue) * desiredStat.importance;
					} break;
					case PreferredValue.OutOfRange:
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
					case PreferredValue.HighestInRange:
					case PreferredValue.LowestInRange:
					{
						rating = -desiredStat.importance;
					} break;
					case PreferredValue.OutOfRange:
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

	[System.Serializable]
	public struct DesiredTag
	{
		public QualityData tagQuality;
		public int minAmount;
		public int maxAmount;
		[Range(0.0f, 1.0f)]
		public float importance;
		public PreferredValue prefferedValue;
	}

	[System.Serializable]
	public struct DesiredFriendship
	{
		public RoleData friendRole;
		public int minLevel;
		public int maxLevel;
		[Range(0.0f, 1.0f)]
		public float importance;
		public PreferredValue prefferedValue;
	}
}