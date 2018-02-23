using UnityEngine;

namespace NotABear
{

	[System.Serializable]
	public class StatBonusData
	{
		public string name;
		public CharacterStatDefinition stat;
		public float flatBonus;
		public float bonusPerTimeUnit;
		public int activePeriodTimeUnits;
		public float activePeriodExtensionPerTimeUnit;

		public bool IsInfinite()
		{
			return this.activePeriodTimeUnits < 0;
		}
	}

}