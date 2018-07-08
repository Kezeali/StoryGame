using UnityEngine;
using System.Collections.Generic;

namespace Cloverview
{

	[System.Serializable]
	public class CharacterRef
	{
		public string characterName;
		[System.NonSerialized]
		public Character character;
	}

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
				if (statListPool.Count > 0) {
					newStatus.stats = statListPool.Pop();
				} else {
					newStatus.stats = new List<Stat>();
				}
				return newStatus;
			}

			public void Recycle()
			{
				Debug.Assert(this.stats != null);
				if (this.stats != null) {
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
								
				for (int i = 0; i < this.stats.Count; ++i) {
					Stat stat = this.stats[i];
					if (stat.definition == statDef) {
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
				result.definition = statDef;
				result.value = statDef.baseValue;
								
				for (int i = 0; i < this.stats.Count; ++i) {
					Stat stat = this.stats[i];
					if (stat.definition == statDef) {
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
		public struct BaseStat : ICheckValid
		{
			public CharacterStatDefinition definition;
			public float value;
			public bool IsValid() { return definition != null; }
		}

		[System.Serializable]
		public struct ActiveBonus : ICheckValid
		{
			public StatBonusData definition;
			public float value;
			public int beginTimeUnit;
			public int activePeriodTimeUnits;
			public List<StatBonusSource> sources;

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

			public bool IsPermanent()
			{
				return this.activePeriodTimeUnits < 0;
			}

			public bool IsValid() { return definition != null; }
		}

		[System.Serializable]
		public struct Favourite : ICheckValid
		{
			public QualityData quality;
			public float affinity;
			public bool IsValid() { return quality != null; }
		}

		[System.Serializable]
		public struct Tag : ICheckValid
		{
			public QualityData quality;
			public int amount;
			public bool IsValid() { return quality != null; }
		}

		[System.Serializable]
		public struct Friendship
		{
			public CharacterRef friend;
			public int level;
		}

		public string name;
		public RoleData role;
		public CharacterBody bodyPrefab;
		public OutfitItemData[] outfitItems = new OutfitItemData[0];
		public List<ActiveBonus> activeBonuses = new List<ActiveBonus>();
		public List<ActiveBonus> permanentBonuses = new List<ActiveBonus>();
		public List<Favourite> favourites = new List<Favourite>();
		public List<Tag> tags = new List<Tag>();
		public List<Friendship> friendships = new List<Friendship>();
		public StatBonusData[] friendshipBonuses = new StatBonusData[0];
		public BaseStat[] baseStats = new BaseStat[0];
		[System.NonSerialized]
		public Status status;

		const int MAX_TEMPORARY_BONUSES = 100000;
		// Try to reduce temporary bonuses to this amount if it exceeded the maximum:
		const int SAFE_TEMPORARY_BONUSES = MAX_TEMPORARY_BONUSES - (MAX_TEMPORARY_BONUSES / 100);
		// Value in percent of total active time for the shorter bonus
		const float MAX_MERGE_MARGIN_FOR_BONUSES = 0.05f;

		public static Character Generate(RoleData role)
		{
			Character character = new Character();
			character.role = role;
			if (character.role != null) {
				if (role.nameGenerator != null) {
					character.name = role.nameGenerator.Generate();
				}
				BaseStatsGenerator[] statGenerators = role.statGenerators;
				if (statGenerators != null) {
					List<BaseStat> generatedStats = new List<BaseStat>(statGenerators.Length);
					for (int i = 0; i < statGenerators.Length; ++i) {
						Character.BaseStat baseStat = statGenerators[i].Generate();
						if (baseStat.definition != null) {
							generatedStats.Add(baseStat);
						}
					}
					character.baseStats = generatedStats.ToArray();
				}
				GenerateItems(character.favourites, role.favouritesGenerators);
				GenerateItems(character.tags, role.tagsGenerators);
			}
			return character;
		}

		static void GenerateItems<T>(List<T> destinationList, IGenerator<T>[] generators)
			where T : ICheckValid
		{
			if (generators != null) {
				destinationList.Clear();
				for (int i = 0; i < generators.Length; ++i) {
					T item = generators[i].Generate();
					if (item.IsValid()) {
						destinationList.Add(item);
					}
				}
			}
		}

		static List<T> CloneList<T>(List<T> source)
		{
			if (source != null) {
				return new List<T>(source);
			} else {
				return new List<T>();
			}
		}

		static T[] CloneArray<T>(T[] source)
		{
			if (source != null) {
				T[] clone = new T[source.Length];
				System.Array.Copy(source, 0, clone, 0, source.Length);
				return clone;
			} else {
				return new T[0];
			}
		}

		public Character CreateSimulationClone()
		{
			Character clone = new Character();
			clone.name = this.name;
			clone.activeBonuses = CloneList(this.activeBonuses);
			clone.permanentBonuses = CloneList(this.permanentBonuses);
			clone.favourites = CloneList(this.favourites);
			clone.tags = CloneList(this.tags);
			clone.baseStats = CloneArray(this.baseStats);
			clone.status = this.status.CreateClone();
			return clone;
		}

		public void ApplyStatus(Character simCharacter)
		{
			this.permanentBonuses.Clear();
			this.activeBonuses.Clear();
			this.permanentBonuses.AddRange(simCharacter.permanentBonuses);
			this.activeBonuses.AddRange(simCharacter.activeBonuses);

			this.favourites.Clear();
			this.favourites.AddRange(simCharacter.favourites);

			this.tags.Clear();
			this.tags.AddRange(simCharacter.tags);

			this.CalculateStatus();
		}

		public void Recycle()
		{
			if (Status.IsUsable(this.status)) {
				this.status.Recycle();
			}
			this.status = null;
		}

		public void PostLoadCleanup()
		{
			EnsureNotNull(ref this.activeBonuses);
			EnsureNotNull(ref this.permanentBonuses);
			EnsureNotNull(ref this.favourites);
			EnsureNotNull(ref this.tags);
			EnsureNotNull(ref this.friendships);

			this.activeBonuses.RemoveAll(InvalidBonus);
			this.permanentBonuses.RemoveAll(InvalidBonus);
		}

		void EnsureNotNull<T>(ref T field)
			where T : new()
		{
			if (field == null) {
				field = new T();
			}
		}

		public void CalculateStatus()
		{
			Status newStatus = CalculateStatus(this);
			if (Status.IsUsable(this.status)) {
				this.status.Recycle();
			}
			this.status = newStatus;
		}

		static Stack<List<StatBonusSource>> availableStatBonusSourceLists = new Stack<List<StatBonusSource>>();

		static List<StatBonusSource> MakeSourcesList(StatBonusSource source)
		{
			List<StatBonusSource> list;
			if (availableStatBonusSourceLists.Count == 0) {
				list = new List<StatBonusSource>(1);
			} else {
				list = availableStatBonusSourceLists.Pop();
				list.Clear();
			}
			list.Add(source);
			return list;
		}

		static void ReturnSourcesList(List<StatBonusSource> list)
		{
			availableStatBonusSourceLists.Push(list);
		}

		public void AddStatBonuses(StatBonusData[] bonuses, StatBonusSource source, int beginTimeUnit, int timeSpent)
		{
			Debug.Assert(bonuses != null);
			if (bonuses != null && bonuses.Length > 0) {
				for (int i = 0; i < bonuses.Length; ++i) {
					Debug.Assert(bonuses[i].stat != null);
					if (bonuses[i].stat != null) {
						var activeBonus = new ActiveBonus() {
							definition = bonuses[i],
							beginTimeUnit = beginTimeUnit
						};
						Character.CalculateBonus(out activeBonus.value, out activeBonus.activePeriodTimeUnits, bonuses[i], timeSpent);
						if (activeBonus.IsPermanent()) {
							Character.AddOrMergeActiveBonus(this.permanentBonuses, activeBonus, source);
						} else {
							// TODO(elliot): handle this list getting too big. do a pass over the list with an increasing margin-of-error to try and merge more similar (but not strictly equal) bonuses. widen the margin up to some maximum allowable amount. if the list still isn't shrunk enough at that point it's time to start spitting out errors.
							if (this.activeBonuses.Count >= MAX_TEMPORARY_BONUSES) {
								float margin = 0.001f;
								while (this.activeBonuses.Count > SAFE_TEMPORARY_BONUSES) {
									Character.AttempToMergeBonuses(this.activeBonuses, margin);
									margin *= 2.0f;
									if (margin > MAX_MERGE_MARGIN_FOR_BONUSES) {
										break;
									}
								}
							}

							if (this.activeBonuses.Count < MAX_TEMPORARY_BONUSES) {
								Character.AddOrMergeActiveBonus(this.activeBonuses, activeBonus, source);
							}
						}
					}
				}
				this.CalculateStatus();
			}
		}

		static void AttempToMergeBonuses(List<ActiveBonus> currentList, float mergeMargin)
		{
		}

		static void AddOrMergeActiveBonus(List<ActiveBonus> currentList, ActiveBonus newBonus, StatBonusSource source)
		{
			bool merged = false;
			for (int i = 0; i < currentList.Count; ++i) {
				ActiveBonus existingBonus = currentList[i];
				if (Character.ShouldMerge(existingBonus, newBonus)) {
					existingBonus = Character.Merge(existingBonus, newBonus);
					AddSource(ref existingBonus.sources, source);
					currentList[i] = existingBonus;
					merged = true;
					break;
				}
			}
			if (!merged) {
				newBonus.sources = Character.MakeSourcesList(source);
				currentList.Add(newBonus);
			}
		}

		static void AddSource(ref List<StatBonusSource> existingSources, StatBonusSource newSource)
		{
			if (existingSources == null)
			{
				existingSources = Character.MakeSourcesList(newSource);
				return;
			}
			bool alreadyInList = false;
			for (int existingSourceIndex = 0; existingSourceIndex < existingSources.Count; ++existingSourceIndex)
			{
				bool equal = newSource.Equals(existingSources[existingSourceIndex]);
				if (equal) {
					alreadyInList = true;
					break;
				}
			}
			if (!alreadyInList)
			{
				existingSources.Add(newSource);
			}
		}

		static bool ShouldMerge(ActiveBonus a, ActiveBonus b)
		{
			bool result =
				StatBonusData.AreEqual(a.definition, b.definition) && (
				(a.IsPermanent() && b.IsPermanent()) ||
				(a.beginTimeUnit == b.beginTimeUnit && a.activePeriodTimeUnits == b.activePeriodTimeUnits)
				);
			return result;
		}

		static ActiveBonus Merge(ActiveBonus existingBonus, ActiveBonus addition)
		{
			Debug.Assert(StatBonusData.AreEqual(existingBonus.definition, addition.definition));
			ActiveBonus result = existingBonus;
			result.value += addition.value;
			Merge(ref result.sources, addition.sources);
			return result;
		}

		static void Merge(ref List<StatBonusSource> existingSources, List<StatBonusSource> additions)
		{
			if (additions == null) {
				return;
			}
			if (existingSources == null) {
				existingSources = additions;
				return;
			}
			for (int additionIndex = 0; additionIndex < additions.Count; ++additionIndex)
			{
				bool alreadyInList = false;
				for (int existingSourceIndex = 0; existingSourceIndex < existingSources.Count; ++existingSourceIndex)
				{
					bool equal = additions[additionIndex].Equals(existingSources[existingSourceIndex]);
					if (equal) {
						alreadyInList = true;
						break;
					}
				}
				if (!alreadyInList)
				{
					existingSources.Add(additions[additionIndex]);
				}
			}
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
				Debug.Assert(!bonus.IsPermanent());
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

		public static void CalculateBonus(out float value, out int activePeriodTimeUnits, StatBonusData definition, int timeSpent)
		{
			value = definition.flatBonus + (definition.bonusPerTimeUnit * (float)timeSpent);
			activePeriodTimeUnits = definition.activePeriodTimeUnits + Mathf.FloorToInt(definition.activePeriodExtensionPerTimeUnit * (float)timeSpent);
		}

		public static Status CalculateStatus(Character character)
		{
			Status status = Status.New();

			for (int i = 0; i < character.baseStats.Length; ++i) {
				Stat stat = default(Stat);
				stat.definition = character.baseStats[i].definition;
				stat.value = character.baseStats[i].value;
				status.stats.Add(stat);
			}

			if (character.activeBonuses == null) {
				character.activeBonuses = new List<ActiveBonus>();
			}
			if (character.permanentBonuses == null) {
				character.permanentBonuses = new List<ActiveBonus>();
			}

			Character.ApplyStatBonuses(status, character.activeBonuses);
			Character.ApplyStatBonuses(status, character.permanentBonuses);

			for (int tagIndex = 0; tagIndex < character.tags.Count; ++tagIndex) {
				Tag tag = character.tags[tagIndex];
				Debug.Assert(tag.quality != null);
				if (tag.quality != null) {
					QualityData quality = tag.quality;
					for (int bonusIndex = 0; bonusIndex < quality.statBonuses.Length; ++bonusIndex) {
						StatBonusData bonusData = quality.statBonuses[bonusIndex];
						float value; int unused;
						Character.CalculateBonus(out value, out unused, bonusData, tag.amount);
						Character.ApplyStatBonus(status, bonusData, value);
					}
				}
			}

			for (int friendIndex = 0; friendIndex < character.friendships.Count; ++friendIndex) {
				Friendship friendship = character.friendships[friendIndex];
				Debug.Assert(friendship.friend != null);
				if (friendship.friend != null) {
					Character friend = friendship.friend.character;
					for (int bonusIndex = 0; bonusIndex < friend.friendshipBonuses.Length; ++bonusIndex) {
						StatBonusData bonusData = friend.friendshipBonuses[bonusIndex];
						float value; int unused;
						Character.CalculateBonus(out value, out unused, bonusData, friendship.level);
						Character.ApplyStatBonus(status, bonusData, value);
					}
				}
			}
			
			return status;
		}

		static bool InvalidBonus(ActiveBonus bonus)
		{
			return bonus.definition == null;
		}

		static void ApplyStatBonuses(Status status, List<ActiveBonus> bonuses)
		{
			for (int activeBonusIndex = 0; activeBonusIndex < bonuses.Count; ++activeBonusIndex) {
				ActiveBonus bonus = bonuses[activeBonusIndex];
				Character.ApplyStatBonus(status, bonus.definition, bonus.value);
			}
		}

		static void ApplyStatBonus(Status status, StatBonusData bonusData, float value)
		{
			Debug.Assert(bonusData != null);
			if (bonusData != null)
			{
				var stat = status.GetAndRemoveStat(bonusData.stat);

				// NOTE(elliot): this is adding the bonus value on to the existing stat value retrieved
				stat.value += value;
				stat.valueIncludesBonus += value;

				// add the updated stat back to the list
				status.stats.Add(stat);
			}
		}
	}

}