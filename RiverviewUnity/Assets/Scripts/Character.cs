using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

	[System.Serializable]
	public class Character
	{
		[System.Serializable]
		public struct Status
		{
			public List<Stat> stats;

			static Stack<List<Stat>> statListPool = new Stack<List<Stat>>();
			public static Status New()
			{
				var clone = new Status();
				if (statListPool.Count > 0)
				{
					clone.stats = statListPool.Pop();
				}
				else
				{
					clone.stats = new List<Stat>();
				}
				return clone;
			}

			public void Recycle()
			{
				this.stats.Clear();
				statListPool.Push(this.stats);
				this.stats = null;
			}

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

			// Used to retrieve & remove (as necessary) the relevant stat from the list to update it
			public Stat GetAndRemoveStat(CharacterStatDefinition statDef)
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
						this.stats.RemoveAt(i);
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
			public StatBonusData definition;
			public float value;
			public int beginTimeUnit;
			public int activePeriodTimeUnits;

			public int TimeElapsed(int currentTimeUnit)
			{
				int result = currentTimeUnit - this.beginTimeUnit;
				return result;
			}

			public int RemainingTime(int currentTimeUnit)
			{
				int result = this.activePeriodTimeUnits - TimeElapsed(currentTimeUnit);
				return result;
			}

			public bool IsInfinite()
			{
				return this.activePeriodTimeUnits < 0;
			}
		}

		public string name;
		public List<ActiveBonus> activeBonuses;
		public List<ActiveBonus> permanentBonuses;
		public BaseStat[] baseStats;
		[System.NonSerialized]
		public Status status;

		public void CalculateStatus()
		{
			Status newStatus = CalculateStatus(this);
			this.status.Recycle();
			this.status = newStatus;
		}

		public static Status AddStatBonuses(Character character, StatBonusData[] bonuses, int beginTimeUnit, int timeSpent)
		{
			for (int i = 0; i < bonuses.Length; ++i)
			{
				var activeBonus = new ActiveBonus()
				{
					beginTimeUnit = beginTimeUnit
				};
				CalculateBonus(ref activeBonus, bonuses[i], timeSpent);
				character.activeBonuses.Add(activeBonus);
			}

			return CalculateStatus(character);
		}

		public void ClearStatBonuses()
		{
			this.activeBonuses.Clear();
			this.status = CalculateStatus(this);
		}

		public void UpdateStatBonuses(int currentTimeUnit)
		{
			bool removed = false;
			for (int activeBonusIndex = this.activeBonuses.Count-1; activeBonusIndex >= 0; --activeBonusIndex)
			{
				ActiveBonus bonus = this.activeBonuses[activeBonusIndex];
				if (!bonus.IsInfinite())
				{
					if (bonus.RemainingTime(currentTimeUnit) <= 0)
					{
						this.activeBonuses.RemoveAt(activeBonusIndex);
						removed = true;
					}
				}
			}
			if (removed)
			{
				Status newStatus = CalculateStatus(this);
				this.status.Recycle();
				this.status = newStatus;
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

		public static void CalculateBonus(ref ActiveBonus activeBonus, StatBonusData definition, int timeSpent)
		{
			activeBonus.value = definition.flatBonus + (definition.bonusPerTimeUnit * (float)timeSpent);
			activeBonus.activePeriodTimeUnits = definition.activePeriodTimeUnits + Mathf.FloorToInt(definition.activePeriodExtensionPerTimeUnit * (float)timeSpent);
		}

		public static Status CalculateStatus(Character character)
		{
			Status result = Status.New();

			for (int i = 0; i < character.baseStats.Length; ++i)
			{
				Stat stat = default(Stat);
				stat.definition = character.baseStats[i].definition;
				stat.value = character.baseStats[i].value;
				result.stats.Add(stat);
			}

			ApplyStatBonuses(ref result, character.activeBonuses);
			ApplyStatBonuses(ref result, character.permanentBonuses);
			
			return result;
		}

		static void ApplyStatBonuses(ref Status result, List<ActiveBonus> bonuses)
		{
			for (int activeBonusIndex = 0; activeBonusIndex < bonuses.Count; ++activeBonusIndex)
			{
				ActiveBonus bonus = bonuses[activeBonusIndex];
				
				var stat = result.GetAndRemoveStat(bonus.definition.stat);

				// NOTE(elliot): this is adding the bonus value on to the existing stat value retrieved
				stat.value += bonus.value;
				stat.valueIncludesBonus += bonus.value;

				// add the updated stat back to the list
				result.stats.Add(stat);
			}
		}
	}

}