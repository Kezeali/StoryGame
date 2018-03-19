using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

	[System.Serializable]
	public class Character
	{
		[System.Serializable]
		public class Status
		{
			public List<Stat> stats;

			static Stack<List<Stat>> statListPool = new Stack<List<Stat>>();
			public static Status New()
			{
				var newStatus = new Status();
				if (statListPool.Count > 0)
				{
					newStatus.stats = statListPool.Pop();
				}
				else
				{
					newStatus.stats = new List<Stat>();
				}
				return newStatus;
			}

			public void Recycle()
			{
				Debug.Assert(this.stats != null);
				if (this.stats != null)
				{
					this.stats.Clear();
					statListPool.Push(this.stats);
					this.stats = null;
				}
			}

			public Status CreateClone()
			{
				var newStatus = New();
				newStatus.stats.AddRange(this.stats);
				return newStatus;
			}

			public static bool IsUsable(Status status)
			{
				return status != null && status.stats != null;
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

		public Character CreateSimulationClone()
		{
			Character clone = new Character();
			clone.name = this.name;
			clone.activeBonuses = new List<ActiveBonus>(this.activeBonuses);
			clone.permanentBonuses = new List<ActiveBonus>(this.permanentBonuses);
			clone.baseStats = new BaseStat[this.baseStats.Length];
			System.Array.Copy(this.baseStats, 0, clone.baseStats, 0, this.baseStats.Length);
			clone.status = this.status.CreateClone();
			return clone;
		}

		public void Recycle()
		{
			if (Status.IsUsable(this.status))
			{
				this.status.Recycle();
			}
			this.status = null;
		}

		public void CalculateStatus()
		{
			Status newStatus = CalculateStatus(this);
			if (Status.IsUsable(this.status))
			{
				this.status.Recycle();
			}
			this.status = newStatus;
		}

		public void AddStatBonuses(StatBonusData[] bonuses, int beginTimeUnit, int timeSpent)
		{
			for (int i = 0; i < bonuses.Length; ++i)
			{
				var activeBonus = new ActiveBonus()
				{
					beginTimeUnit = beginTimeUnit
				};
				CalculateBonus(ref activeBonus, bonuses[i], timeSpent);
				if (activeBonus.IsInfinite())
				{
					this.permanentBonuses.Add(activeBonus);
				}
				else
				{
					this.activeBonuses.Add(activeBonus);
				}
			}

			this.CalculateStatus();
		}

		public void ClearTemporaryStatBonuses()
		{
			this.activeBonuses.Clear();
			this.CalculateStatus();
		}

		public void UpdateStatBonuses(int currentTimeUnit)
		{
			bool removed = false;
			for (int activeBonusIndex = this.activeBonuses.Count-1; activeBonusIndex >= 0; --activeBonusIndex)
			{
				ActiveBonus bonus = this.activeBonuses[activeBonusIndex];
				Debug.Assert(!bonus.IsInfinite());
				if (bonus.RemainingTime(currentTimeUnit) <= 0)
				{
					this.activeBonuses.RemoveAt(activeBonusIndex);
					removed = true;
				}
			}
			if (removed)
			{
				this.CalculateStatus();
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

			if (character.activeBonuses == null)
			{
				character.activeBonuses = new List<ActiveBonus>();
			}
			if (character.permanentBonuses == null)
			{
				character.permanentBonuses = new List<ActiveBonus>();
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