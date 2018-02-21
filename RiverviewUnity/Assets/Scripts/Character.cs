using UnityEngine;
using System.Collections.Generic;

namespace NotABear
{

	// Defines the progression of time for activities and stats
	public interface ITerm
	{
		int time { get; }
		int length { get; }
	}

	[System.Serializable]
	public class Character
	{
		[System.Serializable]
		public struct Status
		{
			public List<Stat> stats;
		}

		[System.Serializable]
		public struct Stat
		{
			public CharacterStatDefinition definition;
			public double value;
		}

		[System.Serializable]
		public struct ActiveBonus
		{
			public ITerm term;
			public StatBonusData definition;
			public double value;
			public int beginTimeUnit;
			public int activePeriodTimeUnits;

			public int TimeElapsed()
			{
				int result = this.term.time - this.beginTimeUnit;
				return result;
			}

			public int RemainingTime()
			{
				int result = this.activePeriodTimeUnits - TimeElapsed();
				return result;
			}

			public bool IsInfinite()
			{
				return this.activePeriodTimeUnits < 0;
			}
		}

		public string name;
		public List<ActiveBonus> activeBonuses;
		public Status baseStats;
		public Status status;

		public static ActiveBonus CalculateBonus(StatBonusData definition, int timeSpent)
		{
			var activeBonus = new ActiveBonus()
			{
				value = definition.flatBonus + (definition.bonusPerTimeUnit * (double)timeSpent),
				//activePeriodTimeUnits = Mathf.RoundToInt(definition.activePeriodTimeUnits + (definition.activePeriodExtensionPerTimeUnit * (double)timeSpent))
			};
			return activeBonus;
		}

		public void ApplyStatBonuses(StatBonusData[] bonuses, int timeSpent)
		{
			for (int i = 0; i < bonuses.Length; ++i)
			{
				this.activeBonuses.Add(CalculateBonus(bonuses[i], timeSpent));
			}

			this.status = CalculateStatus(this);
		}

		public static Status CalculateStatus(Character character)
		{
			var result = new Status();
			result.stats = new List<Stat>();
			for (int activeBonusIndex = 0; activeBonusIndex < character.activeBonuses.Count; ++activeBonusIndex)
			{
				var stat = new Stat();
				// stat.
				// result.stats.Add(
			}
			return result;
		}
	}

}