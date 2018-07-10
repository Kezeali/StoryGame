using UnityEngine;

namespace Cloverview
{

	[CreateAssetMenu(fileName="StatBonus.asset", menuName="Cloverview/Stat Bonus Definition")]
	public class StatBonusData : ScriptableObject, IDataItem
	{
		public string name;
		public CharacterStatDefinition stat;
		public float flatBonus;
		public float bonusPerTimeUnit;
		public int activePeriodTimeUnits;
		public float activePeriodExtensionPerTimeUnit;

		public bool IsPermanent()
		{
			return this.activePeriodTimeUnits < 0;
		}

		public static bool AreEqual(StatBonusData a, StatBonusData b)
		{
			return a == b || (
				a.name == b.name &&
				a.stat == b.stat
				);
		}
	}

	[System.Serializable]
	public struct StatBonusSource : System.IEquatable<StatBonusSource>
	{
		public enum SourceType
		{
			None,
			Event,
			Activity,
		}
		public string name;
		public SourceType type;
		public override int GetHashCode()
		{
			unchecked {
				int hash = 17;
				hash *= 23 + name.GetHashCode();
				hash *= 23 + type.GetHashCode();
				return hash;
			}
		}
		public override bool Equals(object other)
		{
			if (other is StatBonusSource)
			{
				return this.Equals((StatBonusSource)other);
			}
			else
			{
				return false;
			}
		}
		public bool Equals(StatBonusSource other)
		{
			return this.name == other.name && this.type == other.type;
		}
	}

}