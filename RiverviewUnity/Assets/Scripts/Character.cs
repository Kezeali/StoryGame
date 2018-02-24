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

			public Stat GetStat(CharacterStatDefinition statDef)
			{
				// Create a default stat in case there is no current value
				Stat result = default(Stat);
				result.value = statDef.baseValue;
								
				for (int i = 0; i < this.stats.Count; ++i)
				{
					Stat stat = this.stats[i];
					if (stat.definition == statDef)
					{
						result = stat;
						break;
					}
				}
				return result;
			}
		}

		[System.Serializable]
		public struct Stat
		{
			public CharacterStatDefinition definition;
			public float value;
			public float valueIncludesBonus;
		}

		[System.Serializable]
		public struct BaseStat
		{
			public CharacterStatDefinition definition;
			public float value;
		}

		[System.Serializable]
		public struct ActiveBonus
		{
			public ITerm term;
			public StatBonusData definition;
			public float value;
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
		public BaseStat[] baseStats;
		[System.NonSerialized]
		public Status status;

		public void CalculateStatus()
		{
			this.status = CalculateStatus(this);
		}

		public void AddStatBonuses(StatBonusData[] bonuses, int timeSpent)
		{
			for (int i = 0; i < bonuses.Length; ++i)
			{
				this.activeBonuses.Add(CalculateBonus(bonuses[i], timeSpent));
			}

			this.status = CalculateStatus(this);
		}

		public void ClearStatBonuses()
		{
			this.activeBonuses.Clear();
			this.status = CalculateStatus(this);
		}

		public void UpdateStatBonuses()
		{
			bool removed = false;
			for (int activeBonusIndex = this.activeBonuses.Count-1; activeBonusIndex >= 0; --activeBonusIndex)
			{
				ActiveBonus bonus = this.activeBonuses[activeBonusIndex];
				if (!bonus.IsInfinite())
				{
					if (bonus.RemainingTime() <= 0)
					{
						this.activeBonuses.RemoveAt(activeBonusIndex);
						removed = true;
					}
				}
			}
			if (removed)
			{
				this.status = CalculateStatus(this);
			}
		}

		public static Stat GetBaseStat(BaseStat[] baseStats, CharacterStatDefinition statDef)
		{
			Stat result = default(Stat);
			result.definition = statDef;
			result.value = statDef.baseValue;
			for (int i = 0; i < baseStats.Length; ++i)
			{
				BaseStat stat = baseStats[i];
				if (stat.definition == statDef)
				{
					result.value = stat.value;
					break;
				}
			}
			return result;
		}

		public static ActiveBonus CalculateBonus(StatBonusData definition, int timeSpent)
		{
			var activeBonus = new ActiveBonus()
			{
				value = definition.flatBonus + (definition.bonusPerTimeUnit * (float)timeSpent),
				activePeriodTimeUnits = definition.activePeriodTimeUnits + Mathf.FloorToInt(definition.activePeriodExtensionPerTimeUnit * (float)timeSpent)
			};
			return activeBonus;
		}

		public static Status CalculateStatus(Character character)
		{
			var result = character.status;
			if (result.stats == null)
			{
				result.stats = new List<Stat>();
			}
			result.stats.Clear();

			for (int i = 0; i < character.baseStats.Length; ++i)
			{
				Stat stat = default(Stat);
				stat.definition = character.baseStats[i].definition;
				stat.value = character.baseStats[i].value;
				result.stats.Add(stat);
			}

			for (int activeBonusIndex = 0; activeBonusIndex < character.activeBonuses.Count; ++activeBonusIndex)
			{
				ActiveBonus bonus = character.activeBonuses[activeBonusIndex];
				
				var stat = character.status.GetStat(bonus.definition.stat);

				// NOTE(elliot): this is adding the bonus value on to the existing stat value retrieved
				stat.value += bonus.value;
				stat.valueIncludesBonus += bonus.value;

				result.stats.Add(stat);
			}
			return result;
		}
	}

}