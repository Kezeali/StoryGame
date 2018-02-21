using UnityEngine;

namespace NotABear
{

	[System.Serializable]
	public class StatBonusData
	{
		public string name;
		public CharacterStatDefinition stat;
		public double flatBonus;
		public double bonusPerTimeUnit;
		public int activePeriodTimeUnits;
		public double activePeriodExtensionPerTimeUnit;

		public bool IsInfinite()
		{
			return this.activePeriodTimeUnits < 0;
		}
	}

}